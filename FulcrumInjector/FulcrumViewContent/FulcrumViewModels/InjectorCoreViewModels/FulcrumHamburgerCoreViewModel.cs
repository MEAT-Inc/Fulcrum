using System;
using System.Collections.ObjectModel;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using FulcrumInjector.FulcrumViewSupport;
using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport;
using SharpLogging;
using Svg;

namespace FulcrumInjector.FulcrumViewContent.FulcrumViewModels.InjectorCoreViewModels
{
    /// <summary>
    /// ViewModel for the main hamburger core object
    /// </summary>
    internal class FulcrumHamburgerCoreViewModel : FulcrumViewModelBase
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Path for Icons and Styles
        public readonly string FulcrumIconPath;
        public readonly dynamic[] FulcrumMenuEntries;

        // Private backing fields for our public properties
        private ObservableCollection<FulcrumNavMenuItem> _injectorMenuEntries;
        private ObservableCollection<FulcrumNavMenuItem> _injectorMenuOptions;

        #endregion // Fields

        #region Properties

        // Public properties for the view to bind onto  
        public ObservableCollection<FulcrumNavMenuItem> InjectorMenuEntries { get => _injectorMenuEntries; set => PropertyUpdated(value); }
        public ObservableCollection<FulcrumNavMenuItem> InjectorMenuOptions { get => _injectorMenuOptions; set => PropertyUpdated(value); }

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        /// <param name="HamburgerUserControl">UserControl which holds the content for our hamburger core</param>
        public FulcrumHamburgerCoreViewModel(UserControl HamburgerUserControl) : base(HamburgerUserControl)
        {
            // Spawn a new logger for this view model instance 
            this.ViewModelLogger = new SharpLogger(LoggerActions.UniversalLogger);
            this.ViewModelLogger.WriteLog("SETTING UP HAMBURGER VIEW BOUND VALUES NOW...", LogType.WarnLog);
            this.ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);

            // Store The path for icons output and the dynamic objects for our icons
            this.ViewModelLogger.WriteLog("BUILDING ICON PATH OUTPUT AND IMPORTING MENU ENTRIES NOW...");
            this.FulcrumMenuEntries = ValueLoaders.GetConfigValue<dynamic[]>("FulcrumMenuEntries");
            this.FulcrumIconPath = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                ValueLoaders.GetConfigValue<string>("FulcrumConstants.InjectorResources.FulcrumIconsPath"));
            this.ViewModelLogger.WriteLog("IMPORTED VALUES FROM JSON FILE OK!", LogType.InfoLog);

            // Ensure output icon path exists
            if (!Directory.Exists(FulcrumIconPath)) {
                Directory.CreateDirectory(FulcrumIconPath);
                this.ViewModelLogger.WriteLog("WARNING! ICON PATH OUTPUT WAS NOT FOUND! IS HAS BEEN BUILT FOR US!", LogType.WarnLog);
            }

            // Build log content helper and return
            this.ViewModelLogger.WriteLog("SETUP NEW HAMBURGER OUTPUT LOG VALUES OK!", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Applies new hamburger menu items here
        /// </summary>
        public FulcrumNavMenuItem[] SetupHamburgerMenuItems()
        {
            // Pull icon objects, store menu entries into a list
            FulcrumNavMenuItem[] OutputMenuItems = Array.Empty<FulcrumNavMenuItem>();
            this.ViewModelLogger.WriteLog("BUILDING BOUND VIEW MODEL ENTRIES FOR MENU OBJECTS NOW...", LogType.InfoLog);
            foreach (var IconObjectEntry in this.FulcrumMenuEntries)
            {
                // Pull values out of icon objects and check if option or not
                string MenuEntryName = IconObjectEntry.MenuEntry;

                // Check if the menu object is enabled or not
                bool MenuOptionEnabled = IconObjectEntry.IsEntryEnabled;
                if (!MenuOptionEnabled) {
                    this.ViewModelLogger.WriteLog($"WARNING! MENU ENTRY {MenuOptionEnabled} IS NOT ENABLED! SKIPPING ADDITION FOR IT!", LogType.WarnLog);
                    continue;
                }

                // If it's an option entry skip it as well
                if (IconObjectEntry.EntryType == "OptionEntry") {
                    // this.ViewModelLogger.WriteLog($"ENTRY OBJECT IS SEEN TO BE AN OPTION ENTRY! NOT INCLUDING THIS IN OUR MENU VALUES", LogType.InfoLog);
                    // this.ViewModelLogger.WriteLog($"SKIPPED ENTRY {MenuEntryName}", LogType.InfoLog);
                    continue;
                }

                // If not an option entry, pull our other values and run the builder
                string MenuViewType = IconObjectEntry.MenuViewType;
                string MenuModelType = IconObjectEntry.MenuModelType;
                string MenuIconSvgPaths = IconObjectEntry.MenuIconSvgPath;

                // Build new menu entry and add to our collection
                OutputMenuItems = OutputMenuItems.Append(this._buildHamburgerNavItem(MenuEntryName, MenuViewType,  MenuModelType, MenuIconSvgPaths)).ToArray();
                this.ViewModelLogger.WriteLog($"STORED NEW MENU ENTRY NAMED {MenuEntryName} OK!", LogType.InfoLog);
            }

            // Return built output object values
            this.InjectorMenuEntries = new ObservableCollection<FulcrumNavMenuItem>(OutputMenuItems);
            return OutputMenuItems;
        }
        /// <summary>
        /// Builds the option item entries for our injector hamburger menu
        /// </summary>
        /// <returns>List of objects built for our hamburger views</returns>
        public FulcrumNavMenuItem[] SetupHamburgerOptionItems()
        {
            // Pull icon objects, store menu entries into a list
            FulcrumNavMenuItem[] OutputOptionEntries = Array.Empty<FulcrumNavMenuItem>();
            this.ViewModelLogger.WriteLog("BUILDING BOUND VIEW MODEL ENTRIES FOR MENU OPTION OBJECTS NOW...", LogType.InfoLog);
            foreach (var IconObjectEntry in this.FulcrumMenuEntries)
            {
                // Pull values out of icon objects and check if option or not
                string MenuEntryName = IconObjectEntry.MenuEntry;

                // Check if the menu object is enabled or not
                bool MenuOptionEnabled = IconObjectEntry.IsEntryEnabled;
                if (!MenuOptionEnabled) {
                    this.ViewModelLogger.WriteLog($"WARNING! MENU ENTRY {MenuOptionEnabled} IS NOT ENABLED! SKIPPING ADDITION FOR IT!", LogType.WarnLog);
                    continue;
                }

                // If it's a menu entry skip it as well]
                if (IconObjectEntry.EntryType == "MenuEntry") {
                    // this.ViewModelLogger.WriteLog($"ENTRY OBJECT IS SEEN TO BE AN MENU ENTRY! NOT INCLUDING THIS IN OUR OPTION VALUES", LogType.InfoLog);
                    // this.ViewModelLogger.WriteLog($"SKIPPED ENTRY {MenuEntryName}", LogType.InfoLog);
                    continue;
                }

                // If not a menu entry, pull our other values and run the builder
                string MenuViewType = IconObjectEntry.MenuViewType;
                string MenuModelType = IconObjectEntry.MenuModelType;
                string MenuIconSvgPaths = IconObjectEntry.MenuIconSvgPath;

                // Build new menu entry and add to our collection
                OutputOptionEntries = OutputOptionEntries.Append(this._buildHamburgerNavItem(MenuEntryName, MenuViewType, MenuModelType, MenuIconSvgPaths)).ToArray();
                this.ViewModelLogger.WriteLog($"STORED NEW OPTION ENTRY NAMED {MenuEntryName} OK!", LogType.InfoLog);
            }

            // Return built output object values
            this.InjectorMenuOptions = new ObservableCollection<FulcrumNavMenuItem>(OutputOptionEntries);
            return OutputOptionEntries;
        }

        /// <summary>
        /// Builds a hamburger entry object from a given set of values for a menu entry.
        /// </summary>
        /// <param name="MenuEntryName">Menu item name</param>
        /// <param name="MenuViewTypeName">View type for the menu item</param>
        /// <param name="MenuModelTypeName">Model type for the menu item</param>
        /// <param name="MenuIconSvgPaths">The SVG Path value for the menu item icon</param>
        /// <returns>A built navigation item for the menu object</returns>
        private FulcrumNavMenuItem _buildHamburgerNavItem(string MenuEntryName, string MenuViewTypeName, string MenuModelTypeName, string MenuIconSvgPaths)
        {
            // Replace Icon path with current text color value
            var CurrentMerged = Application.Current.Resources.MergedDictionaries;
            var ColorResources = CurrentMerged.FirstOrDefault(Dict => Dict.Source.ToString().Contains("AppColorTheme"));
            MenuIconSvgPaths = MenuIconSvgPaths.Replace("currentColor", ColorResources["TextColorBase"].ToString());

            // Log information about imported values
            this.ViewModelLogger.WriteLog($"PULLED NEW MENU OBJECT NAMED: {MenuEntryName}", LogType.TraceLog);
            this.ViewModelLogger.WriteLog($"ICON PATH VALUE LOCATED WAS: {MenuIconSvgPaths}", LogType.TraceLog);

            // Read in the content of the SVG object, store it in a temp file, then convert to a PNG and store as a resource
            string IconName = $"{MenuEntryName.Replace(' ', '-')}_Icon.png";
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

            // Build a new instance of each type quickly to store something in our view configuration
            Type MenuContentType = Type.GetType(MenuViewTypeName);
            Type MenuViewModelType = Type.GetType(MenuModelTypeName);
            this.ViewModelLogger.WriteLog("PULLED IN NEW TYPES FOR ENTRY OBJECT OK!", LogType.InfoLog);
            this.ViewModelLogger.WriteLog($"VIEW TYPE:       {MenuViewTypeName}", LogType.InfoLog);
            this.ViewModelLogger.WriteLog($"VIEW MODEL TYPE: {MenuModelTypeName}", LogType.InfoLog);

            // Generate output result object.
            var NewResult = new FulcrumNavMenuItem()
            {
                // Stores the content type for view and view model
                NavUserControlType = MenuContentType,
                NavViewModelType = MenuViewModelType,

                // Configure the label and the name of the menu entry
                Label = MenuEntryName,
                Glyph = new Uri(OutputIconFileName, UriKind.RelativeOrAbsolute).ToString(),
            };

            // Log the result of the binding actions
            this.ViewModelLogger.WriteLog($"CAST NEW OBJECT AND STORED NEW CONTENTS FOR ENTRY {MenuEntryName} OK!", LogType.InfoLog);
            return NewResult;
        }
    }
}
