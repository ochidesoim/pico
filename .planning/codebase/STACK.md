# Tech Stack

This document details the technologies, frameworks, and tools used in the project.

## Language & Runtime
- **C#** using the **.NET 9.0** SDK.
- The project is configured as a Console Application (`OutputType=Exe`).
- Implicit Usings and Nullable enablements are active.

## Core Libraries
- **PicoGK (v1.7.7.5)**: A highly specialized voxel-based computation engine for generating and manipulating robust 3D geometry.
- **MathNet.Numerics (v5.0.0)**: Used for mathematical computing and algorithms within the ecosystem.
- **LEAP71 ShapeKernel**: A local dependency used for advanced parametric modeling and shape generation alongside PicoGK.

## External Toolchain & Binaries
The pipeline heavily relies on executing external computational engineering software:
- **fTetWild**: A robust C++ tetrahedral mesher that converts STL geometry into Medit format `.msh` files.
- **CalculiX (ccx v2.21.0)**: An open-source finite element analysis (FEA) solver used to perform static stress analysis.
- **Python / ccx2paraview**: A Python-based script (`ccx2paraview`) used to convert CalculiX `.frd` output files into `.vtu` format for easier programmatic parsing.

## Build System
- Standard MSBuild/dotnet CLI (`dotnet run`, `dotnet build`) with the standard `pico.csproj` file.
- `vcpkg` and CMake are utilized locally for compiling C++ components (like `fTetWild`) from source.
