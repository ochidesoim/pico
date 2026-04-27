# VeloForge GUI — Implementation Phases

## OVERVIEW
Add a GUI to VeloForge without touching
existing codebase except ONE small change
to Program.cs to read config.json.

Architecture:
  gui.py        → tkinter GUI (new file)
  config.json   → parameter bridge (new file)
  Program.cs    → reads config.json (10 lines added)
  Everything else → UNTOUCHED

---

## PHASE 1 — config.json schema
### Create this file in project root

File: config.json

{
  "simulation": {
    "thicknessCandidates": [18, 20, 22],
    "safetyFactorTarget": 1.5,
    "loadN": 6000.0,
    "outputDir": "D:\\pico\\output"
  },
  "material": {
    "youngsModulusMpa": 71700.0,
    "poissonRatio": 0.33,
    "densityGPerMm3": 0.00281,
    "yieldStrengthMpa": 503.0
  },
  "tools": {
    "fTetWildExe": "D:\\pico\\fTetWild\\build\\Release\\FloatTetwild_bin.exe",
    "ccxExe": "D:\\pico\\calculix\\CalculiX-2.21.0-win-x64\\bin\\ccx.exe",
    "outputDir": "D:\\pico\\output"
  }
}

Success criteria:
  ✓ File exists in root
  ✓ Valid JSON (validate at jsonlint.com)
  ✓ All current hardcoded values match

---

## PHASE 2 — Program.cs config reader
### Only change to existing codebase
### Add 10 lines at TOP of Program.cs

Add BEFORE Library.Go() call:

using System.Text.Json;

// Read config.json if it exists
string configPath = Path.Combine(
  AppDomain.CurrentDomain.BaseDirectory,
  "config.json"
);

if (File.Exists(configPath)) {
  var json = File.ReadAllText(configPath);
  var config = JsonDocument.Parse(json);
  var sim = config.RootElement
    .GetProperty("simulation");
  var tools = config.RootElement
    .GetProperty("tools");
  var mat = config.RootElement
    .GetProperty("material");

  // Override hardcoded values:
  // Pass to relevant constructors
  // via static Config class below
}

Create new file: src/Config.cs

public static class Config {
  public static float[] ThicknessCandidates
    = { 18f, 20f, 22f };
  public static float SafetyFactor = 1.5f;
  public static double LoadN = 6000.0;
  public static string OutputDir
    = @"D:\pico\output";
  public static string FTetWildExe
    = @"D:\pico\fTetWild\...";
  public static string CcxExe
    = @"D:\pico\calculix\...";
  public static double YoungsModulus
    = 71700.0;
  public static double PoissonRatio = 0.33;
  public static double YieldStrength
    = 503.0;

  public static void LoadFromJson(
    string path) {
    // reads config.json
    // overwrites static values above
  }
}

Then replace hardcoded values in:
  Program.cs line 15  → Config.OutputDir
  Program.cs line 16  → Config.ThicknessCandidates
  Program.cs line 53  → Config.SafetyFactor
  InpSerializer.cs 59 → Config.LoadN
  InpSerializer.cs 43 → Config.YoungsModulus
  VtuResultParser.cs 16 → Config.YieldStrength
  PipelineOrchestrator.cs 10 → Config.FTetWildExe
  PipelineOrchestrator.cs 11 → Config.CcxExe

Files touched: Program.cs · Config.cs (new)
               InpSerializer.cs · 
               VtuResultParser.cs ·
               PipelineOrchestrator.cs
               (each 1 line change only)

Success criteria:
  ✓ dotnet run still works with config.json
  ✓ dotnet run still works WITHOUT config.json
    (falls back to hardcoded defaults)
  ✓ Change thickness in config.json
    run again · different result

---

## PHASE 3 — gui.py skeleton
### New file in root folder
### Python + tkinter (zero install needed)

File: gui.py

import tkinter as tk
from tkinter import ttk, filedialog
import json · os · subprocess · threading

Window: 600×700px
Title: "VeloForge — Simulation Runner"
Background: #1a1a1a (dark)
Accent: #FF6B00 (orange)

LAYOUT:
  Header:
    "VELOFORGE" · white · bold · 24px
    "Computational Engineering Platform"
    · orange · 11px

  Tabs:
    Tab 1: "SIMULATION"  ← default open
    Tab 2: "MATERIAL"
    Tab 3: "TOOLS"

  Footer:
    [ RUN SIMULATION ] button · orange bg
    Status label · shows current state

Success criteria:
  ✓ python gui.py opens window
  ✓ Window shows without errors
  ✓ Dark theme renders correctly
  ✓ All 3 tabs clickable

---

## PHASE 4 — Simulation tab inputs
### Tab 1 content

SIMULATION TAB fields:

  Thickness Candidates (mm):
    Label: "THICKNESS SWEEP (mm)"
    3 input boxes side by side:
      Min: [18]  Mid: [20]  Max: [22]
    Subtext: "Comma-separated values
              to test in sequence"

  Safety Factor Target:
    Label: "SAFETY FACTOR TARGET"
    Input: [1.5]
    Subtext: "Minimum acceptable SF
              (Al 7075-T6 yield: 503 MPa)"

  Load Force:
    Label: "APPLIED LOAD (N)"
    Input: [6000]
    Subtext: "Vertical bump load
              at axle bore nodes"

  Output Directory:
    Label: "OUTPUT FOLDER"
    Input: [D:\pico\output]
    [ Browse ] button → opens folder picker

All fields:
  Dark input bg: #2a2a2a
  Orange border on focus
  White text
  Orange label text · 9px · uppercase
  letter-spacing: 2px

Success criteria:
  ✓ All fields show default values
  ✓ Browse button opens folder dialog
  ✓ Values editable by user
  ✓ Input validation:
      thickness: numbers only
      safety factor: 0.1 to 10.0
      load: positive number only

---

## PHASE 5 — Material + Tools tabs
### Tab 2 and Tab 3 content

MATERIAL TAB fields:
  Young's Modulus (MPa): [71700]
  Poisson's Ratio:       [0.33]
  Density (g/mm³):       [0.00281]
  Yield Strength (MPa):  [503.0]

  Info box below fields:
    "Default: Aluminum 7075-T6
     Do not change unless using
     a different material"
    · orange border · dark bg

TOOLS TAB fields:
  fTetWild Executable:
    Input + [ Browse ] → file picker
    Default: D:\pico\fTetWild\...

  CalculiX Executable:
    Input + [ Browse ] → file picker
    Default: D:\pico\calculix\...

  Validate button:
    [ VALIDATE TOOL PATHS ]
    Checks both .exe files exist
    Shows green ✓ or red ✗ per tool

Success criteria:
  ✓ Material values editable
  ✓ Tool path browse buttons work
  ✓ Validate button checks files exist
  ✓ Green tick or red cross shown

---

## PHASE 6 — config.json save/load
### GUI reads and writes config.json

On startup:
  If config.json exists:
    Load values into all fields
  Else:
    Show hardcoded defaults

On [ RUN SIMULATION ] click:
  Step 1: validate all fields
  Step 2: write config.json:
    {
      "simulation": {
        "thicknessCandidates":
          [value1, value2, value3],
        "safetyFactorTarget": value,
        "loadN": value,
        "outputDir": "path"
      },
      "material": { ... },
      "tools": { ... }
    }
  Step 3: confirm file written
  Step 4: proceed to Phase 7

Save/Load buttons in header:
  [ SAVE CONFIG ] → saves current fields
                    to config.json
  [ LOAD CONFIG ] → opens file picker
                    loads any config.json

Success criteria:
  ✓ config.json written correctly
  ✓ Reopen GUI → values restored
  ✓ Load different config → fields update
  ✓ Invalid values show error message
    before writing

---

## PHASE 7 — Run simulation + live output
### The main feature

[ RUN SIMULATION ] button behavior:

  Button disables → shows "RUNNING..."
  Status: "Initializing pipeline..."

  Runs in background thread:
    subprocess.Popen(
      ["dotnet", "run"],
      cwd=project_root,
      stdout=PIPE,
      stderr=STDOUT
    )

  OUTPUT PANEL appears below button:
    Scrollable text area · dark bg
    Monospace font · 11px
    Each stdout line appends in realtime

    Lines colored by prefix:
      [MESH]  → #00CFFF cyan
      [FEA]   → #FF6B00 orange
      [SOLVE] → #FFAA00 yellow
      [PASS]  → #00FF88 green
      [FAIL]  → #FF2200 red
      [DONE]  → #FFFFFF white

  Progress bar:
    Fills as known steps complete:
      Geometry built:    25%
      Mesh complete:     50%
      Solver converged:  75%
      Result parsed:     100%

  On completion:
    If PASS:
      Status: "✓ VALIDATED — SF: X.XXX"
              green text
      [ OPEN IN PARAVIEW ] button appears
        → subprocess.Popen(paraview result.vtu)

    If FAIL:
      Status: "✗ NO VALID DESIGN FOUND"
              red text
      Shows best SF achieved

  [ CANCEL ] button:
    Kills subprocess
    Status: "Cancelled"

Success criteria:
  ✓ Output streams live not after finish
  ✓ Colors match line prefixes
  ✓ Progress bar moves
  ✓ PASS shows green + Paraview button
  ✓ FAIL shows red + best SF
  ✓ Cancel kills the process

---

## PHASE 8 — Paraview auto-open
### Opens result in Paraview automatically

On PASS:
  Find result .vtu file in output folder
  Detect Paraview installation:
    Check common paths:
      C:\Program Files\ParaView*\bin\paraview.exe
      /usr/bin/paraview
      /Applications/ParaView*.app

  If found:
    Auto-open: subprocess.Popen([paraview, vtu])
    Status: "Opening in Paraview..."

  If not found:
    Show dialog:
      "Paraview not found.
       Please select paraview.exe"
    File picker → remember path
    Save to config.json as:
      "paraviewExe": "path/to/paraview.exe"

Success criteria:
  ✓ .vtu file found automatically
  ✓ Paraview opens with result loaded
  ✓ If not found → file picker shown
  ✓ Path saved so not asked again

---

## PHASE 9 — Polish + packaging
### Make it distributable

Window polish:
  App icon: VeloForge logo (orange V)
  Taskbar icon set
  Window min size: 600×700
  Resizable: yes · responsive layout

Error handling:
  dotnet not installed → clear message
  Output folder missing → auto-create
  Tool not found → highlight Tools tab
  JSON parse error → show which field

Package as .exe (Windows):
  pip install pyinstaller
  pyinstaller --onefile --windowed
    --icon=logo.ico gui.py
  Output: dist/VeloForge.exe
  Double-click to run
  No Python install needed

README update:
  Add "GUI Usage" section
  Screenshot of window
  How to change parameters

Success criteria:
  ✓ VeloForge.exe runs on clean Windows
  ✓ No terminal window visible
  ✓ All phases work end to end
  ✓ Non-technical person can use it

---

## EXECUTION ORDER

Phase 1 → config.json schema
Phase 2 → Program.cs config reader
Phase 3 → gui.py window skeleton
Phase 4 → Simulation tab inputs
Phase 5 → Material + Tools tabs
Phase 6 → Save/load config.json
Phase 7 → Run + live output stream
Phase 8 → Paraview auto-open
Phase 9 → Polish + package as .exe

Test after EACH phase before proceeding.
Never skip a phase.
Never run phases simultaneously.