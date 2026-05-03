# 🚀 VeloForge

> **Imagine a robot engineer that designs a motorcycle part, tests whether it is strong enough, and fixes it automatically — all without a human touching CAD software.**
> That is what VeloForge does.

---

## 🧒 What does VeloForge actually do? (Plain English)

1. You tell it: *"I need a brake bracket for a motorcycle. It must not break under a 600 kg bump load."*
2. VeloForge **draws** the part in 3D — by writing code, not clicking buttons.
3. It **stress-tests** the part inside the computer — like a virtual crash test.
4. If the part is **too weak**, it automatically makes it thicker and tests again.
5. When the part **passes**, it saves the final design and tells you the result.
6. You can watch all of this happen live in a desktop app with a progress bar.

---

## 🗺️ The Big Picture

```
┌─────────────────────────────────────────────────────────┐
│                     You (the user)                      │
│         Opens VeloForge.exe  →  Logs in                 │
│         Sets load = 6000 N, thickness candidates        │
│         Clicks ▶ RUN SIMULATION                         │
└───────────────────────┬─────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────┐
│              Python GUI  (gui.py + tkinter)             │
│   Shows live output, progress bar, opens ParaView       │
└───────────────────────┬─────────────────────────────────┘
                        │ launches
                        ▼
┌─────────────────────────────────────────────────────────┐
│           C# Optimization Orchestrator (pico.exe)       │
│                                                         │
│   FOR each thickness in [18 mm, 20 mm, 22 mm]:         │
│   ┌──────────────────────────────────────────────┐      │
│   │  PHASE 1 → Draw the 3D part                  │      │
│   │  PHASE 2 → Test it for strength              │      │
│   │  IF passes → STOP and save ✅                │      │
│   │  IF fails  → try next thickness ❌           │      │
│   └──────────────────────────────────────────────┘      │
└─────────────────────────────────────────────────────────┘
```

---

## 🎨 Phase 1 — Drawing the Part (Generative Geometry)

**Think of it like:** Building with LEGO — but using code instead of your hands.

```
  Code says:                          What you get:
  ┌──────────────────────┐            ┌─────────────┐
  │ Add a box (body)     │  ──────►   │  ████████   │
  │ Remove cylinder      │            │  █ ○ ██ ○   │  ← bolt holes
  │   (bolt holes x4)    │            │  █      █   │
  │ Remove cylinder      │            │  █  ( )  █  │  ← axle hole
  │   (axle bore)        │            │  ████████   │
  │ Remove pockets       │            │  █ [  ] █   │  ← lightweighting
  │   (save weight)      │            └─────────────┘
  └──────────────────────┘            3D mesh file (.stl)
```

**Files involved:**
- `src/BrakeBracket.cs` — the code that draws the bracket
- `LEAP71_ShapeKernel/` — the library of 3D shape tools (boxes, cylinders, etc.)

**Technologies used:**

| Tool | What it is | Explanation |
|------|-----------|--------------------------|
| **C#** | Programming language | Like Python but used a lot in game engines and industry tools |
| **PicoGK** | 3D geometry engine | The engine that turns code into actual 3D shapes |
| **Leap71 ShapeKernel** | Shape library | A toolbox of pre-built 3D shapes you can combine |

**Output:** A `.stl` file — a 3D mesh made of tiny triangles, like a 3D print file.

---

## 🔬 Phase 2 — Stress Testing the Part (FEA Pipeline)

**Think of it like:** A virtual crash test — the computer squeezes the part and measures how much it bends.

FEA stands for **Finite Element Analysis** — it splits the part into thousands of tiny chunks and calculates the forces on each one.

```
.stl file (triangle shell)
        │
        ▼  Step 1: Fill it solid
   ┌─────────────────────────────────────────────┐
   │  fTetWild                                   │
   │  Converts hollow shell mesh → solid mesh    │
   │  (like filling a bag with sand)             │
   │  Output: .mesh file (solid tetrahedra)      │
   └─────────────────────────┬───────────────────┘
                             │
                             ▼  Step 2: Set up the test
   ┌─────────────────────────────────────────────┐
   │  InpSerializer.cs  (custom C# code)         │
   │  Writes the simulation "recipe":            │
   │  • Material: Aluminium 7075-T6              │
   │  • Fixed points: the 4 bolt holes           │
   │  • Applied force: 6000 N pushing UP         │
   │    at the axle bore                         │
   │  Output: .inp file (instructions for solver)│
   └─────────────────────────┬───────────────────┘
                             │
                             ▼  Step 3: Run the test
   ┌─────────────────────────────────────────────┐
   │  CalculiX  (ccx.exe)                        │
   │  The FEA solver — does the maths            │
   │  Calculates stress & deformation            │
   │  on every tiny element                      │
   │  Output: .frd file (raw results)            │
   └─────────────────────────┬───────────────────┘
                             │
                             ▼  Step 4: Convert results
   ┌─────────────────────────────────────────────┐
   │  ccx2paraview                               │
   │  Translates .frd → .vtu (readable format)   │
   │  (Like converting a .doc to a .pdf)         │
   └─────────────────────────┬───────────────────┘
                             │
                             ▼  Step 5: Read the score
   ┌─────────────────────────────────────────────┐
   │  extract_vtu_results.py  (Python script)    │
   │  Reads the .vtu file and extracts:          │
   │  • Peak Stress (von Mises)                  │
   │  • Max Displacement                         │
   │  • Safety Factor = Yield ÷ Peak Stress      │
   └─────────────────────────┬───────────────────┘
                             │
                             ▼
              Safety Factor ≥ 1.5? → ✅ PASS
              Safety Factor < 1.5? → ❌ FAIL
```

**What is Safety Factor?**
> If a bridge can hold 150 kg and you put 100 kg on it — the safety factor is 1.5. VeloForge requires a minimum of 1.5, meaning the part must be **50% stronger** than the worst-case load.

**Technologies used:**

| Tool | What it is | Kid-friendly explanation |
|------|-----------|--------------------------|
| **fTetWild** | Mesh tool | Fills a hollow 3D shape with thousands of tiny pyramids |
| **CalculiX** | FEA solver | Does the engineering maths to find where parts will break |
| **ccx2paraview** | Converter | Changes the result file into a format you can view in 3D |
| **Python** | Language | Used to read and understand the result numbers |

---

## 🔁 Phase 3 — The Optimization Loop

**Think of it like:** Goldilocks testing porridge — too thin, too thin... just right!

```
  Start: thickness = 18 mm
         │
         ▼
  ┌─────────────────────┐
  │ Draw part           │
  │ Test part           │
  │ SF = 1.1 → FAIL ❌  │
  └──────────┬──────────┘
             │ try thicker
             ▼
  ┌─────────────────────┐
  │ Draw part           │
  │ Test part           │
  │ SF = 1.3 → FAIL ❌  │
  └──────────┬──────────┘
             │ try thicker
             ▼
  ┌─────────────────────┐
  │ Draw part           │
  │ Test part           │
  │ SF = 1.7 → PASS ✅  │
  │ Save design!        │
  └─────────────────────┘
```

**File:** `Program.cs` — the main orchestrator that runs this loop.

---

## 🖥️ The Desktop App

**Think of it like:** A cockpit for the simulation — buttons, live output, and a progress bar.

```
┌──────────────────────────────────────────────────────────┐
│  BROWSER (login page)              DESKTOP APP (GUI)     │
│                                                          │
│  ┌──────────────────┐              ┌──────────────────┐  │
│  │                  │   sign in    │  VELOFORGE       │  │
│  │  ┌────────────┐  │ ──────────►  │  ──────────────  │  │
│  │  │  Email     │  │              │  [SIMULATION]    │  │
│  │  │  Password  │  │              │  Thickness: 18   │  │
│  │  │  [LOG IN]  │  │              │  Load: 6000 N    │  │
│  │  └────────────┘  │              │                  │  │
│  │                  │              │  ▶ RUN           │  │
│  └──────────────────┘              │  ────────────    │  │
│     Built with Next.js             │  [LOG OUTPUT]    │  │
│     Styled with CSS + GSAP         │  [PROGRESS BAR]  │  │
│                                    └──────────────────┘  │
│                                    Built with Python      │
│                                    tkinter               │
└──────────────────────────────────────────────────────────┘
```

### How the login works

```
gui.py starts
    │
    ▼
Starts a tiny web server on your computer (port is chosen randomly)
    │
    ▼
Opens your browser at http://127.0.0.1:<port>/login
    │
    ▼
You type your email/password and click Sign In
    │
    ▼
Browser sends POST /auth to the tiny server
    │
    ▼
Server says "OK!" → GUI window opens
```

**Technologies used:**

| Tool | What it is | Kid-friendly explanation |
|------|-----------|--------------------------|
| **Next.js** | Web framework | Builds the login website that runs in your browser |
| **GSAP** | Animation library | Makes the login page look smooth and fancy |
| **Python tkinter** | GUI toolkit | Builds the desktop control panel window |
| **PyInstaller** | Packager | Bundles everything into one `VeloForge.exe` file |
| **http.server** | Python module | The tiny web server that serves the login page locally |

---

## 📦 The Installer Pipeline

**Think of it like:** Packing a lunchbox — all the pieces go in so someone else can eat it anywhere.

```
build_installer.ps1 runs in order:
┌────────────────────────────────────────────────────────────┐
│                                                            │
│  Step 1: Build the login website                           │
│  cd web && npm run build                                   │
│  → Creates web/out/ (static HTML/CSS/JS files)             │
│                                                            │
│  Step 2: Build the C# simulation engine                    │
│  dotnet publish → dist/pipeline/pico.exe                   │
│  (self-contained: no .NET install needed on target PC)     │
│                                                            │
│  Step 3: Pack the Python GUI into an exe                   │
│  PyInstaller gui.py → dist/VeloForge.exe                   │
│  (bundles Python, login page, and pipeline together)       │
│                                                            │
│  Step 4: Collect external tools                            │
│  Copy fTetWild.exe, ccx.exe, ccx2paraview.exe              │
│  Verify SHA-256 checksums (security check ✅)              │
│                                                            │
│  Step 5: Create Windows installer                          │
│  Inno Setup compiles everything →                          │
│  dist/installer/VeloForge_Setup.exe                        │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

**Technologies used:**

| Tool | What it is | Kid-friendly explanation |
|------|-----------|--------------------------|
| **PowerShell** | Scripting language | Runs the build steps automatically on Windows |
| **Inno Setup** | Installer creator | Makes the Setup.exe with the Next / Back / Install wizard |
| **npm** | Package manager | Downloads and runs JavaScript tools (like app stores for code) |
| **.NET SDK** | Build tool | Compiles C# code into an executable program |
| **SHA-256** | Checksum | A fingerprint check to make sure files weren't tampered with |

---

## 📂 Folder Map

```
d:\pico\
│
├── Program.cs              ← Main loop (optimization orchestrator)
├── gui.py                  ← Desktop GUI (Python + tkinter)
├── build_installer.ps1     ← Build script (one click → installer)
├── setup.iss               ← Inno Setup recipe for the installer
├── verify_install.ps1      ← Health check after installing
│
├── src/
│   └── BrakeBracket.cs     ← Draws the 3D brake bracket
│   └── FEAPipeline/
│       ├── FEARunner.cs    ← Runs fTetWild, ccx, ccx2paraview
│       ├── InpSerializer.cs← Writes the .inp simulation recipe
│       └── extract_vtu_results.py ← Reads stress/SF from results
│
├── web/                    ← Login page (Next.js website)
│   ├── pages/login.tsx     ← The login form
│   └── out/                ← Built static files (served by gui.py)
│
├── fTetWild/               ← Mesh tool binary
├── calculix/               ← FEA solver binary
│
├── dist/
│   ├── VeloForge.exe       ← The packed Python GUI
│   ├── pipeline/pico.exe   ← The packed C# engine
│   ├── bins/               ← fTetWild, ccx, ccx2paraview copies
│   └── installer/
│       └── VeloForge_Setup.exe  ← The final installer ← SHIP THIS
│
└── output/                 ← Your simulation results go here
    ├── brake_bracket.stl   ← Passing design mesh
    └── *.vtu               ← Stress results (open in ParaView)
```

---

## 🧰 Full Technology List

| Layer | Technology | Language | Purpose |
|-------|-----------|----------|---------|
| Geometry | PicoGK + Leap71 ShapeKernel | C# | Programmatic 3D part generation |
| Meshing | fTetWild | C++ binary | STL shell → solid tet mesh |
| FEA setup | InpSerializer | C# | Writes CalculiX input deck |
| FEA solver | CalculiX (ccx) | Fortran/C binary | Structural stress simulation |
| Result conversion | ccx2paraview | Python | FRD → VTU format |
| Result parsing | extract_vtu_results.py | Python | Extract peak stress & SF |
| Orchestration | Program.cs | C# | Thickness sweep loop |
| Login UI | Next.js + GSAP | TypeScript/CSS | Browser-based login page |
| Desktop GUI | tkinter | Python | Control panel window |
| GUI packaging | PyInstaller | — | Packs gui.py → VeloForge.exe |
| C# packaging | dotnet publish | — | Packs pipeline → pico.exe |
| Installer | Inno Setup | Pascal | Creates VeloForge_Setup.exe |
| Build script | PowerShell | — | Automates all build steps |

---

## ▶️ Quick Start

### Install (easiest way)
1. Download `VeloForge_Setup.exe` from [GitHub Releases](https://github.com/ochidesoim/pico/releases/tag/v0.1.0-alpha)
2. Double-click it → follow the wizard
3. Launch **VeloForge** from the Start Menu

### Run from source (for developers)
```bash
# 1. Build the login page
cd web && npm run build && cd ..

# 2. Run the GUI (opens login in browser first)
python gui.py

# 3. Or run the simulation engine directly (no GUI)
dotnet run
```

### Build the installer yourself
```powershell
# In the repo root — requires Node.js, .NET 9, Python 3, Inno Setup 6
.\build_installer.ps1
# Output: dist\installer\VeloForge_Setup.exe
```

---

## 📋 Dependencies

For the full list of every library and version used in C#, Python, and Next.js, see [`libraries.md`](libraries.md).

For developer build instructions and prerequisites, see [`README_BUILD.md`](README_BUILD.md).

---

## 📜 License

MIT — see [`LICENSE.txt`](LICENSE.txt)
