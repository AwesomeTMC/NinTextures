using Binary_Stream;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics;
using System.Drawing;

namespace NinTextures
{
    public class RGB5A3 : ITexture
    {
        public static int BlockSize => 0x20;

        public static int BlockWidth => 0x4;

        public static int BlockHeight => 0x4;

        public static void DecodeBlock(BinaryStream reader, Image<Rgba32> image, int startX, int startY)
        {
            long startPos = reader.Position;

            if (reader.Length - startPos < BlockSize)
                throw new InvalidOperationException($"Not enough data to decode a 4x4 RGB5A3 block at stream position {startPos}.");

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
                    image[x, y] = DecodePixel(reader);
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
                    writer.WriteUInt16(EncodePixel(image[x, y]));
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
                palette.Add(DecodePixel(reader));
            }
            return palette;
        }

        public static void EncodePalette(BinaryStream writer, List<Rgba32> palette)
        {
            foreach (var color in palette)
            {
                EncodePixel(color, writer);
            }
        }

        public static void EncodePixel(Rgba32 color, BinaryStream writer)
        {
            writer.WriteUInt16(EncodePixel(color));
        }

        public static ushort EncodePixel(Rgba32 color)
        {
            ushort encoded;
            if (color.A < 0xFF)
            {
                // 0AAARRRRGGGGBBBB
                encoded = (ushort)(((color.A / 0x20) << 12) | // AAA
                    ((color.R / 0x11) << 8) | // RRRR
                    ((color.G / 0x11) << 4) | // GGGG
                    (color.B / 0x11)); // BBBB
            }
            else
            {
                // 1RRRRRGGGGGBBBBB
                encoded = (ushort)(0x8000 | // 1
                    ((color.R / 8) << 10) | // RRRRR
                    ((color.G / 8) << 5) | // GGGGG
                    (color.B / 8)); // BBBBB
            }
            return encoded;
        }

        public static Rgba32 DecodePixel(BinaryStream reader)
        {
            return DecodePixel(reader.ReadUInt16());
        }

        public static Rgba32 DecodePixel(ushort color)
        {
            var hasAlpha = (color & 0x8000) == 0;
            byte r, g, b, a;
            if (hasAlpha)
            {
                // 0AAARRRRGGGGBBBB
                a = (byte)(((color >> 12) & 0x7) * 0x20); // AAA
                r = (byte)(((color >> 8) & 0xF) * 0x11); // RRRR
                g = (byte)(((color >> 4) & 0xF) * 0x11); // GGGG
                b = (byte)((color & 0xF) * 0x11); // BBBB
            }
            else
            {
                // 1RRRRRGGGGGBBBBB
                a = 0xFF;
                r = (byte)(((color >> 10) & 0x1F) * 8); // RRRRR
                g = (byte)(((color >> 5) & 0x1F) * 8); // GGGGG
                b = (byte)((color & 0x1F) * 8); // BBBBB
            }
            return new Rgba32(r, g, b, a);
        }
    }
}
