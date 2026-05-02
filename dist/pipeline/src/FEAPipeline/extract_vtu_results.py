"""
extract_vtu_results.py
Reads a VTU file with pyvista and prints peak S_Mises (MPa) and
max displacement magnitude (mm).

Usage: python extract_vtu_results.py <path/to/file.vtu>
"""
import sys
import numpy as np
import pyvista as pv

path = sys.argv[1]
mesh = pv.read(path)

# Von Mises stress
mises_max = float(mesh.point_data['S_Mises'].max())

# Displacement magnitude (U is Nx3)
u = mesh.point_data['U']
u_mag_max = float(np.linalg.norm(u, axis=1).max())

print(f"S_MISES={mises_max:.6g}")
print(f"U_MAG={u_mag_max:.6g}")
