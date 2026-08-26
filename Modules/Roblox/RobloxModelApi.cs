using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Commands;
using Modules.Parser;

namespace Modules.Roblox {
    public class RobloxModelApi : RobloxMainApi
    {
        public RobloxModelApi(CommandBase.IKey Key) : base(Key) {}
        
        // api section
        public async Task<JsonObject> RequestAssetApi(long AssetId)
        {
            var Api = $"https://apis.roblox.com/asset-delivery-api/v1/assetId/{AssetId}";
            var Resp = await Http.GetAsync(Api);
            var JsonData = await Resp.Content.ReadFromJsonAsync<JsonObject>();

            return JsonData!;
        }

        // buffers
        public async Task<byte[]> LoadFtsDirect(string FtsUrl)
        {
            var Resp = await Http.GetAsync(FtsUrl);
            var BufferData = await Resp.Content.ReadAsByteArrayAsync();

            return BufferData;
        }

        // api section
        private static float[] ToFloatArray(
            IReadOnlyList<float> values)
        {
            if (values is float[] array)
                return array;

            var result = new float[values.Count];

            for (int i = 0; i < values.Count; i++)
                result[i] = values[i];

            return result;
        }

        private static byte[] ToByteArray(
            IReadOnlyList<float> values)
        {
            var result = new byte[values.Count];

            for (int i = 0; i < values.Count; i++)
                result[i] = checked((byte)values[i]);

            return result;
        }

        private static uint[] ToUIntArray(
            IReadOnlyList<uint> values)
        {
            if (values is uint[] array)
                return array;

            var result = new uint[values.Count];

            for (int i = 0; i < values.Count; i++)
                result[i] = values[i];

            return result;
        }
        public class Bone
        {
            public string Name { get; set; } = string.Empty;
            public Bone? Parent { get; set; }
            public Bone? LodParent { get; set; }
            public float Culling { get; set; }
            public float[] CFrame { get; set; } = new float[12];
        }

        public class MeshData
        {
            public float[] Vertices { get; set; } = Array.Empty<float>();
            public float[] Normals { get; set; } = Array.Empty<float>();
            public float[] Uvs { get; set; } = Array.Empty<float>();
            public uint[] Faces { get; set; } = Array.Empty<uint>();
            public float[] Tangents { get; set; } = Array.Empty<float>();
            public byte[]? VertexColors { get; set; }
            public ushort[]? SkinIndices { get; set; }
            public float[]? SkinWeights { get; set; }
            public uint[] Lods { get; set; } = new uint[] {};
            public Bone[]? Bones { get; set; }
        }
        public async Task<JsonObject> MeshParser(byte[] Buffer)
        {
            var assert = async(bool Logic, string Message) =>
            {
                if(Logic)
                {
                    throw new Exception(Message);
                }
            };
            var ParseText = (string String) =>
            {
                var Lines = Regex.Split(String, @"/\r?\n/");
                assert(Lines.Length == 3, "Invalid mesh version 1 file (Wrong amount of lines)");

                var Version = Lines[0];
                var FaceCount = long.Parse(Lines[1] ?? "0");
                var Data = Lines[2];

                var RegexVector = new Regex(@"/\s+/g");
                var VectorsNotSliced = RegexVector.Replace(Data, "");
                var Vectors = VectorsNotSliced[1..^1].Split("]["); // .slice(1, -1)
                assert(Vectors.Length == FaceCount * 9, "Length Mismatch");

                var ScaleMultiplier = Version == "version 1.00" ? 0.5 : 1;
                var VertexCount = FaceCount * 3;
                var Vertices = new float[VertexCount * 3];
                var Normals = new float[VertexCount * 3];
                var Uvs = new float[VertexCount * 2];
                var Faces = new uint[VertexCount];

                for (int i = 0; i < VertexCount; i++)
                {
                    var n = i * 3;
                    var Vertex = Vectors[n].Split(",");
                    var Normal = Vectors[n + 1].Split(",");
                    var Uv = Vectors[n + 2].Split(",");

                    Vertices[n] = float.Parse(Vertex[0], CultureInfo.InvariantCulture) * (float)ScaleMultiplier;
                    Vertices[n + 1] = float.Parse(Vertex[1], CultureInfo.InvariantCulture) * (float)ScaleMultiplier;
                    Vertices[n + 2] = float.Parse(Vertex[2], CultureInfo.InvariantCulture) * (float)ScaleMultiplier;

                    Normals[n] = float.Parse(Normal[0]);
                    Normals[n + 1] = float.Parse(Normal[1]);
                    Normals[n + 2] = float.Parse(Normal[2]);

                    Uvs[i * 2] = float.Parse(Uv[0]);
                    Uvs[i * 2 + 1] = float.Parse(Uv[1]);
                    Faces[i] = (uint)i;
                }

                return new
                {
                    Vertices,
                    Normals,
                    Uvs,
                    Faces,
                    Lods = new[] {0, FaceCount}
                };
            };
            var ParseBin = (byte[] Buffer, string Version) =>
            {
                var Reader = new ByteReader(Buffer);
                assert(Reader.String(12) == $"version {Version}", "Bad header");

                byte newline = Reader.UInt8();
                assert(newline == 0x0A || (newline == 0x0D && Reader.UInt8() == 0x0A), "Bad newline");

                int begin = Reader.GetIndex();

                int headerSize = 0;
                int vertexSize = 0;
                int faceSize = 12;
                int lodSize = 4;
                int nameTableSize = 0;
                int facsDataSize = 0;

                int lodCount = 0;
                int vertexCount = 0;
                int faceCount = 0;
                int boneCount = 0;
                int subsetCount = 0;

                if (Version == "2.00")
                {
                    headerSize = Reader.UInt16LE();
                    assert(headerSize >= 12, $"Invalid header size {headerSize}");

                    vertexSize = Reader.UInt8();
                    faceSize = Reader.UInt8();
                    vertexCount = (int)Reader.UInt32LE();
                    faceCount = (int)Reader.UInt32LE();
                }
                else if (Version.StartsWith("3."))
                {
                    headerSize = Reader.UInt16LE();
                    assert(headerSize >= 16, $"Invalid header size {headerSize}");

                    vertexSize = Reader.UInt8();
                    faceSize = Reader.UInt8();
                    lodSize = Reader.UInt16LE();
                    lodCount = Reader.UInt16LE();
                    vertexCount = (int)Reader.UInt32LE();
                    faceCount = (int)Reader.UInt32LE();
                }
                else if (Version.StartsWith("4."))
                {
                    headerSize = Reader.UInt16LE();
                    assert(headerSize >= 24, $"Invalid header size {headerSize}");

                    Reader.Jump(2); // uint16 lodType;
                    vertexCount = (int)Reader.UInt32LE();
                    faceCount = (int)Reader.UInt32LE();
                    lodCount = Reader.UInt16LE();
                    boneCount = Reader.UInt16LE();
                    nameTableSize = (int)Reader.UInt32LE();
                    subsetCount = Reader.UInt16LE();
                    Reader.Jump(2); // byte numHighQualityLODs, unused;

                    vertexSize = 40;
                }
                else if (Version.StartsWith("5."))
                {
                    headerSize = Reader.UInt16LE();
                    assert(headerSize >= 32, $"Invalid header size {headerSize}");

                    Reader.Jump(2); // uint16 meshCount;
                    vertexCount = (int)Reader.UInt32LE();
                    faceCount = (int)Reader.UInt32LE();
                    lodCount = Reader.UInt16LE();
                    boneCount = Reader.UInt16LE();
                    nameTableSize = (int)Reader.UInt32LE();
                    subsetCount = Reader.UInt16LE();
                    Reader.Jump(2); // byte numHighQualityLODs, unused;
                    Reader.Jump(4); // uint32 facsDataFormat;
                    facsDataSize = (int)Reader.UInt32LE();

                    vertexSize = 40;
                }

                Reader.SetIndex(begin + headerSize);

                assert(vertexSize >= 36, $"Invalid vertex size {vertexSize}");
                assert(faceSize >= 12, $"Invalid face size {faceSize}");
                assert(lodSize >= 4, $"Invalid lod size {lodSize}");

                int fileEnd = Reader.GetIndex()
                    + (vertexCount * vertexSize)
                    + (boneCount > 0 ? vertexCount * 8 : 0)
                    + (faceCount * faceSize)
                    + (lodCount * lodSize)
                    + (boneCount * 60)
                    + (nameTableSize)
                    + (subsetCount * 72)
                    + (facsDataSize);

                assert(fileEnd == Reader.GetLength(), $"Invalid file size (expected {Reader.GetLength()}, got {fileEnd})");

                uint[] faces = new uint[faceCount * 3];
                float[] vertices = new float[vertexCount * 3];
                float[] normals = new float[vertexCount * 3];
                float[] uvs = new float[vertexCount * 2];
                float[] tangents = new float[vertexCount * 4];
                byte[]? vertexColors = vertexSize >= 40 ? new byte[vertexCount * 4] : null;
                List<int> lods = new List<int>();

                MeshData mesh = new MeshData
                {
                    VertexColors = vertexColors,
                    Vertices = vertices,
                    Tangents = tangents,
                    Normals = normals,
                    Faces = faces,
                    Lods = lods.Select(sel => (uint)sel).ToArray(),
                    Uvs = uvs
                };

                // Vertex[vertexCount]
                for (int i = 0; i < vertexCount; i++)
                {
                    vertices[i * 3]     = Reader.FloatLE();
                    vertices[i * 3 + 1] = Reader.FloatLE();
                    vertices[i * 3 + 2] = Reader.FloatLE();

                    normals[i * 3]     = Reader.FloatLE();
                    normals[i * 3 + 1] = Reader.FloatLE();
                    normals[i * 3 + 2] = Reader.FloatLE();

                    uvs[i * 2]     = Reader.FloatLE();
                    uvs[i * 2 + 1] = 1.0f - Reader.FloatLE();

                    // tangents mapped from [0, 254] to [-1, 1]
                    tangents[i * 4]     = Reader.UInt8() / 127.0f - 1.0f;
                    tangents[i * 4 + 1] = Reader.UInt8() / 127.0f - 1.0f;
                    tangents[i * 4 + 2] = Reader.UInt8() / 127.0f - 1.0f;
                    tangents[i * 4 + 3] = Reader.UInt8() / 127.0f - 1.0f;

                    if (vertexColors != null)
                    {
                        vertexColors[i * 4]     = Reader.UInt8();
                        vertexColors[i * 4 + 1] = Reader.UInt8();
                        vertexColors[i * 4 + 2] = Reader.UInt8();
                        vertexColors[i * 4 + 3] = Reader.UInt8();

                        Reader.Jump(vertexSize - 40);
                    }
                    else
                    {
                        Reader.Jump(vertexSize - 36);
                    }
                }

                // Envelope[vertexCount]
                if (boneCount > 0)
                {
                    mesh.SkinIndices = new ushort[vertexCount * 4];
                    mesh.SkinWeights = new float[vertexCount * 4];

                    for (int i = 0; i < vertexCount; i++)
                    {
                        mesh.SkinIndices[i * 4 + 0] = Reader.UInt8();
                        mesh.SkinIndices[i * 4 + 1] = Reader.UInt8();
                        mesh.SkinIndices[i * 4 + 2] = Reader.UInt8();
                        mesh.SkinIndices[i * 4 + 3] = Reader.UInt8();

                        mesh.SkinWeights[i * 4 + 0] = Reader.UInt8() / 255.0f;
                        mesh.SkinWeights[i * 4 + 1] = Reader.UInt8() / 255.0f;
                        mesh.SkinWeights[i * 4 + 2] = Reader.UInt8() / 255.0f;
                        mesh.SkinWeights[i * 4 + 3] = Reader.UInt8() / 255.0f;
                    }
                }

                // Face[faceCount]
                for (int i = 0; i < faceCount; i++)
                {
                    faces[i * 3]     = Reader.UInt32LE();
                    faces[i * 3 + 1] = Reader.UInt32LE();
                    faces[i * 3 + 2] = Reader.UInt32LE();

                    Reader.Jump(faceSize - 12);
                }

                // LodLevel[lodCount]
                if (lodCount <= 2)
                {
                    lods.Add(0);
                    lods.Add(faceCount);
                    Reader.Jump(lodCount * lodSize);
                }
                else
                {
                    for (int i = 0; i < lodCount; i++)
                    {
                        lods.Add((int)Reader.UInt32LE());
                        Reader.Jump(lodSize - 4);
                    }
                }

                // Bone[boneCount]
                if (boneCount > 0)
                {
                    int nameTableStart = Reader.GetIndex() + boneCount * 60;
                    mesh.Bones = new Bone[boneCount];

                    for (int i = 0; i < boneCount; i++)
                    {
                        Bone bone = new Bone();

                        int nameStart = nameTableStart + (int)Reader.UInt32LE();
                        int nameEnd = Reader.IndexOf(0, nameStart);

                        bone.Name = Encoding.UTF8.GetString(Reader.Subarray(nameStart, nameEnd));
                        
                        ushort parentIndex = Reader.UInt16LE();
                        bone.Parent = parentIndex < mesh.Bones.Length ? mesh.Bones[parentIndex] : null;

                        ushort lodParentIndex = Reader.UInt16LE();
                        bone.LodParent = lodParentIndex < mesh.Bones.Length ? mesh.Bones[lodParentIndex] : null;

                        bone.Culling = Reader.FloatLE();

                        for (int j = 0; j < 9; j++)
                        {
                            bone.CFrame[j + 3] = Reader.FloatLE();
                        }

                        for (int j = 0; j < 3; j++)
                        {
                            bone.CFrame[j] = Reader.FloatLE();
                        }

                        mesh.Bones[i] = bone;
                    }
                }

                // byte[nameTableSize]
                if (nameTableSize > 0)
                {
                    Reader.Jump(nameTableSize);
                }

                // MeshSubset[subsetCount]
                if (subsetCount > 0 && mesh.SkinIndices != null)
                {
                    ushort[] boneIndices = new ushort[26];

                    for (int i = 0; i < subsetCount; i++)
                    {
                        Reader.UInt32LE(); // facesBegin
                        Reader.UInt32LE(); // facesLength
                        uint vertsBegin = Reader.UInt32LE();
                        uint vertsLength = Reader.UInt32LE();
                        Reader.UInt32LE(); // numBoneIndices

                        for (int j = 0; j < 26; j++)
                        {
                            boneIndices[j] = Reader.UInt16LE();
                        }

                        uint vertsEnd = vertsBegin + vertsLength;
                        for (uint j = vertsBegin; j < vertsEnd; j++)
                        {
                            mesh.SkinIndices[j * 4 + 0] = boneIndices[mesh.SkinIndices[j * 4 + 0]];
                            mesh.SkinIndices[j * 4 + 1] = boneIndices[mesh.SkinIndices[j * 4 + 1]];
                            mesh.SkinIndices[j * 4 + 2] = boneIndices[mesh.SkinIndices[j * 4 + 2]];
                            mesh.SkinIndices[j * 4 + 3] = boneIndices[mesh.SkinIndices[j * 4 + 3]];
                        }
                    }
                }

                // byte[facsDataSize]
                if (facsDataSize > 0)
                {
                    Reader.Jump(facsDataSize);
                }

                return mesh;
            };
            var ParseChunk = (byte[] Buffer, string Version) =>
            {
                var Reader = new ByteReader(Buffer);

                string header = Reader.String(12);

                if (header != $"version {Version}")
                    throw new Exception("Bad header");

                byte newline = Reader.UInt8();

                if (newline != 0x0A &&
                    !(newline == 0x0D && Reader.UInt8() == 0x0A))
                {
                    throw new Exception("Bad newline");
                }

                var mesh = new MeshData();

                while (Reader.GetRemaining() >= 16)
                {
                    string chunkType = Reader.String(8);
                    uint chunkVersion = Reader.UInt32LE();
                    uint chunkSize = Reader.UInt32LE();

                    byte[] chunkData = Reader.Array(checked((int)chunkSize));

                    switch (chunkType)
                    {
                        case "COREMESH":
                        {
                            var chunk = new ByteReader(chunkData);

                            switch (chunkVersion)
                            {
                                case 1:
                                {
                                    uint numVertsRaw = chunk.UInt32LE();
                                    int numVerts = checked((int)numVertsRaw);

                                    mesh.Vertices = new float[numVerts * 3];
                                    mesh.Normals = new float[numVerts * 3];
                                    mesh.Uvs = new float[numVerts * 2];
                                    mesh.Tangents = new float[numVerts * 4];
                                    mesh.VertexColors = new byte[numVerts * 4];

                                    for (int i = 0; i < numVerts; i++)
                                    {
                                        // Position
                                        mesh.Vertices[i * 3] = chunk.FloatLE();
                                        mesh.Vertices[i * 3 + 1] = chunk.FloatLE();
                                        mesh.Vertices[i * 3 + 2] = chunk.FloatLE();

                                        // Normal
                                        mesh.Normals[i * 3] = chunk.FloatLE();
                                        mesh.Normals[i * 3 + 1] = chunk.FloatLE();
                                        mesh.Normals[i * 3 + 2] = chunk.FloatLE();

                                        // UV
                                        mesh.Uvs[i * 2] = chunk.FloatLE();
                                        mesh.Uvs[i * 2 + 1] = 1.0f - chunk.FloatLE();

                                        // Tangent
                                        // byte tx, ty, tz, ts
                                        mesh.Tangents[i * 4] =
                                            chunk.UInt8() / 127.0f - 1.0f;

                                        mesh.Tangents[i * 4 + 1] =
                                            chunk.UInt8() / 127.0f - 1.0f;

                                        mesh.Tangents[i * 4 + 2] =
                                            chunk.UInt8() / 127.0f - 1.0f;

                                        mesh.Tangents[i * 4 + 3] =
                                            chunk.UInt8() / 127.0f - 1.0f;

                                        // Color
                                        // byte r, g, b, a
                                        mesh.VertexColors[i * 4] = chunk.UInt8();
                                        mesh.VertexColors[i * 4 + 1] = chunk.UInt8();
                                        mesh.VertexColors[i * 4 + 2] = chunk.UInt8();
                                        mesh.VertexColors[i * 4 + 3] = chunk.UInt8();
                                    }

                                    int numFaces =
                                        checked((int)chunk.UInt32LE());

                                    mesh.Faces = new uint[numFaces * 3];

                                    for (int i = 0; i < numFaces; i++)
                                    {
                                        mesh.Faces[i * 3] = chunk.UInt32LE();
                                        mesh.Faces[i * 3 + 1] = chunk.UInt32LE();
                                        mesh.Faces[i * 3 + 2] = chunk.UInt32LE();
                                    }

                                    if (mesh.Lods == null)
                                    {
                                        mesh.Lods = new uint[]
                                        {
                                            0,
                                            (uint)numFaces
                                        };
                                    }

                                    break;
                                }

                                case 2:
                                {
                                    int bitstreamSize =
                                        checked((int)chunk.UInt32LE());

                                    var stream = new ByteReader(
                                        chunk.Array(bitstreamSize)
                                    );

                                    var data = DracoBitstream.Parse(stream);

                                    if (stream.GetRemaining() != 0)
                                        throw new Exception(
                                            "Draco bitstream has extra data"
                                        );

                                    foreach (var attribute in data.Attributes)
                                    {
                                        switch (attribute.UniqueId)
                                        {
                                            case 0:
                                                // Position
                                                mesh.Vertices =
                                                    ToFloatArray((float[])attribute.Output!);
                                                break;

                                            case 1:
                                                // Normals
                                                mesh.Normals =
                                                    ToFloatArray((float[])attribute.Output!);
                                                break;

                                            case 2:
                                            {
                                                // UVs
                                                mesh.Uvs =
                                                    ToFloatArray((float[])attribute.Output!);

                                                for (int i = 1;
                                                    i < mesh.Uvs.Length;
                                                    i += 2)
                                                {
                                                    mesh.Uvs[i] =
                                                        1.0f - mesh.Uvs[i];
                                                }

                                                break;
                                            }

                                            case 3:
                                            {
                                                // Tangents
                                                mesh.Tangents =
                                                    ToFloatArray((float[])attribute.Output!);

                                                for (int i = 0;
                                                    i < mesh.Tangents.Length;
                                                    i++)
                                                {
                                                    mesh.Tangents[i] =
                                                        mesh.Tangents[i] / 127.0f
                                                        - 1.0f;
                                                }

                                                break;
                                            }

                                            case 4:
                                                // Colors
                                                mesh.VertexColors =
                                                    ToByteArray((float[])attribute.Output!);
                                                break;

                                            default:
                                                Console.WriteLine(
                                                    $"[BTRoblox] Unknown draco attribute {attribute.UniqueId}"
                                                );
                                                break;
                                        }
                                    }

                                    mesh.Faces = ToUIntArray(data.Faces);

                                    if (mesh.Lods == null)
                                    {
                                        mesh.Lods = new uint[]
                                        {
                                            0,
                                            (uint)(mesh.Faces.Length / 3)
                                        };
                                    }

                                    break;
                                }

                                default:
                                    Console.WriteLine(
                                        $"[RBXMeshParser] Unknown COREMESH version {chunkVersion}"
                                    );
                                    break;
                            }

                            if (chunk.GetRemaining() != 0)
                                throw new Exception(
                                    "Chunks Error"
                                );

                            break;
                        }

                        case "LODS\0\0\0\0":
                        {
                            var chunk = new ByteReader(chunkData);

                            switch (chunkVersion)
                            {
                                case 1:
                                {
                                    ushort lodType = chunk.UInt16LE();

                                    byte numHighQualityLODs =
                                        chunk.UInt8();

                                    uint numLodsRaw =
                                        chunk.UInt32LE();

                                    int numLods =
                                        checked((int)numLodsRaw);

                                    if (numLods <= 2)
                                    {
                                        // LOD levels are ignored when there
                                        // aren't at least 3 levels.
                                        chunk.Jump(
                                            checked(numLods * 4)
                                        );
                                    }
                                    else
                                    {
                                        mesh.Lods = new uint[numLods];

                                        for (int i = 0;
                                            i < numLods;
                                            i++)
                                        {
                                            mesh.Lods[i] =
                                                chunk.UInt32LE();
                                        }
                                    }

                                    break;
                                }

                                default:
                                    Console.WriteLine(
                                        $"[RBXMeshParser] Unknown LODS version {chunkVersion}"
                                    );
                                    break;
                            }

                            if (chunk.GetRemaining() != 0)
                                throw new Exception(
                                    "LODS chunk has extra data"
                                );

                            break;
                        }

                        case "SKINNING":
                        {
                            var chunk = new ByteReader(chunkData);

                            switch (chunkVersion)
                            {
                                case 1:
                                {
                                    int numVerts =
                                        checked((int)chunk.UInt32LE());

                                    mesh.SkinIndices =
                                        new ushort[numVerts * 4];

                                    mesh.SkinWeights =
                                        new float[numVerts * 4];

                                    for (int i = 0;
                                        i < numVerts;
                                        i++)
                                    {
                                        mesh.SkinIndices[i * 4] =
                                            chunk.UInt8();

                                        mesh.SkinIndices[i * 4 + 1] =
                                            chunk.UInt8();

                                        mesh.SkinIndices[i * 4 + 2] =
                                            chunk.UInt8();

                                        mesh.SkinIndices[i * 4 + 3] =
                                            chunk.UInt8();

                                        mesh.SkinWeights[i * 4] =
                                            chunk.UInt8() / 255.0f;

                                        mesh.SkinWeights[i * 4 + 1] =
                                            chunk.UInt8() / 255.0f;

                                        mesh.SkinWeights[i * 4 + 2] =
                                            chunk.UInt8() / 255.0f;

                                        mesh.SkinWeights[i * 4 + 3] =
                                            chunk.UInt8() / 255.0f;
                                    }

                                    int numBones =
                                        checked((int)chunk.UInt32LE());

                                    mesh.Bones =
                                        new Bone[numBones];

                                    /*
                                    * JS:
                                    *
                                    * const nameTableOffset =
                                    *     chunk.GetIndex() +
                                    *     numBones * 60 + 4
                                    */
                                    int nameTableOffset =
                                        chunk.GetIndex() +
                                        numBones * 60 +
                                        4;

                                    for (int i = 0;
                                        i < numBones;
                                        i++)
                                    {
                                        var bone = new Bone();

                                        int nameStart =
                                            checked(
                                                nameTableOffset +
                                                (int)chunk.UInt32LE()
                                            );

                                        int nameEnd =
                                            chunk.IndexOf(
                                                0,
                                                nameStart
                                            );

                                        if (nameEnd < 0)
                                        {
                                            throw new Exception(
                                                "Bone name is not null terminated"
                                            );
                                        }

                                        byte[] nameBytes =
                                            chunk.Subarray(
                                                nameStart,
                                                nameEnd
                                            );

                                        bone.Name =
                                            Encoding.UTF8.GetString(
                                                nameBytes
                                            );

                                        ushort parentIndex =
                                            chunk.UInt16LE();

                                        ushort lodParentIndex =
                                            chunk.UInt16LE();

                                        bone.Parent =
                                            parentIndex < mesh.Bones.Length
                                                ? mesh.Bones[parentIndex]
                                                : null;

                                        bone.LodParent =
                                            lodParentIndex < mesh.Bones.Length
                                                ? mesh.Bones[lodParentIndex]
                                                : null;

                                        bone.Culling =
                                            chunk.FloatLE();

                                        bone.CFrame =
                                            new float[12];

                                        // JS:
                                        //
                                        // for(i = 0; i < 9; i++)
                                        //     cframe[i + 3] = FloatLE()

                                        for (int j = 0; j < 9; j++)
                                        {
                                            bone.CFrame[j + 3] =
                                                chunk.FloatLE();
                                        }

                                        // JS:
                                        //
                                        // for(i = 0; i < 3; i++)
                                        //     cframe[i] = FloatLE()

                                        for (int j = 0; j < 3; j++)
                                        {
                                            bone.CFrame[j] =
                                                chunk.FloatLE();
                                        }

                                        mesh.Bones[i] = bone;
                                    }

                                    uint nameTableSize =
                                        chunk.UInt32LE();

                                    chunk.Jump(
                                        checked((int)nameTableSize)
                                    );

                                    int numSubsets =
                                        checked((int)chunk.UInt32LE());

                                    var boneIndices =
                                        new ushort[26];

                                    for (int subset = 0;
                                        subset < numSubsets;
                                        subset++)
                                    {
                                        // facesBegin
                                        chunk.UInt32LE();

                                        // facesLength
                                        chunk.UInt32LE();

                                        uint vertsBeginRaw =
                                            chunk.UInt32LE();

                                        uint vertsLengthRaw =
                                            chunk.UInt32LE();

                                        // numBoneIndices
                                        chunk.UInt32LE();

                                        int vertsBegin =
                                            checked((int)vertsBeginRaw);

                                        int vertsLength =
                                            checked((int)vertsLengthRaw);

                                        for (int j = 0; j < 26; j++)
                                        {
                                            boneIndices[j] =
                                                chunk.UInt16LE();
                                        }

                                        int vertsEnd =
                                            checked(
                                                vertsBegin + vertsLength
                                            );

                                        for (int i = vertsBegin;
                                            i < vertsEnd;
                                            i++)
                                        {
                                            int index = i * 4;

                                            mesh.SkinIndices[index] =
                                                boneIndices[
                                                    mesh.SkinIndices[index]
                                                ];

                                            mesh.SkinIndices[index + 1] =
                                                boneIndices[
                                                    mesh.SkinIndices[index + 1]
                                                ];

                                            mesh.SkinIndices[index + 2] =
                                                boneIndices[
                                                    mesh.SkinIndices[index + 2]
                                                ];

                                            mesh.SkinIndices[index + 3] =
                                                boneIndices[
                                                    mesh.SkinIndices[index + 3]
                                                ];
                                        }
                                    }

                                    break;
                                }

                                default:
                                    Console.WriteLine(
                                        $"[RBXMeshParser] Unknown SKINNING version {chunkVersion}"
                                    );
                                    break;
                            }

                            if (chunk.GetRemaining() != 0)
                                throw new Exception(
                                    "Chunked mesh has extra data"
                                );

                            break;
                        }

                        case "FACS\0\0\0\0":
                        {
                            // Face stuff not supported.
                            break;
                        }

                        case "HSRAVIS\0":
                        {
                            // HSR not supported.
                            break;
                        }

                        default:
                            Console.WriteLine(
                                $"[RBXMeshParser] Unknown chunkType {chunkType}"
                            );
                            break;
                    }
                }

                if (Reader.GetRemaining() != 0)
                    throw new Exception(
                        "Chunked mesh has extra data"
                    );

                return mesh;
            };
            var Parse = (byte[] Buffer) =>
            {
                var Reader = new ByteReader(Buffer);
                assert(Reader.String(8) == "version ", "Invalid Mesh File");

                var Version = Reader.String(4);
                switch(Version)
                {
                    case "1.00":
                    case "1.01":
                        return JsonSerializer.SerializeToNode(ParseText(Encoding.UTF8.GetString(Buffer)));
                    case "2.00":
                    case "3.00":
                    case "3.01":
                    case "4.00":
                    case "4.01":
                    case "5.00":
                        return JsonSerializer.SerializeToNode(ParseBin(Buffer, Version));
                    case "6.00":
                    case "7.00":
                        return JsonSerializer.SerializeToNode(ParseChunk(Buffer, Version));
                    default:
                        throw new Exception($"Unsupported mesh version {Version}");  
                }
            };

            return Parse(Buffer).AsObject();
        }

        // rbxm
        public async Task<string> GetRbxmFile(long ModelId)
        {
            var ReqAsset = await RequestAssetApi(ModelId);
            if(ReqAsset["assetTypeId"]!.GetValue<int>() != 40 && ReqAsset["assetTypeId"]!.GetValue<int>() != 10) {
                throw new Exception("Asset cant be downloaded!");
            }
            var FtsFile = await LoadFtsDirect(ReqAsset["location"]!.GetValue<string>());

            var Base64File = Convert.ToBase64String(FtsFile);
            return $"data:binary/octet-stream;base64,{Base64File}";
        }
        public async Task<string> GetMeshFile(long MeshId)
        {
            var ReqAsset = await RequestAssetApi(MeshId);
            if(ReqAsset["assetTypeId"]!.GetValue<int>() != 4) {
                throw new Exception("Mesh cant be downloaded!");
            }
            var FtsFile = await LoadFtsDirect(ReqAsset["location"]!.GetValue<string>());
            var Mesh = await MeshParser(FtsFile);

            var lines = new List<string>();

            lines.Append("o Mesh");
            for(int i = 0, len = Mesh["Vertices"]!.AsArray().Count(); i < len; i += 3)
            {
              lines.Append($"v {Mesh["Vertices"]![i]} {Mesh["Vertices"]![i + 1]} {Mesh["Vertices"]![i + 2]}");  
            }
            lines.Append("");
            for(int i = 0, len = Mesh["Normals"]!.AsArray().Count(); i < len; i += 3)
            {
              lines.Append($"v {Mesh["Normals"]![i]} {Mesh["Normals"]![i + 1]} {Mesh["Normals"]![i + 2]}");  
            }
            lines.Append("");
            for(int i = 0, len = Mesh["Uvs"]!.AsArray().Count(); i < len; i += 3)
            {
              lines.Append($"v {Mesh["Uvs"]![i]} {Mesh["Uvs"]![i + 1]} {Mesh["Uvs"]![i + 2]}");  
            }
            lines.Append("");

            var LodsStart = Mesh["Lods"]![0]!.GetValue<int>() * 3;
            var LodsEnd = Mesh["Lods"]![1]!.GetValue<int>() * 3;
            var Faces = Mesh["Faces"]!.AsArray().ToArray()[LodsStart..LodsEnd];

            for (int i = 0, len = Faces.Length; i < len; i += 3)
            {
                var A = Faces[i]!.GetValue<int>() + 1;
                var B = Faces[i + 1]!.GetValue<int>() + 1;
                var C = Faces[i + 2]!.GetValue<int>() + 1;
                lines.Append($"f {A}/{A}/{A} {B}/{B}/{B} {C}/{C}/{C}");
            }

            var Buffer = Encoding.UTF8.GetBytes(string.Join("\n"));
            return Convert.ToBase64String(Buffer);
        }
        public async Task<string> GetImageFile(long ImageId)
        {
            var ReqAsset = await RequestAssetApi(ImageId);
            if(ReqAsset["assetTypeId"]!.GetValue<int>() != 1) {
                throw new Exception("Image cant be downloaded!");
            }
            var FtsFile = await LoadFtsDirect(ReqAsset["location"]!.GetValue<string>());

            return Convert.ToBase64String(FtsFile);
        }

        // parse rbxassetid://
        public async Task<string> ParseRbxAssetId(string Url)
        {
            if(!Url.Contains("rbxassetid://") && Url.Length < 14) {
                throw new Exception("Bad Request");
            }
            
            var slicesId = Url.Split("rbxassetid://");
            var assetType = (await RequestAssetApi(int.Parse(slicesId[1])))["assetTypeId"]!.GetValue<int>();
            return assetType == 4 ? $"mesh/{slicesId[1]}" : (assetType == 1 ? $"image/{slicesId[1]}" : "");
        }
    }
}