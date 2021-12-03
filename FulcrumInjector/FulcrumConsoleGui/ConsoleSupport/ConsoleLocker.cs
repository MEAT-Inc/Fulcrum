using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumJsonHelpers;
using SharpLogger.LoggerSupport;

// Static logger reference
using static SharpLogger.LogBroker;

namespace FulcrumInjector.FulcrumConsoleGui.ConsoleSupport
{
    /// <summary>
    /// Moves a specified window around to a desired location
    /// </summary>
    public class ConsoleLocker
    {
        // Window Pointer and screen configurations
        public IntPtr WindowPointer;                        // Handle/pointer
        public Rectangle LockToThisShape;                   // Shape to set
        public ConsoleScreenConfiguration ScreenLayout;     // Screen config

        // Cancelation token for locking config.
        private CancellationTokenSource TokenSource;
        public static readonly bool ForceOnTop = ValueLoaders.GetConfigValue<bool>("FulcrumConsole.ForceConsoleOnTop");

        // -----------------------------------------------------------------------------

        /// <summary>
        /// Builds new window relocator.
        /// </summary>
        /// <param name="WindowPtr"></param>
        public ConsoleLocker(Rectangle DesiredShape, IntPtr WindowPtr)
        {
            // Store value set
            this.LockToThisShape = DesiredShape;
            this.ScreenLayout = new ConsoleScreenConfiguration();
            this.WindowPointer = WindowPtr == IntPtr.Zero ? ScreenLayout.ConsolePointer : WindowPtr;
            Logger.WriteLog("SETUP NEW CONSOLE WINDOW LOCKER CLASS OK!", LogType.InfoLog);
            Logger.WriteLog("DESIRED RECTANGLE: " + this.LockToThisShape, LogType.DebugLog);

            // Task Tokens
            this.TokenSource = new CancellationTokenSource();
            Logger.WriteLog("TOKENS FOR WINDOW LOCKER SETUP OK!", LogType.InfoLog);
        }


        /// <summary>
        /// Locks the current window location to the specified point.
        /// </summary>
        public void LockWindowLocation()
        {
            // Run Async in a loop forever
            Logger.WriteLog("LOCKING CONSOLE WINDOW TASK IS BEING KICKED OFF NOW...", LogType.InfoLog);
            Task.Run(() =>
            {
                // Run this while not requesting cancelation
                while (!this.TokenSource.Token.IsCancellationRequested)
                    this.LockWindowOnce(this.LockToThisShape);

                // Cancel source and return.
                this.TokenSource.Cancel();
                this.TokenSource = new CancellationTokenSource();

            }, TokenSource.Token);
        }
        /// <summary>
        /// Runs a single window lock call
        /// </summary>
        private bool LockWindowOnce(Rectangle RegionsToSet)
        {
            // View current location here.
            Rectangle CurrentShape = new Rectangle();
            ConsoleWin32.GetWindowRect(ConsoleWin32.GetConsoleWindow(), ref CurrentShape);
            if (CurrentShape == RegionsToSet) { return false; }

            // Set Location using pointer values
            ConsoleWin32.SetWindowPos(
                this.WindowPointer, IntPtr.Zero,
                RegionsToSet.X, RegionsToSet.Y,
                RegionsToSet.Width, RegionsToSet.Height,
                ForceOnTop ?
                    ConsoleWin32.SetWindowPosFlags.ShowWindow : // If forced on, show it.
                    default                                     // If not default value.
            );

            // Compare results.
            var NewShape = new Rectangle();
            ConsoleWin32.GetWindowRect(ConsoleWin32.GetConsoleWindow(), ref NewShape);

            // Log and return
            if (CurrentShape != NewShape) { Logger.WriteLog($"CONSOLE LOCATION FIXED: OLD --> {CurrentShape} | NEW --> {NewShape}", LogType.TraceLog); }
            return CurrentShape != NewShape;
        }

        /// <summary>
        /// Stops the cancelation token source and keeps it in local storage.
        /// </summary>
        public void UnlockWindow()
        {
            // Throw the token and return.
            this.TokenSource.Cancel();
            this.TokenSource = new CancellationTokenSource();
            Logger.WriteLog("WINDOW IS BEING UNLOCKED FROM LOCATION NOW!", LogType.WarnLog);
        }
    }
}
