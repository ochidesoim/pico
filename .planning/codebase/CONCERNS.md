# Known Concerns & Tech Debt

This document surfaces risky patterns, technical debt, security items, and fragile areas inside the codebase.

## Fragile Coupling (Paths & Environments)
- **Hardcoded Absolute Paths**: `PipelineOrchestrator.cs` relies heavily on strict hardcoded pathing (e.g., `@"D:\pico\output\brake_bracket.stl"`, `@"D:\pico\fTetWild\build\Release\FloatTetwild_bin.exe"`). Executing this on a different OS (Linux/Mac) or different drive will cause an immediate crash.
- **Python Execution Assumptions**: The conversion script `python -m ccx2paraview` explicitly assumes that Python 3 is bound to `python` rather than `python3`, and that the user's environment has PIP libraries globally accessible without activating a `.venv`.

## Tooling Caveats (CalculiX)
- `CalculiX` (ccx) routinely exits with successful output logs but sometimes throws an ExitCode != 0 at termination. A patchy logic bypass exists: `if (solveResult.ExitCode != 0 && !File.Exists(FrdFile))`. This is inherently risky and could mask true silent failures.
- `fTetWild` relies on the system PATH to find injected DLLs like `gmp.dll`. This environment management sits entirely outside the C# orchestration logic.

## Resource Intensive
- Node searching algorithms inside `InpSerializer.cs` (e.g., `.Where()`) iterate thousands of float nodes iteratively without parallel processing.
- The pipeline assumes vast amounts of processing time (`WaitForExit(timeoutSeconds * 1000)` sets timeouts up to `600` seconds / 10 minutes) blocking the orchestrator flow.

## Memory & Scale
- `MeshParser.cs` reads all lines of a `.msh` file into memory simultaneously: `string[] lines = File.ReadAllLines(meshPath);`. For extremely large meshes common in computational engineering (millions of elements), this will cause severe `OutOfMemoryException` spikes on .NET. Streamed parsing using `StreamReader` is urgently needed for production maturity.
