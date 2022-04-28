using System;
using System.Runtime.InteropServices;

namespace FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruImports
{
    /// <summary>
    /// Model object used to contain our loading methods for the C++ DLL
    /// </summary>
    internal class FulcrumWin32Setup
    {
        // Loading method 
        [DllImport("kernel32.dll", EntryPoint = "LoadLibrary", SetLastError = true)]
        internal static extern IntPtr LoadLibrary(
            [MarshalAs(UnmanagedType.LPStr)] string lpLibFileName);

        // Get Method location
        [DllImport("kernel32.dll", EntryPoint = "GetProcAddress", SetLastError = true)]
        internal static extern IntPtr GetProcAddress(IntPtr hModule, [MarshalAs(UnmanagedType.LPStr)] string lpProcName);

        // Free the library/unload it
        [DllImport("kernel32.dll", EntryPoint = "FreeLibrary", SetLastError = true)]
        internal static extern bool FreeLibrary(IntPtr hModule);

        // Get the error from the import call
        [DllImport("kernel32.dll")]
        internal static extern uint GetLastError();

        // ----------------------------------------------------------------------------------------
    }
}
