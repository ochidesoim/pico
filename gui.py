"""
VeloForge GUI — phases 3-9
python gui.py  (tkinter, zero extra installs)
"""
import tkinter as tk
from tkinter import ttk, filedialog, messagebox
import json, os, subprocess, threading, glob, sys
import http.server
import socketserver
import socket
import webbrowser
import urllib.parse
from pathlib import Path

# ── Resolve the bundled web/out directory ─────────────────────────────────
# When running as a PyInstaller exe, static assets live in _MEIPASS/web_out/.
# When running as a .py script, they live at <ROOT_DIR>/web/out/.
if getattr(sys, 'frozen', False):
    exe_dir = os.path.dirname(sys.executable)
    if os.path.exists(os.path.join(exe_dir, "..", "pico.csproj")):
        ROOT_DIR = os.path.abspath(os.path.join(exe_dir, ".."))
    else:
        ROOT_DIR = exe_dir
    WEB_OUT_DIR = os.path.join(sys._MEIPASS, "web_out")
else:
    ROOT_DIR = os.path.dirname(os.path.abspath(__file__))
    WEB_OUT_DIR = os.path.join(ROOT_DIR, "web", "out")

CONFIG_PATH = os.path.join(ROOT_DIR, "config.json")

DEFAULTS = {
    "simulation": {
        "thicknessCandidates": [18, 20, 22],
        "safetyFactorTarget": 1.5,
        "loadN": 6000.0,
        "outputDir": r"D:\pico\output"
    },
    "material": {
        "youngsModulusMpa": 71700.0,
        "poissonRatio": 0.33,
        "densityGPerMm3": 0.00281,
        "yieldStrengthMpa": 503.0
    },
    "tools": {
        "fTetWildExe": r"D:\pico\fTetWild\build\Release\FloatTetwild_bin.exe",
        "ccxExe": r"D:\pico\calculix\CalculiX-2.21.0-win-x64\bin\ccx.exe",
        "outputDir": r"D:\pico\output",
        "paraviewExe": ""
    }
}

BG    = "#1a1a1a"
BG2   = "#222222"
BG3   = "#2a2a2a"
ACC   = "#FF6B00"
ACC2  = "#FF8C00"
FG    = "#FFFFFF"
FG2   = "#AAAAAA"
GREEN = "#00FF88"
RED   = "#FF2200"
CYAN  = "#00CFFF"
YELL  = "#FFAA00"
FONT_BODY = ("Segoe UI", 10)
FONT_MONO = ("Consolas", 10)

LINE_COLORS = {
    "[MESH]":  CYAN,
    "[FEA]":   ACC,
    "[SOLVE]": YELL,
    "[SOLV]":  YELL,
    "[PASS]":  GREEN,
    "[FAIL]":  RED,
    "[DONE]":  FG,
    "[INFO]":  FG2,
    "[RESL]":  FG2,
    "[CFG]":   FG2,
    "[PROC]":  "#888888",
    "[ERROR]": RED,
    "[WARN]":  YELL,
}

# ── Embedded login HTTP server ────────────────────────────────────────────

def _get_free_port() -> int:
    """Return a random free TCP port on localhost."""
    with socket.socket() as s:
        s.bind(('127.0.0.1', 0))
        return s.getsockname()[1]


class _AuthHandler(http.server.SimpleHTTPRequestHandler):
    """Serves static files and handles POST /auth."""
    auth_event: threading.Event  # injected by LoginServer

    def __init__(self, *args, **kwargs):
        # Serve files from the Next.js static export directory
        super().__init__(*args, directory=WEB_OUT_DIR, **kwargs)

    def do_POST(self):
        if self.path == "/auth":
            # Consume the body (required to keep the connection clean)
            length = int(self.headers.get('Content-Length', 0))
            self.rfile.read(length)
            # Send CORS headers so the browser page can POST cross-origin
            self.send_response(200)
            self.send_header('Access-Control-Allow-Origin', '*')
            self.send_header('Content-Type', 'application/json')
            self.end_headers()
            self.wfile.write(b'{"ok":true}')
            # Signal the waiting main thread
            self.__class__.auth_event.set()
        else:
            self.send_error(404)

    def do_OPTIONS(self):
        """Pre-flight CORS for the POST /auth fetch."""
        self.send_response(204)
        self.send_header('Access-Control-Allow-Origin', '*')
        self.send_header('Access-Control-Allow-Methods', 'POST, OPTIONS')
        self.send_header('Access-Control-Allow-Headers', 'Content-Type')
        self.end_headers()

    def log_message(self, *args):
        pass  # silence request logs


class LoginServer:
    """Lightweight HTTP server that gates the main GUI behind the login page."""

    def __init__(self):
        self._port = _get_free_port()
        self._auth_event = threading.Event()
        _AuthHandler.auth_event = self._auth_event
        self._server = socketserver.TCPServer(
            ('127.0.0.1', self._port), _AuthHandler)
        self._thread = threading.Thread(
            target=self._server.serve_forever, daemon=True)

    @property
    def port(self) -> int:
        return self._port

    def start(self):
        self._thread.start()

    def wait_for_auth(self, timeout: float = 300.0) -> bool:
        """Block until POST /auth received or timeout (seconds)."""
        return self._auth_event.wait(timeout=timeout)

    def stop(self):
        self._server.shutdown()


def show_login_and_wait():
    """Show the login page in the default browser and block until auth."""
    if not os.path.isdir(WEB_OUT_DIR):
        # Web assets not built yet — skip login gate gracefully
        print(f"[WARN] web/out not found at {WEB_OUT_DIR!r}, skipping login gate.")
        return

    server = LoginServer()
    server.start()

    url = f"http://127.0.0.1:{server.port}/login/?port={server.port}"
    webbrowser.open(url)

    # Wait up to 5 minutes for the user to sign in
    server.wait_for_auth(timeout=300.0)
    server.stop()


PROGRESS_MARKERS = {
    "Building geometry": 10,
    "STL →": 25,
    "Meshing complete": 50,
    "Invoking CalculiX": 60,
    "PIPELINE COMPLETE": 75,
    "Extracting results": 85,
    "SWEEP RESULT": 100,
}


def style_entry(e):
    e.config(bg=BG3, fg=FG, insertbackground=FG, relief="flat",
              highlightthickness=1, highlightcolor=ACC,
              highlightbackground="#444444", font=FONT_BODY)


def label(parent, text, small=False):
    size = 9 if small else 10
    return tk.Label(parent, text=text, bg=BG, fg=ACC if not small else FG2,
                    font=("Segoe UI", size, "bold" if not small else "normal"))


def section_label(parent, text):
    lbl = tk.Label(parent, text=text.upper(), bg=BG, fg=ACC,
                   font=("Segoe UI", 8, "bold"), anchor="w")
    return lbl


def make_entry(parent, default="", width=30):
    v = tk.StringVar(value=str(default))
    e = tk.Entry(parent, textvariable=v, width=width)
    style_entry(e)
    return e, v


def browse_folder(var):
    p = filedialog.askdirectory(initialdir=var.get() or ROOT_DIR)
    if p:
        var.set(p)


def browse_file(var, title="Select executable"):
    p = filedialog.askopenfilename(title=title,
                                   filetypes=[("Executable", "*.exe"), ("All", "*.*")])
    if p:
        var.set(p)


def detect_paraview():
    import glob as _g
    patterns = [
        r"C:\Program Files\ParaView*\bin\paraview.exe",
        r"C:\Program Files (x86)\ParaView*\bin\paraview.exe",
        "/usr/bin/paraview",
        "/usr/local/bin/paraview",
    ]
    for pat in patterns:
        found = _g.glob(pat)
        if found:
            return found[0]
    return ""


class VeloForgeApp(tk.Tk):
    def __init__(self):
        super().__init__()
        self.title("VeloForge — Simulation Runner")
        self.configure(bg=BG)
        self.minsize(620, 720)
        self.resizable(True, True)
        self._set_icon()
        self._proc = None
        self._running = False
        self._build_ui()
        self._load_config_file(CONFIG_PATH, silent=True)

    def _set_icon(self):
        try:
            icon = tk.PhotoImage(width=32, height=32)
            # Draw orange "V" shape
            for y in range(32):
                for x in range(32):
                    if (x < 6 and y < 20 and x + y < 20) or \
                       (x > 25 and y < 20 and (31 - x) + y < 20) or \
                       (abs(x - 16) < 5 and y > 18):
                        icon.put("#FF6B00", (x, y))
                    else:
                        icon.put("#1a1a1a", (x, y))
            self.iconphoto(True, icon)
        except Exception:
            pass

    def _build_ui(self):
        self._build_header()
        self._build_tabs()
        self._build_footer()

    def _build_header(self):
        hdr = tk.Frame(self, bg="#111111", pady=12)
        hdr.pack(fill="x")

        tk.Label(hdr, text="VELOFORGE", bg="#111111", fg=FG,
                 font=("Segoe UI", 22, "bold")).pack(side="left", padx=20)
        tk.Label(hdr, text="Computational Engineering Platform", bg="#111111",
                 fg=ACC, font=("Segoe UI", 10)).pack(side="left")

        btn_f = tk.Frame(hdr, bg="#111111")
        btn_f.pack(side="right", padx=12)
        self._btn(btn_f, "LOAD CONFIG", self._on_load_config, w=12).pack(side="right", padx=4)
        self._btn(btn_f, "SAVE CONFIG", self._on_save_config, w=12).pack(side="right", padx=4)

    def _btn(self, parent, text, cmd, w=16, accent=True):
        c = ACC if accent else "#444444"
        b = tk.Button(parent, text=text, command=cmd,
                      bg=c, fg=FG, activebackground=ACC2, activeforeground=FG,
                      font=("Segoe UI", 9, "bold"), relief="flat",
                      padx=10, pady=6, width=w, cursor="hand2")
        b.bind("<Enter>", lambda e: b.config(bg=ACC2))
        b.bind("<Leave>", lambda e: b.config(bg=c))
        return b

    def _build_tabs(self):
        style = ttk.Style(self)
        style.theme_use("default")
        style.configure("TNotebook", background=BG, borderwidth=0)
        style.configure("TNotebook.Tab", background=BG2, foreground=FG2,
                        font=("Segoe UI", 9, "bold"), padding=[16, 8])
        style.map("TNotebook.Tab",
                  background=[("selected", BG)],
                  foreground=[("selected", ACC)])

        self._nb = ttk.Notebook(self)
        self._nb.pack(fill="both", expand=True, padx=8, pady=4)

        self._sim_tab   = tk.Frame(self._nb, bg=BG)
        self._mat_tab   = tk.Frame(self._nb, bg=BG)
        self._tools_tab = tk.Frame(self._nb, bg=BG)

        self._nb.add(self._sim_tab,   text="  SIMULATION  ")
        self._nb.add(self._mat_tab,   text="  MATERIAL  ")
        self._nb.add(self._tools_tab, text="  TOOLS  ")

        self._build_sim_tab()
        self._build_mat_tab()
        self._build_tools_tab()

    def _build_sim_tab(self):
        p = self._sim_tab
        pad = dict(padx=20, pady=6)

        # Thickness
        section_label(p, "Thickness Sweep (mm)").pack(anchor="w", **pad)
        tf = tk.Frame(p, bg=BG)
        tf.pack(anchor="w", padx=20, pady=2)
        self._thick_vars = []
        for lbl, default in [("Min", 18), ("Mid", 20), ("Max", 22)]:
            c = tk.Frame(tf, bg=BG)
            c.pack(side="left", padx=6)
            tk.Label(c, text=lbl, bg=BG, fg=FG2, font=("Segoe UI", 8)).pack()
            e, v = make_entry(c, default, width=6)
            e.pack()
            self._thick_vars.append(v)
        tk.Label(p, text="Values tested in sequence until SF target met",
                 bg=BG, fg=FG2, font=("Segoe UI", 8)).pack(anchor="w", padx=20, pady=2)

        # Safety factor
        section_label(p, "Safety Factor Target").pack(anchor="w", **pad)
        self._sf_entry, self._sf_var = make_entry(p, 1.5, 12)
        self._sf_entry.pack(anchor="w", padx=20, pady=2)
        tk.Label(p, text="Min acceptable SF  (Al 7075-T6 yield: 503 MPa)",
                 bg=BG, fg=FG2, font=("Segoe UI", 8)).pack(anchor="w", padx=20, pady=2)

        # Load
        section_label(p, "Applied Load (N)").pack(anchor="w", **pad)
        self._load_entry, self._load_var = make_entry(p, 6000, 12)
        self._load_entry.pack(anchor="w", padx=20, pady=2)
        tk.Label(p, text="Vertical bump load at axle bore nodes",
                 bg=BG, fg=FG2, font=("Segoe UI", 8)).pack(anchor="w", padx=20, pady=2)

        # Output dir
        section_label(p, "Output Folder").pack(anchor="w", **pad)
        of = tk.Frame(p, bg=BG)
        of.pack(anchor="w", padx=20, pady=2, fill="x")
        self._outdir_entry, self._outdir_var = make_entry(of, r"D:\pico\output", 36)
        self._outdir_entry.pack(side="left")
        self._btn(of, "Browse", lambda: browse_folder(self._outdir_var), w=8).pack(side="left", padx=6)

    def _build_mat_tab(self):
        p = self._mat_tab
        pad = dict(padx=20, pady=6)

        fields = [
            ("Young's Modulus (MPa)", "youngsModulusMpa", 71700.0),
            ("Poisson's Ratio",       "poissonRatio",     0.33),
            ("Density (g/mm³)",       "densityGPerMm3",   0.00281),
            ("Yield Strength (MPa)",  "yieldStrengthMpa", 503.0),
        ]
        self._mat_vars = {}
        for lbl_text, key, default in fields:
            section_label(p, lbl_text).pack(anchor="w", **pad)
            e, v = make_entry(p, default, 20)
            e.pack(anchor="w", padx=20, pady=2)
            self._mat_vars[key] = v

        info = tk.Frame(p, bg=BG3, highlightbackground=ACC,
                        highlightthickness=1)
        info.pack(fill="x", padx=20, pady=12)
        tk.Label(info, text="Default: Aluminum 7075-T6\n"
                            "Do not change unless using a different material.",
                 bg=BG3, fg=FG2, font=("Segoe UI", 9),
                 justify="left", padx=12, pady=10).pack(anchor="w")

    def _build_tools_tab(self):
        p = self._tools_tab
        pad = dict(padx=20, pady=6)

        # fTetWild
        section_label(p, "fTetWild Executable").pack(anchor="w", **pad)
        rf = tk.Frame(p, bg=BG)
        rf.pack(anchor="w", padx=20, pady=2, fill="x")
        self._ftet_entry, self._ftet_var = make_entry(
            rf, r"D:\pico\fTetWild\build\Release\FloatTetwild_bin.exe", 48)
        self._ftet_entry.pack(side="left")
        self._btn(rf, "Browse", lambda: browse_file(self._ftet_var), w=8).pack(side="left", padx=6)

        # CalculiX
        section_label(p, "CalculiX Executable").pack(anchor="w", **pad)
        cf = tk.Frame(p, bg=BG)
        cf.pack(anchor="w", padx=20, pady=2, fill="x")
        self._ccx_entry, self._ccx_var = make_entry(
            cf, r"D:\pico\calculix\CalculiX-2.21.0-win-x64\bin\ccx.exe", 48)
        self._ccx_entry.pack(side="left")
        self._btn(cf, "Browse", lambda: browse_file(self._ccx_var), w=8).pack(side="left", padx=6)

        # Validate
        vf = tk.Frame(p, bg=BG)
        vf.pack(anchor="w", padx=20, pady=14, fill="x")
        self._btn(vf, "VALIDATE TOOL PATHS", self._validate_tools, w=22).pack(side="left")
        self._ftet_status = tk.Label(vf, text="", bg=BG, font=("Segoe UI", 12))
        self._ftet_status.pack(side="left", padx=8)
        self._ccx_status  = tk.Label(vf, text="", bg=BG, font=("Segoe UI", 12))
        self._ccx_status.pack(side="left", padx=4)

    def _validate_tools(self):
        ftet = self._ftet_var.get()
        ccx  = self._ccx_var.get()
        ok_ftet = os.path.isfile(ftet)
        ok_ccx  = os.path.isfile(ccx)
        self._ftet_status.config(
            text=f"fTetWild {'✓' if ok_ftet else '✗'}",
            fg=GREEN if ok_ftet else RED)
        self._ccx_status.config(
            text=f"CalculiX {'✓' if ok_ccx else '✗'}",
            fg=GREEN if ok_ccx else RED)

    def _build_footer(self):
        sep = tk.Frame(self, bg="#333333", height=1)
        sep.pack(fill="x")

        foot = tk.Frame(self, bg="#111111", pady=10)
        foot.pack(fill="x", side="bottom")

        btn_row = tk.Frame(foot, bg="#111111")
        btn_row.pack(fill="x", padx=16)

        self._run_btn = self._btn(btn_row, "▶  RUN SIMULATION", self._on_run, w=20)
        self._run_btn.pack(side="left")

        self._cancel_btn = self._btn(btn_row, "■  CANCEL", self._on_cancel, w=10, accent=False)
        self._cancel_btn.config(state="disabled")
        self._cancel_btn.pack(side="left", padx=8)

        self._pv_btn = self._btn(btn_row, "Open in ParaView", self._on_open_paraview, w=18, accent=False)
        self._pv_btn.pack(side="left", padx=8)

        self._status_lbl = tk.Label(foot, text="Ready", bg="#111111",
                                    fg=FG2, font=("Segoe UI", 9))
        self._status_lbl.pack(anchor="w", padx=18, pady=4)

        # Progress bar
        pb_f = tk.Frame(foot, bg="#111111")
        pb_f.pack(fill="x", padx=16, pady=4)
        style = ttk.Style()
        style.configure("VF.Horizontal.TProgressbar",
                         troughcolor="#333333", background=ACC,
                         thickness=8)
        self._progress = ttk.Progressbar(pb_f, style="VF.Horizontal.TProgressbar",
                                          length=580, mode="determinate")
        self._progress.pack(fill="x")

        # Output panel
        out_f = tk.Frame(self, bg=BG)
        out_f.pack(fill="both", expand=True, padx=8, pady=4)
        self._output = tk.Text(out_f, bg="#0d0d0d", fg=FG, font=FONT_MONO,
                               relief="flat", wrap="word", state="disabled",
                               height=14)
        scroll = ttk.Scrollbar(out_f, command=self._output.yview)
        self._output.config(yscrollcommand=scroll.set)
        self._output.pack(side="left", fill="both", expand=True)
        scroll.pack(side="right", fill="y")

        # Configure color tags
        for prefix, color in LINE_COLORS.items():
            self._output.tag_config(prefix, foreground=color)
        self._output.tag_config("DEFAULT", foreground=FG2)
        self._output.tag_config("PASS_STATUS", foreground=GREEN)
        self._output.tag_config("FAIL_STATUS", foreground=RED)

    # ── Config I/O ────────────────────────────────────────────────────────────

    def _fields_to_dict(self):
        try:
            thicknesses = [float(v.get()) for v in self._thick_vars]
        except ValueError:
            thicknesses = DEFAULTS["simulation"]["thicknessCandidates"]
        return {
            "simulation": {
                "thicknessCandidates": thicknesses,
                "safetyFactorTarget": float(self._sf_var.get()),
                "loadN": float(self._load_var.get()),
                "outputDir": self._outdir_var.get(),
            },
            "material": {
                "youngsModulusMpa": float(self._mat_vars["youngsModulusMpa"].get()),
                "poissonRatio": float(self._mat_vars["poissonRatio"].get()),
                "densityGPerMm3": float(self._mat_vars["densityGPerMm3"].get()),
                "yieldStrengthMpa": float(self._mat_vars["yieldStrengthMpa"].get()),
            },
            "tools": {
                "fTetWildExe": self._ftet_var.get(),
                "ccxExe": self._ccx_var.get(),
                "outputDir": self._outdir_var.get(),
                "paraviewExe": getattr(self, "_pv_exe", ""),
            }
        }

    def _apply_dict(self, cfg):
        sim  = cfg.get("simulation", {})
        mat  = cfg.get("material", {})
        tools = cfg.get("tools", {})
        tc = sim.get("thicknessCandidates", DEFAULTS["simulation"]["thicknessCandidates"])
        for i, v in enumerate(self._thick_vars):
            v.set(str(tc[i]) if i < len(tc) else "")
        self._sf_var.set(str(sim.get("safetyFactorTarget", 1.5)))
        self._load_var.set(str(sim.get("loadN", 6000)))
        self._outdir_var.set(sim.get("outputDir", DEFAULTS["simulation"]["outputDir"]))
        self._mat_vars["youngsModulusMpa"].set(str(mat.get("youngsModulusMpa", 71700)))
        self._mat_vars["poissonRatio"].set(str(mat.get("poissonRatio", 0.33)))
        self._mat_vars["densityGPerMm3"].set(str(mat.get("densityGPerMm3", 0.00281)))
        self._mat_vars["yieldStrengthMpa"].set(str(mat.get("yieldStrengthMpa", 503)))
        self._ftet_var.set(tools.get("fTetWildExe", DEFAULTS["tools"]["fTetWildExe"]))
        self._ccx_var.set(tools.get("ccxExe", DEFAULTS["tools"]["ccxExe"]))
        self._pv_exe = tools.get("paraviewExe", "")

    def _load_config_file(self, path, silent=False):
        if not os.path.isfile(path):
            return
        try:
            with open(path) as f:
                cfg = json.load(f)
            self._apply_dict(cfg)
            if not silent:
                self._set_status("Config loaded.", FG2)
        except Exception as e:
            if not silent:
                messagebox.showerror("Load Error", str(e))

    def _save_config_file(self, path):
        try:
            cfg = self._fields_to_dict()
            with open(path, "w") as f:
                json.dump(cfg, f, indent=2)
        except Exception as e:
            messagebox.showerror("Save Error", str(e))

    def _on_save_config(self):
        self._save_config_file(CONFIG_PATH)
        self._set_status("Config saved to config.json", FG2)

    def _on_load_config(self):
        p = filedialog.askopenfilename(
            title="Load config.json",
            filetypes=[("JSON", "*.json"), ("All", "*.*")],
            initialdir=ROOT_DIR)
        if p:
            self._load_config_file(p)

    # ── Validation ────────────────────────────────────────────────────────────

    def _validate_fields(self):
        errors = []
        for i, v in enumerate(self._thick_vars):
            try:
                float(v.get())
            except ValueError:
                errors.append(f"Thickness {i+1} must be a number.")
        try:
            sf = float(self._sf_var.get())
            if not (0.1 <= sf <= 10.0):
                errors.append("Safety factor must be between 0.1 and 10.0.")
        except ValueError:
            errors.append("Safety factor must be a number.")
        try:
            ln = float(self._load_var.get())
            if ln <= 0:
                errors.append("Load must be a positive number.")
        except ValueError:
            errors.append("Load must be a number.")
        return errors

    # ── Run / Cancel ─────────────────────────────────────────────────────────

    def _set_status(self, text, color=FG2):
        self._status_lbl.config(text=text, fg=color)

    def _append_output(self, line):
        self._output.config(state="normal")
        tag = "DEFAULT"
        for prefix, _ in LINE_COLORS.items():
            if prefix in line:
                tag = prefix
                break
        self._output.insert("end", line + "\n", tag)
        self._output.see("end")
        self._output.config(state="disabled")

        # Progress
        for marker, pct in PROGRESS_MARKERS.items():
            if marker in line:
                self._progress["value"] = pct
                break

    def _on_run(self):
        errors = self._validate_fields()
        if errors:
            messagebox.showerror("Validation Error", "\n".join(errors))
            return

        outdir = self._outdir_var.get()
        os.makedirs(outdir, exist_ok=True)
        self._save_config_file(CONFIG_PATH)

        self._output.config(state="normal")
        self._output.delete("1.0", "end")
        self._output.config(state="disabled")
        self._progress["value"] = 0

        self._run_btn.config(state="disabled", text="RUNNING…")
        self._cancel_btn.config(state="normal")
        self._set_status("Initializing pipeline…", ACC)
        self._running = True
        self._last_sf = None
        self._pass_vtu = None

        t = threading.Thread(target=self._run_thread, daemon=True)
        t.start()

    def _kill_pico(self):
        """Kill any lingering pico.exe so dotnet run can overwrite the binary."""
        try:
            subprocess.run(
                ["taskkill", "/F", "/IM", "pico.exe"],
                capture_output=True)
        except Exception:
            pass

    def _run_thread(self):
        # Release the file lock from any previous run first
        self._kill_pico()
        import time; time.sleep(0.5)   # brief pause so OS releases the handle

        try:
            check = subprocess.run(["dotnet", "--version"],
                                   capture_output=True, text=True)
            if check.returncode != 0:
                self.after(0, self._append_output,
                           "[ERROR] dotnet is not installed or not on PATH.")
                self.after(0, self._finish_run, False)
                return
        except FileNotFoundError:
            self.after(0, self._append_output,
                       "[ERROR] dotnet is not installed or not on PATH.")
            self.after(0, self._finish_run, False)
            return

        try:
            env = os.environ.copy()
            env["DOTNET_NOLOGO"] = "1"
            env["PYTHONUNBUFFERED"] = "1"
            # Tell .NET to output UTF-8 so Windows cp1252 codec never mismatches
            env["DOTNET_SYSTEM_GLOBALIZATION_INVARIANT"] = "0"
            env["DOTNET_SYSTEM_CONSOLE_ALLOW_ANSI_COLOR_REDIRECTION"] = "1"
            self._proc = subprocess.Popen(
                ["dotnet", "run"],
                cwd=ROOT_DIR,
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                env=env,
                encoding="utf-8",
                errors="replace",
                bufsize=1)

            passed = False
            for line in self._proc.stdout:
                if not self._running:
                    break
                line = line.rstrip()
                self.after(0, self._append_output, line)
                if "SWEEP RESULT: PASS" in line or "[PASS]" in line:
                    passed = True
                if "Safety Factor" in line and ":" in line:
                    try:
                        self._last_sf = line.split(":")[-1].strip().split()[0]
                    except Exception:
                        pass

            self._proc.wait()
            if self._running:
                self.after(0, self._finish_run, passed)

        except Exception as e:
            self.after(0, self._append_output, f"[ERROR] {e}")
            self.after(0, self._finish_run, False)

    def _finish_run(self, passed):
        self._running = False
        self._run_btn.config(state="normal", text="▶  RUN SIMULATION")
        self._cancel_btn.config(state="disabled")
        self._progress["value"] = 100 if passed else self._progress["value"]

        if passed:
            sf_text = f"  SF: {self._last_sf}" if self._last_sf else ""
            self._set_status(f"✓ VALIDATED —{sf_text}", GREEN)
            self._find_and_show_paraview()
        else:
            sf_text = f"  Best SF: {self._last_sf}" if self._last_sf else ""
            self._set_status(f"✗ NO VALID DESIGN FOUND{sf_text}", RED)

    def _on_cancel(self):
        self._running = False
        if self._proc and self._proc.poll() is None:
            self._proc.terminate()
        self._run_btn.config(state="normal", text="▶  RUN SIMULATION")
        self._cancel_btn.config(state="disabled")
        self._set_status("Cancelled", FG2)

    # ── ParaView ──────────────────────────────────────────────────────────────

    def _find_and_show_paraview(self):
        outdir = self._outdir_var.get()
        vtus = glob.glob(os.path.join(outdir, "*.vtu"))
        if not vtus:
            self._pass_vtu = None
            self._set_status("✓ PASS — click 'Open in ParaView' to browse for .vtu", GREEN)
            return
        self._pass_vtu = sorted(vtus)[-1]
        self._set_status(f"✓ PASS — result ready: {os.path.basename(self._pass_vtu)}", GREEN)

    def _on_open_paraview(self):
        # Always let user pick a file manually
        vtu = filedialog.askopenfilename(
            title="Select result .vtu file",
            initialdir=self._outdir_var.get() or ROOT_DIR,
            filetypes=[("VTK Unstructured Grid", "*.vtu"), ("All", "*.*")])
        if not vtu:
            return

        # Resolve ParaView executable
        pv_exe = getattr(self, "_pv_exe", "") or detect_paraview()

        if not pv_exe or not os.path.isfile(pv_exe):
            pv_exe = filedialog.askopenfilename(
                title="Locate paraview.exe",
                filetypes=[("Executable", "*.exe"), ("All", "*.*")])
            if not pv_exe:
                messagebox.showwarning("ParaView Not Found",
                    "Please select the ParaView executable to continue.")
                return
            self._pv_exe = pv_exe
            self._save_config_file(CONFIG_PATH)

        try:
            subprocess.Popen([pv_exe, vtu])
            self._set_status(f"Opening {os.path.basename(vtu)} in ParaView…", FG2)
        except Exception as e:
            messagebox.showerror("ParaView Error", str(e))


if __name__ == "__main__":
    show_login_and_wait()
    app = VeloForgeApp()
    app.mainloop()

