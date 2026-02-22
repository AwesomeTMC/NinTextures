using Binary_Stream;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace NinTextures
{
    public class BTI
    {
        public ImageFormat Format { get; set; }
        public Image<Rgba32> Image { get; set; }
        public bool EnableAlpha { get; set; }
        public byte WrapS { get; set; } = 0;
        public byte WrapT { get; set; } = 0;
        public PaletteFormat PaletteFormat { get; set; } = PaletteFormat.RGB5A3;
        public bool EnableEdgeLOD { get; set; } = false;
        public bool ClampLODBias { get; set; } = false;
        public byte MaxAnisotropy { get; set; } = 0;
        public byte MinFilterType { get; set; } = 0;
        public byte MagFilterType { get; set; } = 0;
        public byte MinLOD { get; set; } = 0;
        public List<Image<Rgba32>> Mipmaps { get; set; } = new List<Image<Rgba32>>();
        public ushort LODBias { get; set; } = 0;
        public BTI(Image<Rgba32> image, ImageFormat format)
        {
            Image = image;
            Format = format;
        }
        public BTI(BinaryStream reader)
        {
            Read(reader);
        }

        public void Read(BinaryStream reader)
        {
            var startPos = reader.Position;
            Format = (ImageFormat)reader.ReadUInt8();
            if (!Enum.IsDefined(typeof(ImageFormat), Format))
                throw new InvalidDataException("Image format " + Format + " isn't supported! Is this a BTI?");
            EnableAlpha = reader.ReadBool();
            ushort width = reader.ReadUInt16();
            ushort height = reader.ReadUInt16();
            WrapS = reader.ReadUInt8();
            WrapT = reader.ReadUInt8();
            reader.Skip(1);
            PaletteFormat = (PaletteFormat)reader.ReadUInt8();
            ushort paletteEntryCount = reader.ReadUInt16();
            var paletteDataOffset = reader.ReadUInt32();
            bool enableMipmap = reader.ReadBool();
            EnableEdgeLOD = reader.ReadBool();
            ClampLODBias = reader.ReadBool();
            MaxAnisotropy = reader.ReadUInt8();
            MinFilterType = reader.ReadUInt8();
            MagFilterType = reader.ReadUInt8();
            MinLOD = (byte)(reader.ReadUInt8() / 8);
            var mipmapCount = (byte)(reader.ReadUInt8() / 8); // technically MaxLOD
            if (reader.ReadUInt8() != mipmapCount + 1)
                throw new InvalidDataException("Image count is not the same as mipmap count + 1");
            reader.Skip(1);
            LODBias = (ushort)(reader.ReadUInt16() / 100);
            var imageDataOffset = reader.ReadUInt32();

            List<Rgba32>? palette = null;
            if (paletteDataOffset != 0 && Util.IsPaletteTexture(Format))
            {
                reader.Position = paletteDataOffset;
                palette = Util.DecodePalette(reader, paletteEntryCount, PaletteFormat);
            }
            reader.Position = imageDataOffset;
            Image = Util.DecodeTexture(reader, width, height, Format, palette);
            Mipmaps.Clear();
            for (int i = 0; i < mipmapCount; i++)
            {
                Debug.WriteLine(reader.Position);
                int downScale = 2 << i;
                Mipmaps.Add(Util.DecodeTexture(reader, width / downScale, height / downScale, Format, palette));
            }

        }

        public void Write(BinaryStream stream, bool autoHandleMipmaps=true)
        {
            if (autoHandleMipmaps)
            {
                GenerateMipmaps((ushort)Mipmaps.Count());
            }

            List<Rgba32> palette = new();
            BinaryStream imageData = new BinaryStream(stream.Endian);
            palette = Util.EncodeTexture(imageData, Image, Format);
            var startPos = stream.Position;
            stream.WriteUInt8((byte)Format);
            stream.WriteBool(EnableAlpha);
            stream.WriteUInt16((ushort)Image.Width);
            stream.WriteUInt16((ushort)Image.Height);
            stream.WriteUInt8(WrapS);
            stream.WriteUInt8(WrapT);
            stream.WriteUInt16((ushort)PaletteFormat);
            stream.WriteUInt16((ushort)palette.Count);
            stream.WriteUInt32(0);
            stream.WriteBool(Mipmaps.Count > 0);
            stream.WriteBool(EnableEdgeLOD);
            stream.WriteBool(ClampLODBias);
            stream.WriteUInt8(MaxAnisotropy);
            stream.WriteUInt8(MinFilterType);
            stream.WriteUInt8(MagFilterType);
            stream.WriteUInt8((byte)(MinLOD * 8));
            stream.WriteUInt8((byte)(Mipmaps.Count * 8));
            stream.WriteUInt8((byte)(Mipmaps.Count + 1));
            stream.WriteUInt8(0);
            stream.WriteUInt16((ushort)(LODBias * 100));
            stream.WriteUInt32(0x20);

            // Write Image Data
            imageData.WriteTo(stream);
            for (int i = 0; i < Mipmaps.Count; i++)
            {
                Util.EncodeTexture(stream, Mipmaps[i], Format);
            }

            
            if (palette.Count > 0)
            {
                // Write Palette Data Offset
                var paletteDataPosition = stream.Position;
                stream.Position = startPos + 0xC;
                stream.WriteUInt32((uint)paletteDataPosition);
                stream.Position = paletteDataPosition;

                // Write Palette Data
                Util.EncodePalette(stream, palette, PaletteFormat);
            }
        }

        public void GenerateMipmaps(ushort mipmapCount)
        {
            Mipmaps.Clear();
            for (int i = 0; i < mipmapCount; i++)
            {
                int newWidth = Image.Width / (2 << i);
                int newHeight = Image.Height / (2 << i);
                if (newWidth == 0 || newHeight == 0)
                    return;
                var options = new ResizeOptions
                {
                    
                    Size = new Size(newWidth, newHeight),
                    Sampler = KnownResamplers.NearestNeighbor
                };
                var downscaledImage = Image.Clone();
                downscaledImage.Mutate(x => x.Resize(options));
                Mipmaps.Add(downscaledImage);
            }
        }

        /// <summary>
        /// Attempts to determine the endianness of the BTI. Only works if it's a separate BTI file, not an embedded one (BMD, BDL, ...).
        /// It uses the image data offset to determine this.
        /// The position of the reader will not be changed after this is called.
        /// </summary>
        /// <param name="reader">A reader which contains the BTI's header.</param>
        /// <returns>The endian if successful. Null if unsuccessful.</returns>
        public static Endian? TryDetermineEndian(BinaryStream reader)
        {
            Endian? result = null;
            reader.Position += 0x20;
            uint imageDataOffset = reader.ReadUInt32();
            if (imageDataOffset == 0x20)
                result = Endian.Big;
            else if (imageDataOffset == 0x20000000)
                result = Endian.Little;
            reader.Position -= 0x24;
            return result;
        }
    }
}
