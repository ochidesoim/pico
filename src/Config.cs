namespace Leap71.VeloForge
{
    using System;
    using System.IO;
    using System.Text.Json;

    /// <summary>
    /// Central configuration store for VeloForge.
    /// Defaults match the original hardcoded values.
    /// Call LoadFromJson() before Library.Go() to override from VeloConfig.json.
    /// </summary>
    public static class VeloConfig
    {
        // ── Simulation ────────────────────────────────────────────────────────
        public static float[]  ThicknessCandidates = { 18f, 20f, 22f };
        public static float    SafetyFactor        = 1.5f;
        public static double   LoadN               = 6000.0;
        public static string   OutputDir           = @"D:\pico\output";

        // ── Material (Al 7075-T6) ─────────────────────────────────────────────
        public static double   YoungsModulus       = 71700.0;
        public static double   PoissonRatio        = 0.33;
        public static double   DensityGPerMm3      = 0.00281;
        public static double   YieldStrengthMpa    = 503.0;

        // ── Tool paths ────────────────────────────────────────────────────────
        public static string   FTetWildExe         = @"D:\pico\fTetWild\build\Release\FloatTetwild_bin.exe";
        public static string   CcxExe              = @"D:\pico\calculix\CalculiX-2.21.0-win-x64\bin\ccx.exe";

        /// <summary>
        /// Reads VeloConfig.json and overwrites any matching static fields above.
        /// Missing keys are silently ignored so partial configs are safe.
        /// </summary>
        public static void LoadFromJson(string path)
        {
            try
            {
                string json = File.ReadAllText(path);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // ── simulation ────────────────────────────────────────────────
                if (root.TryGetProperty("simulation", out var sim))
                {
                    if (sim.TryGetProperty("thicknessCandidates", out var tc))
                    {
                        var list = new System.Collections.Generic.List<float>();
                        foreach (var v in tc.EnumerateArray())
                            list.Add(v.GetSingle());
                        if (list.Count > 0)
                            ThicknessCandidates = list.ToArray();
                    }
                    if (sim.TryGetProperty("safetyFactorTarget", out var sf))
                        SafetyFactor = sf.GetSingle();
                    if (sim.TryGetProperty("loadN", out var ln))
                        LoadN = ln.GetDouble();
                    if (sim.TryGetProperty("outputDir", out var od))
                        OutputDir = od.GetString() ?? OutputDir;
                }

                // ── material ─────────────────────────────────────────────────
                if (root.TryGetProperty("material", out var mat))
                {
                    if (mat.TryGetProperty("youngsModulusMpa", out var ym))
                        YoungsModulus = ym.GetDouble();
                    if (mat.TryGetProperty("poissonRatio", out var pr))
                        PoissonRatio = pr.GetDouble();
                    if (mat.TryGetProperty("densityGPerMm3", out var dn))
                        DensityGPerMm3 = dn.GetDouble();
                    if (mat.TryGetProperty("yieldStrengthMpa", out var ys))
                        YieldStrengthMpa = ys.GetDouble();
                }

                // ── tools ─────────────────────────────────────────────────────
                if (root.TryGetProperty("tools", out var tools))
                {
                    if (tools.TryGetProperty("fTetWildExe", out var fw))
                        FTetWildExe = fw.GetString() ?? FTetWildExe;
                    if (tools.TryGetProperty("ccxExe", out var cx))
                        CcxExe = cx.GetString() ?? CcxExe;
                    if (tools.TryGetProperty("outputDir", out var od2))
                        OutputDir = od2.GetString() ?? OutputDir;
                }

                Console.WriteLine("[CFG] Loaded VeloConfig.json successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CFG] Warning: could not parse VeloConfig.json — using defaults. ({ex.Message})");
            }
        }
    }
}
