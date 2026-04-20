namespace Leap71.VeloForge.FEA
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    // Types required by InpSerializer to compile correctly
    public class Node
    {
        public int Id { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
    }

    public class Element
    {
        public int Id { get; set; }
        public int[] NodeIds { get; set; } = Array.Empty<int>();

        public string ToInpLine()
        {
            return $"{Id},{string.Join(",", NodeIds)}";
        }
    }

    public class MeshParser
    {
        public (List<Node>, List<Element>) Parse(string meshPath)
        {
            var nodes = new List<Node>();
            var elements = new List<Element>();

            if (!File.Exists(meshPath)) return (nodes, elements);

            using var fs = new FileStream(meshPath, FileMode.Open, FileAccess.Read);
            using var br = new BinaryReader(fs);

            // Read line helper for parsing keywords
            string ReadLine()
            {
                var sb = new StringBuilder();
                while (fs.Position < fs.Length)
                {
                    char c = (char)br.ReadByte();
                    if (c == '\n') break;
                    if (c != '\r') sb.Append(c);
                }
                return sb.ToString();
            }

            string line = ReadLine();
            if (line != "$MeshFormat") throw new Exception("Not a GMSH file");

            string formatInfo = ReadLine();
            var formatParts = formatInfo.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (formatParts.Length < 3) throw new Exception("Invalid GMSH format line");
            int fileType = int.Parse(formatParts[1]); // 0=ascii, 1=binary

            if (fileType == 1)
            {
                br.ReadInt32(); // read the 1 for endianness
            }
            if (ReadLine() != "$EndMeshFormat") throw new Exception("Expected $EndMeshFormat");

            while (fs.Position < fs.Length)
            {
                line = ReadLine();
                if (string.IsNullOrEmpty(line)) continue;

                if (line == "$Nodes")
                {
                    int numNodes = int.Parse(ReadLine());
                    if (fileType == 1)
                    {
                        for (int i = 0; i < numNodes; i++)
                        {
                            nodes.Add(new Node
                            {
                                Id = br.ReadInt32(),
                                X = br.ReadDouble(),
                                Y = br.ReadDouble(),
                                Z = br.ReadDouble()
                            });
                        }
                    }
                    else
                    {
                        for (int i = 0; i < numNodes; i++)
                        {
                            var parts = ReadLine().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            nodes.Add(new Node
                            {
                                Id = int.Parse(parts[0]),
                                X = double.Parse(parts[1]),
                                Y = double.Parse(parts[2]),
                                Z = double.Parse(parts[3])
                            });
                        }
                    }
                    string endNodesLine;
                    do { endNodesLine = ReadLine(); } while (string.IsNullOrWhiteSpace(endNodesLine));
                    if (endNodesLine != "$EndNodes") throw new Exception($"Expected $EndNodes, but got '{endNodesLine}'");
                }
                else if (line == "$Elements")
                {
                    int numElements = int.Parse(ReadLine());
                    int elementsRead = 0;

                    if (fileType == 1)
                    {
                        while (elementsRead < numElements)
                        {
                            int type = br.ReadInt32();
                            int numInBlock = br.ReadInt32();
                            int numTags = br.ReadInt32();

                            int nodesPerElement = GetNumNodes(type);

                            for (int i = 0; i < numInBlock; i++)
                            {
                                int elmId = br.ReadInt32();
                                for (int t = 0; t < numTags; t++) br.ReadInt32(); // skip tags
                                
                                int[] nIds = new int[nodesPerElement];
                                for (int n = 0; n < nodesPerElement; n++) nIds[n] = br.ReadInt32();

                                if (type == 4 || type == 29) 
                                {
                                    elements.Add(new Element { Id = elmId, NodeIds = nIds });
                                }
                                elementsRead++;
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < numElements; i++)
                        {
                            var p = ReadLine().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            int elmId = int.Parse(p[0]);
                            int type = int.Parse(p[1]);
                            int numTags = int.Parse(p[2]);

                            int startNodeIdx = 3 + numTags;
                            int[] nIds = new int[p.Length - startNodeIdx];
                            for(int n = 0; n < nIds.Length; n++) nIds[n] = int.Parse(p[startNodeIdx + n]);

                            if (type == 4 || type == 29)
                            {
                                elements.Add(new Element { Id = elmId, NodeIds = nIds });
                            }
                        }
                    }
                    string endElmLine;
                    do { endElmLine = ReadLine(); } while (string.IsNullOrWhiteSpace(endElmLine));
                    if (endElmLine != "$EndElements") throw new Exception($"Expected $EndElements, but got '{endElmLine}'");
                }
            }

            return (nodes, elements);
        }

        private int GetNumNodes(int elmType)
        {
            return elmType switch
            {
                1 => 2,   // 2-node line
                2 => 3,   // 3-node triangle
                3 => 4,   // 4-node quadrangle
                4 => 4,   // 4-node tetrahedron
                5 => 8,   // 8-node hexahedron
                6 => 6,   // 6-node prism
                7 => 5,   // 5-node pyramid
                8 => 3,   // 3-node second order line
                9 => 6,   // 6-node second order triangle
                11=> 10,  // 10-node second order tetrahedron
                15=> 1,   // 1-node point
                29=> 10,  // 10-node second order tetrahedron
                _ => throw new Exception("Unknown element type " + elmType)
            };
        }
    }
}
