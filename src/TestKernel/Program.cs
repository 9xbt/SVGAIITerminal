namespace TestKernel;

using System;
using System.IO;
using System.Text;
using Cosmos.Core;
using Cosmos.Core.Memory;
using Cosmos.System;
using IL2CPU.API.Attribs;
using PrismAPI.Hardware.GPU;
using PrismAPI.Graphics;
using SVGAIITerminal;
using SVGAIITerminal.TextKit;
using SVGAIITerminal = SVGAIITerminal.SVGAIITerminal;

public class Program : Kernel
{
    [ManifestResourceStream(ResourceName = "TestKernel.Resources.DefaultFont.btf")] private static readonly byte[] _rawDefaultFontBtf;
    [ManifestResourceStream(ResourceName = "TestKernel.Resources.Plex.acf")] private static readonly byte[] _rawPlexAcf;
    [ManifestResourceStream(ResourceName = "TestKernel.Resources.Mouse.bmp")] private static readonly byte[] _rawMouseBmp;
    //[ManifestResourceStream(ResourceName = "TestKernel.Resources.lipsum.txt")] private static readonly byte[] _rawLipsum;

    private const string Lipsum = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Duis eu iaculis odio. Duis nec interdum nisl, vel accumsan dolor. Curabitur ornare imperdiet justo, et interdum mauris condimentum at. Sed malesuada accumsan nisi, non bibendum orci molestie et. Integer vulputate, augue eget pellentesque convallis, turpis lacus placerat diam, vel feugiat dolor dolor quis sapien. Quisque imperdiet nisi non purus viverra cursus. Vivamus odio libero, porttitor ac libero vitae, pretium cursus odio. Integer sed mi non dolor dictum placerat a et ligula. Mauris sit amet justo faucibus, maximus odio ut, molestie ante. Mauris vulputate libero et neque semper, sed accumsan sem convallis. Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos. Nam vel hendrerit ante. Suspendisse potenti. Cras ut venenatis ex, vitae convallis metus. Ut a ex ac diam vestibulum tincidunt eu in felis. Praesent hendrerit eu felis at accumsan.\n\nVivamus ipsum nunc, condimentum quis maximus nec, maximus quis nulla. Nulla rutrum eget eros vel vulputate. Donec tristique diam vel consectetur mollis. Nullam sit amet tellus finibus, dignissim nibh sit amet, dignissim arcu. Nulla facilisi. Suspendisse ut orci diam. Donec tincidunt feugiat nisl ac hendrerit. Vivamus semper ipsum eget tempor commodo. Vestibulum consequat dolor a enim posuere luctus. Curabitur pharetra vitae lacus a gravida.\n\nNunc ac diam vitae tellus ullamcorper ultricies a et mauris. Ut suscipit est arcu, nec cursus est auctor non. Donec ultricies venenatis metus. Integer vehicula scelerisque dignissim. Sed lobortis ornare nisi, ut malesuada nisl lacinia vitae. Suspendisse vestibulum tempus massa in condimentum. Aliquam erat volutpat. Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos. Aliquam erat volutpat. Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos. Sed congue sollicitudin libero eget blandit. Nunc gravida, nibh ac dignissim elementum, nulla lectus interdum justo, ullamcorper blandit metus nisl quis augue. Nullam libero est, luctus et sem ac, laoreet rutrum urna.\n\nFusce pellentesque cursus ultricies. Pellentesque neque sapien, pulvinar egestas sapien sed, vulputate venenatis elit. Curabitur suscipit eleifend metus, id elementum eros lobortis sed. Orci varius natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus. Quisque ex justo, accumsan sed metus pretium, facilisis porta metus. Ut mollis volutpat eros vitae feugiat. Mauris sed augue eget metus pellentesque cursus a sed dolor. Proin efficitur nunc vitae porttitor pharetra. In hac habitasse platea dictumst. Quisque enim libero, congue laoreet rutrum eu, tincidunt sit amet massa. Maecenas convallis pulvinar urna non pellentesque. In eleifend lacus ex, eget efficitur mauris hendrerit ut. Proin luctus vel sem condimentum mattis. Integer varius velit non dictum pharetra.\n\nUt ornare accumsan commodo. Vivamus at mollis nunc. Donec ante eros, posuere quis iaculis a, feugiat ullamcorper nibh. Aenean id dui a lectus tincidunt varius non in leo. Donec ultricies sapien eget nisi blandit pellentesque. Aenean fringilla lectus eu rhoncus lobortis. Maecenas lorem lorem, iaculis nec viverra id, ornare eget sapien. Suspendisse ut ipsum condimentum, porta turpis non, laoreet massa. Nullam posuere in lacus et pharetra. Morbi id lectus semper, rutrum odio quis, accumsan arcu. In hac habitasse platea dictumst. Etiam volutpat diam sit amet nibh hendrerit sagittis. Nam finibus varius felis, eget eleifend elit condimentum in. Sed a ex quis lectus gravida sodales sit amet a neque. Ut luctus a mi at accumsan. Maecenas tristique lectus tellus, eget tempus orci vehicula sed.\n\nFusce dolor leo, finibus at venenatis vel, imperdiet eget urna. Cras et neque suscipit, suscipit elit in, pretium metus. Ut a cursus ex. Sed eleifend tortor quis lectus sollicitudin, varius rhoncus dolor venenatis. Nulla facilisi. Maecenas porttitor metus risus, a lobortis sem mollis quis. Vestibulum eleifend finibus purus in malesuada. Pellentesque consequat erat non sem ultricies, vitae cursus purus laoreet. Praesent venenatis posuere nisi a porttitor. Vivamus lobortis magna at ex vestibulum euismod eget a eros. Nulla condimentum nibh magna, vehicula maximus libero cursus non. Proin justo felis, tincidunt non diam quis, porta pharetra elit. Cras sed nibh lacinia, tincidunt ipsum at, finibus diam. Maecenas dignissim tristique ipsum, sed scelerisque nisl tristique ut.\n\nFusce justo lorem, efficitur in tempus vitae, fermentum at leo. In pharetra, justo vel condimentum dapibus, justo nisi commodo lectus, quis dignissim diam turpis et sem. Aenean sed hendrerit erat. Nam varius nunc efficitur, vehicula dolor eget, egestas metus. Vivamus tellus massa, commodo eu pretium porttitor, euismod eget purus. Vestibulum nec rutrum libero, pretium placerat diam. Nam a velit eu mauris molestie dignissim. Duis id purus id libero elementum pretium. Donec a fringilla erat, ac dapibus orci. Vestibulum dignissim non sem at luctus. Nam dignissim est a erat ornare, pulvinar egestas elit auctor. Integer ut vulputate dui, sed faucibus orci. Integer vel posuere elit, ac ultrices neque.\n\nNam nec fermentum nibh, ut gravida ligula. Quisque bibendum pellentesque nisi, non efficitur ipsum molestie vel. Duis commodo, quam interdum mollis molestie, dui lacus interdum libero, non fringilla lectus magna quis odio. Mauris porttitor fermentum massa, eget fermentum tortor imperdiet id. Curabitur id elit tincidunt, dignissim diam eget, congue mauris. Ut vel dolor augue. Sed luctus vel turpis ac lobortis. Sed nec lorem ornare, consectetur arcu id, hendrerit sapien. Cras et aliquet nibh. Vivamus vel erat augue. Nulla ut cursus nibh. Integer tempus dignissim justo in ultrices. Etiam felis ipsum, hendrerit non blandit in, rutrum et odio. Cras lacinia lobortis sem, sit amet porta diam luctus non.\n\nPellentesque finibus lorem id lectus elementum pharetra. Proin pretium, orci non varius consequat, eros nibh elementum velit, non sagittis ipsum enim a augue. Praesent at est dolor. Aliquam tempus placerat vulputate. Maecenas quis interdum elit, sed ultricies ligula. Fusce tincidunt pulvinar felis ac rutrum. Curabitur maximus, massa vel suscipit luctus, lectus nibh lacinia nisl, nec ullamcorper felis risus vitae turpis. Aenean efficitur, neque at convallis aliquam, enim nisi tempor nulla, at gravida lacus augue ac eros. Quisque pretium gravida dui, et imperdiet ante sagittis eget. Nunc vestibulum dui augue, in vulputate libero finibus a. Aliquam eget sapien quis velit auctor sagittis vel vel sem. Nam laoreet facilisis nunc, nec rhoncus tellus condimentum vitae.\n\nVivamus fermentum ornare lectus in maximus. Nam nisl justo, sagittis vitae tristique non, bibendum quis tellus. Ut arcu mi, tincidunt ac venenatis non, convallis id nisl. Phasellus et blandit nisi. Integer sit amet erat id nulla tempor viverra. Nam nec egestas dolor. Praesent malesuada ultrices interdum. Vivamus placerat pellentesque hendrerit. Pellentesque odio leo, ullamcorper a iaculis at, placerat vitae nunc. Proin et ipsum vel lacus semper euismod. Curabitur congue nulla malesuada risus feugiat porta. Donec ultrices ultricies orci vel venenatis.\n\nIn egestas, libero id ultrices ultrices, lectus sem accumsan nisi, sodales molestie arcu nisl id enim. Nunc aliquam tristique diam nec ultrices. Vestibulum sed luctus magna. Sed sollicitudin massa accumsan, auctor est non, sagittis risus. Praesent urna sem, auctor sit amet velit et, dictum molestie est. Integer finibus enim lorem, vitae volutpat tortor finibus et. Nunc sollicitudin magna massa, a feugiat justo tincidunt ac. Proin non malesuada velit. Aliquam quis purus tempor, pharetra ante a, vehicula eros. Nunc quis augue justo.\n\nPhasellus non neque odio. Morbi eu ante in elit eleifend porta cursus vitae justo. Praesent in quam posuere, pulvinar lectus nec, tincidunt metus. Nunc condimentum sed risus ut vestibulum. Donec ac magna eget orci consequat venenatis quis vitae mi. Fusce dapibus aliquet rhoncus. In varius nulla purus, nec blandit nunc cursus nec. Proin posuere eros non tortor sodales, vitae luctus sapien volutpat. In hac habitasse platea dictumst. Etiam vulputate vehicula viverra. Donec vehicula nec nunc nec pulvinar. Nam congue nisl non suscipit efficitur. Proin ornare ante id vestibulum dictum.\n\nMauris bibendum non erat ut aliquet. Ut gravida quis tortor commodo vestibulum. Mauris mollis imperdiet metus, id dapibus quam sollicitudin accumsan. Fusce id tempus libero, et congue enim. Cras euismod quis diam nec consequat. Aliquam erat volutpat. Curabitur luctus, justo vel feugiat sodales, nunc tellus accumsan purus, eget finibus neque risus sed mauris. Fusce ac cursus enim, nec dictum ipsum. Donec elementum vel lorem quis scelerisque. Nunc nec volutpat dolor. Nam porta quam pulvinar lacus dictum, nec iaculis purus faucibus. Quisque in est non urna hendrerit commodo ut a erat.\n\nDonec semper ultrices nisi, nec aliquet eros iaculis sit amet. Etiam lobortis dui eu diam imperdiet, a imperdiet tellus facilisis. Vestibulum egestas dignissim neque sit amet ultricies. Nunc rhoncus augue lacus, eget pretium neque hendrerit et. Nullam justo metus, blandit placerat lacus a, tincidunt dapibus nisi. Fusce finibus faucibus dolor, sit amet faucibus ligula lobortis non. Cras non tortor leo. Duis imperdiet ligula eget ultrices congue.\n\nUt sem metus, malesuada ut orci et, eleifend suscipit mi. Vivamus nisl lorem, consequat id feugiat ut, tristique at odio. Mauris at diam tincidunt, mattis ex suscipit, consectetur urna. Duis ut lacus luctus, lacinia nibh ut, vestibulum magna. Curabitur eu facilisis ante. Vestibulum aliquam mattis mollis. Phasellus congue turpis enim. Aliquam vestibulum, arcu non tristique sodales, diam velit bibendum lorem, non laoreet purus nisi id lectus. Praesent eget nulla pretium, sollicitudin tortor sed, finibus risus. Nunc accumsan ut dolor id rhoncus. Curabitur eros leo, luctus id felis vitae, facilisis tincidunt id.";
    private static readonly FontFace IBM = new BtfFontFace(_rawDefaultFontBtf, 16);
    private static readonly FontFace Plex = new AcfFontFace(new MemoryStream(_rawPlexAcf));
    private static readonly Canvas Mouse = Image.FromBitmap(_rawMouseBmp, false);

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

            Console = new SVGAIITerminal(Screen.Width, Screen.Height, Plex, Update)
            {
                IdleRequest = Idle,
                ScrollRequest = Scroll,
                ParentHeight = Screen.Height / Plex.GetHeight()
            };

            SuccessLog("Display driver initialized");
            SuccessLog("SVGAIITerminal initialized");

            MouseManager.ScreenWidth = Screen.Width;
            MouseManager.ScreenHeight = Screen.Height;

            SuccessLog("Mouse driver initialized\n");

            Console.WriteLine("+------------------------------+\n" +
                              "|  SVGAIITerminal Test Kernel  |\n" +
                              "|        Version 2.6.2         |\n" +
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
                case "help" or "?":
                    Console.WriteLine("Available commands: help/?, about, res, mem, fps, cursor, lipsum, gc, echo, font, reboot, shutdown",
                                    ConsoleColor.Gray);
                    break;

                case "about":
                    Console.WriteLine("SVGAIITerminal TestKernel shell v1.5\n" +
                                      "Copyright (c) 2024 xrc2", ConsoleColor.Gray);
                    break;

                case "res":
                    Console.WriteLine("Current resolution: " + Screen.Width + "x" + Screen.Height + "@32bpp\n" +
                                      "Terminal resolution: " + Console.Contents.Width + "x" + Console.Contents.Height +
                                      "@32bpp",
                        ConsoleColor.Gray);
                    break;

                case "mem":
                    Console.WriteLine("Available memory: " + GCImplementation.GetAvailableRAM() + " MB\n" +
                                      "Used memory: " + GCImplementation.GetUsedRAM() / 1e6 + " MB\n" +
                                      "Free memory: " +
                                      (GCImplementation.GetAvailableRAM() - GCImplementation.GetUsedRAM() / 1e6) +
                                      " MB\n" +
                                      "Total memory: " + CPU.GetAmountOfRAM() + " MB\n" +
                                      "Memory used by SVGAIITerminal: " +
                                      Console.Contents.Width * Console.Contents.Height * 4 / 1e6 + " MB\n" +
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
                    string lipsum = Lipsum + " " + Lipsum + "\n\nThis is a test.";
                    Console.WriteLine(lipsum, ConsoleColor.Gray);
                    break;

                case "gc":
                    Heap.Collect();
                    break;

                case { } a when a.StartsWith("echo "):
                    Console.WriteLine(input.Substring(5), ConsoleColor.Gray);
                    break;

                case "clear":
                    Console.Clear();
                    _termY = 0;
                    break;
                
                case "font":
                    Console.WriteLine("Font style: " + Console.Font.GetStyleName() +
                                      "Font family: " + Console.Font.GetFamilyName() +"" +
                                      "Font size: " + Console.Font.GetHeight(),
                                      ConsoleColor.Gray);
                    break;

                case "reboot":
                    Power.Reboot();
                    break;

                case "shutdown":
                    Power.Shutdown();
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

    private int _termY;

    private void Update()
    {
        Screen.DrawImage(0, _termY, Console.Contents, false);
        Screen.Update();

        Heap.Collect();
    }

    private void Idle()
    {
        Screen.SetCursor(MouseManager.X, MouseManager.Y, true);

        if (MouseManager.ScrollDelta != 0)
        {
            _termY -= MouseManager.ScrollDelta * Console.Font.GetHeight() * 3;

            if (_termY > 0) _termY = 0;
            else if (_termY < -(Console.CursorTop * Console.Font.GetHeight())) _termY = -(Console.CursorTop * Console.Font.GetHeight());
            else if (_termY < -(Console.Contents.Height - Screen.Height)) _termY = -(Console.Contents.Height - Screen.Height);

            Screen.Clear(Console.BackgroundColor);
            Update();

            MouseManager.ResetScrollDelta();
        }
    }

    private void Scroll()
        => _termY = Screen.Height - ((Console.CursorTop + 1) * Console.Font.GetHeight());

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