# VeloForge: Generative Engineering & FEA Pipeline

VeloForge is an end-to-end parametric generative engineering and automated Finite Element Analysis (FEA) pipeline built in C#. It aims to fully automate the design, structural analysis, and optimization of physical parts—demonstrated here by creating a 3D motorcycle parts like brake brackets etc.

## Project Architecture

The pipeline consists of two main phases that are orchestrated together to automatically iterate on part parameters until structural requirements are met.

### Phase 1: Generative Geometry (`BrakeBracket.cs`)
Using the **Leap71 ShapeKernel** and **PicoGK**, VeloForge programmatically builds a 3D parametric model of a brake caliper bracket without any manual CAD software interaction:
* **Generative Construction**: Solid geometry is constructed continuously by defining analytical volumes (boxes, cylinders)—uniting solid body material and removing volumes for the axle space, brake mounting bolts, and lightweighting pockets.
* **Parametric Sweep**: Key dimensions, such as the `bodyDep` (body thickness) and `pocketDep` (lightweighting depth), are variable and easily iterated.
* **Output**: Exports a watertight `.stl` mesh of the part.

### Phase 2: Automated FEA Pipeline (`FEAPipeline/`)
Takes the generated `.stl` and runs a completely headless structural simulation to determine if the part meets a target safety factor (e.g., ≥ 1.5) using **Aluminum 7075-T6**. The pipeline runs through multiple subprocess tools:
1. **Volumetric Meshing**: Runs `fTetWild` to convert the `.stl` shell mesh into a solid 3D tetrahedral `.mesh` file.
2. **Setup Serialization**: A custom C# `InpSerializer` translates the tet-mesh into an Abaqus-format input deck(`.inp`), configuring the C3D10 quadratic elements, Al 7075-T6 material properties, and boundary conditions.
3. **FEA Solver**: Invokes `CalculiX` (`ccx`) to perform structural analysis, simulating a 6000N vertical bump load applied at the axle bore nodes while fixing the bolt holes.
4. **Data Conversion**: `ccx2paraview` translates raw results (`.frd`) into structured XML format (`.vtu`).
5. **Result Parsing**: Extracts the *Peak von Mises Stress* and *Max Displacement* fields from the compressed `.vtu` format to evaluate the part.

## Automated Optimization Loop (`Program.cs`)

VeloForge features an autonomous optimization sweep to "design to constraints". 

The orchestrator loops through various part thicknesses (e.g., 18mm, 20mm, 22mm). For each candidate:
1. It builds the 3D geometry using Phase 1.
2. It streams the geometry directly into the Phase 2 FEA pipeline.
3. Once the analysis solves, it assesses the Safety Factor against the material yield limit (503.0 MPa).
4. **Result:** If the part succeeds (SF ≥ 1.5), the loop halts and saves the viable model (e.g. `brake_bracket_optimized.stl`), achieving a strictly validated design seamlessly.

## Desktop GUI & Login Interface (`gui.py` / `web/`)

VeloForge provides a desktop interface to configure the simulation, visualize progress, and authenticate users.

* **Embedded Login Page (`web/`)**: Built with **Next.js** and **GSAP**, the login page provides a polished, aerospace-themed authentication interface. It is statically exported (`web/out/`) and bundled directly into the executable.
* **Orchestrator Wrapper (`gui.py`)**: A standalone Python `tkinter` GUI that embeds a lightweight HTTP server to serve the login page. Once authenticated, the GUI allows users to configure material parameters, define loads, specify the output directory, and visually track the backend C# optimization sweep in real-time.
* **PyInstaller Packaging**: The entire Python GUI and Next.js static build are packaged into a single standalone executable (`VeloForge.exe`), abstracting away the command line for end users.

## Quick Start
1. Ensure the following binaries are on your system `PATH` (or configured via the GUI):
   - `fTetWild.exe`
   - `ccx.exe` (CalculiX)
   - `ccx2paraview.exe`
2. **Run the Application**:
   - **Using the Executable:** Run the packaged `dist/VeloForge.exe`.
   - **From Source:**
     ```bash
     # Build the frontend static export first
     cd web && npm run build
     cd ..
     # Run the GUI
     python gui.py
     ```
   *(Alternatively, you can run `dotnet run` directly to bypass the GUI and execute the core optimization loop headlessly).*
3. Sign in via the browser interface.
4. Configure your simulation parameters in the GUI and click **Run Simulation**.
5. Monitor the iteration progress and solver convergence over standard output or via the GUI progress bar.
6. Retrieve the optimal `.stl` and generated simulation artifacts from your configured output directory.

## Dependencies

For a complete list of libraries and versions used across the C#, Python, and Next.js environments, please see [`libraries.md`](libraries.md).
