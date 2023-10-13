using System;
using System.Runtime.InteropServices;

namespace FulcrumInjector.FulcrumViewSupport
{
    /// <summary>
    /// Internal static helper class which contains all the routines needed to PInvoke a DLL object
    /// </summary>
    public class FulcrumWin32Invokers
    {
        // Loads a DLL into the memory.
        [DllImport("kernel32.dll", EntryPoint = "LoadLibrary", SetLastError = true)]
        public static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpLibFileName);

        // Gets function address in the memory.
        [DllImport("kernel32.dll", EntryPoint = "GetProcAddress")]
        public static extern IntPtr GetProcAddress(IntPtr hModule, [MarshalAs(UnmanagedType.LPStr)] string lpProcName);

        // Unloads the lib object.
        [DllImport("kernel32.dll", EntryPoint = "FreeLibrary")]
        public static extern bool FreeLibrary(IntPtr hModule);

        // Get the error from the import call
        [DllImport("kernel32.dll")]
        public static extern uint GetLastError();
    }
}
