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
            // ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
            // NOTE: Settings are broken down in the enum list below. With the flags attribute, we can pull each path part for the settings value 
            //       and build the path to our settings object that way. This should prevent issues with finding paths in the future
            // ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            // FulcrumConstants Settings Entries
            [Description("FulcrumConstants")]              FULCRUM_CONSTANTS                = 0x00100000,                                        // FulcrumConstants
            [Description("AppInstanceName")]                  APP_INSTANCE_NAME             = FULCRUM_CONSTANTS | 0x0000001,                     // \__ FulcrumConstants.AppInstanceName
            [Description("ShimInstanceName")]                 SHIM_INSTANCE_NAME            = FULCRUM_CONSTANTS | 0x0000002,                     // \__ FulcrumConstants.ShimInstanceName
            [Description("InjectorResources")]                INJECTOR_RESOURCES            = FULCRUM_CONSTANTS | 0x0001000,                     // \__ FulcrumConstants.InjectorResources
            [Description("FulcrumXamlPath")]                    FULCRUM_XAML                  = INJECTOR_RESOURCES | 0x0000001,                  //     \__ FulcrumConstants.InjectorResources.FulcrumXamlPath
            [Description("FulcrumIconsPath")]                   FULCRUM_ICONS                 = INJECTOR_RESOURCES | 0x0000002,                  //     \__ FulcrumConstants.InjectorResources.FulcrumIconsPath
            [Description("FulcrumImportFilePath")]              FULCRUM_IMPORTS               = INJECTOR_RESOURCES | 0x0000003,                  //     \__ FulcrumConstants.InjectorResources.FulcrumImportFilePath
            [Description("FulcrumConversionsPath")]             FULCRUM_CONVERSIONS           = INJECTOR_RESOURCES | 0x0000004,                  //     \__ FulcrumConstants.InjectorResources.FulcrumConversionsPath
            [Description("FulcrumExpressionsPath")]             FULCRUM_EXPRESSIONS           = INJECTOR_RESOURCES | 0x0000005,                  //     \__ FulcrumConstants.InjectorResources.FulcrumExpressionsPath
            [Description("FulcrumSimulationsPath")]             FULCRUM_SIMULATIONS           = INJECTOR_RESOURCES | 0x0000006,                  //     \__ FulcrumConstants.InjectorResources.FulcrumSimulationsPath
            [Description("InjectorHardwareRefresh")]          INJECTOR_HARDWARE_REFRESH     = FULCRUM_CONSTANTS | 0x0002000,                     // \__ FulcrumConstants.InjectorHardwareRefresh
            [Description("RefreshDevicesInterval")]             REFRESH_DEVICE_INTERVAL       = INJECTOR_HARDWARE_REFRESH | 0x0000001,           //     \__ FulcrumConstants.InjectorHardwareRefresh.RefreshDevicesInterval
            [Description("RefreshDLLsInterval")]                REFRESH_DLL_INTERVAL          = INJECTOR_HARDWARE_REFRESH | 0x0000002,           //     \__ FulcrumConstants.InjectorHardwareRefresh.RefreshDLLsInterval
            [Description("IgnoredDLLNames")]                    IGNORED_DLL_NAMES             = INJECTOR_HARDWARE_REFRESH | 0x0000003,           //     \__ FulcrumConstants.InjectorHardwareRefresh.IgnoredDLLNames
            
            // ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
            
            // FulcrumLogging Settings Entries
            [Description("FulcrumLogging")]                FULCRUM_LOGGING                  = 0x00200000,                                        // FulcrumLogging
            [Description("LogBrokerConfiguration")]           LOG_BROKER_CONFIG             = FULCRUM_LOGGING | 0x00000001,                      // \__ FulcrumLogging.LogBrokerConfiguration
            [Description("LogBrokerName")]                       LOG_BROKER_NAME            = LOG_BROKER_CONFIG | 0x00000001,                    //     \__ FulcrumLogging.LogBrokerConfiguration.LogBrokerName
            [Description("LogFilePath")]                         LOG_FILE_PATH                 = LOG_BROKER_CONFIG | 0x00000001,                 //     \__ FulcrumLogging.LogBrokerConfiguration.LogFilePath
            [Description("LogFileName")]                         LOG_FILE_NAME                 = LOG_BROKER_CONFIG | 0x00000001,                 //     \__ FulcrumLogging.LogBrokerConfiguration.LogFileName
            [Description("MinLogLevel")]                         MIN_LOG_LEVEL                 = LOG_BROKER_CONFIG | 0x00000001,                 //     \__ FulcrumLogging.LogBrokerConfiguration.MinLogLevel
            [Description("MaxLogLevel")]                         MAX_LOG_LEVEL                 = LOG_BROKER_CONFIG | 0x00000001,                 //     \__ FulcrumLogging.LogBrokerConfiguration.MaxLogLevel
            [Description("LogArchiveConfiguration")]          LOG_ARCHIVER_CONFIG           = FULCRUM_LOGGING | 0x00000002,                      // \__ FulcrumLogging.LogArchiverConfiguration
            [Description("SearchPath")]                          SEARCH_PATH                   = LOG_ARCHIVER_CONFIG | 0x00000001,               //     \__ FulcrumLogging.LogArchiverConfiguration.SearchPath
            [Description("ArchivePath")]                         ARCHIVE_PATH                  = LOG_ARCHIVER_CONFIG | 0x00000002,               //     \__ FulcrumLogging.LogArchiverConfiguration.ArchivePath
            [Description("ArchiveFileFilter")]                   ARCHIVE_FILTER                = LOG_ARCHIVER_CONFIG | 0x00000003,               //     \__ FulcrumLogging.LogArchiverConfiguration.ArchiveFileFilter
            [Description("ArchiveFileSetSize")]                  ARCHIVE_SET_SIZE              = LOG_ARCHIVER_CONFIG | 0x00000004,               //     \__ FulcrumLogging.LogArchiverConfiguration.ArchiveFileSetSize
            [Description("ArchiveOnFileCount")]                  ARCHIVE_ON_COUNT              = LOG_ARCHIVER_CONFIG | 0x00000005,               //     \__ FulcrumLogging.LogArchiverConfiguration.ArchiveOnFileCount
            [Description("ArchiveCleanupFileCount")]             ARCHIVE_CLEANUP_COUNT         = LOG_ARCHIVER_CONFIG | 0x00000006,               //     \__ FulcrumLogging.LogArchiverConfiguration.ArchiveCleanupFileCount
            [Description("SubFolderCleanupFileCount")]           SUB_DIR_FILE_COUNT            = LOG_ARCHIVER_CONFIG | 0x00000007,               //     \__ FulcrumLogging.LogArchiverConfiguration.SubFolderCleanupFileCount
            [Description("SubFolderRemainingFileCount")]         SUB_DIR_REMAINING_COUNT       = LOG_ARCHIVER_CONFIG | 0x00000008,               //     \__ FulcrumLogging.LogArchiverConfiguration.SubFolderRemainingFileCount
            [Description("CompressionLevel")]                    COMPRESSION_LEVEL             = LOG_ARCHIVER_CONFIG | 0x00000009,               //     \__ FulcrumLogging.LogArchiverConfiguration.CompressionLevel
            [Description("CompressionStyle")]                    COMPRESSION_STYLE             = LOG_ARCHIVER_CONFIG | 0x00000010,               //     \__ FulcrumLogging.LogArchiverConfiguration.CompressionStyle

            // ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            // FulcrumServices Settings Entries
            [Description("FulcrumServices")]               FULCRUM_SERVICES                 = 0x00400000,                                        // FulcrumServices
            [Description("FulcrumWatchdogService")]           FULCRUM_WATCHDOG                 = FULCRUM_SERVICES | 0x00010000,                  // \__ FulcrumServices.FulcrumWatchdogService
            [Description("ServiceEnabled")]                      WATCHDOG_ENABLED                 = FULCRUM_WATCHDOG | 0x00000001,               //     \__ FulcrumServices.FulcrumWatchdogService.ServiceEnabled
            [Description("ServiceName")]                         WATCHDOG_SERVICE_NAME            = FULCRUM_WATCHDOG | 0x00000002,               //     \__ FulcrumServices.FulcrumWatchdogService.ServiceName
            [Description("ExecutionGap")]                        EXECUTION_GAP                    = FULCRUM_WATCHDOG | 0x00000003,               //     \__ FulcrumServices.FulcrumWatchdogService.ExecutionGap
            [Description("WatchedFolders")]                      WATCHDOG_FOLDERS                 = FULCRUM_WATCHDOG | 0x00000004,               //     \__ FulcrumServices.FulcrumWatchdogService.WatchedFolders
            [Description("FulcrumDriveService")]              FULCRUM_DRIVE                    = FULCRUM_SERVICES | 0x00020000,                  // \__ FulcrumServices.FulcrumDriveService
            [Description("ServiceEnabled")]                      DRIVE_ENABLED                    = FULCRUM_DRIVE | 0x00000010,                  //     \__ FulcrumServices.FulcrumDriveService.ServiceEnabled
            [Description("ServiceName")]                         DRIVE_SERVICE_NAME               = FULCRUM_DRIVE | 0x00000020,                  //     \__ FulcrumServices.FulcrumDriveService.ServiceName
            [Description("ApplicationName")]                     DRIVE_APP_NAME                   = FULCRUM_DRIVE | 0x00000040,                  //     \__ FulcrumServices.FulcrumDriveService.ApplicationName
            [Description("GoogleDriveId")]                       GOOGLE_DRIVE_ID                  = FULCRUM_DRIVE | 0x00000080,                  //     \__ FulcrumServices.FulcrumDriveService.GoogleDriveId
            [Description("ExplorerAuthorization")]               EXPLORER_AUTHORIZATION           = FULCRUM_DRIVE | 0x00000100,                  //     \__ FulcrumServices.FulcrumDriveService.ExplorerAuthorization
            [Description("type")]                                   EXPLORER_AUTH_TYPE               = EXPLORER_AUTHORIZATION | 0x00000001,      //         \__ FulcrumServices.FulcrumDriveService.ExplorerAuthorization.type
            [Description("project_id")]                             EXPLORER_PROJECT_ID              = EXPLORER_AUTHORIZATION | 0x00000002,      //         \__ FulcrumServices.FulcrumDriveService.ExplorerAuthorization.project_id
            [Description("private_key_id")]                         EXPLORER_PRIVATE_KEY_ID          = EXPLORER_AUTHORIZATION | 0x00000003,      //         \__ FulcrumServices.FulcrumDriveService.ExplorerAuthorization.private_key_id
            [Description("private_key")]                            EXPLORER_PRIVATE_KEY             = EXPLORER_AUTHORIZATION | 0x00000004,      //         \__ FulcrumServices.FulcrumDriveService.ExplorerAuthorization.private_key
            [Description("client_email")]                           EXPLORER_CLIENT_EMAIL            = EXPLORER_AUTHORIZATION | 0x00000005,      //         \__ FulcrumServices.FulcrumDriveService.ExplorerAuthorization.client_email
            [Description("client_id")]                              EXPLORER_CLIENT_ID               = EXPLORER_AUTHORIZATION | 0x00000006,      //         \__ FulcrumServices.FulcrumDriveService.ExplorerAuthorization.client_id
            [Description("auth_uri")]                               EXPLORER_AUTH_URI                = EXPLORER_AUTHORIZATION | 0x00000007,      //         \__ FulcrumServices.FulcrumDriveService.ExplorerAuthorization.auth_uri
            [Description("token_uri")]                              EXPLORER_TOKEN_URI               = EXPLORER_AUTHORIZATION | 0x00000008,      //         \__ FulcrumServices.FulcrumDriveService.ExplorerAuthorization.token_uri
            [Description("auth_provider_x509_cert_url")]            EXPLORER_AUTH_PROVIDER           = EXPLORER_AUTHORIZATION | 0x00000009,      //         \__ FulcrumServices.FulcrumDriveService.ExplorerAuthorization.auth_provider_x509_cert_url
            [Description("client_x509_cert_url")]                   EXPLORER_CERT_URL                = EXPLORER_AUTHORIZATION | 0x0000000A,      //         \__ FulcrumServices.FulcrumDriveService.ExplorerAuthorization.client_x509_cert_url
            [Description("universe_domain")]                        EXPLORER_UNIVERSE_DOMAIN         = EXPLORER_AUTHORIZATION | 0x0000000B,      //         \__ FulcrumServices.FulcrumDriveService.ExplorerAuthorization.universe_domain
            [Description("FulcrumEmailService")]              FULCRUM_EMAIL                    = FULCRUM_SERVICES | 0x00040000,                  // \__ FulcrumServices.FulcrumEmailService
            [Description("ServiceEnabled")]                      EMAIL_ENABLED                    = FULCRUM_EMAIL | 0x00000100,                  //     \__ FulcrumServices.FulcrumEmailService.ServiceEnabled
            [Description("ServiceName")]                         EMAIL_SERVICE_NAME               = FULCRUM_EMAIL | 0x00000200,                  //     \__ FulcrumServices.FulcrumEmailService.ServiceName
            [Description("SmtpServerSettings")]                  EMAIL_SMTP_SETTINGS              = FULCRUM_EMAIL | 0x00000400,                  //     \__ FulcrumServices.FulcrumEmailService.SmtpServerSettings
            [Description("ServerPort")]                             SMTP_SERVER_PORT                 = EMAIL_SMTP_SETTINGS | 0x00000001,         //         \__ FulcrumServices.FulcrumEmailService.SmtpServerSettings.ServerPort
            [Description("ServerTimeout")]                          SMTP_SERVER_TIMEOUT              = EMAIL_SMTP_SETTINGS | 0x00000002,         //         \__ FulcrumServices.FulcrumEmailService.SmtpServerSettings.ServerTimeout
            [Description("ServerName")]                             SMTP_SERVER_NAME                 = EMAIL_SMTP_SETTINGS | 0x00000003,         //         \__ FulcrumServices.FulcrumEmailService.SmtpServerSettings.ServerName
            [Description("SenderConfiguration")]                 SENDER_CONFIGURATION             = FULCRUM_EMAIL | 0x00000800,                  //     \__ FulcrumServices.FulcrumEmailService.SenderConfiguration
            [Description("ReportSenderName")]                       REPORT_SENDER_NAME               = SENDER_CONFIGURATION | 0x00000001,        //         \__ FulcrumServices.FulcrumEmailService.SenderConfiguration.ReportSenderName
            [Description("DefaultReportRecipients")]                DEFAULT_RECIPIENTS               = SENDER_CONFIGURATION | 0x00000002,        //         \__ FulcrumServices.FulcrumEmailService.SenderConfiguration.DefaultReportRecipients
            [Description("IncludeInjectorLog")]                     INCLUDE_INJECTOR_LOG             = SENDER_CONFIGURATION | 0x00000003,        //         \__ FulcrumServices.FulcrumEmailService.SenderConfiguration.IncludeInjectorLog   
            [Description("IncludeServiceLogs")]                     INCLUDE_SERVICE_LOGS             = SENDER_CONFIGURATION | 0x00000004,        //         \__ FulcrumServices.FulcrumEmailService.SenderConfiguration.IncludeServiceLogs
            [Description("DefaultEmailBodyText")]                   DEFAULT_EMAIL_BODY               = SENDER_CONFIGURATION | 0x00000005,        //         \__ FulcrumServices.FulcrumEmailService.SenderConfiguration.DefaultEmailBodyText
            [Description("ReportSenderEmail")]                      REPORT_SENDER_EMAIL              = SENDER_CONFIGURATION | 0x00000006,        //         \__ FulcrumServices.FulcrumEmailService.SenderConfiguration.ReportSenderEmail
            [Description("ReportSenderPassword")]                   REPORT_SENDER_PASSWORD           = SENDER_CONFIGURATION | 0x00000007,        //         \__ FulcrumServices.FulcrumEmailService.SenderConfiguration.ReportSenderPassword
            [Description("FulcrumUpdaterService")]            FULCRUM_UPDATER                  = FULCRUM_SERVICES | 0x00080000,                  // \__ FulcrumServices.FulcrumUpdaterService
            [Description("ServiceEnabled")]                      UPDATER_ENABLED                  = FULCRUM_UPDATER | 0x00000001,                //     \__ FulcrumServices.FulcrumUpdaterService.ServiceEnabled
            [Description("ServiceName")]                         UPDATER_SERVICE_NAME             = FULCRUM_UPDATER | 0x00000002,                //     \__ FulcrumServices.FulcrumUpdaterService.ServiceName
            [Description("ForceUpdateReady")]                    FORCE_UPDATE_READY               = FULCRUM_UPDATER | 0x00000003,                //     \__ FulcrumServices.FulcrumUpdaterService.ForceUpdateReady
            [Description("IncludePreReleases")]                  INCLUDE_PRE_RELEASES             = FULCRUM_UPDATER | 0x00000004,                //     \__ FulcrumServices.FulcrumUpdaterService.IncludePreReleases
            [Description("RefreshTimerDelay")]                   REFRESH_TIMER_DELAY              = FULCRUM_UPDATER | 0x00000005,                //     \__ FulcrumServices.FulcrumUpdaterService.RefreshTimerDelay
            [Description("EnableAutomaticUpdates")]              ENABLE_AUTOMATIC_UPDATES         = FULCRUM_UPDATER | 0x00000006,                //     \__ FulcrumServices.FulcrumUpdaterService.EnableAutomaticUpdates
            [Description("UpdaterOrgName")]                      UPDATER_ORG_NAME                 = FULCRUM_UPDATER | 0x00000007,                //     \__ FulcrumServices.FulcrumUpdaterService.UpdaterOrgName
            [Description("UpdaterRepoName")]                     UPDATER_REPO_NAME                = FULCRUM_UPDATER | 0x00000008,                //     \__ FulcrumServices.FulcrumUpdaterService.UpdaterRepoName
            [Description("UpdaterUserName")]                     UPDATER_USER_NAME                = FULCRUM_UPDATER | 0x00000009,                //     \__ FulcrumServices.FulcrumUpdaterService.UpdaterUserName
            [Description("UpdaterSecretKey")]                    UPDATER_SECRET_KEY               = FULCRUM_UPDATER | 0x0000000A,                //     \__ FulcrumServices.FulcrumUpdaterService.UpdaterSecretKey

            // ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            // NOTE: These settings entries do not contain accessible child properties!
            [Description("FulcrumOeApplications")]         FULCRUM_OE_APPLICATIONS          = 0x00800000,                                        // FulcrumOeApplications
            [Description("FulcrumMenuEntries")]            FULCRUM_MENU_ENTRIES             = 0x01000000,                                        // FulcrumMenuEntries
            [Description("FulcrumUserSettings")]           FULCRUM_USER_SETTINGS            = 0x02000000,                                        // FulcrumUserSettings

            // ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            // FulcrumSimConfigurations Settings Entries
            [Description("FulcrumSimConfigurations")]     FULCRUM_SIM_CONFIGURATIONS        = 0x04000000,                                        // FulcrumSimConfigurations
            [Description("SupportedProtocols")]              SUPPORTED_PROTOCOLS               = FULCRUM_SIM_CONFIGURATIONS | 0x00000001,        // \__ FulcrumSimConfigurations.SupportedProtocols
            [Description("PredefinedConfigurations")]        PRE_DEFINED_CONFIGURATIONS        = FULCRUM_SIM_CONFIGURATIONS | 0x00000002,        // \__ FulcrumSimConfigurations.PredefinedConfigurations
            [Description("UserDefinedConfigurations")]       USER_DEFINED_CONFIGURATIONS       = FULCRUM_SIM_CONFIGURATIONS | 0x00000003,        // \__ FulcrumSimConfigurations.UserDefinedConfigurations

            // ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            // FulcrumAppThemes Settings Entries
            [Description("FulcrumAppThemes")]             FULCRUM_APP_THEMES                = 0x08000000,                                        // FulcrumAppThemes
            [Description("AppliedAppTheme")]                 APPLIED_APP_THEME                 = FULCRUM_APP_THEMES | 0x00000001,                // \__ FulcrumAppThemes.AppliedAppTheme
            [Description("GeneratedAppPresets")]             GENERATED_APP_PRESETS             = FULCRUM_APP_THEMES | 0x00000002,                // \__ FulcrumAppThemes.GeneratedAppPresets
            [Description("UserEnteredThemes")]               USER_ENTERED_THEMES               = FULCRUM_APP_THEMES | 0x00000003,                // \__ FulcrumAppThemes.UserEnteredThemes

            // ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            // FulcrumEncryption Settings Entries
            [Description("FulcrumEncryption")]            FULCRUM_ENCRYPTION                = 0x10000000,                                        // FulcrumEncryption
            [Description("CustomAuthKey")]                   CUSTOM_AUTH_KEY                   = FULCRUM_ENCRYPTION | 0x00000001,                // \__ FulcrumEncryption.CustomAuthKey
            [Description("CustomCryptoKey")]                 CUSTOM_CRYPTO_KEY                 = FULCRUM_ENCRYPTION | 0x00000002,                // \__ FulcrumEncryption.CustomCryptoKey
            [Description("EncryptedSections")]               ENCRYPTED_SECTIONS                = FULCRUM_ENCRYPTION | 0x00000003,                // \__ FulcrumEncryption.EncryptedSections
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
        /// Public extension method used to convert a JSON Section enum flag object into a settings path
        /// </summary>
        /// <param name="SectionEnum">The enumeration object we're converting into a string</param>
        /// <returns>The string value for our setting location</returns>
        public static string ToSettingsPath(this JsonSections SectionEnum)
        {
            // Find the flags for the enum value here and combine them all
            string FlagsString = SectionEnum.ToString();
            string PathCombinedString = string.Join(".", FlagsString.Split(','));

            // Return out our string value for the flags pulled in here 
            return PathCombinedString;
        }
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
