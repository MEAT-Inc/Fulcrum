using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Shapes;
using FulcrumInjector.AppStyles.AppStyleLogic;
using FulcrumInjector.ViewControl;
using MahApps.Metro.Controls;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class InjectorMainWindow : MetroWindow
    {
        // Logger object.
        private static SubServiceLogger InjectorMainLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("InjectorMainLogger")) ?? new SubServiceLogger("InjectorMainLogger");

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new main window instance we can use to show our configuration application
        /// </summary>
        public InjectorMainWindow()
        {
            // Init main component and blur background of the main window.
            InitializeComponent();
            App.WindowBlurHelper = new WindowBlurSetup(this);
            App.WindowBlurHelper.ShowBlurEffect();
            InjectorMainLogger.WriteLog("SETUP NEW BLUR EFFECT ON MAIN WINDOW INSTANCE OK!", LogType.InfoLog);

            // Store constants in here.
            InjectorConstants.ConfigureViewControls(this);
            InjectorMainLogger.WriteLog("WATCHDOG CONSTANTS STORED OK! APP SHOULD BE VISIBLE AND OPERATIONAL NOW", LogType.InfoLog);
        }
    }
}
