using System;
using System.IO;

namespace SVGAIITerminal.TextKit
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
            for (char c = ' '; c < (char)128; c++)
                _glyphs[c - 0x20] = ParseGlyph(c);
        }

        /// <summary>
        /// Parses a single glyph in the BTF font face from the stream
        /// </summary>
        /// <param name="c">Character to parse</param>
        private Glyph ParseGlyph(char c)
        {
            // Create new empty glyph.
            PrismAPI.Graphics.Fonts.Glyph Temp = new(0, _size);

            // Get the index of the char in the font.
            int Index = PrismAPI.Graphics.Fonts.Font.DefaultCharset.IndexOf(c);

            if (Index < 0)
            {
                return new(0, 0, _size / 2, _size / 2, _size, Temp.Points);
            }

            ushort SizePerFont = (ushort)(_size * _size8 * Index);

            for (int I = 0; I < _size * _size8; I++)
            {
                int X = I % _size8;
                int Y = I / _size8;

                for (int ww = 0; ww < 8; ww++)
                {
                    if ((_binary[SizePerFont + (Y * _size8) + X] & (0x80 >> ww)) != 0)
                    {
                        int Max = (X * 8) + ww;

                        Temp.Points.Add((Max, Y));

                        // Get max font width used.
                        Temp.Width = (ushort)Math.Max(Temp.Width, Max);
                    }
                }
            }

            // Return the glyph.
            return new(0, 0, Temp.Width, Temp.Width, _size, Temp.Points);
        }

        public override string GetFamilyName() => _familyName;

        public override string GetStyleName() => _styleName;

        public override int GetHeight() => _size;

        public override Glyph? GetGlyph(char c)
        {
            if (c < 0x20 || c >= _glyphs.Length + 0x20)
            {
                return null;
            }

            return _glyphs[c - 0x20];
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