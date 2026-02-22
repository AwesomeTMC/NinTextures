using Binary_Stream;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace NinTextures
{
    public class RGBA32 : ITexture
    {
        public static int BlockSize => 0x40;

        public static int BlockWidth => 0x4;

        public static int BlockHeight => 0x4;

        public static void DecodeBlock(BinaryStream reader, Image<Rgba32> image, int startX, int startY)
        {
            long startPos = reader.Position;

            if (reader.Length - startPos < BlockSize)
                throw new InvalidOperationException($"Not enough data to decode a 4x4 RGBA32 block at stream position {startPos}.");

            int endX = startX + BlockWidth;
            int endY = startY + BlockHeight;

            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    if (x >= image.Width || y >= image.Height)
                    {
                        reader.Skip(4);
                        continue;
                    }
                    long pos = reader.Position;

                    // Read A, R from first group
                    byte a = reader.ReadUInt8();
                    byte r = reader.ReadUInt8();

                    // Read G, B from the second group
                    reader.Position = pos + 0x20;
                    byte g = reader.ReadUInt8();
                    byte b = reader.ReadUInt8();
                    reader.Position = pos + 2;

                    image[x, y] = new Rgba32(r, g, b, a);
                }
            }

            // Account for any bytes we skipped over in the block (if width/height not multiple of 4)
            reader.Position = startPos + BlockSize;
        }

        public static void EncodeBlock(BinaryStream writer, Image<Rgba32> image, int startX, int startY)
        {
            long startPos = writer.Position;
            writer.Write(new byte[BlockSize]); // Reserve space for the block
            writer.Position = startPos;

            int endX = Math.Min(startX + BlockWidth, image.Width);
            int endY = Math.Min(startY + BlockHeight, image.Height);

            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    if (x >= image.Width || y >= image.Height)
                    {
                        writer.WriteUInt32(0);
                        continue; 
                    }
                    var pos = writer.Position;
                    Rgba32 pixel = image[x, y];

                    // Write A, R to the first group
                    writer.WriteUInt8(pixel.A);
                    writer.WriteUInt8(pixel.R);

                    // Write G, B to the second group
                    writer.Position = pos + 0x20;
                    writer.WriteUInt8(pixel.G);
                    writer.WriteUInt8(pixel.B);
                    writer.Position = pos + 2;
                }
            }

            // Account for any bytes we skipped over in the block (if width/height not multiple of BlockSize)
            writer.Position = startPos + BlockSize;
        }
    }
}
