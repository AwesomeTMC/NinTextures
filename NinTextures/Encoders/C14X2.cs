using Binary_Stream;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace NinTextures
{
    public class C14X2 : IPaletteTexture
    {
        public static int BlockSize => 0x20;

        public static int BlockWidth => 0x4;

        public static int BlockHeight => 0x4;

        public static int MaxColors => 16384; // 2 to the power of 14

        public static void DecodeBlock(BinaryStream reader, Image<Rgba32> image, int startX, int startY, List<Rgba32> palette)
        {
            long startPos = reader.Position;

            if (reader.Length - startPos < BlockSize)
                throw new InvalidOperationException($"Not enough data to decode a 4x4 C14X2 block at stream position {startPos}.");

            int endX = startX + BlockWidth;
            int endY = startY + BlockHeight;

            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    if (x >= image.Width || y >= image.Height)
                    {
                        reader.Skip(2);
                        continue;
                    }
                    int index = reader.ReadUInt16() & 0x3FFF;
                    image[x, y] = palette[index];
                }
            }

            // Account for any bytes we skipped over in the block
            reader.Position = startPos + BlockSize;
        }
        public static void EncodeBlock(BinaryStream writer, Image<Rgba32> image, int startX, int startY, List<Rgba32> uniqueColors)
        {
            long startPos = writer.Position;
            writer.Write(new byte[BlockSize]); // Reserve space for the block
            writer.Position = startPos;

            int endX = startX + BlockWidth;
            int endY = startY + BlockHeight;

            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    int index = PaletteTextureEncoder<C14X2>.EncodeIndex(image, uniqueColors, x, y) & 0x3FFF;
                    writer.WriteUInt16((ushort)index);
                }
            }

            // Account for any bytes we skipped over in the block
            writer.Position = startPos + BlockSize;
        }


    }
}
