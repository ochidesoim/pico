using System.IO;
using Leap71.ShapeKernel;
using Leap71.VeloForge;
using Leap71.VeloForge.FEA;
using PicoGK;

// ── Thickness optimisation sweep ─────────────────────────────────────────────
// Analytical estimate for bending-dominated bracket:
//   σ ∝ 1/thickness²  →  need thickness = 15 * √(SF_target/SF_actual)
//                    = 15 * √(1.5/1.15) ≈ 17.1 mm
// Sweep: [18, 20, 22] mm.  Stop when SF ≥ 1.5.
// pocketDep = bodyDep + 6 mm always cuts fully through.
// ─────────────────────────────────────────────────────────────────────────────

const string OutputDir = @"D:\pico\output";
float[] candidates     = { 18f, 20f, 22f };

Library.Go(0.3f, () =>
{
    Directory.CreateDirectory(OutputDir);

    SimulationResult? winner   = null;
    float             winDep   = 0f;
    string            winStl   = string.Empty;

    Library.Log("╔══════════════════════════════════════════╗");
    Library.Log("║  BRAKE BRACKET THICKNESS SWEEP           ║");
    Library.Log("║  Target: Safety Factor ≥ 1.5             ║");
    Library.Log("╚══════════════════════════════════════════╝");

    foreach (float dep in candidates)
    {
        float pocketDep = dep + 6f;
        string tag      = $"bracket_dep{(int)dep}";
        string stlPath  = Path.Combine(OutputDir, tag + ".stl");

        Library.Log($"\n── Candidate: BODY_DEP = {dep} mm  (pocket = {pocketDep} mm) ──");

        // ── Phase 1: Build parametric geometry ───────────────────────────────
        Library.Log("Building geometry...");
        Voxels vox = BrakeBracket.BuildGeometry(bodyDep: dep, pocketDep: pocketDep);
        Sh.PreviewVoxels(vox, Cp.strBlue);

        vox.mshAsMesh().SaveToStlFile(stlPath);
        Library.Log($"STL → {tag}.stl  ({new FileInfo(stlPath).Length / 1_000_000.0:F1} MB)");

        // ── Phase 2: FEA pipeline ─────────────────────────────────────────────
        try
        {
            var pipeline = new PipelineOrchestrator(stlPath, OutputDir);
            SimulationResult result = pipeline.RunAsync().GetAwaiter().GetResult();

            if (result.SafetyFactor >= 1.5)
            {
                winner  = result;
                winDep  = dep;
                winStl  = stlPath;
                Library.Log($"✓ SF = {result.SafetyFactor:F2} ≥ 1.5  →  STOPPING SWEEP");
                break;
            }
            else
            {
                Library.Log($"✗ SF = {result.SafetyFactor:F2} < 1.5  →  TRYING NEXT CANDIDATE");
            }
        }
        catch (Exception ex)
        {
            Library.Log($"FEA failed for dep={dep}: {ex.Message}");
        }
    }

    // ── Final report ─────────────────────────────────────────────────────────
    Library.Log("\n╔══════════════════════════════════════════╗");
    if (winner.HasValue)
    {
        Library.Log("║  SWEEP RESULT: PASS                      ║");
        Library.Log("╚══════════════════════════════════════════╝");
        Library.Log($"  Optimal BODY_DEP : {winDep} mm");
        Library.Log($"  Safety Factor    : {winner.Value.SafetyFactor:F2}");
        Library.Log($"  Peak von Mises   : {winner.Value.PeakVonMisesMpa:F1} MPa");
        Library.Log($"  Max displacement : {winner.Value.MaxDisplacementMm:F3} mm");

        // Copy winning STL as the canonical output
        string dest = Path.Combine(OutputDir, "brake_bracket_optimized.stl");
        File.Copy(winStl, dest, overwrite: true);
        Library.Log($"  Saved optimized  → brake_bracket_optimized.stl");
    }
    else
    {
        Library.Log("║  SWEEP RESULT: FAIL — increase range     ║");
        Library.Log("╚══════════════════════════════════════════╝");
        Library.Log("  No candidate achieved SF ≥ 1.5.");
        Library.Log($"  Last candidates tried: {string.Join(", ", candidates)} mm");
    }
    Library.Log("╚══════════════════════════════════════════╝");
});