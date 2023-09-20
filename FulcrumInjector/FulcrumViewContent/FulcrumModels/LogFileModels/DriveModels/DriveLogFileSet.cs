using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using FulcrumInjector.FulcrumViewSupport;
using FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using NLog.Filters;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewContent.FulcrumModels.LogFileModels.DriveModels
{
    /// <summary>
    /// Internal class holding the values for our different types of log files supported on a google drive location
    /// </summary>
    internal class DriveLogFileSet : LogFileSet
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Private backing fields for folder configuration
        private readonly File _sourceDriveFolder;           // Folder object used to build this log model set
        private readonly Regex _nameParseRegex = new(       // The regex used to filter names of file sets
            @"(\d{4})_([^_]+)_([^_]+)_([^\s]+)", 
            RegexOptions.Compiled);

        #endregion // Fields

        #region Properties

        // Public facing readonly properties holding information about our log set
        public string LogSetVIN { get; private set; }
        public string LogSetYear { get; private set; }
        public string LogSetMake { get; private set; }
        public string LogSetModel { get; private set; }
        public string LogSetName { get; private set; }

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Spawns a new log file object instance and configures fields/properties of it
        /// </summary>
        /// <param name="SourceDriveFolder">The google drive log folder we're building this set from</param>
        /// <exception cref="NullReferenceException">Thrown when the input log object is null</exception>
        /// <exception cref="ArgumentException">Thrown when the input log file object is not a file</exception>
        /// <exception cref="InvalidOperationException">Thrown when the drive service can not be allocated</exception>
        public DriveLogFileSet(File SourceDriveFolder) : base()
        {
            // Make sure the input file object is a type of folder
            if (SourceDriveFolder == null)
                throw new NullReferenceException("Error! Input log folder was null!");
            if (SourceDriveFolder.MimeType != FulcrumDriveBroker.ResultTypes.FOLDERS_ONLY.ToDescriptionString())
                throw new ArgumentException("Error! Input drive object is not a folder!");

            // Store the input log folder and find the files in it
            this._sourceDriveFolder = SourceDriveFolder;

            // Configure properties of the log folder
            var FilterResults = this._nameParseRegex.Match(SourceDriveFolder.Name);
            if (!FilterResults.Success)
            {
                // Log out that this parse routine failed and exit out
                _logSetLogger.WriteLog($"ERROR! FAILED TO PARSE LOG FOLDER NAME {SourceDriveFolder.Name}!", LogType.ErrorLog);
                return; 
            }

            // Only store these values if the parse routine passes correctly.
            this.LogSetName = this._sourceDriveFolder.Name;
            this.LogSetYear = FilterResults.Groups[1].Value;
            this.LogSetMake = FilterResults.Groups[2].Value;
            this.LogSetModel = FilterResults.Groups[3].Value;
            this.LogSetVIN = FilterResults.Groups[4].Value;
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Helper method which is used to refresh all files in the current folder and store them on this set
        /// </summary>
        /// <returns>True if the refresh routine passes. False if not </returns>
        /// <exception cref="InvalidOperationException">Thrown when the drive service can not be allocated</exception>
        public bool RefreshFolderFiles()
        {
            // Build a request to list all the files in the folder 
            if (!FulcrumDriveBroker.ListFolderContents(this._sourceDriveFolder.Id, out var LocatedFiles, FulcrumDriveBroker.ResultTypes.FILES_ONLY))
                throw new InvalidOperationException($"Error! Failed to refresh Drive Contents for location {this._sourceDriveFolder.Id}!");

            // Clear out the existing log file models if needed
            this[LogFileModel.LogFileTypes.PASSTHRU_FILE].Clear();
            this[LogFileModel.LogFileTypes.EXPRESSIONS_FILE].Clear();
            this[LogFileModel.LogFileTypes.SIMULATIONS_FILE].Clear(); 
            this[LogFileModel.LogFileTypes.UNKNOWN_FILE].Clear();

            // Iterate all the located files and store them for this set object one by one
            foreach (var LocatedFile in LocatedFiles)
            {
                // Build a new log file model for the next object provided and store it if needed
                DriveLogFileModel LocatedFileModel = new DriveLogFileModel(LocatedFile);
                if (!this[LocatedFileModel.LogFileType].Contains(LocatedFileModel)) 
                    this[LocatedFileModel.LogFileType].Add(LocatedFileModel);
            }

            // Return out based on how many files exist for this set
            return this.TotalLogCount != 0;
        }
        /// <summary>
        /// Helper method used to download all the log files for a given log set and store them on the disk
        /// </summary>
        /// <param name="DownloadPath">The path of the folder to save log files into</param>
        /// <returns>True if all logs are pulled in. False if not </returns>
        public bool DownloadLogSet(string DownloadPath)
        {
            // TODO: Build logic for downloading all log files
            return true;
        }
    }
}
