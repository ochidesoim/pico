# Architecture

This document describes the architectural patterns and internal structures of the application.

## Pattern & Role
The system is built as a **Linear Pipeline Orchestrator** combined with a **Procedural Geometry Engine**. 

### 1. Geometry Generation Phase (Phase 1)
- Implemented primarily inside `BrakeBracket.cs`.
- Follows a constructive solid geometry (CSG) additive-subtractive pattern.
- Generates base volumes (voxels) and subtracts voids.
- Converts voxels into mesh and exports directly to the file system as an `.stl`.

### 2. FEA Pipeline Phase (Phase 2)
- Implemented within the `src/FEAPipeline/` subsystem.
- Modeled as a sequential sequence of tasks using an Orchestrator pattern.
- The pipeline delegates execution to distinct responsibility classes:
  - Validation (`StartupValidator`)
  - Meshing (External execution wrapper)
  - Parsing & Translation (`MeshParser` -> `InpSerializer`)
  - Solving (External execution wrapper)
  - Result parsing (`VtuResultParser`)

## Data Flow
1. **Parametric logic -> Voxels -> STL**: Internal spatial data translates into STL on disk.
2. **STL -> fTetWild -> MSH**: The mesh file represents the discrete physical nodes.
3. **MSH -> InpSerializer -> INP**: C# parses the mesh and geometrically constructs boundary conditions, serializing them into an Abaqus-format `.inp` file.
4. **INP -> CalculiX -> FRD -> VTU**: Hand-offs between external solver and converter.
5. **VTU -> XML Linq -> SimulationResult**: The XML VTU is parsed to extract Peak Von Mises Stress and max displacements, which are translated into a pass/fail `SimulationResult`.

## Key Boundaries
- **IProcessBridge**: Abstraction layer buffering the C# system from direct OS interactions and insulating environment variables for security.
- **System.Xml**: The codebase uses heavy XML DOM loading (`XDocument`) for parsing the result data structures.
