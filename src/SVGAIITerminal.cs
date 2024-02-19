/*
 *  This code is licensed under the ekzFreeUse license
 *  If a license wasn't included with the program,
 *  refer to https://github.com/9xbt/SVGAIITerminal/blob/main/LICENSE.md
 */

using System;
using System.Collections.Generic;
using Cosmos.HAL;
using Cosmos.Core;
using Cosmos.System;
using SVGAIITerminal.TextKit;
using PCSpeaker = Cosmos.System.PCSpeaker;
#if USE_GRAPEGL
using GrapeGL.Graphics;
using GrapeGL.Hardware.GPU;
#else
using PrismAPI.Graphics;
using PrismAPI.Hardware.GPU;
#endif

namespace SVGAIITerminal;

/// <summary>
/// A fast, instanceable & high resolution terminal
/// </summary>
public sealed unsafe class SVGAIITerminal
{
    // TODO: make summaries more descriptive
    
    #region Constructors

    /// <summary>
    /// Creates an instance of <see cref="SVGAIITerminal"/>
    /// </summary>
    /// <param name="Width">Terminal width</param>
    /// <param name="Height">Terminal height</param>
    /// <param name="Font">Terminal font</param>
    public SVGAIITerminal(int Width, int Height, FontFace Font)
    {
        // Null, out of range & out of memory checks
        if (Width is > ushort.MaxValue or < 0) throw new ArgumentOutOfRangeException(nameof(Width));
        if (Height is > ushort.MaxValue or < 0) throw new ArgumentOutOfRangeException(nameof(Height));
        if (GCImplementation.GetAvailableRAM() - GCImplementation.GetUsedRAM() / 1e6 < Width * Height * 4 / 1e6 + 1) throw new OutOfMemoryException();

        // Initialize the terminal
        this.Font = Font;
        _fontWidth = (ushort)GetWidestCharacterWidth();
        this.Width = Width / (_fontWidth);
        this.Height = Height / Font.GetHeight();
        ParentHeight = Height;
        Contents = Display.GetDisplay((ushort)Width, (ushort)Height);
        UpdateRequest = () =>
        {
            ((Display)Contents).Update();
        };
    }

    /// <summary>
    /// Creates an instance of <see cref="SVGAIITerminal"/>
    /// </summary>
    /// <param name="Width">Terminal width</param>
    /// <param name="Height">Terminal height</param>
    /// <param name="Font">Terminal font</param>
    /// <param name="Screen">Screen to render the terminal to</param>
    public SVGAIITerminal(int Width, int Height, FontFace Font, Display Screen)
    {
        // Null, out of range & out of memory checks
        if (Width is > ushort.MaxValue or < 0) throw new ArgumentOutOfRangeException(nameof(Width));
        if (Height is > ushort.MaxValue or < 0) throw new ArgumentOutOfRangeException(nameof(Height));
        if (Screen == null) throw new ArgumentNullException(nameof(Screen));
        if (GCImplementation.GetAvailableRAM() - GCImplementation.GetUsedRAM() / 1e6 < Width * Height * 4 / 1e6 + 1) throw new OutOfMemoryException();

        // Initialize the terminal
        this.Font = Font;
        _fontWidth = (ushort)GetWidestCharacterWidth();
        this.Width = Width / (_fontWidth);
        this.Height = Height / Font.GetHeight();
        ParentHeight = Height;
        Contents = Screen;
        UpdateRequest = Screen.Update;
    }

    /// <summary>
    /// Creates an instance of <see cref="SVGAIITerminal"/>
    /// </summary>
    /// <param name="Width">Terminal width</param>
    /// <param name="Height">Terminal height</param>
    /// <param name="Font">Terminal font</param>
    /// <param name="UpdateRequest">Called when the terminal finishes rendering text</param>
    public SVGAIITerminal(int Width, int Height, FontFace Font, Action? UpdateRequest)
    {
        // Null, out of range & out of memory checks
        if (Width is > ushort.MaxValue or < 0) throw new ArgumentOutOfRangeException(nameof(Width));
        if (Height is > ushort.MaxValue or < 0) throw new ArgumentOutOfRangeException(nameof(Height));
        if (GCImplementation.GetAvailableRAM() - GCImplementation.GetUsedRAM() / 1e6 < Width * Height * 4 / 1e6 + 1) throw new OutOfMemoryException();

        // Initialize the fields
        this.Font = Font;
        _fontWidth = (ushort)GetWidestCharacterWidth();
        this.Width = Width / _fontWidth;
        this.Height = Height / Font.GetHeight();
        this.UpdateRequest = UpdateRequest;
        ParentHeight = Height;
        Contents = new Canvas((ushort)Width, (ushort)Height);
    }

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
                    Write(TabIndentation);
                    break;

                default:
                    Contents.DrawFilledRectangle(_fontWidth * CursorLeft, Font.GetHeight() * CursorTop, _fontWidth, (ushort)Font.GetHeight(), 0, backColor);

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

                        if (glyph.Points.Count == 0)
                        {
                            // Get the X and Y position of where to draw the glyph at
                            var x = _fontWidth * CursorLeft + glyph.Left;
                            var y = Font.GetHeight() * CursorTop + Font.GetHeight() - glyph.Top - _fontExcessOffset;
                            
                            // Draw the ACF character
                            for (int yy = 0; yy < glyph.Height; yy++)
                            {
                                for (int xx = 0; xx < glyph.Width; xx++)
                                {
                                    uint alpha = glyph.Bitmap[yy * glyph.Width + xx];
                                    uint invAlpha = 256 - alpha;

                                    int canvasIdx = (y + yy) * Contents.Width + x + xx;

                                    uint backgroundArgb = Contents.Internal[canvasIdx];
                                    uint glyphColorArgb = foreColor.ARGB;
                                    
                                    byte backgroundR = (byte)((backgroundArgb >> 16) & 0xFF);
                                    byte backgroundG = (byte)((backgroundArgb >> 8) & 0xFF);
                                    byte backgroundB = (byte)(backgroundArgb & 0xFF);

                                    byte foregroundR = (byte)((glyphColorArgb >> 16) & 0xFF);
                                    byte foregroundG = (byte)((glyphColorArgb >> 8) & 0xFF);
                                    byte foregroundB = (byte)((glyphColorArgb) & 0xFF);
                                    
                                    byte a = 255;
                                    byte r = (byte)((alpha * foregroundR + invAlpha * backgroundR) >> 8);
                                    byte g = (byte)((alpha * foregroundG + invAlpha * backgroundG) >> 8);
                                    byte b = (byte)((alpha * foregroundB + invAlpha * backgroundB) >> 8);
                                    
                                    uint color = ((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | b;

                                    Contents.Internal[canvasIdx] = color;
                                }
                            }
                        }
                        else
                        {
                            // Draw the BTF character
                            for (int j = 0; j < glyph.Points.Count; j++)
                            {
                                Contents[_fontWidth * CursorLeft + glyph.Points[j].X,
                                    Font.GetHeight() * CursorTop + glyph.Points[j].Y] = foreColor;
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
                    Contents.DrawFilledRectangle(_fontWidth * CursorLeft, Font.GetHeight() * CursorTop, _fontWidth, (ushort)Font.GetHeight(), 0, BackgroundColor);
                    
                    TryScroll();

                    CursorLeft = 0;
                    CursorTop++;
                    _lastInput = input;
                    return input;

                case ConsoleKeyEx.Backspace:
                    if (!(CursorLeft == startX && CursorTop == startY))
                    {
                        Contents.DrawFilledRectangle(_fontWidth * CursorLeft, Font.GetHeight() * CursorTop, _fontWidth, (ushort)Font.GetHeight(), 0, BackgroundColor);
                        CursorTop -= CursorLeft == 0 ? 1 : 0;
                        CursorLeft -= CursorLeft == 0 ? Width - 1 : 1;
                        Contents.DrawFilledRectangle(_fontWidth * CursorLeft, Font.GetHeight() * CursorTop, _fontWidth, (ushort)Font.GetHeight(), 0, BackgroundColor);
                        
                        input = input.Remove(input.Length - 1);
                    }

                    ForceDrawCursor();
                    break;

                case ConsoleKeyEx.Tab:
                    Write('\t');
                    input += TabIndentation;

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
    public void Beep(uint freq = 800, uint duration = 125) => PCSpeaker.Beep(freq, duration);

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
            Contents.DrawImage(0, (Height - CursorTop - 1) * Font.GetHeight(), Contents, false);
            Contents.DrawFilledRectangle(0, Contents.Height - (CursorTop - Height + 1) * Font.GetHeight(), Contents.Width, (ushort)((CursorTop - Height + 1) * Font.GetHeight()), 0, BackgroundColor);
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
            CursorShape == CursorShape.Underline ? Font.GetHeight() * (CursorTop + 1) - 2 : Font.GetHeight() * CursorTop,
            (ushort)(CursorShape == CursorShape.Caret ? 2 : _fontWidth),
            (ushort)(CursorShape == CursorShape.Underline ? 2 : Font.GetHeight()), 0, invert ? BackgroundColor : ForegroundColor);

        UpdateRequest?.Invoke();
    }

    /// <summary>
    /// Draws the cursor if needed
    /// </summary>
    private void TryDrawCursor()
    {
        if (!CursorVisible || RTC.Second == _lastSecond) return;
        
        _lastSecond = RTC.Second;
        _cursorState = !_cursorState;
        
        ForceDrawCursor(_cursorState);
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
            // Get the glyph
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
    /// Terminal width in characters
    /// </summary>
    public readonly int Width;

    /// <summary>
    /// Terminal height in characters
    /// </summary>
    public readonly int Height;

    /// <summary>
    /// Converts <see cref="ConsoleColor"/> to <see cref="Color"/>
    /// </summary>
    public readonly List<Color> ColorConverter = new()
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
    /// Console contents
    /// </summary>
    public readonly Canvas Contents;
    
    /// <summary>
    /// Console font
    /// </summary>
    public readonly FontFace Font;

    /// <summary>
    /// Update request action
    /// </summary>
    public readonly Action? UpdateRequest;
    
    /// <summary>
    /// Cursor X coordinate
    /// </summary>
    public int CursorLeft;

    /// <summary>
    /// Cursor Y coordinate
    /// </summary>
    public int CursorTop;
    
    /// <summary>
    /// The parent canvas's height. Used for handling scrolling
    /// </summary>
    public int ParentHeight;
    
    /// <summary>
    /// Is cursor visible
    /// </summary>
    public bool CursorVisible = true;

    /// <summary>
    /// Foreground console color
    /// </summary>
    public Color ForegroundColor = Color.White;

    /// <summary>
    /// Background console color
    /// </summary>
    public Color BackgroundColor = Color.Black;

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
    /// Called in a loop when ReadLine or ReadKey are idling
    /// </summary>
    public Action? IdleRequest;

    /// <summary>
    /// Called when the terminal wants to scroll up but the buffer doesn't need to
    /// </summary>
    public Action? ScrollRequest;

    /// <summary>
    /// Tab indentation
    /// </summary>
    private const string TabIndentation = "    ";

    /// <summary>
    /// Font width
    /// </summary>
    private readonly ushort _fontWidth;
    
    /// <summary>
    /// Last second
    /// </summary>
    private static byte _lastSecond = RTC.Second;

    /// <summary>
    /// Cursor state
    /// </summary>
    private static bool _cursorState = true;
    
    /// <summary>
    /// Font excess offset
    /// </summary>
    private int _fontExcessOffset;
    
    /// <summary>
    /// Last input
    /// </summary>
    private string _lastInput = string.Empty;
    
    /// <summary>
    /// Cursor shape
    /// </summary>
    private CursorShape _cursorShape = CursorShape.Block;

    #endregion
}