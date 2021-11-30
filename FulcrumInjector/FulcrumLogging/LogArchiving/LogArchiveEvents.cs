using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace FulcrumInjector.FulcrumLogging.LogArchiving
{
    /// <summary>
    /// Event arguments for updating the current archive object.
    /// </summary>
    public class ArchiveProgressEventArgs : EventArgs
    {
        // Zip Archive
        public ZipArchive ArchiveObject;

        // File infos
        public string ArchiveFile;
        public string ArchiveFileName => new FileInfo(this.ArchiveFile).Name;

        // Current File object.
        public string FileAdded;
        public string[] CombinedFileSet;
        public string[] PreviousFiles => this.CombinedFileSet.TakeWhile(FileObj => new FileInfo(FileObj).Name != new FileInfo(FileAdded).Name).ToArray();
        public string[] RemainingFiles => this.CombinedFileSet.SkipWhile(FileObj => new FileInfo(FileObj).Name != new FileInfo(FileAdded).Name).ToArray();

        // Progress Infos
        public TimeSpan TimeSpentRunning;
        public int FilesAdded => this.ArchiveObject.Entries.Count;
        public int FilesRemaining => this.NumberOfFilesToArchive - FilesAdded;
        public int NumberOfFilesToArchive => CombinedFileSet.Length;
        public double PercentDone => ((double)this.FilesAdded / (double)this.NumberOfFilesToArchive) * 100;

        // ------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new event arg object for an added file entry on the archiver.
        /// </summary>
        public ArchiveProgressEventArgs(string CurrentFile, LogArchiver ArchiverInstance)
        {
            // Store values here. Most of these are pulled by class instances.
            this.FileAdded = CurrentFile;
            this.ArchiveObject = ArchiverInstance.ArchiveObject;
            this.ArchiveFile = ArchiverInstance.OutputFileName;
            this.CombinedFileSet = ArchiverInstance.LogFilesImported;
            this.TimeSpentRunning = ArchiverInstance.CompressionTimer.Elapsed;
        }
    }
}
