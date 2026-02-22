using Binary_Stream;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace NinTextures
{
    public class C8 : IPaletteTexture
    {
        public static int BlockSize => 0x20;

        public static int BlockWidth => 0x8;

        public static int BlockHeight => 0x4;

        public static int MaxColors => 256; // 2 to the power of 8

        public static void DecodeBlock(BinaryStream reader, Image<Rgba32> image, int startX, int startY, List<Rgba32> palette)
        {
            long startPos = reader.Position;

            if (reader.Length - startPos < BlockSize)
                throw new InvalidOperationException($"Not enough data to decode a 8x4 C8 block at stream position {startPos}.");

            int endX = startX + BlockWidth;
            int endY = startY + BlockHeight;

            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    if (x >= image.Width || y >= image.Height)
                    {
                        reader.Skip(1);
                        continue;
                    }
                    byte index = reader.ReadUInt8();
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
                    byte index = (byte)PaletteTextureEncoder<C8>.EncodeIndex(image, uniqueColors, x, y);
                    writer.WriteUInt8(index);
                }
            }

            // Account for any bytes we skipped over in the block
            writer.Position = startPos + BlockSize;
        }


    }
}
