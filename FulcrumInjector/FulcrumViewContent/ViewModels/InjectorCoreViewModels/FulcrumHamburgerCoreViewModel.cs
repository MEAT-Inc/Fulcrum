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
using System.Web.UI.WebControls;
using System.Windows;
using FulcrumInjector.FulcrumViewContent.Models;
using FulcrumInjector.FulcrumViewContent.Views.InjectorCoreViews;
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
        internal List<HamburgerNavMenuItem> SetupHamburgerMenuItems()
        {
            // Ensure output icon path exists
            string IconBasePath = ValueLoaders.GetConfigValue<string>("FulcrumInjectorConstants.FulcrumIconsPath");
            if (!Directory.Exists(IconBasePath)) { Directory.CreateDirectory(IconBasePath); }
           
            // Pull icon objects, store menu entries into a list
            List<HamburgerNavMenuItem> OutputMenuItems = new List<HamburgerNavMenuItem>();
            var IconObjects = ValueLoaders.GetConfigValue<dynamic[]>("FulcrumCoreIcons");
            ViewModelLogger.WriteLog("--> BUILDING BOUND VIEW MODEL ENTRIES FOR MENU OBJECTS NOW...", LogType.InfoLog);
            foreach (var IconObjectEntry in IconObjects)
            {
                // Pull values out of icon objects
                string MenuEntryName = IconObjectEntry.MenuEntry;
                string MenuEntryType = IconObjectEntry.MenuEntryType;
                string MenuEntryContent = IconObjectEntry.MenuEntryContent;
                string MenuIconSvgPaths = IconObjectEntry.IconContentSvgPath;

                // Replace Icon path with current text color value
                var CurrentMerged = Application.Current.Resources.MergedDictionaries;
                var ColorResources = CurrentMerged.FirstOrDefault(Dict => Dict.Source.ToString().Contains("AppColorTheme"));
                MenuIconSvgPaths = MenuIconSvgPaths.Replace("currentColor", ColorResources["TextColorBase"].ToString());

                // Log information about imported values
                ViewModelLogger.WriteLog($"   --> PULLED NEW MENU OBJECT NAMED: {MenuEntryName}", LogType.TraceLog);
                ViewModelLogger.WriteLog($"       --> MENU OBJECT CONTENT PATH: {MenuEntryContent}", LogType.TraceLog);
                ViewModelLogger.WriteLog($"       --> ICON PATH VALUE LOCATED WAS: {MenuIconSvgPaths}", LogType.TraceLog);

                // Read in the content of the SVG object, store it in a temp file, then convert to a PNG and store as a resource
                string OutputFileName = $"{MenuEntryName.Replace(' ', '-') }_Icon.png";
                string OutputFile = Path.Combine(IconBasePath, OutputFileName); 
                using (var SvgContentStream = new MemoryStream(Encoding.ASCII.GetBytes(MenuIconSvgPaths)))
                {
                    // Read the SVG content input and store it as a stream object. Then draw it to a Bitmap
                    var OpenedDocument = SvgDocument.Open<SvgDocument>(SvgContentStream);
                    var SvgDocAsBitmap = OpenedDocument.Draw();

                    // Overwrite the file object if needed and write new bitmap output to file
                    if (File.Exists(OutputFile)) { File.Delete(OutputFile); }
                    SvgDocAsBitmap.Save(OutputFile, ImageFormat.Png);
                }

                // Build and store our new icon object
                OutputMenuItems.Add(new HamburgerNavMenuItem()
                {
                    // Configure the label and the name of the menu entry
                    Label = MenuEntryName,
                    Glyph = new Uri(Path.GetFullPath(Path.Combine(IconBasePath, OutputFileName)), UriKind.Absolute).ToString(),

                    // Store the content of the view each time we open
                    NavigationType = Type.GetType(MenuEntryType),
                    NavigationDestination = new Uri(MenuEntryContent)
                });

                // Log the result of the binding actions
                ViewModelLogger.WriteLog($"   --> CAST NEW OBJECT AND STORED NEW CONTENTS FOR ENTRY {MenuEntryName} OK!", LogType.InfoLog);
            }

            // Return built output object values
            return OutputMenuItems;
        }
    }
}
