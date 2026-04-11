# Directory Structure

This document outlines the organization pattern of the repository.

## Root Level
- `Program.cs`: Application entry point. Triggers geometry or FEA phases.
- `pico.csproj`: Project configurations and NuGet dependencies.
- `pico.sln`: Visual studio solution wrapper.

## `src/` Directory
Contains all primary commercial logic and execution code.
- `BrakeBracket.cs`: Code that explicitly defines the geometry of the physical component.

## `src/FEAPipeline/` Subsystem
Encapsulates all logic required for the computational engineering analysis loop.
- `PipelineOrchestrator.cs`: Central loop execution and standard output reporter.
- `ProcessBridge.cs`: Process instantiation and `IProcessBridge` contract.
- `StartupValidator.cs`: System integrity checks ensuring files and disk space exist.
- `MeshParser.cs`: Reads ASCII MEDIT formats and maps them into memory arrays.
- `InpSerializer.cs`: Generates the calculix `.inp` load steps and constructs FEA sets.
- `VtuResultParser.cs`: Result extractor.

## Supporting Folders
- `output/`: Dump directory for generated `.stl`, `.msh`, `.inp`, and results `.vtu`. Ignored in Git tracking.
- `fTetWild/`: Cloned sub-repository for the mesher. Built locally via Visual Studio Build Tools.
- `calculix/`: Local binary directory hosting the Windows ccx.exe runtime.
- `LEAP71_ShapeKernel-1.0.0/`: C# toolkit component library integrated locally into the solution.
- `vcpkg/`: C++ package manager repository used to build missing DLLs for `fTetWild`.
