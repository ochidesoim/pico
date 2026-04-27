using System.IO;
using System.Text.Json;
using Leap71.ShapeKernel;
using Leap71.VeloForge;
using Leap71.VeloForge.FEA;
using PicoGK;

// Force stdout to flush every line so the GUI subprocess pipe receives output in real-time
// and set UTF-8 so box-drawing characters (║ ╔ ═) survive the Windows cp1252 code page
Console.OutputEncoding = System.Text.Encoding.UTF8;
var autoFlushWriter = new StreamWriter(Console.OpenStandardOutput(), System.Text.Encoding.UTF8) { AutoFlush = true };
Console.SetOut(autoFlushWriter);

// ── Load config.json if it exists (overrides hardcoded defaults) ──────────────
string configPath = Path.Combine(
    AppDomain.CurrentDomain.BaseDirectory,
    "config.json");

if (File.Exists(configPath))
    VeloConfig.LoadFromJson(configPath);

// ── Thickness optimisation sweep ─────────────────────────────────────────────
// Analytical estimate for bending-dominated bracket:
//   σ ∝ 1/thickness²  →  need thickness = 15 * √(SF_target/SF_actual)
//                    = 15 * √(1.5/1.15) ≈ 17.1 mm
// Sweep: [18, 20, 22] mm.  Stop when SF ≥ SafetyFactor.
// pocketDep = bodyDep + 6 mm always cuts fully through.
// ─────────────────────────────────────────────────────────────────────────────

Library.Go(0.3f, () =>
{
    Directory.CreateDirectory(VeloConfig.OutputDir);

    SimulationResult? winner   = null;
    float             winDep   = 0f;
    string            winStl   = string.Empty;

    void Log(string msg) {
        Console.WriteLine(msg);
        Library.Log(msg);
    }

    Log("╔══════════════════════════════════════════╗");
    Log("║  BRAKE BRACKET THICKNESS SWEEP           ║");
    Log($"║  Target: Safety Factor ≥ {VeloConfig.SafetyFactor:F1}             ║");
    Log("╚══════════════════════════════════════════╝");

    foreach (float dep in VeloConfig.ThicknessCandidates)
    {
        float pocketDep = dep + 6f;
        string tag      = $"bracket_dep{(int)dep}";
        string stlPath  = Path.Combine(VeloConfig.OutputDir, tag + ".stl");

        Log($"\n── Candidate: BODY_DEP = {dep} mm  (pocket = {pocketDep} mm) ──");

        // ── Phase 1: Build parametric geometry ───────────────────────────────
        Log("Building geometry...");
        Voxels vox = BrakeBracket.BuildGeometry(bodyDep: dep, pocketDep: pocketDep);
        Sh.PreviewVoxels(vox, Cp.strBlue);

        vox.mshAsMesh().SaveToStlFile(stlPath);
        Log($"STL → {tag}.stl  ({new FileInfo(stlPath).Length / 1_000_000.0:F1} MB)");

        // ── Phase 2: FEA pipeline ─────────────────────────────────────────────
        try
        {
            var pipeline = new PipelineOrchestrator(stlPath, VeloConfig.OutputDir);
            SimulationResult result = pipeline.RunAsync().GetAwaiter().GetResult();

            if (result.SafetyFactor >= VeloConfig.SafetyFactor)
            {
                winner  = result;
                winDep  = dep;
                winStl  = stlPath;
                Log($"[PASS] ✓ SF = {result.SafetyFactor:F2} ≥ {VeloConfig.SafetyFactor:F1}  →  STOPPING SWEEP");
                break;
            }
            else
            {
                Log($"[FAIL] ✗ SF = {result.SafetyFactor:F2} < {VeloConfig.SafetyFactor:F1}  →  TRYING NEXT CANDIDATE");
            }
        }
        catch (Exception ex)
        {
            Log($"[FAIL] FEA failed for dep={dep}: {ex.Message}");
        }
    }

    // ── Final report ─────────────────────────────────────────────────────────
    Log("\n╔══════════════════════════════════════════╗");
    if (winner.HasValue)
    {
        Log("║  SWEEP RESULT: PASS                      ║");
        Log("╚══════════════════════════════════════════╝");
        Log($"  Optimal BODY_DEP : {winDep} mm");
        Log($"  Safety Factor    : {winner.Value.SafetyFactor:F2}");
        Log($"  Peak von Mises   : {winner.Value.PeakVonMisesMpa:F1} MPa");
        Log($"  Max displacement : {winner.Value.MaxDisplacementMm:F3} mm");

        // Copy winning STL as the canonical output
        string dest = Path.Combine(VeloConfig.OutputDir, "brake_bracket_optimized.stl");
        File.Copy(winStl, dest, overwrite: true);
        Log($"  Saved optimized  → brake_bracket_optimized.stl");
    }
    else
    {
        Log("║  SWEEP RESULT: FAIL — increase range     ║");
        Log("╚══════════════════════════════════════════╝");
        Log("  No candidate achieved SF ≥ 1.5.");
        Log($"  Last candidates tried: {string.Join(", ", VeloConfig.ThicknessCandidates)} mm");
    }
    Log("╚══════════════════════════════════════════╝");
    Environment.Exit(0);
});
