// See https://aka.ms/new-console-template for more information
// Console.WriteLine("Hello, World!");

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

internal static class Program
{
    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_ID_TOGGLE = 1;

    private const uint MOD_NONE = 0x0000;
    private const uint MOD_NOREPEAT = 0x4000;
    private const uint VK_F8 = 0x77;

    private const int INPUT_MOUSE = 0;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;

    private static int _intervalMs = 100;
    private static CancellationTokenSource? _clickCancellationTokenSource;
    private static Task? _clickTask;

    public static int Main(string[] args)
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.Error.WriteLine("ERROR: MouseClicker only supports Windows.");
            return 1;
        }

        _intervalMs = ParseIntervalMs(args, 100);

        Console.Title = "MouseClicker";

        Console.WriteLine("MouseClicker");
        Console.WriteLine($"Interval: {_intervalMs} ms");
        Console.WriteLine("F8 = toggle clicking ON/OFF");
        Console.WriteLine("Ctrl+C or close this window = exit");
        Console.WriteLine();

        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            StopClicking();
            UnregisterHotKey(IntPtr.Zero, HOTKEY_ID_TOGGLE);
            PostQuitMessage(0);
        };

        if (!RegisterHotKey(IntPtr.Zero, HOTKEY_ID_TOGGLE, MOD_NONE | MOD_NOREPEAT, VK_F8))
        {
            int errorCode = Marshal.GetLastWin32Error();

            Console.Error.WriteLine("ERROR: Could not register F8 as a global hotkey.");
            Console.Error.WriteLine($"Windows error code: {errorCode}");
            Console.Error.WriteLine("Likely cause: another application already uses F8.");
            return 1;
        }

        try
        {
            while (GetMessage(out MSG message, IntPtr.Zero, 0, 0) > 0)
            {
                if (message.message == WM_HOTKEY && message.wParam == (IntPtr)HOTKEY_ID_TOGGLE)
                {
                    ToggleClicking();
                }

                TranslateMessage(ref message);
                DispatchMessage(ref message);
            }
        }
        finally
        {
            StopClicking();
            UnregisterHotKey(IntPtr.Zero, HOTKEY_ID_TOGGLE);
        }

        return 0;
    }

    private static int ParseIntervalMs(string[] args, int defaultIntervalMs)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], "--interval-ms", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(args[i + 1], out int intervalMs))
                {
                    return Math.Clamp(intervalMs, 1, 60_000);
                }
            }
        }

        return defaultIntervalMs;
    }

    private static void ToggleClicking()
    {
        if (_clickCancellationTokenSource == null)
        {
            StartClicking();
            Console.WriteLine("ON");
        }
        else
        {
            StopClicking();
            Console.WriteLine("OFF");
        }
    }

    private static void StartClicking()
    {
        if (_clickCancellationTokenSource != null)
        {
            return;
        }

        _clickCancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancellationToken = _clickCancellationTokenSource.Token;

        _clickTask = Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                SendLeftClick();

                try
                {
                    await Task.Delay(_intervalMs, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }, cancellationToken);
    }

    private static void StopClicking()
    {
        CancellationTokenSource? cancellationTokenSource = _clickCancellationTokenSource;
        _clickCancellationTokenSource = null;

        if (cancellationTokenSource == null)
        {
            return;
        }

        try
        {
            cancellationTokenSource.Cancel();
            _clickTask?.Wait(250);
        }
        catch
        {
            // Ignore shutdown exceptions.
        }
        finally
        {
            cancellationTokenSource.Dispose();
            _clickTask = null;
        }
    }

    private static void SendLeftClick()
    {
        INPUT[] inputs =
        [
            new INPUT
            {
                type = INPUT_MOUSE,
                U = new InputUnion
                {
                    mi = new MOUSEINPUT
                    {
                        dwFlags = MOUSEEVENTF_LEFTDOWN
                    }
                }
            },
            new INPUT
            {
                type = INPUT_MOUSE,
                U = new InputUnion
                {
                    mi = new MOUSEINPUT
                    {
                        dwFlags = MOUSEEVENTF_LEFTUP
                    }
                }
            }
        ];

        uint sentInputCount = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());

        if (sentInputCount == 0)
        {
            // Intentionally silent.
            // SendInput can fail for protected/elevated windows.
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage(ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern void PostQuitMessage(int nExitCode);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint cInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr hwnd;
        public int message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public POINT pt;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public int type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public MOUSEINPUT mi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
}