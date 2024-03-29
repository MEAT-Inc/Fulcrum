﻿using System.Windows.Controls;
using FulcrumInjector.FulcrumViewSupport;
using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport;
using FulcrumInjector.FulcrumViewSupport.FulcrumModels;
using FulcrumJson;
using FulcrumSupport;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewContent.FulcrumViewModels
{
    /// <summary>
    /// ViewModel logic for our title view component
    /// </summary>
    public class FulcrumTitleViewModel : FulcrumViewModelBase
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Private backing fields for our public properties
        private string _titleTextString;          // Private value for title view title text
        private string _injectorVersionString;    // Private value for title view version text
        private string _shimDLLVersionString;     // Private value for title version for Shim DLL
        private bool _injectorUpdateReady;        // Private value for injector update ready or not.

        #endregion // Fields

        #region Properties

        // Public properties for the view to bind onto  
        public string TitleTextString { get => _titleTextString; set => PropertyUpdated(value); }
        public bool InjectorUpdateReady { get => _injectorUpdateReady; set => PropertyUpdated(value); }
        public string ShimDLLVersionString { get => _shimDLLVersionString; set => PropertyUpdated(value); }
        public string InjectorVersionString { get => _injectorVersionString; set => PropertyUpdated(value); }

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        /// <param name="TitleViewUserControl">UserControl which holds the content for our title view</param>
        public FulcrumTitleViewModel(UserControl TitleViewUserControl) : base(TitleViewUserControl)
        {
            // Spawn a new logger for this view model instance 
            this.ViewModelLogger = new SharpLogger(LoggerActions.UniversalLogger);
            this.ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);
            this.ViewModelLogger.WriteLog("SETTING UP TITLE VIEW BOUND VALUES NOW...", LogType.WarnLog);

            // Store title and version string values now.
            this.ShimDLLVersionString = $"Shim Version: {FulcrumVersionInfo.ShimVersionString}";
            this.InjectorVersionString = $"Version: {FulcrumVersionInfo.InjectorVersionString}";
            this.TitleTextString = ValueLoaders.GetConfigValue<string>("FulcrumConstants.AppInstanceName");

            // Log output information
            this.ViewModelLogger.WriteLog("PULLED NEW TITLE AND VERSION VALUES OK!");
            this.ViewModelLogger.WriteLog($"INJECTOR:  {InjectorVersionString}");
            this.ViewModelLogger.WriteLog($"SHIM:      {ShimDLLVersionString}");
            this.ViewModelLogger.WriteLog($"VIEW MODEL TYPE {this.GetType().Name} HAS BEEN CONSTRUCTED CORRECTLY!", LogType.InfoLog);
        }
    }
}
