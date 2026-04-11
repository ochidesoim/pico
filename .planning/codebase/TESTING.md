# Testing

This document explains testing methodologies, frameworks, and continuous validation practices.

## Current Testing State
At this stage in development, the codebase relies purely on **runtime integration testing** and structural validation. 
There is currently **NO traditional unit testing framework** (e.g., xUnit, NUnit, MSTest) implemented in the repository.

## Validation Mechanisms
- **Startup Integrity (`StartupValidator.cs`)**: Checks constraints like disk space limits (2GB free) and maximum input file size (500MB).
- **Process Verification**: `ProcessBridge.cs` rigorously checks external tooling `ExitCode`. A non-zero exit code throws an exception, providing a hard stop to prevent propagating flawed physical data.
- **Result Integrity**: `VtuResultParser.cs` verifies specific `DataArray` attributes (like `S_Mises`) inside the XML tree, gracefully degrading to `double.NaN` rather than failing outright if missing.

## Recommendations for Future Quality Assurance
- Implement isolated unit tests for `MeshParser` and `InpSerializer` by injecting mocked node sets to ensure consistent generation of `.inp` boundary conditions.
- Mock the `IProcessBridge` to test the state machine within `PipelineOrchestrator` without triggering resource-intensive `CalculiX` executions.
- Create automated End-to-End integration test fixtures over controlled geometric primitives.
