# VeloForge Libraries & Dependencies

This document lists all the third-party libraries, packages, and frameworks used across the VeloForge project, separated by their respective ecosystems.

## C# / .NET (Backend & Simulation Orchestrator)
These dependencies are defined in `pico.csproj`.

| Library | Version | Description |
|---|---|---|
| `K4os.Compression.LZ4` | 1.3.8 | LZ4 compression library, used for handling compressed VTU outputs from CalculiX. |
| `MathNet.Numerics` | 5.0.0 | Numerical computing library for C#. |
| `PicoGK` | 1.7.7.5 | Robust voxel-based geometry kernel for generating the 3D parametric models. |
| `.NET SDK` | 9.0 | The target framework for the C# orchestrator. |

## Web Frontend (Next.js Login Page)
These dependencies are defined in `web/package.json`.

| Library | Version | Description |
|---|---|---|
| `next` | 16.2.4 | React framework used for building the static login interface. |
| `react` | 19.2.4 | Core UI library for the frontend. |
| `react-dom` | 19.2.4 | React package for working with the DOM. |
| `gsap` | ^3.15.0 | GreenSock Animation Platform, used for the high-performance micro-animations and page transitions. |
| `@gsap/react` | ^2.1.2 | React integration for GSAP. |

### Frontend Dev Dependencies
| Library | Version | Description |
|---|---|---|
| `typescript` | ^5 | TypeScript language support. |
| `@types/node` | ^20 | TypeScript definitions for Node.js. |
| `@types/react` | ^19 | TypeScript definitions for React. |
| `@types/react-dom` | ^19 | TypeScript definitions for React DOM. |
| `eslint` | ^9 | JavaScript/TypeScript linter. |
| `eslint-config-next` | 16.2.4 | Next.js specific ESLint configuration. |

## Python (GUI, Web Server, Data Extraction)
Python is used for the desktop GUI wrapper (`gui.py`) and for extracting VTU results (`extract_vtu_results.py`).

| Library | Version | Description |
|---|---|---|
| `pyvista` | (latest) | 3D visualization and mesh analysis library. Used in `extract_vtu_results.py` to parse VTU files and extract stress/displacement. |
| `numpy` | (latest) | Fundamental package for scientific computing with Python. Used alongside PyVista. |
| `pyinstaller` | (latest) | Used to package `gui.py` and the `web/out` Next.js static export into a single standalone `VeloForge.exe`. |

*Note: The GUI script (`gui.py`) exclusively uses Python standard libraries (e.g., `tkinter`, `http.server`, `threading`, `subprocess`, `webbrowser`, `json`) to minimize native dependencies.*

## External Executables / Tools
These standalone binaries are invoked by the pipeline.

| Tool | Description |
|---|---|
| `fTetWild` | Converts the generated STL surface meshes into volumetric tetrahedral meshes. |
| `ccx` (CalculiX) | Open-source finite element solver used for structural analysis. |
| `ccx2paraview` | Converts CalculiX raw output (`.frd`) into XML VTU format. |
| `ParaView` | (Optional) Used for manual inspection and visualization of the FEA results. |
