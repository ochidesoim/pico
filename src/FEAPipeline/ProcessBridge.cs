namespace Leap71.VeloForge.FEA
{
    using System;
    using System.Diagnostics;
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

            // Prepend required tool directories to PATH so python, ccx2paraview,
            // and CalculiX resolve correctly without clearing the inherited env
            // (clearing breaks DLL search for native binaries like ccx.exe).
            string existingPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            psi.Environment["PATH"] = string.Join(";",
                @"C:\Users\Dhrumil\AppData\Local\Programs\Python\Python311",
                @"C:\Users\Dhrumil\AppData\Local\Programs\Python\Python311\Scripts",
                @"D:\pico\calculix\CalculiX-2.21.0-win-x64\bin",
                @"C:\Windows\System32",
                existingPath);

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
}
