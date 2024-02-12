/*
 *  This file is part of the Mirage Desktop Environment.
 *  github.com/mirage-desktop/Mirage
 *  Modified by xrc2
 */
namespace Mirage.TextKit
{
    /// <summary>
    /// Represents a single, reusable glyph for a character.
    /// </summary>
    public class Glyph
    {   
        /// <summary>
        /// Initialise a new glyph.
        /// </summary>
        /// <param name="left">The horizontal offset of the glyph's bitmap.</param>
        /// <param name="top">The vertical offset of the glyph's bitmap.</param>
        /// <param name="advanceX">How far to horizontally advance after blitting this glyph, in pixels.</param>
        /// <param name="width">The width of the glyph's bitmap in pixels.</param>
        /// <param name="height">The height of the glyph's bitmap in pixels.</param>
        /// <param name="bitmap">The buffer of the glyph's bitmap, as an array of alpha values.</param>
        public Glyph(int left, int top, int advanceX, int width, int height, byte[] bitmap)
        {
            Width = width;
            Height = height;
            Bitmap = bitmap;
        }

        /// <summary>
        /// The width of the glyph's bitmap in pixels.
        /// </summary>
        public readonly int Width;

        /// <summary>
        /// The height of the glyph's bitmap in pixels.
        /// </summary>
        public readonly int Height;

        /// <summary>
        /// The buffer of the glyph's bitmap, as an array of alpha values.
        /// </summary>
        public readonly byte[] Bitmap;
    }
}
