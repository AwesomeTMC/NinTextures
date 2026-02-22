using Binary_Stream;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics;
using System.Reflection.PortableExecutable;

namespace NinTextures
{
    public static class Util
    {
        public static readonly Rgba32 TRANSPARENT_COLOR = new(0, 0, 0, 0);

        public static List<Rgba32> DecodePalette(BinaryStream reader, int count, PaletteFormat format)
        {
            switch (format)
            {
                case PaletteFormat.IA8:
                    return IA8.DecodePalette(reader, count);
                case PaletteFormat.RGB565:
                    return RGB565.DecodePalette(reader, count);
                case PaletteFormat.RGB5A3:
                    return RGB5A3.DecodePalette(reader, count);
                default:
                    throw new Exception("Invalid PaletteFormat " + format.ToString());
            }
        }

        public static void EncodePalette(BinaryStream writer, List<Rgba32> palette, PaletteFormat format)
        {
            switch (format)
            {
                case PaletteFormat.IA8:
                    IA8.EncodePalette(writer, palette);
                    break;
                case PaletteFormat.RGB565:
                    RGB565.EncodePalette(writer, palette);
                    break;
                case PaletteFormat.RGB5A3:
                    RGB5A3.EncodePalette(writer, palette);
                    break;
                default:
                    throw new Exception("Invalid PaletteFormat " + format.ToString());
            }
        }

        public static Image<Rgba32> DecodeTexture(BinaryStream reader, int width, int height, ImageFormat format, List<Rgba32>? palette = null)
        {
            switch (format)
            {
                case ImageFormat.C4:
                    if (palette == null)
                        throw new ArgumentNullException("Palette is null for a palette texture!");
                    return PaletteTextureEncoder<C4>.Decode(reader, width, height, palette);
                case ImageFormat.C8:
                    if (palette == null)
                        throw new ArgumentNullException("Palette is null for a palette texture!");
                    return PaletteTextureEncoder<C8>.Decode(reader, width, height, palette);
                case ImageFormat.C14X2:
                    if (palette == null)
                        throw new ArgumentNullException("Palette is null for a palette texture!");
                    return PaletteTextureEncoder<C14X2>.Decode(reader, width, height, palette);
                case ImageFormat.CMPR:
                    return TextureEncoder<CMPR>.Decode(reader, width, height);
                case ImageFormat.I4:
                    return TextureEncoder<I4>.Decode(reader, width, height);
                case ImageFormat.I8:
                    return TextureEncoder<I8>.Decode(reader, width, height);
                case ImageFormat.IA4:
                    return TextureEncoder<IA4>.Decode(reader, width, height);
                case ImageFormat.IA8:
                    return TextureEncoder<IA8>.Decode(reader, width, height);
                case ImageFormat.RGB565:
                    return TextureEncoder<RGB565>.Decode(reader, width, height);
                case ImageFormat.RGB5A3:
                    return TextureEncoder<RGB5A3>.Decode(reader, width, height);
                case ImageFormat.RGBA32:
                    return TextureEncoder<RGBA32>.Decode(reader, width, height);
                default:
                    throw new InvalidDataException("Image format " + format.ToString() + " is not a valid image format.");
            }
        }

        public static List<Rgba32> EncodeTexture(BinaryStream writer, Image<Rgba32> image, ImageFormat format)
        {
            switch (format)
            {
                case ImageFormat.C4:
                    return PaletteTextureEncoder<C4>.Encode(writer, image);
                case ImageFormat.C8:
                    return PaletteTextureEncoder<C8>.Encode(writer, image);
                case ImageFormat.C14X2:
                    return PaletteTextureEncoder<C14X2>.Encode(writer, image);
                case ImageFormat.CMPR:
                    TextureEncoder<CMPR>.Encode(writer, image);
                    break;
                case ImageFormat.I4:
                    TextureEncoder<I4>.Encode(writer, image);
                    break;
                case ImageFormat.I8:
                    TextureEncoder<I8>.Encode(writer, image);
                    break;
                case ImageFormat.IA4:
                    TextureEncoder<IA4>.Encode(writer, image);
                    break;
                case ImageFormat.IA8:
                    TextureEncoder<IA8>.Encode(writer, image);
                    break;
                case ImageFormat.RGB565:
                    TextureEncoder<RGB565>.Encode(writer, image);
                    break;
                case ImageFormat.RGB5A3:
                    TextureEncoder<RGB5A3>.Encode(writer, image);
                    break;
                case ImageFormat.RGBA32:
                    TextureEncoder<RGBA32>.Encode(writer, image);
                    break;
                default:
                    throw new InvalidDataException("Image format " + format.ToString() + " is not a valid image format.");
            }
            return new();
        }

        public static bool IsPaletteTexture(ImageFormat format)
        {
            switch (format)
            {
                case ImageFormat.C4:
                case ImageFormat.C8:
                case ImageFormat.C14X2:
                    return true;
                default:
                    return false;
            }
        }
    }

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
        static abstract void EncodeBlock(BinaryStream writer, Image<Rgba32> image, int startX, int startY, List<Rgba32> uniqueColors);
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
        public static List<Rgba32> Encode(BinaryStream writer, Image<Rgba32> image, List<Rgba32>? uniqueColors = null)
        {
            if (uniqueColors == null)
                uniqueColors = new List<Rgba32>();
            int blocksX = (image.Width + T.BlockWidth - 1) / T.BlockWidth;
            int blocksY = (image.Height + T.BlockHeight - 1) / T.BlockHeight;
            for (int by = 0; by < blocksY; by++)
            {
                for (int bx = 0; bx < blocksX; bx++)
                {
                    T.EncodeBlock(writer, image, bx * T.BlockWidth, by * T.BlockHeight, uniqueColors);
                }
            }
            return uniqueColors.ToList();
        }
        public static int EncodeIndex(Image<Rgba32> image, List<Rgba32> uniqueColors, int x, int y)
        {
            if (x >= image.Width || y >= image.Height)
                return -1;
            Rgba32 pixel = image[x, y];
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