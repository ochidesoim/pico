# Phase 2 — FEA Pipeline: Complete 5-Document System

---

# DOCUMENT 1: PRD — Phase 2

## Core Thesis
Take the STL exported in Phase 1 and run it through a fully automated FEA pipeline. No human touches anything between STL and stress result. The output is a single number: **peak von Mises stress and safety factor logged to console.**

## Confirmed Phase 1 Output
```
D:\pico\output\brake_bracket.stl  ← input to Phase 2
```

## Features

**F1 — Volumetric Meshing**
Invoke fTetWild as headless subprocess. Convert STL → tetrahedral volume mesh. Output: `.mesh` file.

**F2 — Mesh Conversion to Abaqus format**
Convert fTetWild `.mesh` output → Abaqus `.inp` format via C# serializer. Write NODE block, ELEMENT block, MATERIAL block, BOUNDARY block, LOAD block.

**F3 — CalculiX FEA Solve**
Invoke `ccx` as headless subprocess. Pass `.inp` file. Capture stdout convergence lines in real time. Output: `.frd` results file.

**F4 — Result Conversion**
Invoke `ccx2paraview` as headless subprocess. Convert `.frd` → `.vtu` XML format.

**F5 — Result Parsing**
Parse `.vtu` XML using `System.Xml.Linq`. Extract peak von Mises stress, max displacement, strain energy. Calculate safety factor against Al 7075-T6 yield strength (503 MPa). Log structured result block to console.

## Anti-Features
- No topology optimization
- No multi-load-case (single bump load only)
- No GUI result viewer
- No cloud execution
- No iterative optimization loop

## Material — Al 7075-T6
| Property | Value |
|---|---|
| Young's Modulus | 71.7 GPa |
| Poisson's Ratio | 0.33 |
| Yield Strength | 503 MPa |
| Density | 2.81 g/cm³ |

## Load Case — Vertical Bump
| Parameter | Value |
|---|---|
| Magnitude | 6000 N |
| Direction | -Z (vertical down) |
| Application point | Axle bore node set |
| Boundary condition | Fixed at mounting bolt holes |

## Success Criteria
```
Peak von Mises stress: XXX.X MPa
Yield strength:        503.0 MPa  
Safety factor:         X.XX
Max displacement:      X.XXX mm
Pipeline duration:     XX min XX sec
STL file found ✓
Mesh generated ✓
Solver converged ✓
Results parsed ✓
```

## Edge Cases
| Scenario | Behavior |
|---|---|
| fTetWild not on PATH | Startup check fails fast with clear error |
| Mesh has negative Jacobians | Downgrade C3D10 → C3D4, retry once |
| CalculiX non-zero exit | Catch exit code, dump stderr to diagnostics file |
| VTU field missing | Log WARNING, continue with available fields |
| STL not found | Halt immediately with path error before any subprocess |

---

# DOCUMENT 2: App Flow — Phase 2

## System States

| State | Description |
|---|---|
| S0 | Phase 2 pipeline launched |
| S1 | Startup validation — binaries + STL found |
| S2 | fTetWild meshing in progress |
| S3 | C# .inp serialization |
| S4 | CalculiX solving |
| S5 | ccx2paraview converting |
| S6 | VTU parsing |
| S7 | SUCCESS — results logged |
| S8 | DEGRADED — fallback triggered |
| S9 | HARD FAIL — diagnostic dump written |

## Primary Flow

```
dotnet run --phase 2
        │
        ▼
STARTUP VALIDATION
        │
        ├─ STL exists at D:\pico\output\brake_bracket.stl?
        │       NO → [S9] HALT "STL not found"
        │
        ├─ fTetWild on PATH?
        │       NO → [S9] HALT "fTetWild not found"
        │
        ├─ ccx on PATH?
        │       NO → [S9] HALT "CalculiX not found"
        │
        ├─ ccx2paraview on PATH?
        │       NO → [S9] HALT "ccx2paraview not found"
        │
        └─ Disk space > 2GB?
                NO → [S9] HALT "Insufficient disk"

        ALL PASS → [S1] READY
        │
        ▼
═══════════════════════════
PHASE 2A: MESHING
═══════════════════════════
        │
fTetWild invoked (timeout 300s)
        │
        ├─ ATTEMPT 1: target edge length = 2.0mm
        │       Fails? → increase to 3.0mm, retry once
        │       Still fails? → [S9] HALT
        │
        ├─ Mesh generated → Jacobian check
        │       Negative Jacobians? → [S8] flag, continue
        │
        └─ Output: brake_bracket.mesh
        │
        ▼
═══════════════════════════
PHASE 2B: INP SERIALIZATION
═══════════════════════════
        │
        ├─ Parse .mesh file → nodes + elements
        ├─ Write *NODE block
        ├─ Write *ELEMENT,TYPE=C3D10 block
        ├─ Write *MATERIAL,NAME=AL7075
        ├─ Write *ELASTIC: 71700,0.33
        ├─ Write *BOUNDARY (fixed bolt holes)
        ├─ Write *CLOAD (6000N at axle bore)
        │
        └─ Output: brake_bracket.inp
        │
        ▼
═══════════════════════════
PHASE 2C: FEA SOLVE
═══════════════════════════
        │
ccx invoked (timeout 600s)
        │
        ├─ Stream stdout → log convergence lines
        ├─ Non-zero exit? → [S8] retry with C3D4
        │       Still fails? → [S9] HALT
        │
        └─ Output: brake_bracket.frd
        │
        ▼
═══════════════════════════
PHASE 2D: RESULT CONVERSION
═══════════════════════════
        │
ccx2paraview invoked
        │
        ├─ Fails? → [S9] HALT "VTU conversion failed"
        │
        └─ Output: brake_bracket.vtu
        │
        ▼
═══════════════════════════
PHASE 2E: RESULT PARSING
═══════════════════════════
        │
        ├─ Parse VTU XML (System.Xml.Linq)
        ├─ Extract S_Mises → peak value
        ├─ Extract U_magnitude → max displacement
        ├─ Calculate safety factor = 503 / peak_mises
        │
        └─ [S7] SUCCESS
        │
        ▼
════════════════════════════════════════
RESULT BLOCK LOGGED TO CONSOLE
════════════════════════════════════════
Peak von Mises:   XXX.X MPa
Yield strength:   503.0 MPa
Safety factor:    X.XX
Max displacement: X.XXX mm
Solver status:    CONVERGED
Element type:     C3D10
Duration:         XX min XX sec
════════════════════════════════════════
```

---

# DOCUMENT 3: Design Document — Phase 2

## New Files Added to Project

```
D:\pico\
├── Program.cs                          ← updated for phase 2
├── src\
│   ├── BrakeBracket.cs                ← Phase 1 unchanged
│   └── FEAPipeline\
│       ├── StartupValidator.cs         ← binary + file checks
│       ├── ProcessBridge.cs            ← all subprocess calls
│       ├── MeshParser.cs               ← .mesh → nodes/elements
│       ├── InpSerializer.cs            ← nodes → .inp file
│       ├── VtuResultParser.cs          ← .vtu → SimulationResult
│       └── PipelineOrchestrator.cs     ← runs all steps in order
└── output\
    ├── brake_bracket.stl              ← Phase 1 output
    ├── brake_bracket.mesh             ← fTetWild output
    ├── brake_bracket.inp              ← serialized FEA deck
    ├── brake_bracket.frd              ← CalculiX raw output
    ├── brake_bracket.vtu              ← converted results
    └── diagnostics\                   ← failure dumps
```

## Console Output Format

```
════════════════════════════════════════
  VeloForge FEA Pipeline — Phase 2
════════════════════════════════════════
[INIT] STL found: brake_bracket.stl ✓
[INIT] fTetWild found ✓
[INIT] CalculiX found ✓
[INIT] ccx2paraview found ✓

[MESH] Invoking fTetWild...
[MESH] Edge length: 2.0mm
[MESH] Elements generated: 184,291
[MESH] Jacobian check: PASS ✓
[MESH] Duration: 47.2s

[SOLV] Serializing .inp deck...
[SOLV] Nodes: 28,441
[SOLV] Elements: 184,291
[SOLV] Material: Al 7075-T6
[SOLV] Load: 6000N vertical bump
[SOLV] Invoking CalculiX...
[SOLV] Iteration 1... converging
[SOLV] Iteration 2... converging
[SOLV] CONVERGED ✓
[SOLV] Duration: 3m 12s

[RESL] Converting .frd → .vtu...
[RESL] Parsing VTU fields...
[RESL] S_Mises found ✓
[RESL] U_magnitude found ✓

════════════════════════════════════════
  PIPELINE COMPLETE — FULL RESULT
════════════════════════════════════════
  Peak von Mises:   312.4 MPa
  Yield strength:   503.0 MPa
  Safety factor:    1.61 ✓
  Max displacement: 0.847 mm
  Element type:     C3D10
  Total duration:   4m 07s
════════════════════════════════════════
```

## Updated Program.cs

```csharp
using Leap71.ShapeKernel;
using PicoGK;

string strOutputFolder = @"D:\pico\output";
string strStlPath      = @"D:\pico\output\brake_bracket.stl";

// Phase 1: Geometry (comment out when running Phase 2)
// PicoGK.Library.Go(0.5f, Leap71.VeloForge.BrakeBracket.Task, strOutputFolder);

// Phase 2: FEA Pipeline
var pipeline = new Leap71.VeloForge.FEA.PipelineOrchestrator(strStlPath, strOutputFolder);
await pipeline.RunAsync();
```

---

# DOCUMENT 4: Back-End Document — Phase 2

## Tech Stack Additions

| Component | Tool | Reason |
|---|---|---|
| Volumetric mesher | fTetWild binary | Guaranteed valid tets from imperfect STL |
| FEA solver | CalculiX (ccx) binary | Open source, Abaqus .inp compatible |
| Result converter | ccx2paraview binary | .frd → standard VTU XML |
| Result parser | System.Xml.Linq | Built-in .NET, zero dependencies |
| Subprocess manager | System.Diagnostics.Process | Native .NET, full stdout/stderr capture |

## Core Interfaces

```csharp
// All subprocess calls go through one interface
public interface IProcessBridge
{
    Task<ProcessResult> ExecuteAsync(
        string binaryPath,
        string arguments,
        string workingDirectory,
        int timeoutSeconds,
        CancellationToken ct = default);
}

public readonly record struct ProcessResult(
    int ExitCode,
    string Stdout,
    string Stderr,
    TimeSpan Duration,
    bool TimedOut);
```

## ProcessBridge Implementation

```csharp
public sealed class ProcessBridge : IProcessBridge
{
    public async Task<ProcessResult> ExecuteAsync(
        string binaryPath, string arguments,
        string workingDir, int timeoutSeconds,
        CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName               = binaryPath,
            Arguments              = arguments,
            UseShellExecute        = false, // SECURITY: never true
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            CreateNoWindow         = true,
            WorkingDirectory       = workingDir
        };

        // SECURITY: clear inherited environment
        psi.Environment.Clear();
        psi.Environment["PATH"] = @"C:\Windows\System32";

        var sw = Stopwatch.StartNew();
        using var process = new Process { StartInfo = psi };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        process.OutputDataReceived += (_, e) => {
            if (e.Data != null) {
                stdout.AppendLine(e.Data);
                Console.WriteLine($"[PROC] {e.Data}");
            }
        };
        process.ErrorDataReceived += (_, e) => {
            if (e.Data != null) stderr.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        bool finished = await Task.Run(() =>
            process.WaitForExit(timeoutSeconds * 1000), ct);

        return new ProcessResult(
            finished ? process.ExitCode : -1,
            stdout.ToString(),
            stderr.ToString(),
            sw.Elapsed,
            !finished);
    }
}
```

## InpSerializer — Key Structure

```csharp
public sealed class InpSerializer
{
    public void Serialize(
        List<Node> nodes,
        List<Element> elements,
        string outputPath)
    {
        using var sw = new StreamWriter(outputPath);

        // Nodes
        sw.WriteLine("*NODE");
        foreach (var n in nodes)
            sw.WriteLine($"{n.Id},{n.X:F6},{n.Y:F6},{n.Z:F6}");

        // Elements — C3D10 quadratic tets
        sw.WriteLine("*ELEMENT,TYPE=C3D10,ELSET=BRACKET");
        foreach (var e in elements)
            sw.WriteLine(e.ToInpLine());

        // Material — Al 7075-T6
        sw.WriteLine("*MATERIAL,NAME=AL7075");
        sw.WriteLine("*ELASTIC");
        sw.WriteLine("71700.0,0.33");
        sw.WriteLine("*DENSITY");
        sw.WriteLine("2.81E-3");

        // Section
        sw.WriteLine("*SOLID SECTION,ELSET=BRACKET,MATERIAL=AL7075");

        // Boundary — fixed at bolt hole nodes
        sw.WriteLine("*BOUNDARY");
        sw.WriteLine("BOLT_NODES,1,6,0");

        // Step + Load
        sw.WriteLine("*STEP");
        sw.WriteLine("*STATIC");
        sw.WriteLine("*CLOAD");
        sw.WriteLine("AXLE_NODES,3,-6000.0"); // 6000N in -Z
        sw.WriteLine("*NODE FILE");
        sw.WriteLine("U");
        sw.WriteLine("*EL FILE");
        sw.WriteLine("S");
        sw.WriteLine("*END STEP");
    }
}
```

## VtuResultParser

```csharp
public sealed class VtuResultParser
{
    private const double YieldStrengthMpa = 503.0; // Al 7075-T6

    public SimulationResult Parse(string vtuPath)
    {
        var doc    = XDocument.Load(vtuPath);
        var ns     = doc.Root?.GetDefaultNamespace();

        double peakMises = ExtractPeakScalar(doc, "S_Mises");
        double maxDisp   = ExtractPeakScalar(doc, "U_magnitude");
        double sf        = YieldStrengthMpa / peakMises;

        return new SimulationResult(
            PeakVonMisesMpa:  peakMises,
            MaxDisplacementMm: maxDisp,
            SafetyFactor:     sf,
            Status: sf > 1.5 ? "PASS" : "FAIL — below 1.5 target"
        );
    }

    private double ExtractPeakScalar(XDocument doc, string fieldName)
    {
        // Find DataArray with Name=fieldName
        // Parse space-delimited floats
        // Return maximum value
        var dataArrays = doc.Descendants("DataArray");
        var target = dataArrays
            .FirstOrDefault(e =>
                e.Attribute("Name")?.Value == fieldName);

        if (target == null)
        {
            Console.WriteLine($"[WARN] Field '{fieldName}' not found in VTU");
            return double.NaN;
        }

        return target.Value
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(double.Parse)
            .Max();
    }
}

public readonly record struct SimulationResult(
    double PeakVonMisesMpa,
    double MaxDisplacementMm,
    double SafetyFactor,
    string Status);
```

## Pipeline Orchestrator

```csharp
public sealed class PipelineOrchestrator
{
    private readonly string _stlPath;
    private readonly string _outputDir;
    private readonly IProcessBridge _proc;

    public PipelineOrchestrator(string stlPath, string outputDir)
    {
        _stlPath   = stlPath;
        _outputDir = outputDir;
        _proc      = new ProcessBridge();
    }

    public async Task RunAsync()
    {
        var sw = Stopwatch.StartNew();

        // 1. Validate
        StartupValidator.Validate(_stlPath);

        // 2. Mesh
        Console.WriteLine("[MESH] Invoking fTetWild...");
        var meshResult = await _proc.ExecuteAsync(
            "fTetWild",
            $"-i {_stlPath} -o {_outputDir}\\brake_bracket.mesh",
            _outputDir, 300);
        if (meshResult.ExitCode != 0)
            throw new Exception("fTetWild failed:\n" + meshResult.Stderr);

        // 3. Serialize .inp
        Console.WriteLine("[SOLV] Serializing .inp...");
        var parser     = new MeshParser();
        var (nodes, elements) = parser.Parse(
            $"{_outputDir}\\brake_bracket.mesh");
        var serializer = new InpSerializer();
        serializer.Serialize(nodes, elements,
            $"{_outputDir}\\brake_bracket.inp");

        // 4. Run CalculiX
        Console.WriteLine("[SOLV] Invoking CalculiX...");
        var solveResult = await _proc.ExecuteAsync(
            "ccx",
            $"{_outputDir}\\brake_bracket",
            _outputDir, 600);
        if (solveResult.ExitCode != 0)
            throw new Exception("CalculiX failed:\n" + solveResult.Stderr);

        // 5. Convert .frd → .vtu
        Console.WriteLine("[RESL] Converting .frd → .vtu...");
        await _proc.ExecuteAsync(
            "ccx2paraview",
            $"{_outputDir}\\brake_bracket.frd {_outputDir}\\brake_bracket.vtu",
            _outputDir, 120);

        // 6. Parse results
        Console.WriteLine("[RESL] Parsing VTU...");
        var resultParser = new VtuResultParser();
        var result = resultParser.Parse(
            $"{_outputDir}\\brake_bracket.vtu");

        // 7. Print result block
        PrintResultBlock(result, sw.Elapsed);
    }

    private void PrintResultBlock(SimulationResult r, TimeSpan duration)
    {
        Console.WriteLine("════════════════════════════════════════");
        Console.WriteLine("  PIPELINE COMPLETE");
        Console.WriteLine("════════════════════════════════════════");
        Console.WriteLine($"  Peak von Mises:   {r.PeakVonMisesMpa:F1} MPa");
        Console.WriteLine($"  Yield strength:   503.0 MPa");
        Console.WriteLine($"  Safety factor:    {r.SafetyFactor:F2}");
        Console.WriteLine($"  Max displacement: {r.MaxDisplacementMm:F3} mm");
        Console.WriteLine($"  Status:           {r.Status}");
        Console.WriteLine($"  Duration:         {duration:mm\\:ss}");
        Console.WriteLine("════════════════════════════════════════");
    }
}
```

---

# DOCUMENT 5: Security Architecture — Phase 2

## New Attack Surface vs Phase 1

Phase 2 introduces 3 external subprocess calls and file IO between them. Each is a potential injection or resource exhaustion point.

## Mandatory Security Rules

```
SUBPROCESS — ALL THREE BINARIES
[ ] UseShellExecute = false on every Process call
[ ] Arguments built as strings with NO user input concatenated
[ ] Environment cleared — only safe PATH exposed
[ ] Timeouts enforced: fTetWild=300s, ccx=600s, ccx2paraview=120s
[ ] Non-zero exit codes always caught — never silently ignored
[ ] Stderr always captured — written to diagnostics on failure

PATH SECURITY
[ ] STL path validated before pipeline starts
[ ] All output paths constructed from known base dir only
[ ] No path traversal — resolve to absolute, check against root

RESOURCE LIMITS
[ ] Single pipeline execution at a time — SemaphoreSlim(1,1)
[ ] Disk space check before meshing: 2GB minimum
[ ] fTetWild edge length floor: 0.5mm — prevents trillion-element mesh
[ ] CalculiX node count ceiling: 500,000 nodes max before abort

INPUT FILES
[ ] STL file size cap: 500MB maximum before meshing
[ ] .mesh file size cap: 2GB maximum before serialization
[ ] .frd file size cap: 5GB maximum before conversion
[ ] .vtu field names validated against allowlist before parsing

OWASP ALIGNMENT
[ ] A03 Injection: no shell metacharacters in any path or argument
[ ] A04 Insecure Design: resource ceilings on all file sizes
[ ] A05 Misconfiguration: binary paths from allowlist only
[ ] A09 Logging: no file paths with usernames in shared logs
```

## Startup Validator

```csharp
public static class StartupValidator
{
    public static void Validate(string stlPath)
    {
        // STL exists
        if (!File.Exists(stlPath))
            throw new FileNotFoundException($"STL not found: {stlPath}");

        // STL not too large
        var stlInfo = new FileInfo(stlPath);
        if (stlInfo.Length > 500L * 1024 * 1024)
            throw new InvalidOperationException("STL exceeds 500MB limit");

        // Binaries on PATH from allowlist only
        CheckBinary("fTetWild");
        CheckBinary("ccx");
        CheckBinary("ccx2paraview");

        // Disk space
        var drive = new DriveInfo(Path.GetPathRoot(stlPath)!);
        if (drive.AvailableFreeSpace < 2L * 1024 * 1024 * 1024)
            throw new InvalidOperationException("Less than 2GB free disk space");

        Console.WriteLine("[INIT] All startup checks passed ✓");
    }

    private static void CheckBinary(string name)
    {
        var result = Environment.GetEnvironmentVariable("PATH")!
            .Split(';')
            .Select(dir => Path.Combine(dir, name + ".exe"))
            .Any(File.Exists);

        if (!result)
            throw new FileNotFoundException(
                $"Binary '{name}' not found on PATH. " +
                $"Install and add to PATH before running Phase 2.");
    }
}
```

---

# AI Agent Execution Prompt — Phase 2

Paste this to your AI coding agent to build the FEA pipeline:

```
You are building Phase 2 of VeloForge — a FEA pipeline in C#.

ENVIRONMENT:
- Project: D:\pico\ single VS project
- Input file: D:\pico\output\brake_bracket.stl (already exists)
- Output dir: D:\pico\output\
- Runtime: .NET 9 C#

YOUR TASK: Build these files in src\FEAPipeline\:
1. ProcessBridge.cs    — subprocess wrapper
2. StartupValidator.cs — binary + file checks  
3. MeshParser.cs       — parse fTetWild .mesh output
4. InpSerializer.cs    — write Abaqus .inp file
5. VtuResultParser.cs  — parse .vtu XML for stress
6. PipelineOrchestrator.cs — run all steps in order

PIPELINE SEQUENCE:
STL → fTetWild → .mesh → InpSerializer → .inp
→ ccx → .frd → ccx2paraview → .vtu → parse → console result

CRITICAL RULES:
- UseShellExecute = false on ALL Process calls
- Timeouts: fTetWild=300s, ccx=600s, ccx2paraview=120s
- Capture stdout line by line and log with [PROC] prefix
- Non-zero exit code = throw descriptive exception
- Parse VTU using System.Xml.Linq — no third party libs

MATERIAL: Al 7075-T6
- Young's modulus: 71700 MPa
- Poisson's ratio: 0.33
- Yield strength: 503 MPa (for safety factor calc)

LOAD CASE: Vertical bump
- 6000N in -Z direction
- Applied at node set named AXLE_NODES
- Fixed boundary at node set named BOLT_NODES

SUCCESS = console prints:
Peak von Mises: XXX.X MPa
Safety factor:  X.XX
Status:         PASS or FAIL

Build all 6 files now. Start with ProcessBridge.cs.
```