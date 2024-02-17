using System.IO;
using Cosmos.HAL;
using Cosmos.System;

namespace Mirage.TextKit
{
    /// <summary>
    /// A bitfont format font face
    /// </summary>
    public class BtfFontFace : FontFace
    {
        /// <summary>
        /// Creates an instance of <see cref="BtfFontFace"/>
        /// </summary>
        /// <param name="binary">The font data</param>
        /// <param name="size">The size (height) of the font</param>
        public BtfFontFace(byte[] binary, ushort size)
        {
            _binary = binary;

            ParseHeight(size);
            ParseGlyphs();
        }

        /// <summary>
        /// Parse the line height of the ACF font face
        /// </summary>
        /// <exception cref="InvalidDataException">Thrown when the line height is zero</exception>
        private void ParseHeight(ushort size)
        {
            _size = size;
            _size8 = (ushort)(size / 8);
            if (_size == 0)
                throw new InvalidDataException("Invalid font height!");
        }

        /// <summary>
        /// Parses the glyphs in the BTF font face from the stream
        /// </summary>
        private void ParseGlyphs()
        {
            for (int i = 0; i < 96; i++)
                _glyphs[i] = ParseGlyph(i);
        }

        /// <summary>
        /// Parses a single glyph in the BTF font face from the stream
        /// </summary>
        /// <param name="i">Glyph index</param>
        private Glyph ParseGlyph(int i)
        {
            // Create new empty glyph.
            Glyph glyph = new(0, 0, _size / 2, _size / 2, _size, new byte[_size * (_size / 2)]);
            SerialPort.SendString("Created glyph! Left: " + glyph.Left + ", Top: " + glyph.Top + ", AdvanceX: " + glyph.AdvanceX + ", Width: " + glyph.Width + ", Height: " + glyph.Height + "\n");
            SerialPort.SendString("Glyph bitmap:\n");

            if (i == 0) return glyph;

            // TODO: fix this broken ass mess
            for (int o = 0; o < _size * _size8; o++)
            {
                int X = o % _size8;
                int Y = o / _size8;

                for (int ww = 0; ww < 8; ww++)
                {
                    if ((_binary[(ushort)(_size * _size8 * i) + (Y * _size8) + X] & (0x80 >> ww)) != 0)
                    {
                        glyph.Bitmap[Y * _size + ((X * 8) + ww)] = 0xFF;
                        SerialPort.SendString("x ");
                    }
                    else
                    {
                        SerialPort.Send(' ');
                    }
                }
                    
                SerialPort.Send('\n');
            }

            return glyph;
        }

        public override string GetFamilyName() => _familyName;

        public override string GetStyleName() => _styleName;

        public override int GetHeight() => _size;

        public override Glyph? GetGlyph(char c)
        {
            if (c < 32 || c > 127)
            {
                return null;
            }

            return _glyphs[c - 32];
        }

        /// <summary>
        /// The binary data of the BTF font face
        /// </summary>
        private readonly byte[] _binary;

        /// <summary>
        /// The line height of the font face
        /// </summary>
        private ushort _size;

        /// <summary>
        /// The line height of the font face divided by 8
        /// </summary>
        private ushort _size8;

        /// <summary>
        /// The name of the font family
        /// </summary>
        private string _familyName = "N/A";

        /// <summary>
        /// The name of the font style
        /// </summary>
        private string _styleName = "N/A";

        /// <summary>
        /// The glyphs of the font face in ASCII
        /// </summary>
        private readonly Glyph[] _glyphs = new Glyph[96];
    }
}