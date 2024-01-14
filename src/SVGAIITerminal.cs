/* This code is licensed under the ekzFreeUse license
 * If a license wasn't included with the program,
 * refer to https://github.com/9xbt/SVGAIITerminal/blob/main/LICENSE.md */

using System;
using Cosmos.System;
using PrismAPI.Hardware.GPU;
using PrismAPI.Graphics;
using PrismAPI.Graphics.Fonts;

/// <summary>
/// A fast, instanceable & high resolution terminal
/// </summary>
public sealed class SVGAIITerminal
{
    #region Constructors

    /// <summary>
    /// Creates an instance of <see cref="SVGAIITerminal"/>
    /// </summary>
    /// <param name="width">Terminal width</param>
    /// <param name="height">Terminal height</param>
    /// <param name="font">Terminal font</param>
    public SVGAIITerminal(int width, int height, Font font)
    {
        Display screen = Display.GetDisplay((ushort)width, (ushort)height);

        Font = font;
        Width = width / (Font.Size / 2);
        Height = height / Font.Size;
        Contents = screen;
        UpdateRequest = () =>
        {
            screen.DrawImage(0, 0, this.Contents, false);
            screen.Update();
        };
    }

    /// <summary>
    /// Creates an instance of <see cref="SVGAIITerminal"/>
    /// </summary>
    /// <param name="width">Terminal width</param>
    /// <param name="height">Terminal height</param>
    /// <param name="font">Terminal font</param>
    /// <param name="screen">Screen the terminal renders to</param>
    public SVGAIITerminal(int width, int height, Font font, Display screen)
    {
        Font = font;
        Width = width / (font.Size / 2);
        Height = height / font.Size;
        Contents = screen;
        UpdateRequest = () =>
        {
            screen.DrawImage(0, 0, this.Contents, false);
            screen.Update();
        };
    }

    /// <summary>
    /// Creates an instance of <see cref="SVGAIITerminal"/>
    /// </summary>
    /// <param name="width">Terminal width</param>
    /// <param name="height">Terminal height</param>
    /// <param name="font">Terminal font</param>
    /// <param name="updateRequest">Update request action, user can manually manage where and how to render the terminal</param>
    public SVGAIITerminal(int width, int height, Font font, Action updateRequest)
    {
        Font = font;
        Width = width / (font.Size / 2);
        Height = height / font.Size;
        Contents = new Canvas((ushort)Width, (ushort)Height);
        UpdateRequest = updateRequest;
    }

    #endregion
    
    #region Properties

    /// <summary>
    /// The color of the pixel at the specified X & Y coordinates
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    public Color this[int x, int y]
    {
        get => Contents[x, y];
        set => Contents[x, y] = value;
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

    /// <summary>
    /// Print a string to the terminal
    /// </summary>
    /// <param name="str">String to print</param>
    public void Write(object str) => Write(str, ForegroundColor);

    /// <summary>
    /// Print a colored string to the terminal
    /// </summary>
    /// <param name="str">String to print</param>
    /// <param name="color">String color</param>
    public void Write(object str, Color color)
    {
        foreach (char c in str.ToString()!)
        {
            TryScroll();

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
                    Write(new string(' ', 4));
                    break;

                default:
                    Contents.DrawFilledRectangle(Font.Size / 2 * CursorLeft, Font.Size * CursorTop, Convert.ToUInt16(Font.Size / 2), Font.Size, 0, BackgroundColor);
                    Contents.DrawString(Font.Size / 2 * CursorLeft, Font.Size * CursorTop, c.ToString(), Font, color);
                    CursorLeft++;
                    break;
            }
        }

        UpdateRequest?.Invoke();
    }

    /// <summary>
    /// Print a string to the terminal with a new line character
    /// </summary>
    /// <param name="str">String to print</param>
    public void WriteLine(object? str = null) => Write(str + "\n");

    /// <summary>
    /// Print a colored string to the terminal with a new line character
    /// </summary>
    /// <param name="str">String to print</param>
    /// <param name="color">String color</param>
    public void WriteLine(object str, Color color) => Write(str + "\n", color);

    /// <summary>
    /// Gets input from the user
    /// </summary>
    /// <param name="intercept">If set to false, the key pressed will be printed to the terminal</param>
    /// <returns>Key pressed</returns>
    public ConsoleKeyInfo ReadKey(bool intercept = false)
    {
        while (true)
        {
            TryDrawCursor();

            if (!KeyboardManager.TryReadKey(out var key)) continue;
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

            if (!KeyboardManager.TryReadKey(out var key)) continue;
            
            switch (key.Key)
            {
                case ConsoleKeyEx.Enter:
                    Contents.DrawFilledRectangle(Font.Size / 2 * CursorLeft, Font.Size * CursorTop, Convert.ToUInt16(Font.Size / 2), Font.Size, 0, BackgroundColor);
                    TryScroll();
                    
                    CursorLeft = 0;
                    CursorTop++;
                    _lastInput = input;
                    
                    return input;

                case ConsoleKeyEx.Backspace:
                    if (!(CursorLeft == startX && CursorTop == startY))
                    {
                        Contents.DrawFilledRectangle(Font.Size / 2 * CursorLeft, Font.Size * CursorTop, Convert.ToUInt16(Font.Size / 2), Font.Size, 0, BackgroundColor);
                        CursorTop -= CursorLeft == 0 ? 1 : 0;
                        CursorLeft -= CursorLeft == 0 ? Width - 1 : 1;
                        Contents.DrawFilledRectangle(Font.Size / 2 * CursorLeft, Font.Size * CursorTop, Convert.ToUInt16(Font.Size / 2), Font.Size, 0, BackgroundColor);

                        input = input.Remove(input.Length - 1); // Remove the last character of the string
                    }

                    ForceDrawCursor();
                    break;

                case ConsoleKeyEx.Tab:
                    Write('\t');
                    input += new string(' ', 4);
                        
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
                        
                    Write(key.KeyChar.ToString());
                    TryScroll();
                    input += key.KeyChar;

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
    /// Plays a beep sound
    /// </summary>
    /// <param name="freq">Sound frequency</param>
    /// <param name="duration">Sound duration</param>
    public void Beep(uint freq = 800, uint duration = 125) => PCSpeaker.Beep(freq, duration);

    /// <summary>
    /// Try to scroll terminal
    /// </summary>
    private void TryScroll()
    {
        if (CursorLeft >= Width)
        {
            CursorLeft = 0;
            CursorTop++;
        }

        while (CursorTop >= Height)
        {
            Contents.DrawImage(0, -Font.Size, Contents, false);
            Contents.DrawFilledRectangle(0, Contents.Height - Font.Size, Contents.Width, Font.Size, 0, BackgroundColor);
            UpdateRequest?.Invoke();
            CursorTop--;
        }
    }

    /// <summary>
    /// Force draw cursor
    /// </summary>
    private void ForceDrawCursor(bool unDraw = false)
    {
        if (CursorVisible)
        {
            Contents.DrawFilledRectangle(Font.Size / 2 * CursorLeft, Font.Size * CursorTop, Convert.ToUInt16(Font.Size / 2), Font.Size, 0, unDraw ? BackgroundColor : ForegroundColor);
            UpdateRequest?.Invoke();
        }
    }

    /// <summary>
    /// Try to draw cursor
    /// </summary>
    private void TryDrawCursor()
    {
        if (CursorVisible && Cosmos.HAL.RTC.Second != _lastSecond)
        {
            Contents.DrawFilledRectangle(Font.Size / 2 * CursorLeft, Font.Size * CursorTop, Convert.ToUInt16(Font.Size / 2), Font.Size, 0, _cursorState ? ForegroundColor : BackgroundColor);
            UpdateRequest?.Invoke();

            _lastSecond = Cosmos.HAL.RTC.Second;
            _cursorState = !_cursorState;
        }
    }

    #endregion

    #region Fields

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
    public int CursorLeft = 0;

    /// <summary>
    /// Cursor Y coordinate
    /// </summary>
    public int CursorTop = 0;

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
    /// Console contents
    /// </summary>
    public Canvas Contents;

    /// <summary>
    /// Console font
    /// </summary>
    public Font Font;
    
    /// <summary>
    /// Update request action
    /// </summary>
    public Action UpdateRequest;
    
    /// <summary>
    /// Last input
    /// </summary>
    private string _lastInput = string.Empty;

    /// <summary>
    /// Last second
    /// </summary>
    private byte _lastSecond = Cosmos.HAL.RTC.Second;

    /// <summary>
    /// Cursor state
    /// </summary>
    private bool _cursorState = true;

    #endregion
}