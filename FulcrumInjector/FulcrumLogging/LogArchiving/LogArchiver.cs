using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using FulcrumInjector.FulcrumJsonHelpers;
using FulcrumInjector.FulcrumLogging.LoggerSupport;
using static FulcrumInjector.FulcrumLogging.FulcrumLogBroker;

namespace FulcrumInjector.FulcrumLogging.LogArchiving
{
    /// <summary>
    /// Compression type methods for this compressor
    /// </summary>
    public enum CompressionType
    {
        ZIP_COMPRESSION,        // ZIP File compression
        GZIP_COMPRESSION,       // GZ file compression
    }

    /// <summary>
    /// Class object which is used to generate compressed files from log file sets
    /// Use this to cleanup logging outputs 
    /// </summary>
    public class LogArchiver
    {
        // Log Archive Configuration
        public Stopwatch CompressionTimer;
        public static LogArchiveConfiguration ArchiveConfig;

        // Basic Reference values for this object
        public static string AppName { get; private set; }
        public static string OutputPath => ArchiveConfig.LogArchivePath;

        // Archive object.
        public ZipArchive ArchiveObject { get; private set; }
        public List<GZipStream> StreamsBuilt { get; private set; }

        // Info for instance objects
        public string OutputFileName { get; private set; }
        public string[] LogFilesImported { get; private set; }

        // -----------------------------------------------------------------------------------------------------

        // Event to process progress changed and done archiving.
        public event EventHandler<ArchiveProgressEventArgs> FileOperationFailure;
        public event EventHandler<ArchiveProgressEventArgs> FileAddedToArchive;
        public event EventHandler<ArchiveProgressEventArgs> ArchiveCompleted;

        /// <summary>
        /// Event trigger for progress on file stream compression built.
        /// </summary>
        /// <param name="e">Argument object of type ArchiveProgressEventArgs</param>
        protected virtual void OnFileOperationFailure(ArchiveProgressEventArgs e, Exception ExThrown)
        {
            // Write some info about this archive process here.
            WriteLogEntry($"[{e.ArchiveFileName}] ::: FAILED TO PERFORM OPERATION ON FILE!", LogType.ErrorLog);
            WriteLogEntry($"[{e.ArchiveFileName}] ::: EXCEPTION THROWN: {ExThrown.Message}");
            FileOperationFailure?.Invoke(this, e);
        }
        /// <summary>
        /// Event trigger for progress on the log file archive process
        /// </summary>
        /// <param name="e">Argument object of type ArchiveProgressEventArgs</param>
        protected virtual void OnFileAddedToArchive(ArchiveProgressEventArgs e)
        {
            // Write some info about this archive process here.
            string NameOnly = Path.GetFileNameWithoutExtension(e.FileAdded);
            WriteLogEntry($"[{e.ArchiveFileName}] ::: ADDED {NameOnly} TO ARCHIVE OK! ({e.FilesRemaining} FILES LEFT. {e.PercentDone.ToString("F2")}%)");
            FileAddedToArchive?.Invoke(this, e);
        }
        /// <summary>
        /// Event trigger for when an archive is done.
        /// </summary>
        /// <param name="e">Argument object of type ArchiveProgressEventArgs</param>
        protected virtual void OnArchiveCompleted(ArchiveProgressEventArgs e)
        {
            // Write some info about this archive process here.
            long FileSizeString = new FileInfo(e.ArchiveFile).Length;
            string TimeSpentString = e.TimeSpentRunning.ToString("mm\\:ss\\:fff");
            Logger?.WriteLog($"[{e.ArchiveFileName}] ::: ARCHIVE WAS BUILT OK! WROTE OUT {FileSizeString} BYES IN {TimeSpentString}", LogType.InfoLog);
            ArchiveCompleted?.Invoke(this, e);
        }


        /// <summary>
        /// Writes event output info to either the console or the output window.
        /// </summary>
        /// <param name="LogEntry"></param>
        /// <param name="LogLevel"></param>
        private void WriteLogEntry(string LogEntry, LogType LogLevel = LogType.TraceLog)
        {
            // Write to console/NLogger OR just write to debug output.
            if (ArchiveConfig.ProgressToConsole) { Logger?.WriteLog(LogEntry, LogLevel); }
            else { Debug.WriteLine($"[{LogLevel.ToString().ToUpper()}] ::: {LogEntry}"); }
        }

        // -----------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new log archiving instance. 
        /// </summary>
        /// <param name="LogFilesToArchive">Files to archive</param>
        public LogArchiver(string[] LogFilesToArchive, string ArchiveNameBase = null)
        {
            // Check config object.
            AppName = ArchiveNameBase ?? AppInstanceName;
            ArchiveConfig ??= new LogArchiveConfiguration
            {
                ProgressToConsole = false,
                LogArchivePath = ValueLoaders.GetConfigValue<string>("FulcrumLogging.LogArchiveSetup.LogArchivePath"),
                ArchiveOnFileCount = 50,
                ArchiveFileSetSize = 15,
                CompressionLevel = CompressionLevel.Optimal,
                CompressionStyle = CompressionType.ZIP_COMPRESSION
            };

            // Store values.
            this.CompressionTimer = new Stopwatch();
            this.LogFilesImported = LogFilesToArchive;

            // Find matches.
            string MatchPattern = @"(\d{2})(\d{2})(\d{4})-(\d{6})";
            FileInfo FirstFile = new FileInfo(LogFilesImported.FirstOrDefault());
            FileInfo LastFile = new FileInfo(LogFilesImported.LastOrDefault());
            var RegexMatchStart = Regex.Match(FirstFile.Name, MatchPattern);
            var RegexMatchEnd = Regex.Match(LastFile.Name, MatchPattern);

            // Get date values.
            string StartTime = $"{RegexMatchStart.Groups[1].Value}{RegexMatchStart.Groups[2].Value}{RegexMatchStart.Groups[3].Value.Substring(1)}-{RegexMatchStart.Groups[4].Value}";
            string StopTime = $"{RegexMatchEnd.Groups[1].Value}{RegexMatchEnd.Groups[2].Value}{RegexMatchEnd.Groups[3].Value.Substring(1)}-{RegexMatchEnd.Groups[4].Value}";
            string ArchiveExt = ArchiveConfig.CompressionStyle == CompressionType.GZIP_COMPRESSION ? "gz" : "zip";
            this.OutputFileName = $"{AppName}_{StartTime}__{StopTime}.{ArchiveExt}";

            // Store file name and delete old instances of it.
            Directory.CreateDirectory(ArchiveConfig.LogArchivePath);
            this.OutputFileName = Path.Combine(OutputPath, this.OutputFileName);
            if (File.Exists(this.OutputFileName))
            {
                try { File.Delete(OutputFileName); }
                catch { Logger?.WriteLog("FAILED TO REMOVE THE EXISTING FILE OBJECT!", LogType.ErrorLog); }
            }

            // Log name and files.
            Logger?.WriteLog($"--> NEW ARCHIVE FILE NAME IS {this.OutputFileName}");
            Logger?.WriteLog($"--> ({LogFilesImported.Length} FILES) -- {FirstFile.Name} THROUGH {LastFile.Name}", LogType.TraceLog);
        }


        /// <summary>
        /// Builds a new ZipFile archive object with a file name based on the input log files.
        /// </summary>
        /// <returns>The zip archive object we build from this method.</returns>
        private ZipArchive CreateArchive()
        { 
            // Build new archive
            FileStream OutputZipStream = new FileStream(OutputFileName, FileMode.OpenOrCreate);
            ZipArchive OutputArchive = new ZipArchive(OutputZipStream, ZipArchiveMode.Update);

            // Return archive
            this.ArchiveObject = OutputArchive;
            return this.ArchiveObject;
        }

        /// <summary>
        /// Compresses a set of files given to the object in this class into a list of possible 
        /// </summary>
        public bool CompressFiles(out ZipArchive ArchiveBuilt)
        {
            // Loop the file in the list of log file objects. Append them all.
            this.CompressionTimer.Start();

            // Now make a zip archive from the set of logs compressed.
            var FileInfoSet = LogFilesImported
                .Select(LogObj => new FileInfo(LogObj))
                .ToArray();

            // Build output ZIP
            using (ArchiveBuilt = this.CreateArchive())
            {
                // Append all e`ntries now.
                foreach (var LogFileInfo in FileInfoSet)
                {
                    try
                    {
                        // Get name of file and use it for entry contents.
                        string FullPath = LogFileInfo.FullName;
                        string NameOnly = Path.GetFileName(LogFileInfo.Name);
                        ArchiveBuilt.CreateEntryFromFile(FullPath, NameOnly, CompressionLevel.Optimal);

                        // Add to entry and update progress
                        this.OnFileAddedToArchive(new ArchiveProgressEventArgs(FullPath, this));
                    }
                    catch (Exception Ex) { this.OnFileOperationFailure(new ArchiveProgressEventArgs(LogFileInfo.FullName, this), Ex); }

                    // Remove the old base file now.
                    try { File.Delete(LogFileInfo.FullName); }
                    catch { WriteLogEntry($"FAILED TO DELETE INPUT LOG FILE {LogFileInfo.Name}!", LogType.WarnLog); }
                }
            }

            // Send out a new archive done event
            this.OnArchiveCompleted(new ArchiveProgressEventArgs(this.OutputFileName, this));

            // Store archive object Remove the Temp Directory and then remove the original log files.
            this.ArchiveObject = ArchiveBuilt;
            return true;
        }
    }
}
