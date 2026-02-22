using Binary_Stream;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing.Processors.Quantization;
using System.Numerics;

namespace NinTextures
{
    public class CMPR : ITexture
    {
        public static int BlockSize => 0x20;

        public static int BlockWidth => 0x8;

        public static int BlockHeight => 0x8;

        public static void DecodeBlock(BinaryStream reader, Image<Rgba32> image, int startX, int startY)
        {
            long startPos = reader.Position;

            if (reader.Length - startPos < BlockSize)
                throw new InvalidOperationException($"Not enough data to decode a 8x8 CMPR block at stream position {startPos}.");

            int endX = startX + BlockWidth;
            int endY = startY + BlockHeight;

            for (int y = startY; y < endY; y += 4)
            {
                for (int x = startX; x < endX; x += 4)
                {
                    DecodeSubBlock(reader, image, x, y);
                }
            }
        }

        private static void DecodeSubBlock(BinaryStream reader, Image<Rgba32> image, int startX, int startY)
        {
            long startPos = reader.Position;
            if (reader.Length - startPos < 8)
                throw new InvalidOperationException($"Not enough data to decode a 4x4 CMPR sub-block at stream position {startPos}.");
            var data1 = reader.ReadUInt16();
            var data2 = reader.ReadUInt16();
            var color1 = RGB565.DecodePixel(data1);
            var color2 = RGB565.DecodePixel(data2);
            Rgba32 color3;
            Rgba32 color4;
            if (data1 > data2)
            {
                color3 = new Rgba32(
                    (byte)((2 * color1.R + color2.R) / 3),
                    (byte)((2 * color1.G + color2.G) / 3),
                    (byte)((2 * color1.B + color2.B) / 3),
                    0xFF);
                color4 = new Rgba32(
                    (byte)((color1.R + 2 * color2.R) / 3),
                    (byte)((color1.G + 2 * color2.G) / 3),
                    (byte)((color1.B + 2 * color2.B) / 3),
                    0xFF);
            }
            else
            {
                color3 = new Rgba32(
                    (byte)((color1.R + color2.R) / 2),
                    (byte)((color1.G + color2.G) / 2),
                    (byte)((color1.B + color2.B) / 2),
                    0xFF);
                color4 = new Rgba32(0, 0, 0, 0);
            }

            Rgba32[] palette = new Rgba32[] { color1, color2, color3, color4 };
            for (int y = 0; y < 4; y++)
            {
                byte indexes = reader.ReadUInt8();
                for (int x = 0; x < 4; x++)
                {
                    if (startX + x >= image.Width || startY + y >= image.Height)
                        continue;
                    // get 2 bits
                    int shift = 6 - 2 * x;
                    byte index = (byte)((indexes >> shift) & 0x3);
                    image[startX + x, startY + y] = palette[index];

                }
            }
            reader.Position = startPos + 8; // Move to the next sub-block
        }

        public static void EncodeBlock(BinaryStream writer, Image<Rgba32> image, int startX, int startY)
        {
            long startPos = writer.Position;
            writer.Write(new byte[BlockSize]); // Reserve space for the block
            writer.Position = startPos;

            int endX = startX + BlockWidth;
            int endY = startY + BlockHeight;

            for (int y = startY; y < endY; y += 4)
            {
                for (int x = startX; x < endX; x += 4)
                {
                    EncodeSubBlock(writer, image, x, y);
                }
            }
        }

        public static void EncodeSubBlock(BinaryStream writer, Image<Rgba32> image, int startX, int startY)
        {
            // 1. Get this block's slice of the image. Preprocess it with an alpha threshold.
            Image<Rgba32> reducedBlock = new Image<Rgba32>(4, 4);
            List<Rgba32> uniqueColors = new List<Rgba32>();
            bool hasTransparent = false;
            for (int y = startY; y < startY + 4 && y < image.Height; y++)
            {
                for (int x = startX; x < startX + 4 && x < image.Width; x++)
                {
                    Rgba32 pixel = image[x, y];

                    // apply alpha threshold
                    if (pixel.A < 128)
                    {
                        pixel = Util.TRANSPARENT_COLOR;
                        hasTransparent = true;
                    }
                    else
                    {
                        pixel.A = (byte)255;
                        if (!uniqueColors.Contains(pixel))
                            uniqueColors.Add(pixel);
                    }

                    reducedBlock[x - startX, y - startY] = pixel;
                }
            }

            // 2. Create a map to baseColor1, baseColor2, extraColor1, and extraColor2.
            //                   (         0,          1,           2,               3)
            // If transparent, extraColor2 should contain the transparent pixel.
            var map = new Dictionary<int, int>();
            int i = 0;
            foreach (var color in uniqueColors)
            {
                map[i] = -1;
                i++;
            }

            // 3. Find the best pair, then map it.
            ColorPair bestPair = CalculateBestPair(uniqueColors, hasTransparent);
            
            for (int j = 0; j < bestPair.IndexesToRemove.Count; j++)
            {
                map[bestPair.IndexesToRemove[j]] = bestPair.PickedMins[j];
            }
            if (bestPair.IndexToKeep1 != -1)
                map[bestPair.IndexToKeep1] = 0;
            if (bestPair.IndexToKeep2 != -1)
                map[bestPair.IndexToKeep2] = 1;

            // 4. Write the base colors
            ushort firstColor = 0;
            if (bestPair.IndexToKeep1 != -1)
                firstColor = RGB565.EncodePixel(uniqueColors[bestPair.IndexToKeep1]);
            ushort secondColor = 0;
            if (bestPair.IndexToKeep2 != -1)
                secondColor = RGB565.EncodePixel(uniqueColors[bestPair.IndexToKeep2]);
            else if (bestPair.Mode == CMPRMode.Halves)
                secondColor = ushort.MaxValue;
            writer.WriteUInt16(firstColor);
            writer.WriteUInt16(secondColor);

            // 5. Write indices
            for (int y = 0; y < 4; y++)
            {
                byte indexes = 0;
                for (int x = 0; x < 4; x++)
                {
                    // effectively writes 0 as the index when past the x and y
                    if (startX + x >= image.Width || startY + y >= image.Height)
                        continue;
                    var color = reducedBlock[x, y];
                    int index = -1;

                    if (color.A == 0)
                        index = 3;
                    else
                    {
                        int uniqueIdx = uniqueColors.FindIndex(c => c.Equals(color));
                        index = map[uniqueIdx];
                    }
                    if (index == -1)
                        throw new InvalidOperationException($"Failed to find or map Color {color} at ({startX + x}, {startY + y}).");
                    indexes <<= 2;
                    indexes |= (byte)(index & 0x3);
                }
                writer.WriteUInt8(indexes);
            }
        }

        public enum CMPRMode
        {
            Thirds,
            Halves
        }

        public class ColorPair
        {
            public ColorPair(int idxA, int idxB, List<Vector4> vectors, CMPRMode mode = CMPRMode.Thirds)
            {
                IndexToKeep1 = idxA;
                IndexToKeep2 = idxB;
                int i = 0;
                IndexesToRemove = new();
                for (int j = 0; j < vectors.Count - 2; j++) 
                {
                    while (i == idxA || i == idxB) i++;
                    IndexesToRemove.Add(i++);
                    PickedMins.Add(3);
                }
                Mode = mode;
            }
            public readonly int IndexToKeep1 = -1;
            public readonly int IndexToKeep2 = -1;
            public readonly List<int> IndexesToRemove;
            public float Score { get; set; } = 0.0f;
            public List<int> PickedMins { get; set; } = new();
            public CMPRMode Mode { get; set; }
            public void TryScore(IEnumerable<Vector4> interpolations, IEnumerable<Vector4> colors, CMPRMode mode)
            {
                List<int> pickedMins = new();
                float curScore = 0.0f;
                foreach (var index in IndexesToRemove)
                {
                    if (index >= colors.Count())
                    {
                        continue;
                    }
                    var color = colors.ElementAt(index);
                    float minDist = float.MaxValue;
                    int pickedMin = -1;
                    int i = 0;
                    foreach (var indexToKeep in new int[]{ IndexToKeep1, IndexToKeep2 })
                    {
                        float dist = Vector4.Distance(color, colors.ElementAt(indexToKeep));
                        if (dist < minDist)
                        {
                            minDist = dist;
                            pickedMin = i;
                        }
                        i++;
                    }
                    foreach (var interp in interpolations)
                    {
                        float dist = Vector4.Distance(color, interp);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            pickedMin = i;
                        }
                        i++;
                    }
                    curScore += minDist;
                    pickedMins.Add(pickedMin);
                }
                if (curScore < Score || Score == 0.0f)
                {
                    Score = curScore;
                    PickedMins = pickedMins;
                    Mode = mode;
                }

            }
            public override string ToString()
            {
                return "Score " + Score + ". Indexes: Keep " + IndexToKeep1 + " and " + IndexToKeep2 + "; Remove "+String.Join(", ", IndexesToRemove)+". Mode: " + Mode;
            }
        }

        public static ColorPair CalculateBestPair(IEnumerable<Rgba32> c, bool hasTransparent)
        {
            List<Rgba32> colors = c.ToList<Rgba32>();
            List<Vector4> vectors = new();
            foreach (Rgba32 color in colors)
            {
                vectors.Add(color.ToVector4());
            }
            var mode = hasTransparent ? CMPRMode.Halves : CMPRMode.Thirds;

            if (vectors.Count == 1)
            {
                if (mode == CMPRMode.Halves)
                    return new ColorPair(0, -1, vectors, mode);
                else
                    return new ColorPair(-1, 0, vectors, mode);
            }

            List<ColorPair> colorPairs = new();
            for (int x = 0; x < vectors.Count; x++)
            {
                for (int y = 0; y < vectors.Count; y++)
                {
                    if (x == y)
                        continue;
                    colorPairs.Add(new ColorPair(x, y, vectors));
                }
            }
            ColorPair? bestPair = null;
            foreach (ColorPair pair in colorPairs)
            {
                var colorToKeep1 = RGB565.EncodePixel(colors[pair.IndexToKeep1]);
                var colorToKeep2 = RGB565.EncodePixel(colors[pair.IndexToKeep2]);
                if (colorToKeep1 > colorToKeep2)
                {
                    // can't use thirds mode when the pixel uses transparent pixels
                    if (hasTransparent)
                        continue;
                    var one_third = Vector4.Lerp(vectors[pair.IndexToKeep1], vectors[pair.IndexToKeep2], 1 / 3f);
                    var two_thirds = Vector4.Lerp(vectors[pair.IndexToKeep1], vectors[pair.IndexToKeep2], 2 / 3f);

                    pair.TryScore(new Vector4[] { one_third, two_thirds }, vectors, CMPRMode.Thirds);
                }
                else
                {
                    var one_half = Vector4.Lerp(vectors[pair.IndexToKeep1], vectors[pair.IndexToKeep2], 0.5f);
                    var transparent = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);

                    pair.TryScore(new Vector4[] { one_half, transparent }, vectors, CMPRMode.Halves);
                }

                if (bestPair == null || bestPair.Score > pair.Score)
                {
                    bestPair = pair;
                }
            }

            return bestPair ?? new ColorPair(-1, -1, vectors, mode);
        }
    }
}
