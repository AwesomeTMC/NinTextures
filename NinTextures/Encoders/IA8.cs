using Binary_Stream;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Reflection.PortableExecutable;

namespace NinTextures
{
    public class IA8 : ITexture
    {
        public static int BlockSize => 0x20;

        public static int BlockWidth => 0x4;

        public static int BlockHeight => 0x4;

        public static void DecodeBlock(BinaryStream reader, Image<Rgba32> image, int startX, int startY)
        {
            long startPos = reader.Position;

            if (reader.Length - startPos < BlockSize)
                throw new InvalidOperationException($"Not enough data to decode a 4x4 IA8 block at stream position {startPos}.");

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
                    byte gray = reader.ReadUInt8();
                    byte alpha = reader.ReadUInt8();
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
                        writer.WriteUInt16(0);
                        continue;
                    }
                    byte gray = image[x, y].R;
                    byte alpha = image[x, y].A;
                    writer.WriteUInt8(gray);
                    writer.WriteUInt8(alpha);
                }
            }

            // Account for any bytes we skipped over in the block
            writer.Position = startPos + BlockSize;
        }

        public static List<Rgba32> DecodePalette(BinaryStream reader, int count)
        {
            List<Rgba32> palette = new List<Rgba32>(count);
            for (int i = 0; i < count; i++)
            {
                byte gray = reader.ReadUInt8();
                byte alpha = reader.ReadUInt8();
                palette.Add(new Rgba32(gray, gray, gray, alpha));
            }
            return palette;
        }

        public static void EncodePalette(BinaryStream writer, List<Rgba32> palette)
        {
            foreach (var color in palette)
            {
                byte gray = color.R;
                byte alpha = color.A;
                writer.WriteUInt8(gray);
                writer.WriteUInt8(alpha);
            }
        }
    }
}
