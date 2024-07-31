using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

class Program
{
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("ntdll.dll", SetLastError = true)]
    private static extern uint NtSuspendProcess(IntPtr processHandle);

    [DllImport("ntdll.dll", SetLastError = true)]
    private static extern uint NtResumeProcess(IntPtr processHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr hObject);

    private const uint PROCESS_SUSPEND_RESUME = 0x0800;
    private const uint PROCESS_QUERY_INFORMATION = 0x0400;
    private const uint PROCESS_ALL_ACCESS = PROCESS_SUSPEND_RESUME | PROCESS_QUERY_INFORMATION;

    private const int HOTKEY_ID_SUSPEND = 1;
    private const int HOTKEY_ID_RESUME = 2;
    private const uint MOD_NONE = 0x0000;

    private static bool isSuspended = false;
    private static Process targetProcess = null;

    static void Main(string[] args)
    {
        uint suspendKey = GetKeyFromUser("카트라이더를 일시 정지할 키를 입력하세요 :");
        uint resumeKey = GetKeyFromUser("\n카트라이더 일시 정지를 중지할 키를 입력하세요 :");
        RegisterHotKey(IntPtr.Zero, HOTKEY_ID_SUSPEND, MOD_NONE, suspendKey);
        RegisterHotKey(IntPtr.Zero, HOTKEY_ID_RESUME, MOD_NONE, resumeKey);

        while (true)
        {
            NativeMethods.MSG msg;
            while (NativeMethods.GetMessage(out msg, IntPtr.Zero, 0, 0))
            {
                if (msg.message == NativeMethods.WM_HOTKEY)
                {
                    switch (msg.wParam.ToInt32())
                    {
                        case HOTKEY_ID_SUSPEND:
                            if (!isSuspended)
                            {
                                targetProcess = GetKartRiderProcess();
                                if (targetProcess != null)
                                {
                                    SuspendProcess(targetProcess);
                                    isSuspended = true;
                                }
                                else
                                {
                                    Console.WriteLine("KartRider.exe를 찾을 수 없습니다.");
                                }
                            }
                            break;

                        case HOTKEY_ID_RESUME:
                            if (isSuspended && targetProcess != null)
                            {
                                ResumeProcess(targetProcess);
                                isSuspended = false;
                            }
                            break;
                    }
                }
                else if ((Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape))
                {
                    UnregisterHotKey(IntPtr.Zero, HOTKEY_ID_SUSPEND);
                    UnregisterHotKey(IntPtr.Zero, HOTKEY_ID_RESUME);
                    return;
                }
            }

            Thread.Sleep(100);
        }
    }

    private static uint GetKeyFromUser(string prompt)
    {
        Console.WriteLine(prompt);
        ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);
        Console.WriteLine($"설정된 키: {keyInfo.KeyChar}");
        return (uint)keyInfo.Key;
    }

    private static Process GetKartRiderProcess()
    {
        Process[] processes = Process.GetProcessesByName("KartRider");
        if (processes.Length > 0)
        {
            return processes[0];
        }
        return null;
    }

    private static void SuspendProcess(Process process)
    {
        IntPtr processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, process.Id);
        if (processHandle == IntPtr.Zero)
        {
            Console.WriteLine("프로세스를 열 수 없습니다.");
            return;
        }

        uint status = NtSuspendProcess(processHandle);
        if (status != 0)
        {
            Console.WriteLine($"프로세스를 일시 정지하지 못했습니다. 상태: {status}");
        }
        CloseHandle(processHandle);
    }

    private static void ResumeProcess(Process process)
    {
        IntPtr processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, process.Id);
        if (processHandle == IntPtr.Zero)
        {
            Console.WriteLine("프로세스를 열 수 없습니다.");
            return;
        }

        uint status = NtResumeProcess(processHandle);
        if (status != 0)
        {
            Console.WriteLine($"프로세스를 다시 시작하지 못했습니다. 상태: {status}");
        }
        CloseHandle(processHandle);
    }

    private static class NativeMethods
    {
        public const int WM_HOTKEY = 0x0312;

        [StructLayout(LayoutKind.Sequential)]
        public struct MSG
        {
            public IntPtr hwnd;
            public int message;
            public IntPtr wParam;
            public IntPtr lParam;
            public int time;
            public POINT pt;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetMessage(out MSG msg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);
    }
}
