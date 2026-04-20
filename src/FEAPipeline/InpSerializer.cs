namespace Leap71.VeloForge.FEA
{
    using System.Collections.Generic;
    using System.IO;

    public sealed class InpSerializer
    {
        public void Serialize(
            List<Node> nodes,
            List<Element> elements,
            string outputPath)
        {
            using var sw = new StreamWriter(outputPath);

            // 1. Determine node sets geometrically
            double minZ = nodes.Min(n => n.Z);
            double maxZ = nodes.Max(n => n.Z);
            double tolerance = 1.0;

            var boltNodes = nodes.Where(n => Math.Abs(n.Z - minZ) <= tolerance).Select(n => n.Id).ToList();
            var axleNodes = nodes.Where(n => Math.Abs(n.Z - maxZ) <= tolerance).Select(n => n.Id).ToList();

            // Nodes
            sw.WriteLine("*NODE");
            foreach (var n in nodes)
                sw.WriteLine($"{n.Id},{n.X:F6},{n.Y:F6},{n.Z:F6}");

            // Elements — C3D4 linear tets
            sw.WriteLine("*ELEMENT,TYPE=C3D4,ELSET=BRACKET");
            foreach (var e in elements)
                sw.WriteLine(e.ToInpLine());

            // Node Sets
            sw.WriteLine("*NSET,NSET=BOLT_NODES");
            WriteNodeSet(sw, boltNodes);

            sw.WriteLine("*NSET,NSET=AXLE_NODES");
            WriteNodeSet(sw, axleNodes);

            // Material — Al 7075-T6
            sw.WriteLine("*MATERIAL,NAME=AL7075");
            sw.WriteLine("*ELASTIC");
            sw.WriteLine("71700.0,0.33");
            sw.WriteLine("*DENSITY");
            sw.WriteLine("2.81E-3");

            // Section
            sw.WriteLine("*SOLID SECTION,ELSET=BRACKET,MATERIAL=AL7075");

            // Boundary — fixed at bolt hole nodes
            sw.WriteLine("*BOUNDARY");
            sw.WriteLine("BOLT_NODES,1,6,0");

            // Step + Load
            sw.WriteLine("*STEP");
            sw.WriteLine("*STATIC");
            sw.WriteLine("*CLOAD");
            
            double forcePerNode = 6000.0 / axleNodes.Count;
            foreach(var id in axleNodes)
            {
                sw.WriteLine($"{id},3,-{forcePerNode:F6}");
            }

            sw.WriteLine("*NODE FILE");
            sw.WriteLine("U");
            sw.WriteLine("*EL FILE");
            sw.WriteLine("S");
            sw.WriteLine("*END STEP");
        }

        private void WriteNodeSet(StreamWriter sw, List<int> nodeIds)
        {
            for (int i = 0; i < nodeIds.Count; i += 16)
            {
                var chunk = nodeIds.Skip(i).Take(16);
                sw.WriteLine(string.Join(",", chunk));
            }
        }
    }
}
