namespace Leap71.VeloForge.FEA
{
    using System;
    using System.Diagnostics;
    using System.IO;

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

            // Binaries
            if (!File.Exists(@"D:\pico\fTetWild\build\Release\FloatTetwild_bin.exe"))
                throw new FileNotFoundException("fTetWild binary not found.");
                
            if (!File.Exists(@"D:\pico\calculix\CalculiX-2.21.0-win-x64\bin\ccx.exe"))
                throw new FileNotFoundException("ccx.exe binary not found.");

            // ccx2paraview (installed in Python 3.11 Scripts)
            var ccxResult = RunWhere("ccx2paraview");
            if (ccxResult == null)
                throw new FileNotFoundException(
                    "ccx2paraview not found on PATH. Run: pip install ccx2paraview");

            // Disk space
            var drive = new DriveInfo(Path.GetPathRoot(stlPath)!);
            if (drive.AvailableFreeSpace < 2L * 1024 * 1024 * 1024)
                throw new InvalidOperationException("Less than 2GB free disk space");

            Console.WriteLine("[INIT] All startup checks passed ✓");
        }

        /// <summary>Runs where.exe to find an executable on PATH. Returns path or null.</summary>
        private static string? RunWhere(string exe)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName               = "where.exe",
                    Arguments              = exe,
                    UseShellExecute        = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow         = true
                };
                using var p = Process.Start(psi)!;
                string output = p.StandardOutput.ReadLine() ?? string.Empty;
                p.WaitForExit();
                return p.ExitCode == 0 && output.Length > 0 ? output.Trim() : null;
            }
            catch { return null; }
        }
    }
}
