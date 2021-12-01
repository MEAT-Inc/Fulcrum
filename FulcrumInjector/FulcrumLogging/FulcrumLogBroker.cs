using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumJsonHelpers;
using FulcrumInjector.FulcrumLogging.LogArchiving;
using FulcrumInjector.FulcrumLogging.LoggerObjects;
using FulcrumInjector.FulcrumLogging.LoggerSupport;
using Newtonsoft.Json;
using NLog;
using NLog.Config;

namespace FulcrumInjector.FulcrumLogging
{
    /// <summary>
    /// Base falcon logging broker object.
    /// </summary>
    public sealed class FulcrumLogBroker
    {
        // Singleton instance configuration from the broker.
        private static FulcrumLogBroker _brokerInstance;
        public static FulcrumLogBroker BrokerInstance => _brokerInstance ?? (_brokerInstance = new FulcrumLogBroker());

        // Logging infos.
        public static string MainLogFileName;
        public static string AppInstanceName;
        public static string BaseOutputPath;
        public static MasterLogger Logger;
        public static WatchdogLoggerQueue LoggerQueue = new WatchdogLoggerQueue();

        // Init Done or not.
        public static LogType MinLevel;
        public static LogType MaxLevel;

        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new ERS Object and generates the logger output object.
        /// </summary>
        /// <param name="LoggerName"></param>
        private FulcrumLogBroker()
        {
            // Setup App constants here.
            if (AppInstanceName == null) 
            {
                // Try and Set Process name. If Null, get the name of the called app
                var ProcessModule = Process.GetCurrentProcess().MainModule;
                AppInstanceName = ProcessModule != null
                    ? new FileInfo(ProcessModule.FileName).Name
                    : new FileInfo(Environment.GetCommandLineArgs()[0]).Name;
            }

            // Path to output and base file name.
            if (BaseOutputPath == null)
            {
                // Setup Outputs in the docs folder.
                string DocsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                BaseOutputPath = Path.Combine(DocsFolder, AppInstanceName + "_Logging");
            }

            // Get Root logger and build queue.
            if (MinLevel == default) MinLevel = LogType.TraceLog;
            if (MaxLevel == default) MaxLevel = LogType.FatalLog;
        }


        /// <summary>
        /// Stores the broker initial object values before calling the CTOR so the values provided may be used for configuration
        /// </summary>
        /// <param name="InstanceName">Name of the app being run.</param>
        /// <param name="BaseLogPath">Path to write output to.</param>
        public static void ConfigureLoggingSession(string InstanceName, string BaseLogPath, int MinLogLevel = 0, int MaxLogLevel = 5)
        {
            // Store values and Log Levels
            AppInstanceName = InstanceName;
            BaseOutputPath = BaseLogPath;

            // Store log values
            MinLevel = (LogType)MinLogLevel;
            MaxLevel = (LogType)MaxLogLevel;
        }
        /// <summary>
        /// Cleans out the log history objects for the current server
        /// <param name="ArchivePath">Path to store the archived files in</param>
        /// <param name="MaxFileCount">Max number of current logs to contain</param>
        /// <param name="ArchiveSetSize">Number of files to contain inside each archive file.</param>
        /// </summary>
        public static void CleanupLogHistory(string ArchiveConfigString, string FileNameFilter = "")
        {
            // Build an archive object here.
            LogArchiveConfiguration Config;
            try
            {
                // Pull value, and store it.
                Config = JsonConvert.DeserializeObject<LogArchiveConfiguration>(ArchiveConfigString);
                Logger?.WriteLog($"PULLED ARCHIVE CONFIG FROM JSON CONFIG FILE OK! JSON: \n{JsonConvert.SerializeObject(Config, Formatting.Indented)}", LogType.TraceLog);
            }
            catch
            {
                // Log failure and build config.
                Logger?.WriteLog("FAILED TO PARSE INPUT JSON FOR ARCHIVE CONFIGURATION VALUES!", LogType.WarnLog);
                Logger?.WriteLog("BUILDING NEW CONFIGURATION NOW...", LogType.WarnLog);

                // Build Config
                Config = new LogArchiveConfiguration
                {
                    ProgressToConsole = false,
                    LogArchivePath = "C:\\Program Files (x86)\\MEAT Inc\\FulcrumShim\\FulcrumLogs\\FulcrumArchives",
                    ArchiveOnFileCount = 50,
                    ArchiveFileSetSize = 15,
                    CompressionLevel = CompressionLevel.Optimal,
                    CompressionStyle = CompressionType.ZIP_COMPRESSION
                };

                // Log new config.
                Logger?.WriteLog($"BUILT NEW JSON ARCHIVE OBJECT: \n{JsonConvert.SerializeObject(Config, Formatting.Indented)}", LogType.TraceLog);
            }

            // Gets the lists of files in the log file directory and splits them into sets for archiving.
            if (FileNameFilter == "") { FileNameFilter = ValueLoaders.GetConfigValue<string>("AppInstanceName"); }
            Logger?.WriteLog("CLEANING UP OLD FILES IN THE LOG OUTPUT DIRECTORY NOW...", LogType.InfoLog);
            string[] LogFilesLocated = Directory.GetFiles(BaseOutputPath).OrderBy(FileObj => new FileInfo(FileObj).CreationTime)
                .Where(FileObj => FileObj.Contains(".log") && FileObj.Contains(FileNameFilter))
                .ToArray();

            // Remove 5 files from this list to keep current log files out.
            LogFilesLocated = LogFilesLocated.Take(LogFilesLocated.Length - 5).ToArray();
            List<string[]> LogFileArchiveSets = LogFilesLocated.Select((FileName, FileIndex) => new { Index = FileIndex, Value = FileName })
                .GroupBy(CurrentFile => CurrentFile.Index / Config.ArchiveFileSetSize)
                .Select(FileSet => FileSet.Select(FileValue => FileValue.Value).ToArray())
                .ToList();

            // Build output dir.
            Directory.CreateDirectory(Config.LogArchivePath);
            Logger?.WriteLog($"VERIFIED DIRECTORY {Config.LogArchivePath} EXISTS FOR OUTPUT GZ FILES!", LogType.InfoLog);
            Logger?.WriteLog($"FOUND A TOTAL OF {LogFilesLocated.Length} FILES AND SPLIT THEM INTO A TOTAL OF {LogFileArchiveSets.Count} SETS OF FILES", LogType.InfoLog);

            // Now loop each set, build a new Archiver and get output objects.
            LogArchiver.ArchiveConfig = Config;
            Logger?.WriteLog("STORED CONFIG FOR ARCHIVES OK! KICKING OFF LOG ARCHIVAL PROCESS IN A BACKGROUND THREAD NOW...", LogType.WarnLog);

            // Run this in a task so we don't hang up the whole main operation of the API
            Task.Run(() =>
            {
                // Run loop on all file set objects
                foreach (var LogFileSet in LogFileArchiveSets)
                {
                    // Build Archiver here then build compressed set.
                    var ArchiveBuilder = new LogArchiver(LogFileSet);
                    string ArchiveName = new FileInfo(ArchiveBuilder.OutputFileName).Name;
                    Logger?.WriteLog($"[{ArchiveName}] --> PULLING ARCHIVE OBJECT TO USE NOW...", LogType.TraceLog);

                    // Get archive object here and then store file information into it.
                    ZipArchive OutputArchive;
                    Logger?.WriteLog($"[{ArchiveName}] --> COMPRESSION FOR FILE SET STARTING NOW...", LogType.TraceLog);

                    // Write entries for the files into the archiver now.
                    if (ArchiveBuilder.CompressFiles(out OutputArchive)) Logger?.WriteLog($"[{ArchiveName}] --> GENERATED NEW ZIP FILE OK!", LogType.InfoLog);
                    else Logger?.WriteLog($"[{ArchiveName}] --> FAILED TO WRITE LOG ENTRIES FOR ARCHIVE SET!", LogType.ErrorLog);
                }
            });
        }


        /// <summary>
        /// Actually spins up a new logger object once the broker is initialized.
        /// </summary>
        public void FillBrokerPool()
        {
            // DO NOT RUN THIS MORE THAN ONCE!
            if (Logger != null) { return; }

            // Make a new NLogger Config
            if (LogManager.Configuration == null) LogManager.Configuration = new LoggingConfiguration();

            // Build logger object now.
            MainLogFileName = Path.Combine(BaseOutputPath, $"{AppInstanceName}_Logging_{DateTime.Now.ToString("MMddyyy-HHmmss")}.log");
            Logger = new MasterLogger(
                $"{AppInstanceName}",
                MainLogFileName,
                (int)MinLevel,
                (int)MaxLevel
            );

            // Build and add to queue.
            LoggerQueue.AddLoggerToPool(Logger);

            // Log output info for the current DLL Assy
            string AssyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Logger.WriteLog("LOGGER BROKER BUILT AND SESSION MAIN LOGGER HAS BEEN BOOTED CORRECTLY!", LogType.WarnLog);
            Logger.WriteLog($"--> TIME OF DLL INIT: {DateTime.Now.ToString("g")}", LogType.InfoLog);
            Logger.WriteLog($"--> DLL ASSEMBLY VER: {AssyVersion}", LogType.InfoLog);
            Logger.WriteLog($"--> HAPPY LOGGING. LETS HOPE EVERYTHING GOES WELL...", LogType.InfoLog);
        }
    }
}
