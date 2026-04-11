# External Integrations

This document tracks all external services, software binaries, and scripts that this codebase interacts with. The project operates entirely locally and does not integrate with remote APIs or cloud databases.

## CLI Integrations (System Subprocesses)

The codebase interfaces with third-party local applications via the `ProcessBridge` wrapper:

### 1. fTetWild Mesher
- **Integration Type**: Headless Command Line Execution.
- **Purpose**: Generates tetrahedral volume meshes from `.stl` input.
- **Interaction Model**: Standard output/error capture via `ProcessBridge.ExecuteAsync`.
- **Artefacts**: Consumes `brake_bracket.stl`, produces `brake_bracket.mesh`.

### 2. CalculiX (`ccx.exe`)
- **Integration Type**: Headless Command Line Execution.
- **Purpose**: Runs static Finite Element Analysis.
- **Interaction Model**: Asynchronous execution via `ProcessBridge`.
- **Artefacts**: Consumes `brake_bracket.inp`, produces `brake_bracket.frd`.

### 3. Python Modules (`ccx2paraview`)
- **Integration Type**: Python script execution (`python -m ccx2paraview`).
- **Purpose**: VTU format conversion.
- **Interaction Model**: Assuming local Python installation with the specific `ccx2paraview` pip module installed globally or within the active environment.
- **Artefacts**: Consumes `brake_bracket.frd`, produces `brake_bracket.vtu`.

## Environment Dependencies
The integration relies on specific system paths being available or hardcoded into the pipeline variables, and relies on the OS path parser. 
