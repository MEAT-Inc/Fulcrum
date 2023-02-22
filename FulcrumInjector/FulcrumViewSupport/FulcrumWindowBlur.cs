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
    internal class FulcrumWindowBlur
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Private fields which hold our window object and the window interop helper
        private bool _isShowingBlur = false;
        private readonly Window _windowInstance;
        private readonly WindowInteropHelper _windowInterop;

        // Fields holding our window color values and opacity values
        public Color _blurColor;
        private uint _blurOpacity;

        #endregion //Fields

        #region Properties

        // Public facing properties holding the blur opacity value and the blur color to apply
        public Color BlurColor
        {
            get => this._blurColor;
            set
            {
                this._blurColor = value;
                if (this._isShowingBlur) this.ShowBlurEffect();
            }
        }
        public double BlurOpacity
        {
            get => _blurOpacity;
            set
            {
                this._blurOpacity = (uint)value;
                if (this._isShowingBlur) this.ShowBlurEffect(); 
            }
        }

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
        /// Builds a new object ot modify and stores it on this object.
        /// </summary>
        /// <param name="WindowToModify">The window instance to blur when applied</param>
        public FulcrumWindowBlur(Window WindowToModify, double OpacityValue = 75.00, Color ColorToBlur = default, bool ShowBlur = true)
        {
            // Store values for the window to blur and our desired opacity value
            this._windowInstance = WindowToModify;
            this._blurOpacity = (uint)OpacityValue;

            // Check the provided color and store it once validated 
            if (ColorToBlur == default) ColorToBlur = ((App)Application.Current).ThemeConfiguration.CurrentAppStyleModel.PrimaryColor; 
            this._blurColor = ColorToBlur;

            // Spawn and store a new window interop helper to invoke the blur operations
            this._windowInterop = new WindowInteropHelper(_windowInstance);
            if (ShowBlur) { this.ShowBlurEffect(); }
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Turns blur effect on for the desired window.
        /// </summary>
        public void ShowBlurEffect()
        {
            // Build object to blur with
            var BlurAccent = new AccentPolicy();
            var ColorUint = (uint)(((_blurColor.A << 24) | (_blurColor.R << 16) | (_blurColor.G << 8) | _blurColor.B) & 0xffffffffL);
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
            this._isShowingBlur = true;
            SetWindowCompositionAttribute(_windowInterop.Handle, ref CompDataObject);
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
            this._isShowingBlur = false;  
            SetWindowCompositionAttribute(_windowInterop.Handle, ref CompDataObject);
            Marshal.FreeHGlobal(AccentPointer);
        }
    }
}
