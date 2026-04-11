# Coding Conventions

This document outlines the stylistic and structural conventions actively observed in the codebase.

## Formatting & Syntax
- **Namespaces**: The system exclusively uses block-scoped namespaces (e.g., `namespace Leap71.VeloForge.FEA { ... }`).
- **Data Models**: Small struct-like data shapes are declared as `readonly record struct` (example: `ProcessResult`, `SimulationResult`).
- **Encapsulation**: Important system classes are denoted with the `sealed` keyword (e.g., `InpSerializer`, `VtuResultParser`, `PipelineOrchestrator`) minimizing inheritance complexity.
- **Interfaces**: Contracts are prefixed with `I` (e.g., `IProcessBridge`).

## Naming Conventions
- In `BrakeBracket.cs`, there is a strong C++ style Hungarian notation adaptation for members:
  - `m_fPlateX` for a member float.
  - `m_vecPlate` for a member Vector3.
- In `src/FEAPipeline/`, more modern C# conventions are used:
  - `_underscore` for private readonly fields (e.g., `_stlPath`, `_proc`).
  - PascalCase for methods (e.g., `RunAsync`, `Serialize`).
  - Constants use PascalCase inside classes (e.g., `FTetWildExe`).

## Error Handling
- Exceptional states use immediate aggressive throw mechanisms, favoring `InvalidOperationException` and `FileNotFoundException` rather than returning failure types.
- Missing dependencies will explicitly halt pipeline orchestration with detailed terminal prints.

## Execution Patterns
- Subprocesses use `Task.Run` and `Sw.Elapsed` tracking, ensuring async handling of standard output via event streams instead of blocking reads.
- C# `StreamWriter` operations utilize `using var sw = new StreamWriter(...)` to maintain safe resource disposal.
