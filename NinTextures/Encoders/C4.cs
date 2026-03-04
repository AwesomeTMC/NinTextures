using Binary_Stream;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace NinTextures
{
    public class C4 : IPaletteTexture
    {
        public static int BlockSize => 0x20;

        public static int BlockWidth => 0x8;

        public static int BlockHeight => 0x8;

        public static int MaxColors => 16; // 2 to the power of 4

        public static void DecodeBlock(BinaryStream reader, Image<Rgba32> image, int startX, int startY, List<Rgba32> palette)
        {
            long startPos = reader.Position;

            if (reader.Length - startPos < BlockSize)
                throw new InvalidOperationException($"Not enough data to decode a 8x8 C4 block at stream position {startPos}.");

            int endX = startX + BlockWidth;
            int endY = startY + BlockHeight;

            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x += 2)
                {
                    if (x >= image.Width || y >= image.Height)
                    {
                        reader.Skip(1);
                        continue;
                    }
                    byte val = reader.ReadUInt8();
                    byte firstHalf = (byte)(val >> 4);
                    byte secondHalf = (byte)(val & 0xF);
                    image[x, y] = palette[firstHalf];
                    if (x + 1 < image.Width)
                        image[x + 1, y] = palette[secondHalf];
                }
            }

            // Account for any bytes we skipped over in the block
            reader.Position = startPos + BlockSize;
        }
        public static void EncodeBlock(BinaryStream writer, Image<Rgba32> image, int startX, int startY, List<Rgba32> uniqueColors, PaletteFormat paletteFormat)
        {
            long startPos = writer.Position;
            writer.Write(new byte[BlockSize]); // Reserve space for the block
            writer.Position = startPos;

            int endX = startX + BlockWidth;
            int endY = startY + BlockHeight;

            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x += 2)
                {
                    byte high = (byte)PaletteTextureEncoder<C4>.EncodeIndex(image, uniqueColors, paletteFormat, x, y);
                    byte low = 0;
                    if (x + 1 < startX + BlockWidth)
                    {
                        low = (byte)(PaletteTextureEncoder<C4>.EncodeIndex(image, uniqueColors, paletteFormat, x + 1, y));
                    }

                    byte packed = (byte)((high << 4) | (low & 0xF));
                    writer.WriteUInt8(packed);
                }
            }

            // Account for any bytes we skipped over in the block
            writer.Position = startPos + BlockSize;
        }


    }
}
