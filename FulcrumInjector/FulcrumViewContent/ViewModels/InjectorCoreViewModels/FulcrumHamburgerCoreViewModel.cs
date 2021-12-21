using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
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

        // Private control values
        private HamburgerNavMenuItem[] _injectorMenuEntries;
        private HamburgerNavMenuItem[] _injectorMenuOptions;

        // Public values for our view to bind to
        public HamburgerNavMenuItem[] InjectorMenuEntries { get => _injectorMenuEntries; set => PropertyUpdated(value); }
        public HamburgerNavMenuItem[] InjectorMenuOptions { get => _injectorMenuOptions; set => PropertyUpdated(value); }

        // --------------------------------------------------------------------------------------------------------------------------

        // Path for Icons and Styles
        public readonly string FulcrumIconPath;
        public readonly dynamic[] FulcrumMenuEntries;

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        public FulcrumHamburgerCoreViewModel()
        {
            // Log information and store values 
            ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog("SETTING UP HAMBURGER VIEW BOUND VALUES NOW...", LogType.WarnLog);

            // Store The path for icons output and the dynamic objects for our icons
            ViewModelLogger.WriteLog("BUILDING ICON PATH OUTPUT AND IMPORTING MENU ENTRIES NOW...", LogType.InfoLog);
            this.FulcrumMenuEntries = ValueLoaders.GetConfigValue<dynamic[]>("FulcrumMenuEntries");
            this.FulcrumIconPath = ValueLoaders.GetConfigValue<string>("FulcrumInjectorConstants.FulcrumIconsPath");
            ViewModelLogger.WriteLog("IMPORTED VALUES FROM JSON FILE OK!", LogType.InfoLog);

            // Ensure output icon path exists
            if (!Directory.Exists(FulcrumIconPath))
            {
                Directory.CreateDirectory(FulcrumIconPath);
                ViewModelLogger.WriteLog("WARNING! ICON PATH OUTPUT WAS NOT FOUND! IS HAS BEEN BUILT FOR US!", LogType.WarnLog);
            }

            // Build log content helper and return
            ViewModelLogger.WriteLog("SETUP NEW HAMBURGER OUTPUT LOG VALUES OK!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Applies new hamburger menu items here
        /// </summary>
        internal HamburgerNavMenuItem[] SetupHamburgerMenuItems()
        {
            // Pull icon objects, store menu entries into a list
            HamburgerNavMenuItem[] OutputMenuItems = Array.Empty<HamburgerNavMenuItem>();
            ViewModelLogger.WriteLog("--> BUILDING BOUND VIEW MODEL ENTRIES FOR MENU OBJECTS NOW...", LogType.InfoLog);
            foreach (var IconObjectEntry in this.FulcrumMenuEntries)
            {
                // Pull values out of icon objects and check if option or not
                string MenuEntryName = IconObjectEntry.MenuEntry;
                if (IconObjectEntry.EntryType == "OptionEntry")
                {
                    ViewModelLogger.WriteLog($"   --> ENTRY OBJECT IS SEEN TO BE AN OPTION ENTRY! NOT INCLUDING THIS IN OUR MENU VALUES", LogType.InfoLog);
                    ViewModelLogger.WriteLog($"   --> SKIPPED ENTRY {MenuEntryName}", LogType.InfoLog);
                    continue;
                }

                // If not an option entry, pull our other values and run the builder
                string MenuEntryType = IconObjectEntry.MenuEntryType;
                string MenuEntryContent = IconObjectEntry.MenuEntryContent;
                string MenuIconSvgPaths = IconObjectEntry.IconContentSvgPath;

                // Build new menu entry and add to our collection
                OutputMenuItems = OutputMenuItems.Append(this.BuildHamburgerNavItem(MenuEntryName, MenuEntryType, MenuEntryContent, MenuIconSvgPaths)).ToArray();
                ViewModelLogger.WriteLog($"   --> STORED NEW MENU ENTRY NAMED {MenuEntryName} OK!", LogType.InfoLog);
            }

            // Return built output object values
            this.InjectorMenuEntries = OutputMenuItems;
            return OutputMenuItems;
        }
        /// <summary>
        /// Builds the option item entries for our injector hamburger menu
        /// </summary>
        /// <returns>List of objects built for our hamburger views</returns>
        internal HamburgerNavMenuItem[] SetupHamburgerOptionItems()
        {
            // Pull icon objects, store menu entries into a list
            HamburgerNavMenuItem[] OutputOptionEntries = Array.Empty<HamburgerNavMenuItem>();
            ViewModelLogger.WriteLog("--> BUILDING BOUND VIEW MODEL ENTRIES FOR MENU OPTION OBJECTS NOW...", LogType.InfoLog);
            foreach (var IconObjectEntry in this.FulcrumMenuEntries)
            {
                // Pull values out of icon objects and check if option or not
                string MenuEntryName = IconObjectEntry.MenuEntry;
                if (IconObjectEntry.EntryType == "MenuEntry")
                {
                    ViewModelLogger.WriteLog($"   --> ENTRY OBJECT IS SEEN TO BE AN MENU ENTRY! NOT INCLUDING THIS IN OUR OPTION VALUES", LogType.InfoLog);
                    ViewModelLogger.WriteLog($"   --> SKIPPED ENTRY {MenuEntryName}", LogType.InfoLog);
                    continue;
                }

                // If not an option entry, pull our other values and run the builder
                string MenuEntryType = IconObjectEntry.MenuEntryType;
                string MenuEntryContent = IconObjectEntry.MenuEntryContent;
                string MenuIconSvgPaths = IconObjectEntry.IconContentSvgPath;

                // Build new menu entry and add to our collection
                OutputOptionEntries = OutputOptionEntries.Append(this.BuildHamburgerNavItem(MenuEntryName, MenuEntryType, MenuEntryContent, MenuIconSvgPaths)).ToArray();
                ViewModelLogger.WriteLog($"   --> STORED NEW OPTION ENTRY NAMED {MenuEntryName} OK!", LogType.InfoLog);
            }

            // Return built output object values
            this.InjectorMenuOptions = OutputOptionEntries;
            return OutputOptionEntries;
        }


        /// <summary>
        /// Builds a hamburger entry object from a given set of values for a menu entry.
        /// </summary>
        /// <param name="IconPath">Path to icons folder</param>
        /// <param name="IconObjectEntry">Icon object to use for set</param>
        /// <returns></returns>
        private HamburgerNavMenuItem BuildHamburgerNavItem(string MenuEntryName, string MenuEntryType, string MenuEntryContent, string MenuIconSvgPaths)
        {
            // Replace Icon path with current text color value
            var CurrentMerged = Application.Current.Resources.MergedDictionaries;
            var ColorResources = CurrentMerged.FirstOrDefault(Dict => Dict.Source.ToString().Contains("AppColorTheme"));
            MenuIconSvgPaths = MenuIconSvgPaths.Replace("currentColor", ColorResources["TextColorBase"].ToString());

            // Log information about imported values
            ViewModelLogger.WriteLog($"   --> PULLED NEW MENU OBJECT NAMED: {MenuEntryName}", LogType.TraceLog);
            ViewModelLogger.WriteLog($"       --> MENU OBJECT CONTENT PATH: {MenuEntryContent}", LogType.TraceLog);
            ViewModelLogger.WriteLog($"       --> ICON PATH VALUE LOCATED WAS: {MenuIconSvgPaths}", LogType.TraceLog);

            // Read in the content of the SVG object, store it in a temp file, then convert to a PNG and store as a resource
            string IconName = $"{MenuEntryName.Replace(' ', '-') }_Icon.png";
            string OutputIconFileName = Path.Combine(this.FulcrumIconPath, IconName);
            using (var SvgContentStream = new MemoryStream(Encoding.ASCII.GetBytes(MenuIconSvgPaths)))
            {
                // Read the SVG content input and store it as a stream object. Then draw it to a Bitmap
                var SvgDocAsBitmap = SvgDocument.Open<SvgDocument>(SvgContentStream).Draw();

                // BUG: THIS IS NOT SCALING CORRECTLY! AS A RESULT WE'RE JUST SCALING INPUT SVG FILES UP
                // Resize bitmap by 200% to prevent scaling distortion on the output
                // var ScaledSvgDoc = new Bitmap(SvgDocAsBitmap.Width * 2, SvgDocAsBitmap.Height * 2);
                // using (Graphics SvgScaleGfx = Graphics.FromImage(SvgDocAsBitmap)) 
                //     SvgScaleGfx.DrawImage(SvgDocAsBitmap, 0, 0, ScaledSvgDoc.Width, ScaledSvgDoc.Height);

                // Overwrite the file object if needed and write new bitmap output to file
                SvgDocAsBitmap.Save(OutputIconFileName, ImageFormat.Png);
            }

            // Build and store our new icon object
            var NewResult = new HamburgerNavMenuItem()
            {
                // Configure the label and the name of the menu entry
                Label = MenuEntryName,
                Glyph = new Uri(Path.GetFullPath(Path.Combine(this.FulcrumIconPath, IconName)), UriKind.Absolute).ToString(),

                // Store the content of the view each time we open
                NavigationType = Type.GetType(MenuEntryType),
                NavigationDestination = new Uri(MenuEntryContent)
            };

            // Log the result of the binding actions
            ViewModelLogger.WriteLog($"   --> CAST NEW OBJECT AND STORED NEW CONTENTS FOR ENTRY {MenuEntryName} OK!", LogType.InfoLog);
            return NewResult;
        }
    }
}
