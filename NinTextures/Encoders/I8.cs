using Binary_Stream;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace NinTextures
{
    public class I8 : ITexture
    {
        public static int BlockSize => 0x20;

        public static int BlockWidth => 0x8;

        public static int BlockHeight => 0x4;

        public static void DecodeBlock(BinaryStream reader, Image<Rgba32> image, int startX, int startY)
        {
            long startPos = reader.Position;

            if (reader.Length - startPos < BlockSize)
                throw new InvalidOperationException($"Not enough data to decode a 8x4 I8 block at stream position {startPos}.");

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
                    byte val = reader.ReadUInt8();
                    image[x, y] = new Rgba32(val, val, val, 255);
                }
            }

            // Account for any bytes we skipped over in the block
            reader.Position = startPos + BlockSize;
        }

        public static void EncodeBlock(BinaryStream writer, Image<Rgba32> image, int startX, int startY)
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
                    if (x >= image.Width || y >= image.Height)
                    {
                        writer.WriteUInt8(0);
                        continue; 
                    }
                    byte val = image[x, y].R; // Uses R for grayscale
                    writer.WriteUInt8(val);
                }
            }

            // Account for any bytes we skipped over in the block
            writer.Position = startPos + BlockSize;
        }
    }
}
