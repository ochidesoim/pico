namespace Leap71.VeloForge.FEA
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;

    /// <summary>
    /// Extracts peak simulation results from a VTU file produced by ccx2paraview.
    /// The VTU uses VTK binary format with multi-block ZLib compression and Float64 data.
    /// Rather than re-implementing VTK's binary reader in C#, we delegate to pyvista
    /// (which is already available in the pipeline environment) for correct decoding.
    /// </summary>
    public sealed class VtuResultParser
    {
        // Yield strength is read from VeloConfig (defaults to Al 7075-T6: 503 MPa)

        // Locate the Python extractor script next to this assembly.
        private static readonly string ScriptPath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".",
            "extract_vtu_results.py");

        public SimulationResult Parse(string vtuPath)
        {
            Console.WriteLine($"[RESL] Extracting results via pyvista from: {vtuPath}");

            (double peakMises, double maxDisp) = RunPythonExtractor(vtuPath);

            double sf = VeloConfig.YieldStrengthMpa / peakMises;

            Console.WriteLine($"[INFO] Peak von Mises: {peakMises:F4} MPa");
            Console.WriteLine($"[INFO] Max displacement: {maxDisp:F6} mm");

            return new SimulationResult(
                PeakVonMisesMpa:   peakMises,
                MaxDisplacementMm: maxDisp,
                SafetyFactor:      sf,
                Status: sf > 1.5 ? "PASS" : "FAIL — below 1.5 target"
            );
        }

        private static (double mises, double disp) RunPythonExtractor(string vtuPath)
        {
            string scriptPath = ScriptPath;
            if (!File.Exists(scriptPath))
            {
                // Fallback: look relative to working directory
                scriptPath = Path.Combine("src", "FEAPipeline", "extract_vtu_results.py");
            }
            if (!File.Exists(scriptPath))
            {
                Console.WriteLine($"[ERROR] extract_vtu_results.py not found at: {scriptPath}");
                return (double.NaN, double.NaN);
            }

            var psi = new ProcessStartInfo
            {
                FileName               = "python",
                Arguments              = $"\"{scriptPath}\" \"{vtuPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true,
            };

            using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start Python.");
            string stdout = proc.StandardOutput.ReadToEnd();
            string stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            if (proc.ExitCode != 0)
            {
                Console.WriteLine($"[ERROR] Python extractor failed (exit {proc.ExitCode}):\n{stderr}");
                return (double.NaN, double.NaN);
            }

            if (!string.IsNullOrWhiteSpace(stderr))
                Console.WriteLine($"[WARN] Python stderr: {stderr.Trim()}");

            double mises = double.NaN;
            double disp  = double.NaN;

            foreach (string line in stdout.Split('\n'))
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("S_MISES=") &&
                    double.TryParse(trimmed.Substring(8), System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out double m))
                    mises = m;

                if (trimmed.StartsWith("U_MAG=") &&
                    double.TryParse(trimmed.Substring(6), System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out double d))
                    disp = d;
            }

            return (mises, disp);
        }
    }

    public readonly record struct SimulationResult(
        double PeakVonMisesMpa,
        double MaxDisplacementMm,
        double SafetyFactor,
        string Status);
}
