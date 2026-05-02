namespace Leap71.VeloForge.FEA
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

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
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                CreateNoWindow         = true,
                WorkingDirectory       = workingDir
            };

            // Build PATH dynamically from configured tool locations — no hardcoded paths.
            string existingPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            var pathDirs = new List<string>();

            // Add the directory containing ccx.exe so CalculiX DLLs resolve
            if (!string.IsNullOrEmpty(VeloConfig.CcxExe))
            {
                string? ccxDir = Path.GetDirectoryName(VeloConfig.CcxExe);
                if (!string.IsNullOrEmpty(ccxDir)) pathDirs.Add(ccxDir);
            }

            // Add the directory containing fTetWild.exe
            if (!string.IsNullOrEmpty(VeloConfig.FTetWildExe))
            {
                string? ftetDir = Path.GetDirectoryName(VeloConfig.FTetWildExe);
                if (!string.IsNullOrEmpty(ftetDir)) pathDirs.Add(ftetDir);
            }

            // Add Python from registry or common install locations so ccx2paraview resolves
            string? pythonPath = FindPythonDir();
            if (pythonPath != null)
            {
                pathDirs.Add(pythonPath);
                pathDirs.Add(Path.Combine(pythonPath, "Scripts"));
            }

            pathDirs.Add(@"C:\Windows\System32");
            pathDirs.Add(existingPath);
            psi.Environment["PATH"] = string.Join(";", pathDirs);

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

        /// <summary>
        /// Finds the Python installation directory by checking the registry
        /// then falling back to common install locations.
        /// Returns null if Python cannot be found (inherited PATH will be used).
        /// </summary>
        private static string? FindPythonDir()
        {
            // 1. Check registry (works for official python.org installers)
            string[] regRoots = {
                @"SOFTWARE\Python\PythonCore",
                @"SOFTWARE\WOW6432Node\Python\PythonCore"
            };
            foreach (var root in regRoots)
            {
                try
                {
                    using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(root)
                                 ?? Microsoft.Win32.Registry.CurrentUser.OpenSubKey(root);
                    if (key == null) continue;
                    foreach (var ver in key.GetSubKeyNames())
                    {
                        using var installKey = key.OpenSubKey($"{ver}\\InstallPath");
                        var path = installKey?.GetValue("") as string;
                        if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                            return path.TrimEnd('\\');
                    }
                }
                catch { /* registry unavailable — fall through */ }
            }

            // 2. Check common per-user and system install locations
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string[] candidates = {
                Path.Combine(userProfile, @"AppData\Local\Programs\Python\Python313"),
                Path.Combine(userProfile, @"AppData\Local\Programs\Python\Python312"),
                Path.Combine(userProfile, @"AppData\Local\Programs\Python\Python311"),
                Path.Combine(userProfile, @"AppData\Local\Programs\Python\Python310"),
                @"C:\Python313", @"C:\Python312", @"C:\Python311", @"C:\Python310",
            };
            foreach (var path in candidates)
            {
                if (File.Exists(Path.Combine(path, "python.exe")))
                    return path;
            }

            // 3. Fall back — let the inherited PATH handle it
            return null;
        }
    }
}
