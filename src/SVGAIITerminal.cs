/* This code is licensed under the ekzFreeUse license
 * If a license wasn't included with the program,
 * refer to https://github.com/9xbt/SVGAIITerminal/blob/main/LICENSE.md */

using Cosmos.System;
using PrismAPI.Hardware.GPU;
using PrismAPI.Graphics;
using PrismAPI.Graphics.Fonts;

public class SVGAIITerminal
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
        Canvas = screen;
        UpdateRequest = () =>
        {
            screen.DrawImage(0, 0, this.Canvas, false);
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
        Canvas = screen;
        UpdateRequest = () =>
        {
            screen.DrawImage(0, 0, this.Canvas, false);
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
        Canvas = new Canvas((ushort)Width, (ushort)Height);
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
        get => Canvas[x, y];
        set
        {
            Canvas[x, y] = value;
        }
    }
    
    #endregion

    #region Methods

    /// <summary>
    /// Clears the terminal
    /// </summary>
    public void Clear()
    {
        Canvas.Clear();
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
                    Canvas.DrawFilledRectangle(Font.Size / 2 * CursorLeft, Font.Size * CursorTop, Convert.ToUInt16(Font.Size / 2), Font.Size, 0, BackgroundColor);
                    Canvas.DrawString(Font.Size / 2 * CursorLeft, Font.Size * CursorTop, c.ToString(), Font, color);
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

    // TODO: fix this shit
    public string LastInput = string.Empty;

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

            if (KeyboardManager.TryReadKey(out var key))
            {
                switch (key.Key)
                {
                    case ConsoleKeyEx.Enter:
                        Canvas.DrawFilledRectangle(Font.Size / 2 * CursorLeft, Font.Size * CursorTop, Convert.ToUInt16(Font.Size / 2), Font.Size, 0, BackgroundColor);
                        CursorLeft = 0;
                        CursorTop++;
                        TryScroll();
                        // TODO: add last input handler
                        return input;

                    case ConsoleKeyEx.Backspace:
                        if (!(CursorLeft == startX && CursorTop == startY))
                        {
                            Canvas.DrawFilledRectangle(Font.Size / 2 * CursorLeft, Font.Size * CursorTop, Convert.ToUInt16(Font.Size / 2), Font.Size, 0, BackgroundColor);
                            CursorTop -= CursorLeft == 0 ? 1 : 0;
                            CursorLeft -= CursorLeft == 0 ? Width - 1 : 1;
                            Canvas.DrawFilledRectangle(Font.Size / 2 * CursorLeft, Font.Size * CursorTop, Convert.ToUInt16(Font.Size / 2), Font.Size, 0, BackgroundColor);

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
                        SetCursorPosition(startX, startY);
                        Write(new string(' ', input.Length));
                        SetCursorPosition(startX, startY);
                        Write(LastInput);
                        input = LastInput;

                        ForceDrawCursor();
                        break;
                    
                    // TODO: DownArrow
                    // TODO: LeftArrow
                    // TODO: RightArrow
                    // TODO: L

                    default:
                        if (KeyboardManager.ControlPressed)
                        {
                            if (key.Key == ConsoleKeyEx.L)
                            {
                                Clear();
                                // TODO: add last input handler
                                return string.Empty;
                            }
                        }
                        else
                        {
                            Write(key.KeyChar.ToString());
                            TryScroll();
                            input += key.KeyChar;
                        }

                        ForceDrawCursor();
                        break;
                }
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
            Canvas.DrawImage(0, -Font.Size, Canvas, false);
            Canvas.DrawFilledRectangle(0, Canvas.Height - Font.Size, Canvas.Width, Font.Size, 0, BackgroundColor);
            UpdateRequest?.Invoke();
            CursorTop--;
        }
    }

    /// <summary>
    /// Force draw cursor
    /// </summary>
    private void ForceDrawCursor()
    {
        if (CursorVisible)
        {
            Canvas.DrawFilledRectangle(Font.Size / 2 * CursorLeft, Font.Size * CursorTop, Convert.ToUInt16(Font.Size / 2), Font.Size, 0, ForegroundColor);
            UpdateRequest?.Invoke();
        }
    }

    /// <summary>
    /// Try to draw cursor
    /// </summary>
    private void TryDrawCursor()
    {
        if (CursorVisible && Cosmos.HAL.RTC.Second != lastSecond)
        {
            Canvas.DrawFilledRectangle(Font.Size / 2 * CursorLeft, Font.Size * CursorTop, Convert.ToUInt16(Font.Size / 2), Font.Size, 0, cursorState ? ForegroundColor : BackgroundColor);
            UpdateRequest?.Invoke();

            lastSecond = Cosmos.HAL.RTC.Second;
            cursorState = !cursorState;
        }
    }

    #endregion

    #region Fields

    /// <summary>
    /// Terminal width
    /// </summary>
    public int Width;

    /// <summary>
    /// Terminal height
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
    /// Console canvas
    /// </summary>
    public Canvas Canvas;

    /// <summary>
    /// Console font
    /// </summary>
    public Font Font;

    /// <summary>
    /// Update request action
    /// </summary>
    public Action UpdateRequest;
    
    /// <summary>
    /// <see cref="SVGAIITerminal"/> version
    /// </summary>
    public const string Version = "v2.0.0";

    /// <summary>
    /// Last second
    /// </summary>
    private byte lastSecond = Cosmos.HAL.RTC.Second;

    /// <summary>
    /// Cursor state
    /// </summary>
    private bool cursorState = true;

    #endregion
}
