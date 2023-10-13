using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace FulcrumInjector.FulcrumViewSupport
{
    /// <summary>
    /// Applies a blur to a window
    /// </summary>
    public static class FulcrumWindowBlur
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields
        #endregion //Fields

        #region Properties
        #endregion //Properties

        #region Structs and Classes

        [DllImport("user32.dll")]
        internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);
        
        // Structures and enumerations used to configure shades and gradients
        internal enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_INVALID_STATE = 4
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }
        internal enum WindowCompositionAttribute
        {
            // ...
            WCA_ACCENT_POLICY = 19
            // ...
        }

        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Turns blur effect on for the desired window.
        /// </summary>
        /// <param name="WindowToModify">The window to blur the background on</param>
        /// <param name="OpacityValue">The opacity to set our blur value to</param>
        /// <param name="ColorToBlur">The color to use for the blurring style</param>
        public static void ShowBlurEffect(Window WindowToModify, double OpacityValue = 75.00, Color ColorToBlur = default)
        {
            // Make sure the window is valid, the opacity is set, and check our default color for blurring
            if (WindowToModify == null || OpacityValue == 0.00) return; 
            if (ColorToBlur == default) ColorToBlur = ((App)Application.Current).ThemeConfiguration.CurrentAppStyleModel.PrimaryColor;
            var ColorUint = (uint)(((ColorToBlur.A << 24) | (ColorToBlur.R << 16) | (ColorToBlur.G << 8) | ColorToBlur.B) & 0xffffffffL);

            // Build object to blur with
            var BlurAccent = new AccentPolicy
            {
                AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND,
                GradientColor = (int)(((uint)OpacityValue << 24) | (ColorUint & 0xFFFFFF))
            };

            // Setup pointer for blur and Marshal out the structure needed
            var AccentStructSize = Marshal.SizeOf(BlurAccent);
            var AccentPointer = Marshal.AllocHGlobal(AccentStructSize);
            Marshal.StructureToPtr(BlurAccent, AccentPointer, false);

            // Configure new marshall for pointer struct.
            var CompDataObject = new WindowCompositionAttributeData
            {
                Data = AccentPointer,
                SizeOfData = AccentStructSize,
                Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
            };

            // Setup a new interop helper and store the new blur state here.
            var InteropHelper = new WindowInteropHelper(WindowToModify);
            SetWindowCompositionAttribute(InteropHelper.Handle, ref CompDataObject);
            Marshal.FreeHGlobal(AccentPointer);
        }
        /// <summary>
        /// Hides the blur effect on the given window instance
        /// </summary>
        /// <param name="WindowToModify">The window to remove our blur effect from</param>
        public static void HideBlurEffect(Window WindowToModify)
        {
            // Make sure the given window instance is not null
            if (WindowToModify == null) return;

            // Build object to blur with
            var BlurAccent = new AccentPolicy { AccentState = AccentState.ACCENT_DISABLED };

            // Setup pointer for blur and marshal out the needed structures
            var AccentStructSize = Marshal.SizeOf(BlurAccent);
            var AccentPointer = Marshal.AllocHGlobal(AccentStructSize);
            Marshal.StructureToPtr(BlurAccent, AccentPointer, false);

            // Configure new marshall for pointer structures.
            var CompDataObject = new WindowCompositionAttributeData
            {
                Data = AccentPointer,
                SizeOfData = AccentStructSize,
                Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
            };

            // Setup a new interop helper and store the new blur state here.
            var InteropHelper = new WindowInteropHelper(WindowToModify);
            SetWindowCompositionAttribute(InteropHelper.Handle, ref CompDataObject);
            Marshal.FreeHGlobal(AccentPointer);
        }
    }
}
