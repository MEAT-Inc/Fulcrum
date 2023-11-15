using System;
using System.IO;
using FulcrumDriveService;
using FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters;
using FulcrumSupport;
using Google.Apis.Drive.v3;
using SharpLogging;

// Static using for google drive file type
using GoogleDriveFile = Google.Apis.Drive.v3.Data.File;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumModels.LogFileModels.DriveModels
{
    /// <summary>
    /// Class which holds information about a log file path pulled in from our google drive
    /// </summary>
    public class DriveLogFileModel : LogFile
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Private backing fields for a drive log file object
        private readonly GoogleDriveFile _sourceDriveFile;

        #endregion // Fields

        #region Properties

        // Public facing properties holding information about the log file instance
        public new bool LogFileExists => (bool)!this._sourceDriveFile?.Trashed;
        public new string LogFileSize => this.LogFileExists ? this._sourceDriveFile?.Size.Value.ToFileSize() : "N/A";

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // --------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Spawns a new log file object instance and configures fields/properties of it
        /// </summary>
        /// <param name="InputDriveFile">The google drive log file we're building this log from</param>
        /// <exception cref="NullReferenceException">Thrown when the input log object is null</exception>
        /// <exception cref="ArgumentException">Thrown when the input log file object is not a file</exception>
        public DriveLogFileModel(GoogleDriveFile InputDriveFile) : base(InputDriveFile)
        {
            // Validate our input object is a file type 
            if (InputDriveFile == null)
                throw new NullReferenceException("Error! Input drive object is null!");
            if (InputDriveFile.MimeType.Contains("folder"))
                throw new ArgumentException("Error! Input drive object can not be a folder!");

            // Store the base log file object
            this._sourceDriveFile = InputDriveFile;
        }

        // --------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Public helper method used to download this log file model from the google drive
        /// </summary>
        /// <param name="DownloadPath">The path to store the log file into for the download</param>
        /// <returns>True if downloaded correctly. False if not or if the file does not exist </returns>
        public bool DownloadLogFile(string DownloadPath)
        {
            try
            {
                // Log out we're downloading our file instance here 
                _logFileLogger.WriteLog($"DOWNLOADING LOG FILE {this.LogFileName} TO {DownloadPath} NOW...", LogType.InfoLog);

                // Download the file using the ID of it and our drive service
                FulcrumDrive DriveService = FulcrumDrive.InitializeDriveService().Result;
                if (!DriveService.DownloadDriveFile(this._sourceDriveFile.Id, DownloadPath))
                    throw new InvalidOperationException("Error! Failed to download log file using FulcrumDriveService!");

                // Return out based on if the file exists or not
                return File.Exists(DownloadPath);
            }
            catch (Exception DownloadEx)
            {
                // Write the exception out for the failed download request
                _logFileLogger.WriteLog($"ERROR! FAILED TO DOWNLOAD FILE {this._sourceDriveFile.Name}!", LogType.ErrorLog);
                _logFileLogger.WriteException("EXCEPTION THROWN IS BEING LOGGED BELOW", DownloadEx);
                return false;
            }
        }
    }
}