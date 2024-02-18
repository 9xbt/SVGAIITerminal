using System;
using System.IO;
using System.Text;
using Cosmos.Core;
using Cosmos.Core.Memory;
using Sys = Cosmos.System;
using IL2CPU.API.Attribs;
using PrismAPI.Hardware.GPU;
using PrismAPI.Graphics;
using PrismAPI.Graphics.Fonts;
using Mirage.TextKit;

namespace TestKernel
{
    public class Kernel : Sys.Kernel
    {
        [ManifestResourceStream(ResourceName = "TestKernel.Resources.DefaultFont.btf")] private static readonly byte[] _rawDefaultFontBtf;
        [ManifestResourceStream(ResourceName = "TestKernel.Resources.Plex.acf")] private static readonly byte[] _rawPlexAcf;
        [ManifestResourceStream(ResourceName = "TestKernel.Resources.Mouse.bmp")] private static readonly byte[] _rawMouseBmp;
        [ManifestResourceStream(ResourceName = "TestKernel.Resources.lipsum.txt")] private static readonly byte[] _rawLipsum;

        private static readonly FontFace VGA = new BtfFontFace(_rawDefaultFontBtf, 16);
        private static readonly FontFace Plex = new AcfFontFace(new MemoryStream(_rawPlexAcf));
        private static readonly Canvas Mouse = Image.FromBitmap(_rawMouseBmp, false);
        private static readonly string Lipsum = Encoding.ASCII.GetString(_rawLipsum);

        private SVGAIITerminal Console;
        private Display Screen;

        protected override void BeforeRun()
        {
            System.Console.Write("Welcome to ");
            System.Console.ForegroundColor = ConsoleColor.Cyan;
            System.Console.Write("SVGAIITerminal Test Kernel");
            System.Console.ForegroundColor = ConsoleColor.White;
            System.Console.WriteLine("!");

            try
            {
                Screen = Display.GetDisplay(1024, 768);
                Screen.DefineCursor(Mouse);

                /*Console = new SVGAIITerminal(Screen.Width, ushort.MaxValue, VGA, Update)
                {
                    IdleRequest = Idle,
                    ScrollRequest = Scroll,
                    FontOffset = 0,
                    ParentHeight = Screen.Height / VGA.GetHeight()
                };*/
                
                Console = new SVGAIITerminal(1024, 768, Plex, Screen)
                {
                    FontOffset = 11
                };

                Console.Clear();
                Console.Beep();
                
                Console.WriteLine("Hello, world!\n");
                
                SuccessLog("Display driver initialized");
                SuccessLog("SVGAIITerminal initialized");

                //Sys.MouseManager.ScreenWidth = Screen.Width;
                //Sys.MouseManager.ScreenHeight = Screen.Height;

                SuccessLog("Mouse driver initialized\n");

                Console.WriteLine("+------------------------------+\n" +
                                  "|  SVGAIITerminal Test Kernel  |\n" +
                                  "|        Version 2.5.3         |\n" +
                                  "| Copyright (c) 2023-2024 xrc2 |\n" +
                                  "+------------------------------+\n");

                for (int i = 0; i < 16; i++)
                {
                    Console.Write("   ", ConsoleColor.Black, (ConsoleColor)i);
                    if (i == 7) Console.WriteLine();
                }

                Console.ResetColor();
                Console.WriteLine("\n");
            }
            catch (Exception ex)
            {
                Screen.IsEnabled = false;
                System.Console.ForegroundColor = ConsoleColor.Red;
                System.Console.Write("[FAIL] ");
                System.Console.ForegroundColor = ConsoleColor.White;
                System.Console.WriteLine("Unhandled exception at bootstrap: " + ex.Message);

                while (true) { }
            }
        }

        protected override void Run()
        {
            try
            {
                Console.Write("$ ");

                string input = Console.ReadLine();
                switch (input.Trim().ToLower())
                {
                    case { } a when a == "help" || a == "?":
                        Console.WriteLine("Available commands: help/?, about, res, mem, fps, cursor, lipsum, gc, echo, reboot, shutdown", ConsoleColor.Gray);
                        break;

                    case "about":
                        Console.WriteLine("SVGAIITerminal TestKernel shell v1.5\n" +
                                          "Copyright (c) 2024 xrc2", ConsoleColor.Gray);
                        break;

                    case "res":
                        Console.WriteLine("Current resolution: " + Screen.Width + "x" + Screen.Height + "@32bpp\n" +
                                          "Terminal resolution: " + Console.Contents.Width + "x" + Console.Contents.Height + "@32bpp",
                                          ConsoleColor.Gray);
                        break;

                    case "mem":
                        Console.WriteLine("Available memory: " + GCImplementation.GetAvailableRAM() + " MB\n" +
                                          "Used memory: " + GCImplementation.GetUsedRAM() / 1e6 + " MB\n" +
                                          "Free memory: " + (GCImplementation.GetAvailableRAM() - (GCImplementation.GetUsedRAM() / 1e6)) + " MB\n" +
                                          "Total memory: " + CPU.GetAmountOfRAM() + " MB\n" +
                                          "Memory used by SVGAIITerminal: " + (Console.Contents.Width * Console.Contents.Height * 4 / 1e6) + " MB\n" +
                                          "Allocated object count in small heap: " + HeapSmall.GetAllocatedObjectCount(),
                                          ConsoleColor.Gray);
                        break;

                    case "fps":
                        Console.WriteLine("Current fps: " + Screen.GetFPS(), ConsoleColor.Gray);
                        break;

                    case { } a when a.StartsWith("cursor "):
                        Console.CursorShape = (CursorShape)int.Parse(input.Substring(7));
                        break;

                    case "lipsum":
                        Console.WriteLine(Lipsum, ConsoleColor.Gray);
                        break;
                    
                    case "gc":
                        Heap.Collect();
                        break;

                    case { } a when a.StartsWith("echo "):
                        Console.WriteLine(input.Substring(5), ConsoleColor.Gray);
                        break;

                    case "clear":
                        Console.Clear();
                        //_termY = 0;
                        break;

                    case "reboot":
                        Sys.Power.Reboot();
                        break;

                    case "shutdown":
                        Sys.Power.Shutdown();
                        break;

                    case "":
                        break;

                    default:
                        Console.WriteLine("Illegal command: " + input, ConsoleColor.Red);
                        break;
                }

                if (GCImplementation.GetAvailableRAM() - (GCImplementation.GetUsedRAM() / 1e6) < 4)
                {
                    Console.WriteLine();
                    ErrorLog("Out of memory!");
                    while (true) { }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                ErrorLog("Unhandled exception at runtime: " + ex);
                while (true) { }
            }
        }

        /*private int _termY;

        private void Update()
        {
            Screen.DrawImage(0, _termY - Console.FontOffset, Console.Contents, false);
            Screen.Update();

            Heap.Collect();
        }

        private void Idle()
        {
            Screen.SetCursor(Sys.MouseManager.X, Sys.MouseManager.Y, true);

            if (Sys.MouseManager.ScrollDelta != 0)
            {
                _termY -= Sys.MouseManager.ScrollDelta * Console.Font.GetHeight() * 3;

                if (_termY > 0) _termY = 0;
                else if (_termY < -(Console.CursorTop * Console.Font.GetHeight())) _termY = -(Console.CursorTop * Console.Font.GetHeight());
                else if (_termY < -(Console.Contents.Height - Screen.Height)) _termY = -(Console.Contents.Height - Screen.Height);

                Screen.Clear(Console.BackgroundColor);
                Update();

                Sys.MouseManager.ResetScrollDelta();
            }
        }

        private void Scroll()
            => _termY = Screen.Height - ((Console.CursorTop + 1) * Console.Font.GetHeight());*/

        private void InfoLog(string msg)
        {
            Console.Write("[INFO] ", ConsoleColor.Blue);
            Console.WriteLine(msg);
        }

        private void SuccessLog(string msg)
        {
            Console.Write("[ OK ] ", ConsoleColor.Green);
            Console.WriteLine(msg);
        }

        private void ErrorLog(string msg)
        {
            Console.Write("[FAIL] ", ConsoleColor.Red);
            Console.WriteLine(msg);
        }
    }
}
