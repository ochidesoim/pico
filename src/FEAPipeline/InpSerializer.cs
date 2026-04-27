namespace Leap71.VeloForge.FEA
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public sealed class InpSerializer
    {
        // ── Brake-bracket geometry constants (must match BrakeBracket.cs) ────
        //
        // All holes are bored through the Y axis.
        // Proximity is measured in the XZ plane:  d = sqrt((x-cx)²+(z-cz)²)
        //
        // FIXED mounting holes  (cx, cz, holeRadius mm)
        //   P1  arm mount           (85,   8.75, 3.5)
        //   P3  bottom-left mount   (-5, -100.0, 4.5)
        //   P4  bottom-right mount  (55,  -65.0, 5.5)
        //   CAL1 caliper bolt       (-15, -35.0, 4.0)
        //   CAL2 caliper bolt       (-5,  -55.0, 4.0)
        //
        // AXLE bore (loaded)
        //   P0  (0, 0, 0)  bore radius 15 mm
        //
        // Band half-width: nodes within [r-BAND, r+BAND] of a bore surface.

        private const double BAND = 2.0;  // ±2 mm tolerance

        private static readonly (double cx, double cz, double r)[] MountHoles =
        {
            ( 85.0,   8.75, 3.5),   // P1  arm mount
            ( -5.0, -100.0, 4.5),   // P3  bottom-left mount
            ( 55.0,  -65.0, 5.5),   // P4  bottom-right mount
            (-15.0,  -35.0, 4.0),   // CAL1 caliper bolt
            ( -5.0,  -55.0, 4.0),   // CAL2 caliper bolt
        };

        private const double AXLE_CX = 0.0;
        private const double AXLE_CZ = 0.0;
        private const double AXLE_R  = 15.0;   // bore radius subtracted in BrakeBracket

        // ── XZ-plane distance helper ──────────────────────────────────────────
        private static double Dxz(Node n, double cx, double cz)
            => Math.Sqrt((n.X - cx) * (n.X - cx) + (n.Z - cz) * (n.Z - cz));

        public void Serialize(
            List<Node> nodes,
            List<Element> elements,
            string outputPath)
        {
            using var sw = new StreamWriter(outputPath);

            // ── 1. Build FIXED node set (all mounting holes) ─────────────────
            var boltSet = new HashSet<int>();
            foreach (var (cx, cz, r) in MountHoles)
            {
                double lo = r - BAND;
                double hi = r + BAND;
                foreach (var n in nodes)
                {
                    double d = Dxz(n, cx, cz);
                    if (d >= lo && d <= hi)
                        boltSet.Add(n.Id);
                }
            }
            var boltNodes = boltSet.ToList();

            // ── 2. Build AXLE (loaded) node set ──────────────────────────────
            double axleLo = AXLE_R - BAND;
            double axleHi = AXLE_R + BAND;
            var axleNodes = nodes
                .Where(n => { double d = Dxz(n, AXLE_CX, AXLE_CZ); return d >= axleLo && d <= axleHi; })
                .Select(n => n.Id)
                .ToList();

            // ── Fallbacks (coarse mesh safety net) ───────────────────────────
            if (boltNodes.Count == 0)
            {
                double minZ = nodes.Min(n => n.Z);
                boltNodes = nodes.Where(n => Math.Abs(n.Z - minZ) <= 1.0).Select(n => n.Id).ToList();
                Console.WriteLine("[WARN] Mounting-hole proximity found 0 nodes — using minZ fallback.");
            }
            if (axleNodes.Count == 0)
            {
                double maxZ = nodes.Max(n => n.Z);
                axleNodes = nodes.Where(n => Math.Abs(n.Z - maxZ) <= 1.0).Select(n => n.Id).ToList();
                Console.WriteLine("[WARN] Axle-bore proximity found 0 nodes — using maxZ fallback.");
            }

            Console.WriteLine($"[INFO] Bolt (fixed) nodes : {boltNodes.Count}");
            Console.WriteLine($"[INFO] Axle (loaded) nodes: {axleNodes.Count}");

            // ── 3. Nodes ──────────────────────────────────────────────────────
            sw.WriteLine("*NODE");
            foreach (var n in nodes)
                sw.WriteLine($"{n.Id},{n.X:F6},{n.Y:F6},{n.Z:F6}");

            // ── 4. Elements — C3D4 linear tets ───────────────────────────────
            sw.WriteLine("*ELEMENT,TYPE=C3D4,ELSET=BRACKET");
            foreach (var e in elements)
                sw.WriteLine(e.ToInpLine());

            // ── 5. Node Sets ──────────────────────────────────────────────────
            sw.WriteLine("*NSET,NSET=BOLT_NODES");
            WriteNodeSet(sw, boltNodes);

            sw.WriteLine("*NSET,NSET=AXLE_NODES");
            WriteNodeSet(sw, axleNodes);

            // ── 6. Material — Al 7075-T6 ──────────────────────────────────────
            sw.WriteLine("*MATERIAL,NAME=AL7075");
            sw.WriteLine("*ELASTIC");
            sw.WriteLine($"{VeloConfig.YoungsModulus:F1},{VeloConfig.PoissonRatio}");
            sw.WriteLine("*DENSITY");
            sw.WriteLine($"{VeloConfig.DensityGPerMm3:E3}");

            // ── 7. Section ────────────────────────────────────────────────────
            sw.WriteLine("*SOLID SECTION,ELSET=BRACKET,MATERIAL=AL7075");

            // ── 8. Boundary — all mounting holes fully fixed (encastre) ───────
            sw.WriteLine("*BOUNDARY");
            sw.WriteLine("BOLT_NODES,1,6,0");

            // ── 9. Step + Load ────────────────────────────────────────────────
            // "Vertical bump load at axle bore nodes" (requirement.md §Phase 4).
            // The road pushes the axle upward → reaction on bracket is +Z (DOF 3).
            sw.WriteLine("*STEP");
            sw.WriteLine("*STATIC");
            sw.WriteLine("*CLOAD");

            double forcePerNode = VeloConfig.LoadN / axleNodes.Count;
            foreach (var id in axleNodes)
                sw.WriteLine($"{id},3,{forcePerNode:F6}");   // +Z = upward bump

            sw.WriteLine("*NODE FILE");
            sw.WriteLine("U");
            sw.WriteLine("*EL FILE");
            sw.WriteLine("S");
            sw.WriteLine("*END STEP");
        }

        private static void WriteNodeSet(StreamWriter sw, List<int> nodeIds)
        {
            for (int i = 0; i < nodeIds.Count; i += 16)
            {
                var chunk = nodeIds.Skip(i).Take(16);
                sw.WriteLine(string.Join(",", chunk));
            }
        }
    }
}
