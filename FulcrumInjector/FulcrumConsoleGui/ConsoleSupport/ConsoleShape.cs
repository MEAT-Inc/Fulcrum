using System;
using System.Drawing;
using FulcrumInjector.FulcrumJsonHelpers;
using NLog.Fluent;
using SharpLogger.LoggerSupport;

// Static broker for logging.
using static SharpLogger.LogBroker;

namespace FulcrumInjector.FulcrumConsoleGui.ConsoleSupport
{
    /// <summary>
    /// Used to boot a new console gui session and configure console layouts 
    /// </summary>
    public static class ConsoleShapeSetup
    {
        // Display Configuration
        public static ConsoleScreenConfiguration ScreenLayout;

        // Public size values and info for console setup. On change, the console is modified.
        public static int[] ConsolePixelSizes
        {
            // Set value and recenter.
            get
            {
                // Get current width and return
                var ConsoleRect = new Rectangle();
                ConsoleWin32.GetWindowRect(ConsoleWin32.GetConsoleWindow(), ref ConsoleRect);
                return new int[2] { ConsoleRect.Width, ConsoleRect.Height };
            }
        }

        // Width and height values stored.
        public static int ConsolePixelWidth => ConsolePixelSizes[0];
        public static int ConsolePixelHeight => ConsolePixelSizes[1];

        // Force Console On Top Value
        public static readonly bool ForceOnTop = ValueLoaders.GetConfigValue<bool>("FulcrumConsole.ForceConsoleOnTop");

        // ----------------------------------------------------------------------------------------------------

        /// <summary>
        /// Setup new console values.
        /// </summary>
        /// <param name="WidthValue"></param>
        /// <param name="HeightValue"></param>
        public static Rectangle InitializeConsole(int NewWidth = 680, int NewHeight = 420)
        {
            // Build display info and set new values.
            ScreenLayout = new ConsoleScreenConfiguration();

            // Find current display size and make sure we're not larger then the current output.
            var OverSizeScaleBack = ValueLoaders.GetConfigValue<int>("FulcrumConsole.OverSizeScaleBack");
            if (ScreenLayout.WorkingDisplayBounds.Width <= (NewWidth - OverSizeScaleBack))
            {
                NewWidth = ScreenLayout.WorkingDisplayBounds.Width - OverSizeScaleBack;
                Logger.WriteLog($"OVERSIZE SCALEBACK IS SET TO {OverSizeScaleBack}", LogType.TraceLog);
                Logger.WriteLog("WIDTH OF NEW CONSOLE WINDOW WAS LARGER THAN THE NEW WIDTH VALUE! SHRINKING IT TO FIT!", LogType.WarnLog);
            }
            if (ScreenLayout.WorkingDisplayBounds.Height <= (NewHeight - OverSizeScaleBack))
            {
                NewHeight = ScreenLayout.WorkingDisplayBounds.Height - OverSizeScaleBack;
                Logger.WriteLog($"OVERSIZE SCALEBACK IS SET TO {OverSizeScaleBack}", LogType.TraceLog);
                Logger.WriteLog("HEIGHT OF NEW CONSOLE WINDOW WAS LARGER THAN THE NEW WIDTH VALUE! SHRINKING IT TO FIT!", LogType.WarnLog);
            }

            // Build new console shape output
            var GeneratedShape = GenerateConsoleShape(NewWidth, NewHeight);
            Logger.WriteLog("CONFIGURING CONSOLE FOR STATIC LOCATION NOW...");
            Logger.WriteLog($"SETUP NEW CONSOLE SHAPE TO LOCK INTO: {GeneratedShape}", LogType.DebugLog);

            // Change the size of the console now.
            ConsoleWin32.SetWindowPos(
                ConsoleWin32.GetConsoleWindow(), IntPtr.Zero,
                GeneratedShape.X, GeneratedShape.Y,
                GeneratedShape.Width, GeneratedShape.Height,
                ForceOnTop ?
                    ConsoleWin32.SetWindowPosFlags.ShowWindow : // If forced on, show it.
                    default                                     // If not default value.
            );

            // Return the built Rectangle.
            Logger.WriteLog("CONSOLE LOCATION UPDATED DURING INITIALIZATION OF SHAPES OK!");
            return GeneratedShape;
        }


        /// <summary>
        /// Runs the center console process again.
        /// </summary>
        public static Rectangle GenerateConsoleShape(int PixelWidth = -1, int PixelHeight = -1)
        {
            // Move to new point. And get current bounding location.
            var NewConsoleBoundingBox = GetConsoleCenterBounds();

            // Check values
            if (PixelWidth == -1) PixelWidth = ConsolePixelWidth;
            if (PixelHeight == -1) PixelHeight = ConsolePixelHeight;

            // MAke sure within bounds
            if (PixelWidth >= ScreenLayout.WorkingDisplayBounds.Width)
                PixelWidth = ScreenLayout.WorkingDisplayBounds.Width - 300;
            if (PixelHeight >= ScreenLayout.WorkingDisplayBounds.Height)
                PixelHeight = ScreenLayout.WorkingDisplayBounds.Height - 300;

            // Assign new values.
            NewConsoleBoundingBox.Width = PixelWidth;
            NewConsoleBoundingBox.Height = PixelHeight;
            NewConsoleBoundingBox.X = (ScreenLayout.WorkingDisplayBounds.Width - PixelWidth) / 2;
            NewConsoleBoundingBox.Y = (ScreenLayout.WorkingDisplayBounds.Height - PixelHeight) / 2;
            NewConsoleBoundingBox.Offset(ScreenLayout.WorkingDisplayBounds.Location);

            // Return the shape.
            return NewConsoleBoundingBox;
        }

        /// <summary>
        /// Gets the center of the display for the console window and returns it out.
        /// </summary>
        /// <returns>Rectangle of the current console window size when centered.</returns>
        private static Rectangle GetConsoleCenterBounds()
        {
            // Move the console Window to the top left of the working display now.
            ScreenLayout = new ConsoleScreenConfiguration();
            ConsoleWin32.SetWindowPos(
                ConsoleWin32.GetConsoleWindow(), IntPtr.Zero,
                ScreenLayout.WorkingDisplayBounds.Location.X,
                ScreenLayout.WorkingDisplayBounds.Location.Y,
                ConsolePixelWidth, ConsolePixelHeight,
                ForceOnTop ?
                    ConsoleWin32.SetWindowPosFlags.ShowWindow : // If forced on, show it.
                    default                                     // If not default value.
            );

            // Get the current box, and compare to the middle.
            Rectangle ConsoleRectangle = new Rectangle();
            ConsoleWin32.GetWindowRect(ConsoleWin32.GetConsoleWindow(), ref ConsoleRectangle);
            int BaseX = ScreenLayout.WorkingDisplayBounds.Location.X < 0 ? ScreenLayout.WorkingDisplayBounds.Location.X : 0;
            var NewConsoleBoundingBox = new Rectangle(new Point(BaseX, 0), ScreenLayout.WorkingDisplayBounds.Size);

            // Return new box value.
            return NewConsoleBoundingBox;
        }
    }
}
