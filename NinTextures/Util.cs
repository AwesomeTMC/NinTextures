using Binary_Stream;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public static HashSet<Rgba32> GetUniqueColorsSet(Image<Rgba32> img)
        {
            HashSet<Rgba32> uniqueColors = new();
            for (int y = 0; y < img.Height; y++)
            {
                for (int x = 0; x < img.Width; x++)
                {
                    var color = img[x, y];
                    if (!uniqueColors.Contains(color))
                    {
                        uniqueColors.Add(color);
                    }
                }
            }
            return uniqueColors;
        }
    }
}
