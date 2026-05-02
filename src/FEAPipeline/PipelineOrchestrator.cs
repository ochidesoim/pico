namespace Leap71.VeloForge.FEA
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;

    public sealed class PipelineOrchestrator
    {
        // Tool paths are read from VeloConfig (loaded from VeloConfig.json at startup)

        private readonly string _stlPath;
        private readonly string _outputDir;
        private readonly string _mshFile;
        private readonly string _inpFile;
        private readonly string _frdFile;
        private readonly string _vtuFile;
        private readonly string _baseName;   // e.g. "bracket_dep18"
        private readonly IProcessBridge _proc;

        public PipelineOrchestrator(string stlPath, string outputDir)
        {
            _stlPath   = stlPath;
            _outputDir = outputDir;
            _proc      = new ProcessBridge();

            // Derive all intermediate filenames from the STL path.
            // fTetWild convention: pass full .msh path as --output; it produces that exact file.
            string noExt  = stlPath.Substring(0, stlPath.Length - 4); // strip ".stl"
            _baseName = Path.GetFileNameWithoutExtension(stlPath);
            _mshFile  = noExt + "_.msh";
            _inpFile  = Path.Combine(outputDir, _baseName + ".inp");
            _frdFile  = Path.Combine(outputDir, _baseName + ".frd");
            _vtuFile  = Path.Combine(outputDir, _baseName + ".vtu");
        }

        /// <summary>
        /// Runs the full FEA pipeline and returns the structural simulation result.
        /// Stages: fTetWild mesh → CalculiX solve → ccx2paraview → pyvista parse.
        /// </summary>
        public async Task<SimulationResult> RunAsync()
        {
            var sw = Stopwatch.StartNew();

            // 1. Validate
            StartupValidator.Validate(_stlPath);

            // 2. Mesh with fTetWild (skip if .msh is newer than .stl)
            bool mshExists = File.Exists(_mshFile);
            bool stlNewer  = mshExists &&
                File.GetLastWriteTimeUtc(_stlPath) <= File.GetLastWriteTimeUtc(_mshFile);

            if (stlNewer)
            {
                Console.WriteLine($"[MESH] Skipping fTetWild — mesh up to date: {_mshFile}");
            }
            else
            {
                Console.WriteLine($"[MESH] Running fTetWild → {Path.GetFileName(_mshFile)}");
                var meshResult = await _proc.ExecuteAsync(
                    VeloConfig.FTetWildExe,
                    $"--input \"{_stlPath}\" --output \"{_mshFile}\"",
                    _outputDir, 600);
                if (meshResult.ExitCode != 0 || !File.Exists(_mshFile))
                    throw new Exception("fTetWild failed:\n" + meshResult.Stderr);
                Console.WriteLine("[MESH] Meshing complete.");
            }

            // 3. Serialize .inp
            Console.WriteLine("[SOLV] Serializing .inp...");
            var parser = new MeshParser();
            var (nodes, elements) = parser.Parse(_mshFile);
            var serializer = new InpSerializer();
            serializer.Serialize(nodes, elements, _inpFile);

            // 4. Run CalculiX  (-i takes the base name, CWD = outputDir)
            Console.WriteLine($"[SOLV] Invoking CalculiX (-i {_baseName})...");
            var solveResult = await _proc.ExecuteAsync(
                VeloConfig.CcxExe,
                $"-i {_baseName}",
                _outputDir, 600);

            // CalculiX sometimes exits non-zero even on success; accept if .frd produced.
            if (solveResult.ExitCode != 0 && !File.Exists(_frdFile))
                throw new Exception("CalculiX failed:\n" + solveResult.Stderr);

            // 5. Fix Windows exponent formatting then convert .frd → .vtu
            Console.WriteLine("[RESL] Fixing FRD Windows formatting...");
            var frdText = File.ReadAllText(_frdFile);
            frdText = System.Text.RegularExpressions.Regex.Replace(
                frdText, @"(-\d\.\d{5}E[+-])0(\d\d)", "$1$2");
            File.WriteAllText(_frdFile, frdText);

            Console.WriteLine("[RESL] Converting .frd → .vtk and .vtu...");
            var vtuResult = await _proc.ExecuteAsync(
                "python",
                $"-m ccx2paraview \"{_frdFile}\" vtk vtu",
                _outputDir, 120);

            if (vtuResult.ExitCode != 0)
                throw new Exception(
                    $"ccx2paraview failed (exit {vtuResult.ExitCode}):\n{vtuResult.Stderr}\n" +
                    "Ensure ccx2paraview is installed: pip install ccx2paraview");

            if (!File.Exists(_vtuFile))
                throw new FileNotFoundException(
                    $"ccx2paraview ran but .vtu not found at expected path: {_vtuFile}\n" +
                    "The tool may have written the file with a different name (e.g. <name>0.vtu).");

            // 6. Parse VTU results
            Console.WriteLine("[RESL] Parsing VTU...");
            var resultParser = new VtuResultParser();
            var result = resultParser.Parse(_vtuFile);

            // 7. Print and return
            PrintResultBlock(result, sw.Elapsed);
            return result;
        }

        private void PrintResultBlock(SimulationResult r, TimeSpan duration)
        {
            Console.WriteLine("════════════════════════════════════════");
            Console.WriteLine("  PIPELINE COMPLETE");
            Console.WriteLine("════════════════════════════════════════");
            Console.WriteLine($"  Candidate:        {_baseName}");
            Console.WriteLine($"  Peak von Mises:   {r.PeakVonMisesMpa:F1} MPa");
            Console.WriteLine($"  Yield strength:   503.0 MPa");
            Console.WriteLine($"  Safety factor:    {r.SafetyFactor:F2}");
            Console.WriteLine($"  Max displacement: {r.MaxDisplacementMm:F3} mm");
            Console.WriteLine($"  Status:           {r.Status}");
            Console.WriteLine($"  Duration:         {duration:mm\\:ss}");
            Console.WriteLine("════════════════════════════════════════");
        }
    }
}
