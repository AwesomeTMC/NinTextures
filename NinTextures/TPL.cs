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
    public class TPLImage
    {
        public Image<Rgba32> Image { get; set; }
        public ImageFormat Format { get; set; }
        public uint WrapS = 0;
        public uint WrapT = 0;
        public uint MinFilter = 0;
        public uint MagFilter = 0;
        public float LODBias = 0;
        public byte EdgeLODEnable = 0;
        public byte MinLOD = 0;
        public byte MaxLOD = 0;
        public byte Unpacked = 0;
        public PaletteFormat PaletteFormat { get; set; } = PaletteFormat.RGB5A3;
        public TPLImage()
        {
        }
        public TPLImage(Image<Rgba32> image, ImageFormat format)
        {
            Image = image;
            Format = format;
        }

        public void ReadHeader(BinaryStream reader, long fileStartOffset, List<Rgba32>? palette = null)
        {
            var height = reader.ReadUInt16();
            var width = reader.ReadUInt16();
            Format = (ImageFormat)reader.ReadUInt32();
            var imageDataAddress = reader.ReadUInt32();
            WrapS = reader.ReadUInt32();
            WrapT = reader.ReadUInt32();
            MinFilter = reader.ReadUInt32();
            MagFilter = reader.ReadUInt32();
            LODBias = reader.ReadSingle();
            EdgeLODEnable = reader.ReadUInt8();
            MinLOD = reader.ReadUInt8();
            MaxLOD = reader.ReadUInt8();
            Unpacked = reader.ReadUInt8();
            var returnPos = reader.Position;
            reader.Position = fileStartOffset + imageDataAddress;
            Image = Util.DecodeTexture(reader, width, height, Format, palette);
            reader.Position = returnPos;
        }

        public void WriteHeader(BinaryStream writer, byte[] imageData)
        {
            var startPos = writer.Position;
            writer.WriteUInt16((ushort)Image.Height);
            writer.WriteUInt16((ushort)Image.Width);
            writer.WriteUInt32((uint)Format);
            writer.WriteUInt32(0);
            writer.WriteUInt32(WrapS);
            writer.WriteUInt32(WrapT);
            writer.WriteUInt32(MinFilter);
            writer.WriteUInt32(MagFilter);
            writer.WriteSingle(LODBias);
            writer.WriteUInt8(EdgeLODEnable);
            writer.WriteUInt8(MinLOD);
            writer.WriteUInt8(MaxLOD);
            writer.WriteUInt8(Unpacked);
            var imageDataAddress = writer.Position + (0x20 - (writer.Position % 0x20));
            writer.Position = startPos + 0x8;
            writer.WriteUInt32((uint)imageDataAddress);
            writer.Position = imageDataAddress;
            writer.Write(imageData);
        }
    }
    public class TPL
    {
        public static uint MAGIC = 0x0020AF30;
        public List<TPLImage> Images { get; set; } = new List<TPLImage>();
        public ushort LODBias { get; set; } = 0;
        public TPL(Image<Rgba32> image, ImageFormat format)
        {
            Images.Clear();
            Images.Add(new TPLImage(image, format));
        }
        public TPL(BinaryStream reader)
        {
            Read(reader);
        }

        public void Read(BinaryStream reader)
        {
            // read 
            var startPos = reader.Position;
            reader.Endian = Endian.Big;
            var magic = reader.ReadUInt32();
            if (magic == MAGIC)
            {
                reader.Endian = Endian.Big;
            }
            else if (magic == 0x30AF2000)
            {
                reader.Endian = Endian.Little;
            }
            else
            {
                throw new InvalidDataException("File does not have TPL magic! Magic: " + magic);
            }
            var imageCount = reader.ReadUInt32();
            var imageTableOffset = reader.ReadUInt32();

            // decrypt image offset table
            reader.Position = startPos + imageTableOffset;
            for (int i = 0; i < imageCount; i++)
            {
                var imageOffset = reader.ReadUInt32();
                var paletteOffset = reader.ReadUInt32();
                var returnPos = reader.Position;
                if (imageOffset == 0)
                {
                    throw new InvalidDataException("Image data in image offset table is null");
                }
                var image = new TPLImage();
                List<Rgba32>? palette = null;
                if (paletteOffset != 0)
                {
                    reader.Position = startPos + paletteOffset;
                    var entryCount = reader.ReadUInt16();
                    reader.Skip(2); // skip unpacked and padding
                    image.PaletteFormat = (PaletteFormat)reader.ReadUInt32();
                    if (!Enum.IsDefined(typeof(PaletteFormat), image.PaletteFormat))
                        throw new InvalidDataException("Palette format " + image.PaletteFormat.ToString() + " is invalid!");
                    var paletteDataAddress = reader.ReadUInt32();
                    reader.Position = startPos + paletteDataAddress;
                    palette = Util.DecodePalette(reader, entryCount, image.PaletteFormat);
                }
                reader.Position = imageOffset;
                image.ReadHeader(reader, startPos, palette);
                reader.Position = returnPos;
                Images.Add(image);
            }
        }

        public void Write(BinaryStream writer)
        {
            writer.WriteUInt32(MAGIC);
            writer.WriteUInt32((uint)Images.Count);
            writer.WriteUInt32((uint)(writer.Position + 4));
            var tableStart = writer.Position;
            writer.Write(new byte[Images.Count * 0x8]);
            //writer.AlignTo(0x20);
            
            for (int i = 0; i < Images.Count; i++)
            {
                TPLImage image = Images[i];
                using BinaryStream imageData = new BinaryStream(writer.Endian);
                List<Rgba32> palette = Util.EncodeTexture(imageData, image.Image, image.Format, image.PaletteFormat);
                uint paletteHeaderOffset = 0;
                if (Util.IsPaletteTexture(image.Format))
                {
                    paletteHeaderOffset = (uint)writer.Position;
                    writer.WriteUInt16((ushort)palette.Count);
                    writer.WriteUInt16(0);
                    writer.WriteUInt32((uint)image.PaletteFormat);
                    writer.WriteUInt32((uint)(writer.Position + 0x4));
                    Util.EncodePalette(writer, palette, image.PaletteFormat);
                }
                var imageHeaderOffset = writer.Position;

                writer.Position = tableStart + (i * 0x8);
                writer.WriteUInt32((uint)imageHeaderOffset);
                writer.WriteUInt32(paletteHeaderOffset);
                writer.Position = imageHeaderOffset;

                image.WriteHeader(writer, imageData.ToArray());
            }
        }
    }
}
