using Binary_Stream;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace NinTextures
{
    public class IA4 : ITexture
    {
        public static int BlockSize => 0x20;

        public static int BlockWidth => 0x8;

        public static int BlockHeight => 0x4;

        public static void DecodeBlock(BinaryStream reader, Image<Rgba32> image, int startX, int startY)
        {
            long startPos = reader.Position;

            if (reader.Length - startPos < BlockSize)
                throw new InvalidOperationException($"Not enough data to decode a 8x4 IA4 block at stream position {startPos}.");

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
                    byte gray = (byte)(val >> 4);
                    byte alpha = (byte)(val & 0xF);
                    // Scale 4-bit value to 8-bit
                    gray = (byte)(gray * 0x11);
                    alpha = (byte)(alpha * 0x11);
                    image[x, y] = new Rgba32(gray, gray, gray, alpha);
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
                    Rgba32 pixel = image[x, y];
                    byte high = (byte)(pixel.R >> 4);
                    byte low = (byte)(pixel.A >> 4);
                    byte packed = (byte)((high << 4) | (low & 0xF));
                    writer.WriteUInt8(packed);
                }
            }

            // Account for any bytes we skipped over in the block
            writer.Position = startPos + BlockSize;
        }
    }
}
