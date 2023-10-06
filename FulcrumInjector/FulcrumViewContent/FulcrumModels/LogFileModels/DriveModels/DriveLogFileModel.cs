using FulcrumInjector.FulcrumViewSupport;
using FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters;
using Google.Apis.Drive.v3;
using System;
using System.IO;
using SharpLogging;

// Static using for google drive file type
using GoogleDriveFile = Google.Apis.Drive.v3.Data.File;

namespace FulcrumInjector.FulcrumViewContent.FulcrumModels.LogFileModels.DriveModels
{
    /// <summary>
    /// Class which holds information about a log file path pulled in from our google drive
    /// </summary>
    public class DriveLogFileModel : LogFileModel
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Private backing fields for a drive log file object
        private readonly GoogleDriveFile _sourceDriveFile;

        #endregion // Fields

        #region Properties

        // Private property holding our drive service object
        private DriveService _driveService
        {
            get
            {
                // If the service exists, don't reconfigure it
                if (FulcrumDriveBroker.DriveService != null)
                    return FulcrumDriveBroker.DriveService;

                // If the drive service is not built, configure it now if possible
                if (!FulcrumDriveBroker.ConfigureDriveService(out var DriveService))
                    throw new InvalidOperationException("Error! Failed to configure Google Drive Service!");

                // Return the built drive service
                return DriveService;

            }
        }

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
                // Download the file using the ID of it and our drive service
                FilesResource.GetRequest LocatedResource = _driveService.Files.Get(this._sourceDriveFile.Id);
                LocatedResource.Download(new FileStream(DownloadPath, FileMode.OpenOrCreate));

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