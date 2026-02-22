using Binary_Stream;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace NinTextures
{
    public class RGB565 : ITexture
    {
        public static int BlockSize => 0x20;

        public static int BlockWidth => 0x4;

        public static int BlockHeight => 0x4;

        public static void DecodeBlock(BinaryStream reader, Image<Rgba32> image, int startX, int startY)
        {
            long startPos = reader.Position;

            if (reader.Length - startPos < BlockSize)
                throw new InvalidOperationException($"Not enough data to decode a 4x4 RGB565 block at stream position {startPos}.");

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
                    ushort color = reader.ReadUInt16();
                    byte r = (byte)((color >> 11) & 0x1F);
                    byte g = (byte)((color >> 5) & 0x3F);
                    byte b = (byte)(color & 0x1F);
                    // Scale bits
                    r = (byte)(r * 0x8);
                    g = (byte)(g * 0x4);
                    b = (byte)(b * 0x8);
                    image[x, y] = new Rgba32(r, g, b, 0xFF);
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
                    Rgba32 pixel = image[x, y];
                    byte r = (byte)(pixel.R >> 3);
                    byte g = (byte)(pixel.G >> 2);
                    byte b = (byte)(pixel.B >> 3);
                    ushort color = (ushort)((r << 11) | (g << 5) | b);
                    writer.WriteUInt16(color);
                }
            }

            // Account for any bytes we skipped over in the block
            writer.Position = startPos + BlockSize;
        }

        public static List<Rgba32> DecodePalette(BinaryStream reader, int count)
        {
            List<Rgba32> palette = new List<Rgba32>(count);
            for (uint i = 0; i < count; i++)
            {
                palette.Add(DecodePixel(reader));
            }
            return palette;
        }

        public static void EncodePalette(BinaryStream writer, List<Rgba32> palette)
        {
            foreach (var color in palette)
            {
                EncodePixel(writer, color);
            }
        }

        public static Rgba32 DecodePixel(BinaryStream reader)
        {
            ushort color = reader.ReadUInt16();
            return DecodePixel(color);
        }

        public static Rgba32 DecodePixel(ushort color)
        {
            byte r = (byte)((color >> 11) & 0x1F);
            byte g = (byte)((color >> 5) & 0x3F);
            byte b = (byte)(color & 0x1F);
            // Scale bits
            r = (byte)(r * 0x8);
            g = (byte)(g * 0x4);
            b = (byte)(b * 0x8);
            return new Rgba32(r, g, b, 0xFF);
        }

        public static void EncodePixel(BinaryStream writer, Rgba32 color)
        {

            writer.WriteUInt16(EncodePixel(color));
        }

        public static ushort EncodePixel(Rgba32 color)
        {
            byte r = (byte)(color.R >> 3);
            byte g = (byte)(color.G >> 2);
            byte b = (byte)(color.B >> 3);
            return (ushort)((r << 11) | (g << 5) | b);
        }
    }
}
