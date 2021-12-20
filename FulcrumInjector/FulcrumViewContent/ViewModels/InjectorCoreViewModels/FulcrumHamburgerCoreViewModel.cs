using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using FulcrumInjector.FulcrumLogic.JsonHelpers;
using MahApps.Metro.Controls;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using System.IO;
using Svg;

namespace FulcrumInjector.FulcrumViewContent.ViewModels.InjectorCoreViewModels
{
    /// <summary>
    /// ViewModel for the main hamburger core object
    /// </summary>
    public class FulcrumHamburgerCoreViewModel : ViewModelControlBase
    {
        // Logger object.
        private static SubServiceLogger ViewModelLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("InjectorHamburgerViewModelLogger")) ?? new SubServiceLogger("InjectorHamburgerViewModelLogger");

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        public FulcrumHamburgerCoreViewModel()
        {
            // Log information and store values 
            ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog("SETTING UP HAMBURGER VIEW BOUND VALUES NOW...", LogType.WarnLog);

            // Build log content helper and return
            ViewModelLogger.WriteLog("SETUP NEW HAMBURGER OUTPUT LOG VALUES OK!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Applies new hamburger menu items here
        /// </summary>
        internal HamburgerMenuItemCollection SetupHamburgerMenuItems(HamburgerMenu InputMenu)
        {
            // Draw our icon objects
            var IconObjects = ValueLoaders.GetConfigValue<dynamic[]>("FulcrumCoreIcons");
            HamburgerMenuItemCollection MenuItemsSource = (HamburgerMenuItemCollection)InputMenu.ItemsSource;
            foreach (var MenuObject in MenuItemsSource)
            {
                // Cast the menu object to a type of HamburgerMenuImageItem
                HamburgerMenuGlyphItem MenuCast = MenuObject as HamburgerMenuGlyphItem;
                if (MenuCast == null) { ViewModelLogger.WriteLog("--> CASTING TO ICON OBJECT FAILED! MOVING ON...", LogType.ErrorLog); continue; }

                // Pull values out of icon objects
                string IconPath = IconObjects[0].IconPath;
                string IconName = IconObjects[0].IconName;
                ViewModelLogger.WriteLog($"   --> PULLED NEW ICON OBJECT NAMED: {IconName}", LogType.TraceLog);
                ViewModelLogger.WriteLog($"   --> ICON PATH VALUE LOCATED WAS: {IconPath}", LogType.TraceLog);

                // Ensure output icon path exists
                string IconBasePath = ValueLoaders.GetConfigValue<string>("FulcrumInjectorConstants.FulcrumIconsPath");
                if (!Directory.Exists(IconBasePath)) { Directory.CreateDirectory(IconBasePath); }

                // Read in the content of the SVG object, store it in a temp file, then convert to a PNG and store as a resource
                string OutputFile = Path.Combine(IconBasePath, IconName);
                using (var SvgContentStream = new MemoryStream(Encoding.ASCII.GetBytes(IconPath)))
                {
                    var OpenedDocument = SvgDocument.Open<SvgDocument>(SvgContentStream);
                    var SvgDocAsBitmap = OpenedDocument.Draw();

                    // Overwrite the file object if needed.
                    if (File.Exists(OutputFile)) { File.Delete(OutputFile); }
                    SvgDocAsBitmap.Save(OutputFile, ImageFormat.Png);
                }

                // Store our new icon object
                MenuCast.Glyph = OutputFile;
                ViewModelLogger.WriteLog($"--> CAST NEW OBJECT AND STORED NEW CONTENTS FOR ICON {IconName} OK!", LogType.InfoLog);
            }

            // Return built output object values
            return MenuItemsSource;
        }
    }
}
