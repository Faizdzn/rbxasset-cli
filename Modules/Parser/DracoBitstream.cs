namespace Modules.Parser
{
    public static class DracoBitstream
    {
        public const int METADATA_FLAG_MASK = 32768;

        public const int POINT_CLOUD = 0;
        public const int TRIANGULAR_MESH = 1;

        public const int MESH_SEQUENTIAL_ENCODING = 0;
        public const int MESH_EDGEBREAKER_ENCODING = 1;

        public const int SEQUENTIAL_COMPRESSED_INDICES = 0;
        public const int SEQUENTIAL_UNCOMPRESSED_INDICES = 1;

        public const int SEQUENTIAL_ATTRIBUTE_ENCODER_GENERIC = 0;
        public const int SEQUENTIAL_ATTRIBUTE_ENCODER_INTEGER = 1;
        public const int SEQUENTIAL_ATTRIBUTE_ENCODER_QUANTIZATION = 2;
        public const int SEQUENTIAL_ATTRIBUTE_ENCODER_NORMALS = 3;

        public const int PREDICTION_NONE = -2;
        public const int PREDICTION_DIFFERENCE = 0;
        public const int MESH_PREDICTION_PARALLELOGRAM = 1;
        public const int MESH_PREDICTION_CONSTRAINED_MULTI_PARALLELOGRAM = 4;
        public const int MESH_PREDICTION_TEX_COORDS_PORTABLE = 5;
        public const int MESH_PREDICTION_GEOMETRIC_NORMAL = 6;

        public const int PREDICTION_TRANSFORM_NONE = -1;
        public const int PREDICTION_TRANSFORM_DELTA = 0;
        public const int PREDICTION_TRANSFORM_WRAP = 1;
        public const int PREDICTION_TRANSFORM_NORMAL_OCTAHEDRON_CANONICALIZED = 3;

        public static readonly string?[] DRACO_DATA_TYPES =
        {
            null, "DT_INT8", "DT_UINT8", "DT_INT16", "DT_UINT16",
            "DT_INT32", "DT_UINT32", "DT_INT64", "DT_UINT64",
            "DT_FLOAT32", "DT_FLOAT64", "DT_BOOL"
        };

        public static readonly int[] DRACO_DATA_TYPE_SIZES =
        {
            0, 1, 1, 2, 2, 4, 4, 8, 8, 4, 8, 1
        };

        public static readonly string[] DRACO_ATTR_TYPES =
        {
            "POSITION", "NORMAL", "COLOR", "TEX_COORD", "GENERIC"
        };

        public static DracoParser Parse(ByteReader stream)
        {
            var parser = new DracoParser();
            var header = ParseHeader(stream);

            parser.MajorVersion = header.MajorVersion;
            parser.MinorVersion = header.MinorVersion;
            parser.EncoderType = header.EncoderType;
            parser.EncoderMethod = header.EncoderMethod;
            parser.Flags = header.Flags;

            Console.WriteLine(
                $"DRACO {parser.MajorVersion}.{parser.MinorVersion} | " +
                $"encoderType: {parser.EncoderType}, " +
                $"encoderMethod: {parser.EncoderMethod}, flags: {parser.Flags}");

            if (parser.EncoderType != TRIANGULAR_MESH)
                throw new NotSupportedException("draco encoderType not implemented");

            if ((parser.Flags & METADATA_FLAG_MASK) != 0)
                throw new NotSupportedException("draco flags not implemented");

            DecodeConnectivityData(stream, parser, parser.EncoderMethod);
            DecodeAttributeData(stream, parser, parser.EncoderMethod);
            GenerateSequence(parser, parser.EncoderMethod);
            DecodeAttributes(stream, parser);

            parser.Attributes = parser.Decoders.Count > 0
                ? parser.Decoders[^1].Attributes
                : new List<DracoAttribute>();

            return parser;
        }

        public static DracoHeader ParseHeader(ByteReader stream)
        {
            if (stream.String(5) != "DRACO")
                throw new InvalidDataException("invalid draco bitstream");

            return new DracoHeader
            {
                MajorVersion = stream.UInt8(),
                MinorVersion = stream.UInt8(),
                EncoderType = stream.UInt8(),
                EncoderMethod = stream.UInt8(),
                Flags = stream.UInt16LE()
            };
        }

        public static void DecodeConnectivityData(
            ByteReader stream, DracoParser parser, int encoderMethod)
        {
            if (encoderMethod == MESH_SEQUENTIAL_ENCODING)
            {
                parser.NumFaces = (int)LEB128(stream);
                parser.NumPoints = (int)LEB128(stream);
                parser.ConnectivityMethod = stream.UInt8();

                parser.Faces = new uint[checked(parser.NumFaces * 3)];

                if (parser.ConnectivityMethod == SEQUENTIAL_COMPRESSED_INDICES)
                    throw new NotSupportedException("draco compressed indices not implemented");

                if (parser.ConnectivityMethod != SEQUENTIAL_UNCOMPRESSED_INDICES)
                    throw new InvalidDataException("draco connectivity method not implemented");

                for (int i = 0; i < parser.NumFaces; i++)
                {
                    int p = i * 3;

                    if (parser.NumPoints < 256)
                    {
                        parser.Faces[p] = stream.UInt8();
                        parser.Faces[p + 1] = stream.UInt8();
                        parser.Faces[p + 2] = stream.UInt8();
                    }
                    else if (parser.NumPoints < (1 << 16))
                    {
                        parser.Faces[p] = stream.UInt16LE();
                        parser.Faces[p + 1] = stream.UInt16LE();
                        parser.Faces[p + 2] = stream.UInt16LE();
                    }
                    else if (parser.NumPoints < (1 << 21))
                    {
                        parser.Faces[p] = LEB128(stream);
                        parser.Faces[p + 1] = LEB128(stream);
                        parser.Faces[p + 2] = LEB128(stream);
                    }
                    else
                    {
                        parser.Faces[p] = stream.UInt32LE();
                        parser.Faces[p + 1] = stream.UInt32LE();
                        parser.Faces[p + 2] = stream.UInt32LE();
                    }
                }
            }
            else if (encoderMethod == MESH_EDGEBREAKER_ENCODING)
            {
                throw new NotSupportedException("draco edgebreaker not implemented");
            }
            else
            {
                throw new NotSupportedException("draco encoderMethod not implemented");
            }
        }

        public static void DecodeAttributeData(
            ByteReader stream, DracoParser parser, int encoderMethod)
        {
            int numAttributeDecoders = stream.UInt8();

            for (int i = 0; i < numAttributeDecoders; i++)
            {
                var decoder = new DracoDecoder { Index = i };
                parser.Decoders.Add(decoder);
            }

            if (encoderMethod == MESH_EDGEBREAKER_ENCODING)
            {
                foreach (var decoder in parser.Decoders)
                {
                    decoder.DataId = stream.UInt8();
                    decoder.DecoderType = stream.UInt8();
                    decoder.TraversalMethod = stream.UInt8();
                }
            }

            foreach (var decoder in parser.Decoders)
            {
                int numAttributes = (int)LEB128(stream);

                for (int j = 0; j < numAttributes; j++)
                {
                    decoder.Attributes.Add(new DracoAttribute
                    {
                        AttributeType = stream.UInt8(),
                        DataType = stream.UInt8(),
                        NumComponents = stream.UInt8(),
                        Normalized = stream.UInt8(),
                        UniqueId = (int)LEB128(stream)
                    });
                }

                foreach (var attribute in decoder.Attributes)
                    attribute.DecoderType = stream.UInt8();
            }
        }

        public static void GenerateSequence(DracoParser parser, int encoderMethod)
        {
            if (encoderMethod == MESH_SEQUENTIAL_ENCODING)
            {
                foreach (var decoder in parser.Decoders)
                {
                    decoder.PointIds = new int[parser.NumPoints];

                    for (int i = 0; i < parser.NumPoints; i++)
                        decoder.PointIds[i] = i;
                }
            }
            else if (encoderMethod == MESH_EDGEBREAKER_ENCODING)
            {
                throw new NotSupportedException("draco edgebreaker not implemented");
            }
        }

        public static void DecodeAttributes(ByteReader stream, DracoParser parser)
        {
            parser.Rans = new RansDecoder();
            parser.BitsValue = 0;
            parser.BitsLength = 0;

            foreach (var decoder in parser.Decoders)
            {
                foreach (var attribute in decoder.Attributes)
                {
                    if (attribute.DecoderType == SEQUENTIAL_ATTRIBUTE_ENCODER_GENERIC)
                        DecodeAttributeGeneric(stream, parser, decoder, attribute);
                    else
                        DecodeAttributeCompressed(
                            stream, parser, decoder, attribute, attribute.DecoderType);
                }

                foreach (var attribute in decoder.Attributes)
                {
                    if (attribute.DecoderType == SEQUENTIAL_ATTRIBUTE_ENCODER_QUANTIZATION)
                        DecodeAndTransformAttributeQuantized(stream, parser, decoder, attribute);
                    else if (attribute.DecoderType == SEQUENTIAL_ATTRIBUTE_ENCODER_NORMALS)
                        DecodeAndTransformAttributeNormals(stream, parser, decoder, attribute);
                    else
                        TransformAttributeGeneric(parser, decoder, attribute);
                }
            }
        }

        public static void DecodeAttributeGeneric(
            ByteReader stream, DracoParser parser,
            DracoDecoder decoder, DracoAttribute attribute)
        {
            int numEntries = decoder.PointIds.Length;
            int numComponents = attribute.NumComponents;
            int numValues = checked(numEntries * numComponents);
            int size = DRACO_DATA_TYPE_SIZES[attribute.DataType];

            switch (size)
            {
                case 1:
                {
                    var output = new ulong[numValues];
                    for (int k = 0; k < numValues; k++) output[k] = stream.UInt8();
                    attribute.Output = output;
                    break;
                }
                case 2:
                {
                    var output = new ulong[numValues];
                    for (int k = 0; k < numValues; k++) output[k] = stream.UInt16LE();
                    attribute.Output = output;
                    break;
                }
                case 4:
                {
                    var output = new ulong[numValues];
                    for (int k = 0; k < numValues; k++) output[k] = stream.UInt32LE();
                    attribute.Output = output;
                    break;
                }
                case 8:
                {
                    var output = new ulong[numValues];
                    for (int k = 0; k < numValues; k++) output[k] = stream.UInt64LE();
                    attribute.Output = output;
                    break;
                }
                default:
                    throw new InvalidDataException("invalid draco data type size");
            }
        }

        public static void DecodeAttributeCompressed(
            ByteReader stream, DracoParser parser,
            DracoDecoder decoder, DracoAttribute attribute, int decoderType)
        {
            int predictionScheme = attribute.PredictionScheme = stream.UInt8();
            int predictionTransformType = PREDICTION_TRANSFORM_NONE;

            if (predictionScheme != PREDICTION_NONE)
                predictionTransformType = attribute.PredictionTransformType = stream.Int8();

            int compressed = stream.UInt8();

            int numEntries = decoder.PointIds.Length;
            int numComponents = attribute.NumComponents;

            if (decoderType == SEQUENTIAL_ATTRIBUTE_ENCODER_NORMALS &&
                predictionScheme == PREDICTION_DIFFERENCE)
            {
                numComponents = 2;
            }

            int numValues = checked(numEntries * numComponents);
            var output = new ulong[numValues];
            attribute.Output = output;

            if (compressed > 0)
            {
                DecodeSymbols(stream, parser, numValues, numComponents, output);
            }
            else
            {
                int size = stream.UInt8();

                switch (size)
                {
                    case 1:
                        for (int k = 0; k < numValues; k++) output[k] = stream.UInt8();
                        break;
                    case 2:
                        for (int k = 0; k < numValues; k++) output[k] = stream.UInt16LE();
                        break;
                    case 4:
                        for (int k = 0; k < numValues; k++) output[k] = stream.UInt32LE();
                        break;
                    case 8:
                        for (int k = 0; k < numValues; k++) output[k] = stream.UInt64LE();
                        break;
                    default:
                        throw new InvalidDataException("draco invalid uncompressed size");
                }
            }

            if (numValues > 0 &&
                predictionTransformType != PREDICTION_TRANSFORM_NORMAL_OCTAHEDRON_CANONICALIZED)
            {
                for (int i = 0; i < output.Length; i++)
                    output[i] = ZigZagDecode(output[i]);
            }

            if (predictionScheme != PREDICTION_NONE)
            {
                DecodePredictionData(
                    stream, parser, decoder, attribute,
                    numValues, numComponents,
                    predictionScheme, predictionTransformType);

                if (numValues > 0)
                {
                    ComputeOriginalValues(
                        parser, decoder, attribute,
                        numValues, numComponents,
                        predictionScheme, predictionTransformType,
                        output);
                }
            }
        }

        private static ulong ZigZagDecode(ulong value)
        {
            return (value & 1UL) != 0
                ? unchecked((ulong)(-(long)((value >> 1) + 1)))
                : value >> 1;
        }

        public static void DecodeSymbols(
            ByteReader stream, DracoParser parser,
            int numValues, int numComponents, ulong[] output)
        {
            const int TAGGED_SYMBOLS = 0;
            const int RAW_SYMBOLS = 1;

            int scheme = stream.UInt8();

            if (scheme == TAGGED_SYMBOLS)
            {
                parser.Rans.InitSymbols(stream, 5);

                for (int i = 0; i < numValues; i += numComponents)
                {
                    int numBits = parser.Rans.ReadSymbol();

                    for (int j = 0; j < numComponents; j++)
                        output[i + j] = ReadBits(stream, parser, numBits);
                }

                FlushBits(parser);
            }
            else if (scheme == RAW_SYMBOLS)
            {
                int maxBitLength = stream.UInt8();

                parser.Rans.InitSymbols(stream, maxBitLength);

                for (int i = 0; i < numValues; i++)
                    output[i] = (ulong)parser.Rans.ReadSymbol();
            }
            else
            {
                throw new InvalidDataException("draco invalid symbol scheme");
            }
        }

        public static void DecodePredictionData(
            ByteReader stream, DracoParser parser,
            DracoDecoder decoder, DracoAttribute attribute,
            int numValues, int numComponents,
            int predictionScheme, int predictionTransformType)
        {
            if (predictionScheme == MESH_PREDICTION_CONSTRAINED_MULTI_PARALLELOGRAM ||
                predictionScheme == MESH_PREDICTION_TEX_COORDS_PORTABLE)
            {
                throw new NotSupportedException("draco edgebreaker not implemented");
            }

            if (predictionTransformType == PREDICTION_TRANSFORM_WRAP)
            {
                attribute.WrapMin = stream.Int32LE();
                attribute.WrapMax = stream.Int32LE();
            }
            else if (predictionTransformType ==
                    PREDICTION_TRANSFORM_NORMAL_OCTAHEDRON_CANONICALIZED)
            {
                attribute.OctaMaxQ = stream.Int32LE();
                _ = stream.Int32LE();
            }

            if (predictionScheme == MESH_PREDICTION_GEOMETRIC_NORMAL)
                throw new NotSupportedException("draco edgebreaker not implemented");
        }

        public static void ComputeOriginalValues(
            DracoParser parser, DracoDecoder decoder,
            DracoAttribute attribute,
            int numValues, int numComponents,
            int predictionScheme, int predictionTransformType,
            ulong[] output)
        {
            if (predictionScheme != PREDICTION_DIFFERENCE)
                throw new NotSupportedException("draco prediction scheme not implemented");

            if (predictionTransformType ==
                PREDICTION_TRANSFORM_NORMAL_OCTAHEDRON_CANONICALIZED)
            {
                ComputeOriginalValuesOcta(attribute, numValues, output);
                return;
            }

            if (predictionTransformType == PREDICTION_TRANSFORM_WRAP)
            {
                int wrapMin = attribute.WrapMin;
                int wrapMax = attribute.WrapMax;
                long maxDif = 1L + wrapMax - wrapMin;

                for (int i = 0; i < numComponents; i++)
                {
                    long value = AddAsSigned(output[i], 0);
                    output[i] = unchecked((ulong)WrapValue(value, wrapMin, wrapMax, maxDif));
                }

                for (int i = numComponents; i < numValues; i += numComponents)
                {
                    for (int j = 0; j < numComponents; j++)
                    {
                        long predicted = AddAsSigned(output[i - numComponents + j], 0);
                        long corr = AddAsSigned(output[i + j], 0);

                        long value = Math.Max(wrapMin, Math.Min(wrapMax, predicted)) + corr;
                        if (value > wrapMax) value -= maxDif;
                        else if (value < wrapMin) value += maxDif;

                        output[i + j] = unchecked((ulong)value);
                    }
                }
            }
            else
            {
                for (int i = 0; i < numComponents; i++)
                    output[i] = output[i];

                for (int i = numComponents; i < numValues; i += numComponents)
                {
                    for (int j = 0; j < numComponents; j++)
                    {
                        long predicted = AddAsSigned(output[i - numComponents + j], 0);
                        long corr = AddAsSigned(output[i + j], 0);
                        output[i + j] = unchecked((ulong)(predicted + corr));
                    }
                }
            }
        }

        private static long WrapValue(long value, int min, int max, long maxDif)
        {
            if (value > max) value -= maxDif;
            else if (value < min) value += maxDif;
            return value;
        }

        private static long AddAsSigned(ulong a, ulong b)
        {
            return unchecked((long)(a + b));
        }

        private static void ComputeOriginalValuesOcta(
            DracoAttribute attribute, int numValues, ulong[] output)
        {
            int maxQuantizedValue = (1 << attribute.OctaMaxQ) - 1;
            int maxValue = maxQuantizedValue - 1;
            double centerValue = maxValue / 2.0;

            double[] InvertDiamond(double s, double t)
            {
                double signS = s >= 0 ? 1 : -1;
                double signT = t >= 0 ? 1 : -1;

                if (s == 0) signS = 1;
                if (t == 0) signT = 1;

                double cornerS = signS * centerValue;
                double cornerT = signT * centerValue;

                double us = t + t - cornerT;
                double ut = s + s - cornerS;

                if (signS * signT >= 0)
                {
                    us = -us;
                    ut = -ut;
                }

                return new[]
                {
                    (us + cornerS) / 2,
                    (ut + cornerT) / 2
                };
            }

            int RotationCount(double x, double y)
            {
                if (x == 0)
                {
                    if (y == 0) return 0;
                    return y > 0 ? 3 : 1;
                }

                if (x > 0)
                    return y >= 0 ? 2 : 1;

                return y <= 0 ? 0 : 3;
            }

            double[] Rotate(double x, double y, int count)
            {
                return count switch
                {
                    1 => new[] { y, -x },
                    2 => new[] { -x, -y },
                    3 => new[] { -y, x },
                    _ => new[] { x, y }
                };
            }

            double ModMax(double x)
            {
                if (x > centerValue) return x - maxQuantizedValue;
                if (x < -centerValue) return x + maxQuantizedValue;
                return x;
            }

            for (int i = 0; i < numValues; i += 2)
            {
                double predX = (long)output[i] - centerValue;
                double predY = (long)output[i + 1] - centerValue;

                long corrX = unchecked((long)output[i]);
                long corrY = unchecked((long)output[i + 1]);

                bool isInDiamond = Math.Abs(predX) + Math.Abs(predY) <= centerValue;

                if (!isInDiamond)
                {
                    var p = InvertDiamond(predX, predY);
                    predX = p[0];
                    predY = p[1];
                }

                bool isInBottomLeft =
                    (predX == 0 && predY == 0) ||
                    (predX < 0 && predY <= 0);

                int rotationCount = RotationCount(predX, predY);

                if (!isInBottomLeft)
                {
                    var p = Rotate(predX, predY, rotationCount);
                    predX = p[0];
                    predY = p[1];
                }

                double origX = ModMax(predX + corrX);
                double origY = ModMax(predY + corrY);

                if (!isInBottomLeft)
                {
                    var p = Rotate(origX, origY, (4 - rotationCount) % 4);
                    origX = p[0];
                    origY = p[1];
                }

                if (!isInDiamond)
                {
                    var p = InvertDiamond(origX, origY);
                    origX = p[0];
                    origY = p[1];
                }

                output[i] = unchecked((ulong)(long)(origX + centerValue));
                output[i + 1] = unchecked((ulong)(long)(origY + centerValue));
            }
        }

        public static void DecodeAndTransformAttributeQuantized(
            ByteReader stream, DracoParser parser,
            DracoDecoder decoder, DracoAttribute attribute)
        {
            int numComponents = attribute.NumComponents;
            int numValues = checked(decoder.PointIds.Length * numComponents);
            var input = GetUInt64Output(attribute);
            var output = new double[numValues];

            var minValues = new float[numComponents];

            for (int i = 0; i < numComponents; i++)
                minValues[i] = stream.FloatLE();

            float range = stream.FloatLE();
            int quantizationBits = stream.UInt8();

            int maxQuantizedValue = (1 << quantizationBits) - 1;
            double delta = range / maxQuantizedValue;

            for (int i = 0; i < numValues; i++)
                output[i] = minValues[i % numComponents] + input[i] * delta;

            attribute.Output = output;
        }

        public static void DecodeAndTransformAttributeNormals(
            ByteReader stream, DracoParser parser,
            DracoDecoder decoder, DracoAttribute attribute)
        {
            int numValues = checked(decoder.PointIds.Length * 2);
            var input = GetUInt64Output(attribute);
            var output = new double[checked(decoder.PointIds.Length * 3)];

            int quantizationBits = stream.UInt8();

            int maxValue = (1 << quantizationBits) - 2;
            double dequantizationScale = 2.0 / maxValue;

            int outputIndex = 0;

            for (int i = 0; i < numValues; i += 2)
            {
                double s = input[i];
                double t = input[i + 1];

                double y = s * dequantizationScale - 1;
                double z = t * dequantizationScale - 1;

                double x = 1 - Math.Abs(y) - Math.Abs(z);

                double xOffset = -x;
                if (xOffset < 0) xOffset = 0;

                y += y < 0 ? xOffset : -xOffset;
                z += z < 0 ? xOffset : -xOffset;

                double normSquared = x * x + y * y + z * z;

                if (normSquared < 1e-6)
                {
                    output[outputIndex++] = 0;
                    output[outputIndex++] = 0;
                    output[outputIndex++] = 0;
                }
                else
                {
                    double d = 1.0 / Math.Sqrt(normSquared);

                    output[outputIndex++] = x * d;
                    output[outputIndex++] = y * d;
                    output[outputIndex++] = z * d;
                }
            }

            attribute.Output = output;
        }

        public static void TransformAttributeGeneric(
            DracoParser parser, DracoDecoder decoder, DracoAttribute attribute)
        {
            var output = GetUInt64Output(attribute);

            if (attribute.DataType == 9)
            {
                var result = new float[output.Length];

                for (int i = 0; i < output.Length; i++)
                    result[i] = BitConverter.UInt32BitsToSingle((uint)output[i]);

                attribute.Output = result;
            }
            else if (attribute.DataType == 10)
            {
                var result = new double[output.Length];

                for (int i = 0; i < output.Length; i++)
                    result[i] = BitConverter.UInt64BitsToDouble(output[i]);

                attribute.Output = result;
            }
            else if (attribute.DataType == 11)
            {
                var result = new bool[output.Length];

                for (int i = 0; i < output.Length; i++)
                    result[i] = output[i] != 0;

                attribute.Output = result;
            }
        }

        private static ulong[] GetUInt64Output(DracoAttribute attribute)
        {
            if (attribute.Output is ulong[] values)
                return values;

            throw new InvalidOperationException("Attribute output is not an integer buffer.");
        }

        public static uint LEB128(ByteReader stream)
        {
            uint result = 0;
            int shift = 0;

            while (true)
            {
                byte value = stream.UInt8();

                if (shift >= 32)
                    throw new InvalidDataException("invalid LEB128");

                result |= (uint)(value & 0x7F) << shift;

                if ((value & 0x80) == 0)
                    return result;

                shift += 7;
            }
        }

        public static ulong ReadBits(
            ByteReader stream, DracoParser parser, int n)
        {
            if (n < 0 || n > 32)
                throw new ArgumentOutOfRangeException(nameof(n));

            while (parser.BitsLength < n)
            {
                byte value = stream.UInt8();

                for (int i = 0; i < 8; i++)
                    parser.BitsValue = (parser.BitsValue << 1) |
                                    (uint)((value >> i) & 1);

                parser.BitsLength += 8;
            }

            ulong result = 0;

            for (int bit = 0; bit < n; bit++)
            {
                parser.BitsLength--;
                result |= ((ulong)((parser.BitsValue >> parser.BitsLength) & 1)) << bit;
            }

            return result;
        }

        public static void FlushBits(DracoParser parser)
        {
            parser.BitsValue = 0;
            parser.BitsLength = 0;
        }
    }

    public sealed class DracoHeader
    {
        public int MajorVersion { get; set; }
        public int MinorVersion { get; set; }
        public int EncoderType { get; set; }
        public int EncoderMethod { get; set; }
        public int Flags { get; set; }
    }

    public sealed class DracoParser
    {
        public int MajorVersion { get; set; }
        public int MinorVersion { get; set; }
        public int EncoderType { get; set; }
        public int EncoderMethod { get; set; }
        public int Flags { get; set; }

        public int NumFaces { get; set; }
        public int NumPoints { get; set; }
        public int ConnectivityMethod { get; set; }

        public uint[] Faces { get; set; } = Array.Empty<uint>();
        public List<DracoDecoder> Decoders { get; } = new();

        public RansDecoder Rans { get; set; } = new();

        public uint BitsValue { get; set; }
        public int BitsLength { get; set; }

        public List<DracoAttribute> Attributes { get; set; } = new();
    }

    public sealed class DracoDecoder
    {
        public int Index { get; set; }
        public List<DracoAttribute> Attributes { get; } = new();
        public int[] PointIds { get; set; } = Array.Empty<int>();

        public int DataId { get; set; }
        public int DecoderType { get; set; }
        public int TraversalMethod { get; set; }
    }

    public sealed class DracoAttribute
    {
        public int AttributeType { get; set; }
        public int DataType { get; set; }
        public int NumComponents { get; set; }
        public int Normalized { get; set; }
        public int UniqueId { get; set; }
        public int DecoderType { get; set; }

        public int PredictionScheme { get; set; }
        public int PredictionTransformType { get; set; }

        public int WrapMin { get; set; }
        public int WrapMax { get; set; }
        public int OctaMaxQ { get; set; }

        public Array? Output { get; set; }
    }

    public sealed class RansDecoder
    {
        private sealed class Probability
        {
            public uint Prob;
            public uint CumProb;
        }

        private Probability[] _probabilityTable = Array.Empty<Probability>();
        private int[] _lookupTable = Array.Empty<int>();

        private byte[]? _buffer;
        private int _startIndex;
        private int _offset;
        private uint _state;
        private uint _base;
        private uint _precision;
        private uint _probZero;

        public void DecodeTables(ByteReader stream, uint expectedCumProb)
        {
            uint numSymbols = DracoBitstream.LEB128(stream);

            var probabilityTable = new Probability[numSymbols];
            var lookupTable = new int[expectedCumProb];

            uint cumProb = 0;
            uint actProb = 0;

            for (uint i = 0; i < numSymbols; i++)
            {
                byte data = stream.UInt8();
                int token = data & 3;

                if (token == 3)
                {
                    int offset = data >> 2;

                    for (int j = 0; j < offset + 1; j++)
                    {
                        probabilityTable[i + j] = new Probability
                        {
                            Prob = 0,
                            CumProb = cumProb
                        };
                    }

                    i += (uint)offset;
                }
                else
                {
                    uint prob = (uint)(data >> 2);

                    for (int j = 0; j < token; j++)
                    {
                        byte eb = stream.UInt8();

                        // JS: prob |= eb << (8 * (j + 1) - 2)
                        int shift = 8 * (j + 1) - 2;
                        prob |= (uint)eb << shift;
                    }

                    probabilityTable[i] = new Probability
                    {
                        Prob = prob,
                        CumProb = cumProb
                    };

                    cumProb += prob;

                    for (uint j = actProb; j < cumProb; j++)
                        lookupTable[j] = (int)i;

                    actProb = cumProb;
                }
            }

            if (cumProb != expectedCumProb)
                throw new InvalidDataException(
                    $"something went wrong in symbols: {cumProb}, expected {expectedCumProb}");

            _probabilityTable = probabilityTable;
            _lookupTable = lookupTable;
        }

        private void Start(
            byte[] buffer, int startIndex,
            int offset, uint baseValue, uint precision)
        {
            _buffer = buffer;
            _startIndex = startIndex;
            _base = baseValue;
            _precision = precision;

            int x = buffer[startIndex + offset - 1] >> 6;

            if (x == 0)
            {
                _offset = offset - 1;
                _state = (uint)(buffer[startIndex + offset - 1] & 0x3F);
            }
            else if (x == 1)
            {
                _offset = offset - 2;
                _state = (uint)(
                    (buffer[startIndex + offset - 1] << 8) |
                    buffer[startIndex + offset - 2]) & 0x3FFF;
            }
            else if (x == 2)
            {
                _offset = offset - 3;
                _state = (uint)(
                    (buffer[startIndex + offset - 1] << 16) |
                    (buffer[startIndex + offset - 2] << 8) |
                    buffer[startIndex + offset - 3]) & 0x3FFFFF;
            }
            else
            {
                _offset = offset - 4;
                _state = (uint)(
                    (buffer[startIndex + offset - 1] << 24) |
                    (buffer[startIndex + offset - 2] << 16) |
                    (buffer[startIndex + offset - 3] << 8) |
                    buffer[startIndex + offset - 4]) & 0x3FFFFFFF;
            }

            _state += baseValue;
        }

        public int ReadSymbol()
        {
            if (_buffer == null)
                throw new InvalidOperationException("rANS decoder is not initialized.");

            while (_state < _base && _offset > 0)
                _state = unchecked(
                    (_state << 8) |
                    _buffer[_startIndex + --_offset]);

            uint quo = _state / _precision;
            uint rem = _state % _precision;

            int symbol = _lookupTable[rem];

            Probability p = _probabilityTable[symbol];

            _state = unchecked(quo * p.Prob + rem - p.CumProb);

            return symbol;
        }

        public void InitSymbols(ByteReader stream, int bitLength)
        {
            int precisionBits = (3 * bitLength) / 2;

            if (precisionBits > 20) precisionBits = 20;
            if (precisionBits < 12) precisionBits = 12;

            uint precision = (uint)(1 << precisionBits);
            uint baseValue = precision * 4;

            DecodeTables(stream, precision);

            int dataSize = checked((int)DracoBitstream.LEB128(stream));

            byte[] data = stream.Array(dataSize).ToArray();

            Start(data, 0, dataSize, baseValue, precision);
        }

        public int ReadBit()
        {
            if (_buffer == null)
                throw new InvalidOperationException("rANS decoder is not initialized.");

            if (_state < _base && _offset > 0)
            {
                _state = unchecked(
                    (_state << 8) |
                    _buffer[_startIndex + --_offset]);
            }

            uint quot = _state / _precision;
            uint rem = _state % _precision;

            uint p = _precision - _probZero;
            bool value = rem < p;

            if (value)
                _state = unchecked(quot * p + rem);
            else
                _state = unchecked(_state - quot * p - p);

            return value ? 1 : 0;
        }

        public void InitBits(ByteReader stream)
        {
            _probZero = stream.UInt8();

            int dataSize = checked((int)DracoBitstream.LEB128(stream));

            byte[] data = stream.Array(dataSize).ToArray();

            Start(data, 0, dataSize, 4096, 256);
        }
    }

}