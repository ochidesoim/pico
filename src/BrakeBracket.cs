using Leap71.ShapeKernel;
using PicoGK;
using System.Numerics;

namespace Leap71.VeloForge
{
    /// <summary>
    /// Motorcycle rear brake caliper bracket.
    ///
    /// Coordinate system (LEAP 71 ShapeKernel defaults):
    ///   LocalX = World X  (width axis)
    ///   LocalY = World Y  (depth axis — through-thickness for Y-bore cylinders)
    ///   LocalZ = World Z  (length axis — BaseBox and BaseCylinder grow in +Z)
    ///
    /// BaseBox(frame, fLength, fWidth, fDepth):
    ///   • frame origin = START of the length axis (bottom of box in +Z)
    ///   • To centre a box at point C: frame.Z = C.Z - fLength/2
    ///   • Width  → Local X (centred: ±fWidth/2  about frame.X)
    ///   • Depth  → Local Y (centred: ±fDepth/2  about frame.Y)
    ///
    /// BaseCylinder(frame, fLength, fRadius):
    ///   • Cylinder grows from frame origin in LocalZ direction.
    ///   • To bore through Y: set LocalZ = +Y, frame.Y = C.Y - fLength/2
    ///
    /// Design steps:
    ///   1. Main rectangular body (solid)
    ///   2. Mounting arm (union)
    ///   3. Subtract 2 × caliper bolt holes (through Y)
    ///   4. Subtract axle bore (through Y)
    ///   5. Subtract 2 × arm mounting holes (through Y)
    ///   6. Subtract lightweighting pocket
    /// </summary>
    public class BrakeBracket
    {
        // ── Structural Nodes ──────────────────────────────────────────────────
        static readonly Vector3 P0 = new Vector3(0f, 0f, 0f);       // Top-Left Axle
        static readonly Vector3 P1 = new Vector3(85f, 0f, 8.75f);   // Top-Right Arm
        static readonly Vector3 P2 = new Vector3(-10f, 0f, -45f);   // Mid-Left Lobe
        static readonly Vector3 P3 = new Vector3(-5f, 0f, -100f);   // Bottom-Left Mount
        static readonly Vector3 P4 = new Vector3(55f, 0f, -65f);    // Bottom-Right Mount

        const float BODY_DEP   = 15f;
        const float POCKET_DEP = 20f;

        // ── Entry point (Phase-1 only, uses default constants) ───────────────
        public static void Task()
        {
            try
            {
                Library.Log("BrakeBracket: starting...");
                Voxels vox = BuildGeometry(BODY_DEP, POCKET_DEP);

                Sh.PreviewVoxels(vox, Cp.strBlue);

                System.IO.Directory.CreateDirectory(@"D:\pico\output");
                vox.mshAsMesh().SaveToStlFile(@"D:\pico\output\brake_bracket.stl");
                Library.Log("BrakeBracket: STL saved to D:\\pico\\output\\brake_bracket.stl");
            }
            catch (Exception e)
            {
                Library.Log($"BrakeBracket FAILED: {e.Message}");
                Library.Log(e.StackTrace ?? "");
            }
        }

        /// <summary>
        /// Parametric factory used by the optimisation sweep.
        /// <paramref name="bodyDep"/> drives the main plate Y-thickness.
        /// <paramref name="pocketDep"/> must exceed bodyDep to cut fully through.
        /// </summary>
        public static Voxels BuildGeometry(float bodyDep, float pocketDep)
        {
            return new BrakeBracket().voxConstruct(bodyDep, pocketDep);
        }

        // ── Build geometry (parametric overload) ──────────────────────────────
        /// <param name="bodyDep">Y-thickness of the main plate (mm).</param>
        /// <param name="pocketDep">Y-depth of lightweighting pocket — must be &gt; bodyDep.</param>
        public Voxels voxConstruct(float bodyDep, float pocketDep)
        {
            float cutLen = bodyDep + 6f; // Overcut for subtractions

            Library.Log($"[1/6] Building main nodes (dep={bodyDep}mm)...");
            Voxels vox = voxCylinderY(P0, bodyDep, 22.5f);
            vox = Sh.voxUnion(vox, voxCylinderY(P1, bodyDep, 13.75f));
            vox = Sh.voxUnion(vox, voxCylinderY(P2, bodyDep, 15f));
            vox = Sh.voxUnion(vox, voxCylinderY(P3, bodyDep, 12f));
            vox = Sh.voxUnion(vox, voxCylinderY(P4, bodyDep, 14f));

            Library.Log("[2/6] Connecting structural webs...");
            // Top arm must be flush with top of P0 (Z = 22.5). Using width 27.5 from Z=8.75 achieves this.
            vox = Sh.voxUnion(vox, voxConnection(new Vector3(0f, 0f, 8.75f), P1, 27.5f, bodyDep));
            
            // Left body and lower arm
            vox = Sh.voxUnion(vox, voxConnection(P0, P2, 30f, bodyDep));
            vox = Sh.voxUnion(vox, voxConnection(P2, P3, 20f, bodyDep));
            
            // Right and diagonal webs to enclose the large cutout
            vox = Sh.voxUnion(vox, voxConnection(P2, P4, 22f, bodyDep));
            vox = Sh.voxUnion(vox, voxConnection(P1, P4, 20f, bodyDep));

            Library.Log("[3/6] Subtracting main axle bore...");
            vox = Sh.voxSubtract(vox, voxCylinderY(P0, cutLen, 15f));

            Library.Log("[4/6] Subtracting mounting holes...");
            vox = Sh.voxSubtract(vox, voxCylinderY(P1, cutLen, 3.5f));
            vox = Sh.voxSubtract(vox, voxCylinderY(P3, cutLen, 4.5f));
            vox = Sh.voxSubtract(vox, voxCylinderY(P4, cutLen, 5.5f));

            Library.Log("[5/6] Subtracting caliper mounting holes...");
            vox = Sh.voxSubtract(vox, voxCylinderY(new Vector3(-15f, 0f, -35f), cutLen, 4f));
            vox = Sh.voxSubtract(vox, voxCylinderY(new Vector3(-5f, 0f, -55f), cutLen, 4f));

            Library.Log($"[6/6] Subtracting top arm slot...");
            // Horizontal slot centered in the top arm
            vox = Sh.voxSubtract(vox, voxCapsuleY(new Vector3(35f, 0f, 8.75f), new Vector3(70f, 0f, 8.75f), 5f, cutLen));

            Library.Log("BrakeBracket: geometry complete.");
            return vox;
        }

        // ── Legacy wrapper (uses class-level constants) ────────────────────────
        public Voxels voxConstruct() => voxConstruct(BODY_DEP, POCKET_DEP);

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a BaseBox voxel centred at <paramref name="centre"/>.
        ///
        /// BaseBox grows from frame origin in +LocalZ (= world +Z by default).
        /// To centre on Z: frame.Z = centre.Z − fLength/2.
        /// Width (LocalX) and Depth (LocalY) are already symmetric about the
        /// spine, so X and Y of the frame equal the centre X and Y.
        /// </summary>
        static Voxels voxBox(Vector3 centre, float fLength, float fWidth, float fDepth)
        {
            // Frame at the bottom (–Z) face of the box, centred in X and Y.
            Vector3 vecFrameOrigin = new Vector3(
                centre.X,
                centre.Y,
                centre.Z - fLength / 2f);

            LocalFrame oFrame = new LocalFrame(vecFrameOrigin);
            return new BaseBox(oFrame, fLength, fWidth, fDepth).oConstructVoxels();
        }

        /// <summary>
        /// Creates a BaseCylinder that bores through the Y-axis,
        /// centred at <paramref name="centre"/>.
        ///
        /// To orient along Y: LocalZ = +Y, LocalX = +X (right-hand: LocalY = −Z).
        /// Cylinder grows from frame origin in +LocalZ (= world +Y).
        /// Frame placed at centre.Y − fLength/2 so the bore is centred in Y.
        /// </summary>
        static Voxels voxCylinderY(Vector3 centre, float fLength, float fRadius)
        {
            Vector3 vecFrameOrigin = new Vector3(
                centre.X,
                centre.Y - fLength / 2f,
                centre.Z);

            // LocalZ = +Y  (bore direction)
            // LocalX = +X  (arbitrary orthogonal; right-hand gives LocalY = -Z)
            LocalFrame oFrame = new LocalFrame(
                vecFrameOrigin,
                new Vector3(0f, 1f, 0f),   // LocalZ → +Y
                new Vector3(1f, 0f, 0f));   // LocalX → +X

            return new BaseCylinder(oFrame, fLength, fRadius).oConstructVoxels();
        }

        /// <summary>
        /// Creates a connecting web (BaseBox) between two points.
        /// </summary>
        static Voxels voxConnection(Vector3 p1, Vector3 p2, float width, float depth)
        {
            Vector3 vec = p2 - p1;
            float len = vec.Length();
            if (len < 0.001f) return new Voxels();
            Vector3 localZ = Vector3.Normalize(vec);
            Vector3 localY = Vector3.UnitY;
            Vector3 localX = Vector3.Normalize(Vector3.Cross(localY, localZ));
            LocalFrame frame = new LocalFrame(p1, localZ, localX);
            return new BaseBox(frame, len, width, depth).oConstructVoxels();
        }

        /// <summary>
        /// Creates a capsule subtraction volume along Y.
        /// </summary>
        static Voxels voxCapsuleY(Vector3 p1, Vector3 p2, float radius, float depth)
        {
            Voxels c1 = voxCylinderY(p1, depth, radius);
            Voxels c2 = voxCylinderY(p2, depth, radius);
            Voxels box = voxConnection(p1, p2, radius * 2f, depth);
            return Sh.voxUnion(Sh.voxUnion(c1, c2), box);
        }
    }
}