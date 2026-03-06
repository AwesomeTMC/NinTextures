using Binary_Stream;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace NinTextures
{
    public class I4 : ITexture
    {
        public static int BlockSize => 0x20;

        public static int BlockWidth => 0x8;

        public static int BlockHeight => 0x8;

        public static void DecodeBlock(BinaryStream reader, Image<Rgba32> image, int startX, int startY)
        {
            long startPos = reader.Position;

            if (reader.Length - startPos < BlockSize)
                throw new InvalidOperationException($"Not enough data to decode a 8x8 I4 block at stream position {startPos}.");

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
                    byte firstHalf = (byte)(val >> 4);     // high nibble
                    byte secondHalf = (byte)(val & 0xF);   // low nibble
                    // Scale 4-bit value to 8-bit
                    firstHalf = (byte)(firstHalf * 0x11);
                    secondHalf = (byte)(secondHalf * 0x11);
                    image[x, y] = new Rgba32(firstHalf, firstHalf, firstHalf, 255);
                    if (x + 1 < image.Width)
                    {
                        image[x + 1, y] = new Rgba32(secondHalf, secondHalf, secondHalf, 255);
                    }
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
                for (int x = startX; x < endX; x += 2)
                {
                    if (x >= image.Width || y >= image.Height)
                    {
                        writer.WriteUInt8(0);
                        continue;
                    }
                    Rgba32 pixel0 = image[x, y];
                    byte high = (byte)(pixel0.R >> 4); // take top 4 bits

                    byte low = 0;
                    if (x + 1 < image.Width)
                    {
                        Rgba32 pixel1 = image[x + 1, y];
                        low = (byte)(pixel1.R >> 4);
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
