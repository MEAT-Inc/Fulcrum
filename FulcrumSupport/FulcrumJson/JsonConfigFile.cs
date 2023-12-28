using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using FulcrumSupport;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using SharpLogging;

namespace FulcrumJson
{
    /// <summary>
    /// Class which contains info about the possible json files to import.
    /// </summary>
    public static class JsonConfigFile
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Private fields used to hold our configuration and logging objects
        private static JObject _applicationConfig;
        private static SharpLogger _backingLogger;

        #endregion //Fields

        #region Properties
        
        // Logger instance for our JSON configuration helpers
        private static SharpLogger _jsonConfigLogger => SharpLogBroker.LogBrokerInitialized
            ? _backingLogger ??= new SharpLogger(LoggerActions.UniversalLogger)
            : null;

        // Tells us if the application configuration is setup or not
        public static bool IsConfigured => 
            !string.IsNullOrWhiteSpace(AppConfigFile) && 
            File.Exists(AppConfigFile) && ApplicationConfig != null;

        // Currently loaded app configuration file and the JSON object built from that file
        public static string AppConfigFile { get; private set; }
        public static JObject ApplicationConfig
        {
            get
            {
                // Return existing object if needed here.
                bool FirstConfig = _applicationConfig == null;

                // Build new here for the desired input file object.
                if (!FirstConfig) return _applicationConfig;
                _jsonConfigLogger?.WriteLog($"BUILDING NEW JCONFIG OBJECT NOW...", LogType.TraceLog);
                _applicationConfig = JObject.Parse(File.ReadAllText(AppConfigFile));
                _jsonConfigLogger?.WriteLog($"GENERATED JSON CONFIG FILE OBJECT OK AND WROTE CONTENT TO {AppConfigFile} OK! RETURNED CONTENTS NOW...", LogType.TraceLog);

                // Return the object.
                return _applicationConfig;
            }
        }

        // List of encrypted sections and config field keys
        public static List<EncryptedConfigSection> EncryptedConfigs { get; private set; }
        public static List<string> EncryptedConfigKeys =>
            EncryptedConfigs == null 
                ? new List<string>() 
                : EncryptedConfigs.SelectMany(ConfigObj => ConfigObj.GetConfigKeys()).ToList();

        #endregion //Properties

        #region Structs and Classes

        /// <summary>
        /// Public enumeration holding locations for different settings objects
        /// Holds the flags attribute for combining values from settings entries
        /// </summary>
        [Flags] public enum JsonSections : uint
        {
            // Base settings entry locations. These are combined with child categories
            [Description("FulcrumServices")]          FULCRUM_SERVICES              = 0x00400000,   // FulcrumServices
            [Description("FulcrumOeApplications")]    FULCRUM_OE_APPLICATIONS       = 0x00800000,   // FulcrumOeApplications
            [Description("FulcrumMenuEntries")]       FULCRUM_MENU_ENTRIES          = 0x01000000,   // FulcrumMenuEntries
            [Description("FulcrumUserSettings")]      FULCRUM_USER_SETTINGS         = 0x02000000,   // FulcrumUserSettings
            [Description("FulcrumSimConfigurations")] FULCRUM_SIM_CONFIGURATIONS    = 0x04000000,   // FulcrumSimConfigurations
            [Description("FulcrumAppThemes")]         FULCRUM_APP_THEMES            = 0x08000000,   // FulcrumAppThemes
            [Description("FulcrumEncryption")]        FULCRUM_ENCRYPTION            = 0x10000000,   // FulcrumEncryption

            // FulcrumConstants Settings Entries
            [Description("FulcrumConstants")]              FULCRUM_CONSTANTS                = 0x00100000,                               // FulcrumConstants
            [Description("AppInstanceName")]                  APP_INSTANCE_NAME             = FULCRUM_CONSTANTS | 0x0000001,            // \__ FulcrumConstants.AppInstanceName
            [Description("ShimInstanceName")]                 SHIM_INSTANCE_NAME            = FULCRUM_CONSTANTS | 0x0000002,            // \__ FulcrumConstants.ShimInstanceName
            [Description("InjectorResources")]                INJECTOR_RESOURCES            = FULCRUM_CONSTANTS | 0x0001000,            // \__ FulcrumConstants.InjectorResources
            [Description("FulcrumXamlPath")]                    FULCRUM_XAML                  = INJECTOR_RESOURCES | 0x0000001,         //     \__ FulcrumConstants.InjectorResources.FulcrumXamlPath
            [Description("FulcrumIconsPath")]                   FULCRUM_ICONS                 = INJECTOR_RESOURCES | 0x0000002,         //     \__ FulcrumConstants.InjectorResources.FulcrumIconsPath
            [Description("FulcrumImportFilePath")]              FULCRUM_IMPORTS               = INJECTOR_RESOURCES | 0x0000003,         //     \__ FulcrumConstants.InjectorResources.FulcrumImportFilePath
            [Description("FulcrumConversionsPath")]             FULCRUM_CONVERSIONS           = INJECTOR_RESOURCES | 0x0000004,         //     \__ FulcrumConstants.InjectorResources.FulcrumConversionsPath
            [Description("FulcrumExpressionsPath")]             FULCRUM_EXPRESSIONS           = INJECTOR_RESOURCES | 0x0000005,         //     \__ FulcrumConstants.InjectorResources.FulcrumExpressionsPath
            [Description("FulcrumSimulationsPath")]             FULCRUM_SIMULATIONS           = INJECTOR_RESOURCES | 0x0000006,         //     \__ FulcrumConstants.InjectorResources.FulcrumSimulationsPath
            [Description("InjectorHardwareRefresh")]          INJECTOR_HARDWARE_REFRESH     = FULCRUM_CONSTANTS | 0x0002000,            // \__ FulcrumConstants.InjectorHardwareRefresh
            [Description("RefreshDevicesInterval")]             REFRESH_DEVICE_INTERVAL       = INJECTOR_HARDWARE_REFRESH | 0x0000001,  //     \__ FulcrumConstants.InjectorHardwareRefresh.RefreshDevicesInterval
            [Description("RefreshDLLsInterval")]                REFRESH_DLL_INTERVAL          = INJECTOR_HARDWARE_REFRESH | 0x0000002,  //     \__ FulcrumConstants.InjectorHardwareRefresh.RefreshDLLsInterval
            [Description("IgnoredDLLNames")]                    IGNORED_DLL_NAMES             = INJECTOR_HARDWARE_REFRESH | 0x0000003,  //     \__ FulcrumConstants.InjectorHardwareRefresh.IgnoredDLLNames

            // FulcrumLogging Settings Entries
            [Description("FulcrumLogging")]                FULCRUM_LOGGING                  = 0x00200000,                               // FulcrumLogging
            [Description("LogBrokerConfiguration")]           LOG_BROKER_CONFIG             = FULCRUM_LOGGING | 0x00000001,             // \__ FulcrumLogging.LogBrokerConfiguration
            [Description("LogBrokerName")]                       LOG_BROKER_NAME            = LOG_BROKER_CONFIG | 0x00000001,           //     \__ FulcrumLogging.LogBrokerConfiguration.LogBrokerName
            [Description("LogFilePath")]                         LOG_FILE_PATH                 = LOG_BROKER_CONFIG | 0x00000001,        //     \__ FulcrumLogging.LogBrokerConfiguration.LogFilePath
            [Description("LogFileName")]                         LOG_FILE_NAME                 = LOG_BROKER_CONFIG | 0x00000001,        //     \__ FulcrumLogging.LogBrokerConfiguration.LogFileName
            [Description("MinLogLevel")]                         MIN_LOG_LEVEL                 = LOG_BROKER_CONFIG | 0x00000001,        //     \__ FulcrumLogging.LogBrokerConfiguration.MinLogLevel
            [Description("MaxLogLevel")]                         MAX_LOG_LEVEL                 = LOG_BROKER_CONFIG | 0x00000001,        //     \__ FulcrumLogging.LogBrokerConfiguration.MaxLogLevel
            [Description("LogArchiveConfiguration")]          LOG_ARCHIVER_CONFIG           = FULCRUM_LOGGING | 0x00000002,             // \__ FulcrumLogging.LogArchiverConfiguration
            [Description("SearchPath")]                          SEARCH_PATH                   = LOG_ARCHIVER_CONFIG | 0x00000001,      //     \__ FulcrumLogging.LogArchiverConfiguration.SearchPath
            [Description("ArchivePath")]                         ARCHIVE_PATH                  = LOG_ARCHIVER_CONFIG | 0x00000002,      //     \__ FulcrumLogging.LogArchiverConfiguration.ArchivePath
            [Description("ArchiveFileFilter")]                   ARCHIVE_FILTER                = LOG_ARCHIVER_CONFIG | 0x00000003,      //     \__ FulcrumLogging.LogArchiverConfiguration.ArchiveFileFilter
            [Description("ArchiveFileSetSize")]                  ARCHIVE_SET_SIZE              = LOG_ARCHIVER_CONFIG | 0x00000004,      //     \__ FulcrumLogging.LogArchiverConfiguration.ArchiveFileSetSize
            [Description("ArchiveOnFileCount")]                  ARCHIVE_ON_COUNT              = LOG_ARCHIVER_CONFIG | 0x00000005,      //     \__ FulcrumLogging.LogArchiverConfiguration.ArchiveOnFileCount
            [Description("ArchiveCleanupFileCount")]             ARCHIVE_CLEANUP_COUNT         = LOG_ARCHIVER_CONFIG | 0x00000006,      //     \__ FulcrumLogging.LogArchiverConfiguration.ArchiveCleanupFileCount
            [Description("SubFolderCleanupFileCount")]           SUB_DIR_FILE_COUNT            = LOG_ARCHIVER_CONFIG | 0x00000007,      //     \__ FulcrumLogging.LogArchiverConfiguration.SubFolderCleanupFileCount
            [Description("SubFolderRemainingFileCount")]         SUB_DIR_REMAINING_COUNT       = LOG_ARCHIVER_CONFIG | 0x00000008,      //     \__ FulcrumLogging.LogArchiverConfiguration.SubFolderRemainingFileCount
            [Description("CompressionLevel")]                    COMPRESSION_LEVEL             = LOG_ARCHIVER_CONFIG | 0x00000009,      //     \__ FulcrumLogging.LogArchiverConfiguration.CompressionLevel
            [Description("CompressionStyle")]                    COMPRESSION_STYLE             = LOG_ARCHIVER_CONFIG | 0x00000010,      //     \__ FulcrumLogging.LogArchiverConfiguration.CompressionStyle

        }

        /// <summary>
        /// Class object which holds the definition for an encrypted configuration file section
        /// </summary>
        public class EncryptedConfigSection
        {
            #region Custom Events
            #endregion // Custom Events

            #region Fields
            #endregion // Fields

            #region Properties

            // Public facing properties holding our encrypted configuration section values
            public string SectionKey { get; set; }
            public string[] SectionFields { get; set; }

            #endregion // Properties

            #region Structs and Classes
            #endregion // Structs and Classes

            // --------------------------------------------------------------------------------------------------------------------------------------

            /// <summary>
            /// Looks at our input path value and builds full key paths for all fields
            /// </summary>
            /// <returns>A list of all the config keys built with their parent paths</returns>
            public List<string> GetConfigKeys()
            {
                // Build a list of string values and return it out
                return this.SectionFields.Select(KeyValue => $"{this.SectionKey}.{KeyValue}").ToList();
            }

        }

        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Loads a new config file, sets the access bool to true if the file exists
        /// </summary>
        /// <param name="NewConfigFileName">Name of our configuration file to use</param>
        /// <param name="ForcedDirectory">The forced path to look in for our configuration file</param>
        public static void SetInjectorConfigFile(string NewConfigFileName, string ForcedDirectory = null)
        {
            // Pull location of the configuration application. If debugging is on, then try and set it using the working dir. 
            string FulcrumInjectorDir;
            _jsonConfigLogger?.WriteLog($"PULLING IN NEW APP CONFIG FILE NAMED {NewConfigFileName} FROM PROGRAM FILES OR WORKING DIRECTORY NOW");

            // Check if we've got a debugger hooked up or not first
            if (Debugger.IsAttached) 
            {
                // If we've got a debugger hooked on, use the forced directory or use the current assembly location
                _jsonConfigLogger?.WriteLog("DEBUGGER OR DEBUG BUILD FOUND! USING DEBUG CONFIGURATION FILE FROM CURRENT WORKING DIR", LogType.InfoLog);
                FulcrumInjectorDir = ForcedDirectory ?? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            }
            else
            {
                // Pull the injector EXE location from the registry and store our directory for it
                FulcrumInjectorDir = ForcedDirectory ?? RegistryControl.InjectorInstallPath;
                if (FulcrumInjectorDir == null)
                {
                    // If the injector registry control object fails to find a key value, use a default path
                    _jsonConfigLogger?.WriteLog("INJECTOR REGISTRY KEY WAS NULL! FALLING BACK NOW...", LogType.WarnLog);
                    FulcrumInjectorDir = @"C:\Program Files (x86)\MEAT Inc\FulcrumInjector";
                }
            }

            // List all the files in the directory we've located now and then find our settings file by name
            Directory.SetCurrentDirectory(FulcrumInjectorDir);
            _jsonConfigLogger?.WriteLog($"INJECTOR DIR PULLED: {FulcrumInjectorDir}", LogType.InfoLog);
            string[] LocatedFilesInDirectory = Directory.GetFiles(FulcrumInjectorDir, "*.json", SearchOption.AllDirectories);
            _jsonConfigLogger?.WriteLog($"LOCATED A TOTAL OF {LocatedFilesInDirectory.Length} FILES IN OUR APP FOLDER WITH A JSON EXTENSION");
            string MatchedConfigFile = LocatedFilesInDirectory
                .OrderBy(FileObj => FileObj.Length)
                .FirstOrDefault(FileObj => FileObj.Contains(NewConfigFileName));

            // Check if the file is null or not found first
            if (MatchedConfigFile == null) throw new FileNotFoundException($"FAILED TO FIND OUR JSON CONFIG FILE!\nFILE: {NewConfigFileName}");
            _jsonConfigLogger?.WriteLog($"LOCATED CONFIG FILE NAME IS: {MatchedConfigFile}", LogType.InfoLog);

            // Log info. Set file state
            AppConfigFile = Path.GetFullPath(MatchedConfigFile);
            _jsonConfigLogger?.WriteLog("STORING NEW JSON FILE NOW!", LogType.InfoLog);
            _jsonConfigLogger?.WriteLog($"EXPECTED TO LOAD JSON CONFIG FILE AT: {AppConfigFile}");

            // Check if the configuration file exists or not 
            if (File.Exists(AppConfigFile)) _jsonConfigLogger?.WriteLog("CONFIG FILE LOADED OK!", LogType.InfoLog);
            else throw new FileNotFoundException($"FAILED TO FIND OUR JSON CONFIG FILE!\nFILE: {AppConfigFile}");

            // Finally, pull our encrypted configuration values from the settings file
            try
            {
                // Pull encrypted field values here and store them if possible
                EncryptedConfigs = ValueLoaders.GetConfigValue<EncryptedConfigSection[]>("FulcrumEncryption.EncryptedSections").ToList();
                _jsonConfigLogger?.WriteLog("LOADED ENCRYPTED CONFIGURATION FIELD VALUES CORRECTLY!", LogType.InfoLog);
                _jsonConfigLogger?.WriteLog($"FOUND A TOTAL OF {EncryptedConfigs.Count} ENCRYPTED SECTIONS AND {EncryptedConfigKeys.Count} ENCRYPTED FIELDS");
            }
            catch (Exception PullEncryptedFieldsEx)
            {
                // Log out our exception trying to pull configuration fields here
                _jsonConfigLogger?.WriteLog("WARNING! CONFIGURATION FOR ENCRYPTED SETTINGS SECTIONS FAILED!", LogType.WarnLog);
                _jsonConfigLogger?.WriteException("EXCEPTION THROWN DURING CONFIGURATION IS LOGGED BELOW", PullEncryptedFieldsEx, LogType.WarnLog);
            }
        }
    }
}
