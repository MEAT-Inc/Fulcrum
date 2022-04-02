using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace FulcrumInjector.FulcrumViewSupport
{
    /// <summary>
    /// Applies a blur to a window.
    /// </summary>
    public class WindowBlurSetup
    {
        [DllImport("user32.dll")]
        internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        // Values used here for generating blue state.
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

        // Window object
        private Window WindowMain;
        private bool IsShowingBlur = false;
        private WindowInteropHelper BlurWindowHelper;

        // Blur Color Values
        public Color ColorToSet;
        
        // Blur Opacity
        private uint _blurOpacity;
        public double BlurOpacity
        {
            get => _blurOpacity; 
            set 
            { 
                _blurOpacity = (uint)value;
                if (this.IsShowingBlur) { this.ShowBlurEffect(); };
            }
        }

        // ----------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new object ot modify and stores it on this object.
        /// </summary>
        /// <param name="WindowToModify"></param>
        public WindowBlurSetup(Window WindowToModify, double OpacityValue = 75.00, Color ColorToSet = default, bool ShowBlur = false)
        {
            // Store value
            this.WindowMain = WindowToModify;
            this.BlurOpacity = OpacityValue;

            // Check color and store.
            if (ColorToSet == default) ColorToSet = App.ThemeConfiguration.CurrentAppTheme.PrimaryColor; 
            this.ColorToSet = ColorToSet;

            // Store window helper
            this.BlurWindowHelper = new WindowInteropHelper(WindowMain);
            if (ShowBlur) { this.ShowBlurEffect(); }
        }

        /// <summary>
        /// Turns blur effect on for the desired window.
        /// </summary>
		public void ShowBlurEffect()
        {
            // Build object to blur with
            var BlurAccent = new AccentPolicy();
            var ColorUint = (uint)(((ColorToSet.A << 24) | (ColorToSet.R << 16) | (ColorToSet.G << 8) | ColorToSet.B) & 0xffffffffL);
            BlurAccent.AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND;
            BlurAccent.GradientColor = (int)((_blurOpacity << 24) | (ColorUint & 0xFFFFFF));
            var AccentStructSize = Marshal.SizeOf(BlurAccent);

            // Setup pointer for blur.
            var AccentPointer = Marshal.AllocHGlobal(AccentStructSize);
            Marshal.StructureToPtr(BlurAccent, AccentPointer, false);

            // Configure new marshall for pointer struct.
            var CompDataObject = new WindowCompositionAttributeData();
            CompDataObject.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
            CompDataObject.SizeOfData = AccentStructSize;
            CompDataObject.Data = AccentPointer;

            // Set state here.
            IsShowingBlur = true;
            SetWindowCompositionAttribute(BlurWindowHelper.Handle, ref CompDataObject);
            Marshal.FreeHGlobal(AccentPointer);
        }
        /// <summary>
        /// Hides the blur effect
        /// </summary>
        public void HideBlurEffect()
        {
            // Build object to blur with
            var BlurAccent = new AccentPolicy();
            BlurAccent.AccentState = AccentState.ACCENT_DISABLED;
            var AccentStructSize = Marshal.SizeOf(BlurAccent);

            // Setup pointer for blur.
            var AccentPointer = Marshal.AllocHGlobal(AccentStructSize);
            Marshal.StructureToPtr(BlurAccent, AccentPointer, false);

            // Configure new marshall for pointer strcut.
            var CompDataObject = new WindowCompositionAttributeData();
            CompDataObject.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
            CompDataObject.SizeOfData = AccentStructSize;
            CompDataObject.Data = AccentPointer;

            // Set state here.
            IsShowingBlur = false;  
            SetWindowCompositionAttribute(BlurWindowHelper.Handle, ref CompDataObject);
            Marshal.FreeHGlobal(AccentPointer);
        }
    }
}
