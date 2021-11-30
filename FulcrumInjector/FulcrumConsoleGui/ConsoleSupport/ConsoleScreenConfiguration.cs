using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FulcrumInjector.FulcrumConsoleGui.ConsoleSupport
{
    /// <summary>
    /// Contains info about the screen configuration for this machine.
    /// </summary>
    public class ConsoleScreenConfiguration
    {
        // Rectangles for screen bounds and infos.       
        public Rectangle AllDisplayBounds => Screen.AllScreens.Select(screen => screen.WorkingArea).Aggregate(Rectangle.Union);
        public Rectangle WorkingDisplayBounds => Screen.GetWorkingArea(new Point(AllDisplayBounds.Left, AllDisplayBounds.Top));

        // Pointer for console.
        public IntPtr ConsolePointer = ConsoleWin32.GetConsoleWindow();

        // ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Populate new screen config values here.
        /// </summary>
        public ConsoleScreenConfiguration() { }
    }
}
