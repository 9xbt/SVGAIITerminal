/* This code is licensed under the ekzFreeUse license
 * If a license wasn't included with the program,
 * refer to https://github.com/9xbt/SVGAIITerminal/blob/main/LICENSE.md */

using System;
using Cosmos.System;
using PrismAPI.Hardware.GPU;
using PrismAPI.Graphics;
using PrismAPI.Graphics.Fonts;
using IL2CPU.API.Attribs;

public class SVGAIITerminal
{
    #region Constructors

    /// <summary>
    /// Creates an instance of <see cref="SVGAIITerminal"/>
    /// </summary>
    /// <param name="Width">Terminal width</param>
    /// <param name="Height">Terminal height</param>
    public SVGAIITerminal(int Width, int Height)
    {
        this.Font = new Font(IBM_btf_raw!, 16);
        this.Width = Width / (Font.Size / 2);
        this.Height = Height / Font.Size;
        this.Screen = Display.GetDisplay((ushort)Width, (ushort)Height);
        this.Contents = Screen;
        this.UpdateRequest = () =>
        {
            Screen.DrawImage(0, 0, this.Contents, false);
            Screen.Update();
        };
    }

    /// <summary>
    /// Creates an instance of <see cref="SVGAIITerminal"/>
    /// </summary>
    /// <param name="Width">Terminal width</param>
    /// <param name="Height">Terminal height</param>
    /// <param name="Screen">Screen the terminal renders to</param>
    public SVGAIITerminal(int Width, int Height, Display Screen)
    {
        this.Font = new Font(IBM_btf_raw!, 16);
        this.Width = Width / (Font.Size / 2);
        this.Height = Height / Font.Size;
        this.Contents = Screen;
        this.UpdateRequest = () =>
        {
            Screen.DrawImage(0, 0, this.Contents, false);
            Screen.Update();
        };
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
        this.Font = Font;
        this.Width = Width / (Font.Size / 2);
        this.Height = Height / Font.Size;
        this.Contents = Screen;
        this.UpdateRequest = () =>
        {
            Screen.DrawImage(0, 0, this.Contents, false);
            Screen.Update();
        };
    }

    /// <summary>
    /// Creates an instance of <see cref="SVGAIITerminal"/>
    /// </summary>
    /// <param name="Width">Terminal width</param>
    /// <param name="Height">Terminal height</param>
    /// <param name="Font">Terminal font</param>
    /// <param name="UpdateRequest">Update request action, user can manually manage where and how to render the terminal</param>
    public SVGAIITerminal(int Width, int Height, Font Font, Action UpdateRequest)
    {
        this.Font = Font;
        this.Width = Width / (Font.Size / 2);
        this.Height = Height / Font.Size;
        this.UpdateRequest = UpdateRequest;
        this.Contents = new Canvas((ushort)Width, (ushort)Height);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Clears the terminal
    /// </summary>
    public void Clear()
    {
        Contents.Clear();
        CursorX = 0;
        CursorY = 0;
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
                case '\n':
                    CursorX = 0;
                    CursorY++;
                    break;

                default:
                    Contents.DrawFilledRectangle(Font.Size / 2 * CursorX, Font.Size * CursorY, Convert.ToUInt16(Font.Size / 2), Font.Size, 0, BackgroundColor);
                    Contents.DrawString(Font.Size / 2 * CursorX, Font.Size * CursorY, c.ToString(), Font, color);
                    CursorX++;
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

    public ConsoleKeyInfo ReadKey(bool intercept = true)
    {
        while (true)
        {
            TryDrawCursor();

            if (KeyboardManager.TryReadKey(out var key))
            {
                if (intercept == false)
                {
                    Write(key.KeyChar);
                }

                bool xShift = (key.Modifiers & ConsoleModifiers.Shift) == ConsoleModifiers.Shift;
                bool xAlt = (key.Modifiers & ConsoleModifiers.Alt) == ConsoleModifiers.Alt;
                bool xControl = (key.Modifiers & ConsoleModifiers.Control) == ConsoleModifiers.Control;

                return new ConsoleKeyInfo(key.KeyChar, key.Key.ToConsoleKey(), xShift, xAlt, xControl);
            }
        }
    }

    public string LastInput;

    /// <summary>
    /// Gets input from the user
    /// </summary>
    /// <returns>Text inputted by user</returns>
    public string ReadLine()
    {
        ForceDrawCursor();

        int startX = CursorX, startY = CursorY;
        string returnValue = string.Empty;

        bool reading = true;
        while (reading)
        {
            TryDrawCursor();

            if (KeyboardManager.TryReadKey(out var key))
            {
                switch (key.Key)
                {
                    case ConsoleKeyEx.Enter:
                        Contents.DrawFilledRectangle(Font.Size / 2 * CursorX, Font.Size * CursorY, Convert.ToUInt16(Font.Size / 2), Font.Size, 0, BackgroundColor);
                        CursorX = 0;
                        CursorY++;
                        TryScroll();
                        LastInput = returnValue;
                        reading = false;
                        break;

                    case ConsoleKeyEx.Backspace:
                        if (!(CursorX == startX && CursorY == startY))
                        {
                            if (CursorX == 0)
                            {
                                Contents.DrawFilledRectangle(Font.Size / 2 * CursorX, Font.Size * CursorY, Convert.ToUInt16(Font.Size / 2), Font.Size, 0, BackgroundColor);
                                CursorY--;
                                CursorX = Contents.Width / (Font.Size / 2) - 1;
                                Contents.DrawFilledRectangle(Font.Size / 2 * CursorX, Font.Size * CursorY, Convert.ToUInt16(Font.Size / 2), Font.Size, 0, BackgroundColor);
                            }
                            else
                            {
                                Contents.DrawFilledRectangle(Font.Size / 2 * CursorX, Font.Size * CursorY, Convert.ToUInt16(Font.Size / 2), Font.Size, 0, BackgroundColor);
                                CursorX--;
                                Contents.DrawFilledRectangle(Font.Size / 2 * CursorX, Font.Size * CursorY, Convert.ToUInt16(Font.Size / 2), Font.Size, 0, BackgroundColor);
                            }

                            returnValue = returnValue.Remove(returnValue.Length - 1); // Remove the last character of the string
                        }

                        ForceDrawCursor();
                        break;

                    case ConsoleKeyEx.Tab:
                        Write('\t');
                        returnValue += new string(' ', 4);

                        ForceDrawCursor();
                        break;

                    case ConsoleKeyEx.UpArrow:
                        SetCursorPosition(startX, startY);
                        Write(new string(' ', returnValue.Length));
                        SetCursorPosition(startX, startY);
                        Write(LastInput);
                        returnValue = LastInput;

                        ForceDrawCursor();
                        break;

                    default:
                        if (KeyboardManager.ControlPressed)
                        {
                            if (key.Key == ConsoleKeyEx.L)
                            {
                                Clear();
                                returnValue = string.Empty;
                                reading = false;
                            }
                        }
                        else
                        {
                            Write(key.KeyChar.ToString());
                            TryScroll();
                            returnValue += key.KeyChar;
                        }

                        ForceDrawCursor();
                        break;
                }
            }
        }

        return returnValue;
    }

    /// <summary>
    /// Sets the cursor position
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    public void SetCursorPosition(int x, int y)
    {
        CursorX = x;
        CursorY = y;
    }

    /// <summary>
    /// Gets the cursor position
    /// </summary>
    /// <returns>X and Y cursor coordinates</returns>
    public (int Left, int Top) GetCursorPosition()
    {
        return (CursorX, CursorY);
    }

    /// <summary>
    /// Plays a beep sound
    /// </summary>
    /// <param name="freq">Sound frequency</param>
    /// <param name="duration">Sound duration</param>
    public void Beep(uint freq = 800, uint duration = 125)
    {
        PCSpeaker.Beep(freq, duration);
    }

    /// <summary>
    /// Try to scroll terminal
    /// </summary>
    private void TryScroll()
    {
        if (CursorX >= Width)
        {
            CursorX = 0;
            CursorY++;
        }

        while (CursorY >= Height)
        {
            Contents.DrawImage(0, -Font.Size, Contents, false);
            Contents.DrawFilledRectangle(0, Contents.Height - Font.Size, Contents.Width, Font.Size, 0, BackgroundColor);
            UpdateRequest?.Invoke();
            CursorY--;
        }
    }

    /// <summary>
    /// Force draw cursor
    /// </summary>
    private void ForceDrawCursor()
    {
        Contents.DrawFilledRectangle(Font.Size / 2 * CursorX, Font.Size * CursorY, Convert.ToUInt16(Font.Size / 2), Font.Size, 0, ForegroundColor);
        UpdateRequest?.Invoke();
    }

    /// <summary>
    /// Try to draw cursor
    /// </summary>
    private void TryDrawCursor()
    {
        if (Cosmos.HAL.RTC.Second != lastSecond)
        {
            Contents.DrawFilledRectangle(Font.Size / 2 * CursorX, Font.Size * CursorY, Convert.ToUInt16(Font.Size / 2), Font.Size, 0, cursorState ? ForegroundColor : BackgroundColor);
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
    public int CursorX = 0;

    /// <summary>
    /// Cursor Y coordinate
    /// </summary>
    public int CursorY = 0;

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
    /// Optional full-screen console display
    /// </summary>
    public Display Screen;

    /// <summary>
    /// Console font
    /// </summary>
    public Font Font;

    /// <summary>
    /// Update request action
    /// </summary>
    public Action UpdateRequest;

    /// <summary>
    /// Last second
    /// </summary>
    private byte lastSecond = Cosmos.HAL.RTC.Second;

    /// <summary>
    /// Cursor state
    /// </summary>
    private bool cursorState = true;

    #pragma warning disable CS8618

    /// <summary>
    /// Raw default font
    /// </summary>
    [ManifestResourceStream(ResourceName = "SVGAIITerminal.Fonts.IBM.btf")] static byte[] IBM_btf_raw;

    #pragma warning restore CS8618

    #endregion
}
