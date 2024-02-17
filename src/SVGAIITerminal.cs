/*
 *  This code is licensed under the ekzFreeUse license
 *  If a license wasn't included with the program,
 *  refer to https://github.com/9xbt/SVGAIITerminal/blob/main/LICENSE.md
 */
using System;
using System.Runtime.InteropServices;
using Cosmos.System;
using System.Collections.Generic;
using Cosmos.Core;
using Cosmos.HAL;
using Mirage.TextKit;
using PCSpeaker = Cosmos.System.PCSpeaker;
#if USE_GRAPEGL
using GrapeGL.Hardware.GPU;
using GrapeGL.Graphics;
using GrapeGL.Graphics.Fonts;
#else
using PrismAPI.Hardware.GPU;
using PrismAPI.Graphics;
using PrismAPI.Graphics.Fonts;
#endif

/// <summary>
/// Cursor shape enum for <see cref="SVGAIITerminal"/>
/// </summary>
public enum CursorShape
{
    Underline,
    Carret,
    Block
}

/// <summary>
/// A fast, instanceable & high resolution terminal
/// </summary>
public sealed unsafe class SVGAIITerminal
{
    // TODO: make summaries more descriptive
    
    #region Constructors

    /*/// <summary>
    /// Creates an instance of <see cref="SVGAIITerminal"/>
    /// </summary>
    /// <param name="Width">Terminal width</param>
    /// <param name="Height">Terminal height</param>
    /// <param name="Font">Terminal font</param>
    public SVGAIITerminal(int Width, int Height, Font Font)
    {
        // Null, out of range & out of memory checks
        if (Width > ushort.MaxValue || Width < 0) throw new ArgumentOutOfRangeException(nameof(Width));
        if (Height > ushort.MaxValue || Height < 0) throw new ArgumentOutOfRangeException(nameof(Height));
        if (GCImplementation.GetAvailableRAM() - GCImplementation.GetUsedRAM() / 1e6 < Width * Height * 4 / 1e6 + 1) throw new OutOfMemoryException();

        // Initialize the terminal
        this.Font = Font;
        this.Width = Width / (_fontWidth);
        this.Height = Height / Font.GetHeight();
        _fontWidth = (ushort)(Font.Size / 2);
        Font.GetHeight() = Font.Size;
        ParentHeight = Height;
        Contents = Display.GetDisplay((ushort)Width, (ushort)Height);
        UpdateRequest = () =>
        {
            Contents.DrawImage(0, 0, Contents, false);
            ((Display)Contents).Update();
        };

        // Generate the font's glyphs
        CacheGlyphs();
    }

    /// <summary>
    /// Creates an instance of <see cref="SVGAIITerminal"/>
    /// </summary>
    /// <param name="Width">Terminal width</param>
    /// <param name="Height">Terminal height</param>
    /// <param name="Font">Terminal font</param>
    /// <param name="Screen">Screen the terminal renders to</param>
    public SVGAIITerminal(int Width, int Height, Font Font, Display Screen)
    {
        // Null, out of range & out of memory checks
        if (Width > ushort.MaxValue || Width < 0) throw new ArgumentOutOfRangeException(nameof(Width));
        if (Height > ushort.MaxValue || Height < 0) throw new ArgumentOutOfRangeException(nameof(Height));
        if (Screen == null) throw new ArgumentNullException(nameof(Screen));
        if (GCImplementation.GetAvailableRAM() - GCImplementation.GetUsedRAM() / 1e6 < Width * Height * 4 / 1e6 + 1) throw new OutOfMemoryException();

        // Initialize the terminal
        this.Font = Font;
        this.Width = Width / (_fontWidth);
        this.Height = Height / Font.GetHeight();
        _fontWidth = (ushort)(Font.Size / 2);
        Font.GetHeight() = Font.Size;
        ParentHeight = Height;
        Contents = Screen;
        UpdateRequest = () =>
        {
            Screen.DrawImage(0, 0, Contents, false);
            Screen.Update();
        };

        // Generate the font's glyphs
        CacheGlyphs();
    }*/

    /// <summary>
    /// Creates an instance of <see cref="SVGAIITerminal"/>
    /// </summary>
    /// <param name="Width">Terminal width</param>
    /// <param name="Height">Terminal height</param>
    /// <param name="Font">Terminal font</param>
    /// <param name="UpdateRequest">Update request action, user can manually manage where and how to render the terminal</param>
    public SVGAIITerminal(int Width, int Height, FontFace Font, Action? UpdateRequest)
    {
        // Null, out of range & out of memory checks
        if (Width > ushort.MaxValue || Width < 0) throw new ArgumentOutOfRangeException(nameof(Width));
        if (Height > ushort.MaxValue || Height < 0) throw new ArgumentOutOfRangeException(nameof(Height));
        if (GCImplementation.GetAvailableRAM() - GCImplementation.GetUsedRAM() / 1e6 < Width * Height * 4 / 1e6 + 1) throw new OutOfMemoryException();

        // Initialize the fields
        this.Font = Font;
        _fontWidth = (ushort)GetWidestCharacterWidth();
        this.Width = Width / (_fontWidth);
        this.Height = Height / Font.GetHeight();
        this.UpdateRequest = UpdateRequest;
        ParentHeight = Height;
        Contents = new Canvas((ushort)Width, (ushort)Height);
    }

    #endregion

    #region Finalizers

    /// <summary>
    /// Frees this instance of <see cref="SVGAIITerminal"/> from memory
    /// </summary>
    ~SVGAIITerminal()
    {
        NativeMemory.Free(Contents.Internal);
    }

    #endregion

    #region Properties

    // TODO: add properties

    #endregion

    #region Methods

    /// <summary>
    /// Clears the terminal
    /// </summary>
    public void Clear()
    {
        Contents.Clear();
        CursorLeft = 0;
        CursorTop = 0;
    }
    
    // TODO: change the order of these

    /// <summary>
    /// Prints a string to the terminal
    /// </summary>
    /// <param name="str">String to print</param>
    public void Write(object str)
        => Write(str, ForegroundColor, BackgroundColor);

    /// <summary>
    /// Prints a colored string to the terminal
    /// </summary>
    /// <param name="str">String to print</param>
    /// <param name="foreColor">String foreground color</param>
    public void Write(object str, ConsoleColor foreColor)
        => Write(str, ColorConverter[(int)foreColor], BackgroundColor);

    /// <summary>
    /// Prints a colored string to the terminal
    /// </summary>
    /// <param name="str">String to print</param>
    /// <param name="foreColor">String foreground color</param>
    /// <param name="backColor">String background color</param>
    public void Write(object str, ConsoleColor foreColor, ConsoleColor backColor)
        => Write(str, ColorConverter[(int)foreColor], ColorConverter[(int)backColor]);

    /// <summary>
    /// Prints a colored string to the terminal
    /// </summary>
    /// <param name="str">String to print</param>
    /// <param name="foreColor">String foreground color</param>
    public void Write(object str, Color foreColor)
        => Write(str, foreColor, BackgroundColor);

    /// <summary>
    /// Prints a colored string to the terminal
    /// </summary>
    /// <param name="str">String to print</param>
    /// <param name="foreColor">String foreground color</param>
    /// <param name="backColor">String background color</param>
    public void Write(object str, Color foreColor, Color backColor)
    {
        // Basic null check
        if (string.IsNullOrEmpty(str.ToString()))
        {
            return;
        }

        // Print the string
        foreach (char c in str.ToString()!)
        {
            switch (c)
            {
                case '\r':
                    CursorLeft = 0;
                    break;

                case '\n':
                    CursorLeft = 0;
                    CursorTop++;
                    break;

                case '\t':
                    Write(_tab);
                    break;

                default:
                    Contents.DrawFilledRectangle(_fontWidth * CursorLeft, (Font.GetHeight() * CursorTop) + FontOffset, _fontWidth, (ushort)Font.GetHeight(), 0, backColor);

                    // Check if it's necessary to draw the character
                    if (c != ' ')
                    {
                        var glyph = Font.GetGlyph(c);

                        // Check if the glyph is null
                        if (glyph == null)
                        {
                            CursorLeft++;
                            break;
                        }
                        
                        var x = _fontWidth * CursorLeft + glyph.Left;
                        var y = Font.GetHeight() * CursorTop + Font.GetHeight() - glyph.Top - _fontExcessOffset;
                            
                        // Draw the character
                        for (int yy = 0; yy < glyph.Height; yy++)
                        {
                            for (int xx = 0; xx < glyph.Width; xx++)
                            {
                                Contents.Internal[((y + yy + FontOffset) * Contents.Width + (x + xx))] = new Color(255,
                                    (uint)((glyph.Bitmap[yy * glyph.Width + xx] * ((foreColor.ARGB >> 16) & 0xFF) + (256 - glyph.Bitmap[yy * glyph.Width + xx]) * ((Contents[xx, yy].ARGB >> 16) & 0xFF)) >> 8),
                                    (uint)((glyph.Bitmap[yy * glyph.Width + xx] * ((foreColor.ARGB >> 8) & 0xFF) + (256 - glyph.Bitmap[yy * glyph.Width + xx]) * ((Contents[xx, yy].ARGB >> 8) & 0xFF)) >> 8),
                                    (uint)((glyph.Bitmap[yy * glyph.Width + xx] * (foreColor.ARGB & 0xFF) + (256 - glyph.Bitmap[yy * glyph.Width + xx]) * (Contents[xx, yy].ARGB & 0xFF))) >> 8).ARGB;
                            }
                        }
                    }

                    CursorLeft++;
                    break;
            }
            TryScroll();
        }

        UpdateRequest?.Invoke();
    }

    /// <summary>
    /// Prints a new line character
    /// </summary>
    public void WriteLine()
    {
        CursorLeft = 0;
        CursorTop++;
    }

    /// <summary>
    /// Prints a string to the terminal with a new line character
    /// </summary>
    /// <param name="str">String to print</param>
    public void WriteLine(object str) => Write(str + "\n");

    /// <summary>
    /// Prints a colored string to the terminal with a new line character
    /// </summary>
    /// <param name="str">String to print</param>
    /// <param name="foreColor">String foreground color</param>
    public void WriteLine(object str, ConsoleColor foreColor) => Write(str + "\n", foreColor);

    /// <summary>
    /// Prints a colored string to the terminal with a new line character
    /// </summary>
    /// <param name="str">String to print</param>
    /// <param name="foreColor">String foreground color</param>
    /// <param name="backColor">String background color</param>
    public void WriteLine(object str, ConsoleColor foreColor, ConsoleColor backColor) => Write(str + "\n", foreColor, backColor);

    /// <summary>
    /// Prints a colored string to the terminal with a new line character
    /// </summary>
    /// <param name="str">String to print</param>
    /// <param name="foreColor">String foreground color</param>
    /// <param name="backColor">String background color</param>
    public void WriteLine(object str, Color foreColor, Color backColor) => Write(str + "\n", foreColor, backColor);

    /// <summary>
    /// Gets input from the user
    /// </summary>
    /// <param name="intercept">If set to false, the key pressed will be printed to the terminal</param>
    /// <returns>Key pressed</returns>
    public ConsoleKeyInfo ReadKey(bool intercept = false)
    {
        ForceDrawCursor();

        while (true)
        {
            TryDrawCursor();

            if (!KeyboardManager.TryReadKey(out var key))
            {
                IdleRequest?.Invoke();
                continue;
            }
            if (!intercept) Write(key.KeyChar);

            return new ConsoleKeyInfo(key.KeyChar, key.Key.ToConsoleKey(),
                (key.Modifiers & ConsoleModifiers.Shift) == ConsoleModifiers.Shift,
                (key.Modifiers & ConsoleModifiers.Alt) == ConsoleModifiers.Alt,
                (key.Modifiers & ConsoleModifiers.Control) == ConsoleModifiers.Control);
        }
    }

    /// <summary>
    /// Gets input from the user
    /// </summary>
    /// <returns>Text inputted</returns>
    public string ReadLine()
    {
        ForceDrawCursor();

        int startX = CursorLeft, startY = CursorTop;
        var input = string.Empty;

        for (;;)
        {
            TryDrawCursor();

            if (!KeyboardManager.TryReadKey(out var key))
            {
                IdleRequest?.Invoke();
                continue;
            }

            switch (key.Key)
            {
                case ConsoleKeyEx.Enter:
                    // TODO: change over to ForceDrawCursor(true)
                    Contents.DrawFilledRectangle(_fontWidth * CursorLeft, (Font.GetHeight() * CursorTop) + FontOffset, _fontWidth, (ushort)Font.GetHeight(), 0, BackgroundColor);
                    
                    TryScroll();

                    CursorLeft = 0;
                    CursorTop++;
                    _lastInput = input;
                    return input;

                case ConsoleKeyEx.Backspace:
                    if (!(CursorLeft == startX && CursorTop == startY))
                    {
                        for (int i = 0; i < (input[^1] == '\t' ? 4 : 1); i++)
                        {
                            Contents.DrawFilledRectangle(_fontWidth * CursorLeft, (Font.GetHeight() * CursorTop) + FontOffset, _fontWidth, (ushort)Font.GetHeight(), 0, BackgroundColor);
                            CursorTop -= CursorLeft == 0 ? 1 : 0;
                            CursorLeft -= CursorLeft == 0 ? Width - 1 : 1;
                            Contents.DrawFilledRectangle(_fontWidth * CursorLeft, (Font.GetHeight() * CursorTop) + FontOffset, _fontWidth, (ushort)Font.GetHeight(), 0, BackgroundColor);
                        }

                        input = input.Remove(input.Length - (input[^1] == '\t' ? 4 : 1));
                    }

                    ForceDrawCursor();
                    break;

                case ConsoleKeyEx.Tab:
                    Write('\t');
                    input += _tab;

                    ForceDrawCursor();
                    break;

                case ConsoleKeyEx.UpArrow:
                    ForceDrawCursor(true);
                    SetCursorPosition(startX, startY);
                    Write(new string(' ', input.Length));
                    SetCursorPosition(startX, startY);
                    Write(_lastInput);
                    input = _lastInput;

                    ForceDrawCursor();
                    break;

                default:
                    if (KeyboardManager.ControlPressed && key.Key == ConsoleKeyEx.L)
                    {
                        Clear();
                        return string.Empty;
                    }

                    if (key.KeyChar >= 32 && key.KeyChar < 128)
                    {
                        Write(key.KeyChar.ToString());
                        input += key.KeyChar;
                    }

                    ForceDrawCursor();
                    break;
            }
        }
    }

    /// <summary>
    /// Sets the cursor position
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    public void SetCursorPosition(int x, int y)
    {
        CursorLeft = x;
        CursorTop = y;
    }

    /// <summary>
    /// Gets the cursor position
    /// </summary>
    /// <returns>X and Y cursor coordinates</returns>
    public (int Left, int Top) GetCursorPosition()
    {
        return (CursorLeft, CursorTop);
    }

    /// <summary>
    /// Plays a beep sound through the PC speaker
    /// </summary>
    /// <param name="freq">Sound frequency</param>
    /// <param name="duration">Sound duration</param>
    public void Beep(uint freq = 800, uint duration = 125)
    {
        PCSpeaker.Beep(freq, duration);
        System.Threading.Thread.Sleep(200); // TODO: remove this delay
    }

    /// <summary>
    /// Sets the foreground and background colors to their defaults
    /// </summary>
    public void ResetColor()
    {
        ForegroundColor = Color.White;
        BackgroundColor = Color.Black;
    }

    /// <summary>
    /// Scrolls the terminal if needed
    /// </summary>
    private void TryScroll()
    {
        if (CursorLeft >= Width)
        {
            CursorLeft = 0;
            CursorTop++;
        }

        if (CursorTop >= Height)
        {
            Contents.DrawImage(0, ((Height - CursorTop - 1) * Font.GetHeight()) + FontOffset, Contents, false);
            Contents.DrawFilledRectangle(0, (Contents.Height - (CursorTop - Height + 1) * Font.GetHeight()) + FontOffset, Contents.Width, (ushort)((CursorTop - Height + 1) * Font.GetHeight()), 0, BackgroundColor);
            CursorTop = Height - 1;
        }

        if (CursorTop >= ParentHeight)
        {
            ScrollRequest?.Invoke();
        }
    }

    /// <summary>
    /// Forcefully draws the cursor
    /// </summary>
    private void ForceDrawCursor(bool invert = false)
    {
        if (!CursorVisible) return;

        Contents.DrawFilledRectangle(_fontWidth * CursorLeft,
            (CursorShape == CursorShape.Underline ? (Font.GetHeight() * (CursorTop + 1) - 2) : Font.GetHeight() * CursorTop) + FontOffset,
            (ushort)(CursorShape == CursorShape.Carret ? 2 : _fontWidth),
            (ushort)(CursorShape == CursorShape.Underline ? 2 : Font.GetHeight()), 0, invert ? BackgroundColor : ForegroundColor);

        UpdateRequest?.Invoke();
    }

    /// <summary>
    /// Draws the cursor if needed
    /// </summary>
    private void TryDrawCursor()
    {
        // Note: do not invert, else the cursor will go brrr
        if (CursorVisible && RTC.Second != _lastSecond)
        {
            ForceDrawCursor(!_cursorState);

            _lastSecond = RTC.Second;
            _cursorState = !_cursorState;
        }
    }

    /// <summary>
    /// Gets the widest ASCII character of the ACF font
    /// </summary>
    /// <returns>The widest character's width</returns>
    private int GetWidestCharacterWidth()
    {
        int width = 0;

        for (char c = ' '; c < ' '; c++)
        {
            var glyph = Font.GetGlyph(c);
            
            // Check if the glyph is null
            if (glyph == null)
            {
                CursorLeft++;
                break;
            }
            
            var glyphWidth = glyph.Width;
            var glyphExcess = Font.GetHeight() - glyph.Top + glyph.Height - Font.GetHeight();
                
            if (glyphWidth > width) width = glyphWidth;
            if (glyphExcess > _fontExcessOffset) _fontExcessOffset = glyphExcess;
        }

        return width;
    }

    #endregion

    #region Fields

    /// <summary>
    /// Converts <see cref="ConsoleColor"/> to <see cref="Color"/>
    /// </summary>
    public readonly List<Color> ColorConverter = new List<Color>
    {
        new Color(0, 0, 0),
        new Color(0, 0, 170),
        new Color(0, 170, 0),
        new Color(0, 170, 170),
        new Color(170, 0, 0),
        new Color(170, 0, 170),
        new Color(170, 85, 0),
        new Color(170, 170, 170),
        new Color(85, 85, 85),
        new Color(85, 85, 255),
        new Color(85, 255, 85),
        new Color(85, 255, 255),
        new Color(255, 85, 85),
        new Color(255, 85, 255),
        new Color(255, 255, 85),
        new Color(255, 255, 255),
    };

    /// <summary>
    /// The parent canvas's height. Used for handling scrolling
    /// </summary>
    public int ParentHeight;

    /// <summary>
    /// Terminal width in characters
    /// </summary>
    public int Width;

    /// <summary>
    /// Terminal height in characters
    /// </summary>
    public int Height;

    /// <summary>
    /// Cursor X coordinate
    /// </summary>
    public int CursorLeft;

    /// <summary>
    /// Cursor Y coordinate
    /// </summary>
    public int CursorTop;

    /// <summary>
    /// Foreground console color
    /// </summary>
    public Color ForegroundColor = Color.White;

    /// <summary>
    /// Background console color
    /// </summary>
    public Color BackgroundColor = Color.Black;

    /// <summary>
    /// Is cursor visible
    /// </summary>
    public bool CursorVisible = true;

    /// <summary>
    /// Cursor state
    /// </summary>
    public CursorShape CursorShape
    {
        get => _cursorShape;
        set
        {
            _cursorShape = value;
            Contents.DrawFilledRectangle((_fontWidth * CursorLeft), Font.GetHeight() * CursorTop, _fontWidth, (ushort)Font.GetHeight(), 0, BackgroundColor);
        }
    }

    /// <summary>
    /// Console contents
    /// </summary>
    public Canvas Contents;

    /// <summary>
    /// Console font
    /// </summary>
    public FontFace Font;

    /// <summary>
    /// Vertical font offset
    /// </summary>
    public int FontOffset;

    /// <summary>
    /// Update request action
    /// </summary>
    public Action? UpdateRequest;

    /// <summary>
    /// Called in a loop when ReadLine or ReadKey are idling
    /// </summary>
    public Action? IdleRequest;

    /// <summary>
    /// Called when the terminal wants to scroll up but the buffer doesn't need to
    /// </summary>
    public Action? ScrollRequest;

    /// <summary>
    /// Gets a value indicating whether a key press is available in the input stream
    /// </summary>
    public bool KeyAvailable => KeyboardManager.KeyAvailable;

    /// <summary>
    /// Tab indentation
    /// </summary>
    private const string _tab = "    ";

    /// <summary>
    /// Charset length
    /// </summary>
    private const byte _charsetLength = 96;

    /// <summary>
    /// Cached bitfont glyphs
    /// </summary>
    private readonly PrismAPI.Graphics.Fonts.Glyph[] Glyphs = new PrismAPI.Graphics.Fonts.Glyph[_charsetLength];

    /// <summary>
    /// Last second
    /// </summary>
    private static byte _lastSecond = RTC.Second;

    /// <summary>
    /// Cursor state
    /// </summary>
    private static bool _cursorState = true;
    
    /// <summary>
    /// Cursor shape
    /// </summary>
    private CursorShape _cursorShape = CursorShape.Block;

    /// <summary>
    /// Last input
    /// </summary>
    private string _lastInput = string.Empty;

    /// <summary>
    /// Font excess offset
    /// </summary>
    private int _fontExcessOffset;

    /// <summary>
    /// Font width
    /// </summary>
    private ushort _fontWidth;

    #endregion
}