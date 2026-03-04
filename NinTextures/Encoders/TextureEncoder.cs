using Binary_Stream;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics;
using System.Reflection.PortableExecutable;

namespace NinTextures
{

    public enum ImageFormat
    {
        I4 = 0x0,
        I8 = 0x1,
        IA4 = 0x2,
        IA8 = 0x3,
        RGB565 = 0x4,
        RGB5A3 = 0x5,
        RGBA32 = 0x6,
        C4 = 0x8,
        C8 = 0x9,
        C14X2 = 0xA,
        CMPR = 0xE
    }

    public enum PaletteFormat
    {
        IA8 = 0x0,
        RGB565 = 0x1,
        RGB5A3 = 0x2
    }

    public interface ITexture
    {
        static abstract int BlockSize { get; }
        static abstract int BlockWidth { get; }
        static abstract int BlockHeight { get; }

        static abstract void DecodeBlock(BinaryStream reader, Image<Rgba32> image, int startX, int startY);
        static abstract void EncodeBlock(BinaryStream writer, Image<Rgba32> image, int startX, int startY);
    }
    public interface IPaletteTexture
    {
        static abstract int BlockSize { get; }
        static abstract int BlockWidth { get; }
        static abstract int BlockHeight { get; }
        static abstract int MaxColors { get; }
        static abstract void DecodeBlock(BinaryStream reader, Image<Rgba32> image, int startX, int startY, List<Rgba32> palette);
        static abstract void EncodeBlock(BinaryStream writer, Image<Rgba32> image, int startX, int startY, List<Rgba32> uniqueColors, PaletteFormat paletteFormat);
    }
    public static class TextureEncoder<T> where T : ITexture
    {
        public static Image<Rgba32> Decode(BinaryStream reader, int width, int height)
        {
            var image = new Image<Rgba32>(width, height);
            int blocksX = (width + T.BlockWidth - 1) / T.BlockWidth;
            int blocksY = (height + T.BlockHeight - 1) / T.BlockHeight;

            for (int by = 0; by < blocksY; by++)
            {
                for (int bx = 0; bx < blocksX; bx++)
                {
                    T.DecodeBlock(reader, image, bx * T.BlockWidth, by * T.BlockHeight);
                }
            }
            return image;
        }

        public static void Encode(BinaryStream writer, Image<Rgba32> image)
        {
            int blocksX = (image.Width + T.BlockWidth - 1) / T.BlockWidth;
            int blocksY = (image.Height + T.BlockHeight - 1) / T.BlockHeight;

            for (int by = 0; by < blocksY; by++)
            {
                for (int bx = 0; bx < blocksX; bx++)
                {
                    T.EncodeBlock(writer, image, bx * T.BlockWidth, by * T.BlockHeight);
                }
            }
        }
    }
    public static class PaletteTextureEncoder<T> where T : IPaletteTexture
    {
        public static Image<Rgba32> Decode(BinaryStream reader, int width, int height, List<Rgba32> palette)
        {
            var image = new Image<Rgba32>(width, height);
            int blocksX = (width + T.BlockWidth - 1) / T.BlockWidth;
            int blocksY = (height + T.BlockHeight - 1) / T.BlockHeight;
            for (int by = 0; by < blocksY; by++)
            {
                for (int bx = 0; bx < blocksX; bx++)
                {
                    T.DecodeBlock(reader, image, bx * T.BlockWidth, by * T.BlockHeight, palette);
                }
            }
            return image;
        }
        public static List<Rgba32> Encode(BinaryStream writer, Image<Rgba32> image, PaletteFormat paletteFormat, List<Rgba32>? uniqueColors = null)
        {
            if (uniqueColors == null)
                uniqueColors = new List<Rgba32>();
            int blocksX = (image.Width + T.BlockWidth - 1) / T.BlockWidth;
            int blocksY = (image.Height + T.BlockHeight - 1) / T.BlockHeight;
            for (int by = 0; by < blocksY; by++)
            {
                for (int bx = 0; bx < blocksX; bx++)
                {
                    T.EncodeBlock(writer, image, bx * T.BlockWidth, by * T.BlockHeight, uniqueColors, paletteFormat);
                }
            }
            return uniqueColors;
        }

        public static int EncodeIndex(Image<Rgba32> image, List<Rgba32> uniqueColors, PaletteFormat paletteFormat, int x, int y)
        {
            if (x >= image.Width || y >= image.Height)
                return -1;
            Rgba32 pixel = Util.ReformatPixel(image[x, y], paletteFormat);
            var index = uniqueColors.IndexOf(pixel);
            if (index == -1)
            {
                if (uniqueColors.Count >= T.MaxColors)
                    throw new InvalidOperationException($"Pixel color {pixel} at ({x}, {y}) exceeds the {T.MaxColors} color limit for {typeof(T).FullName} encoding.");
                uniqueColors.Add(pixel);
                index = uniqueColors.Count - 1;
            }
            return index;
        }


    }

}