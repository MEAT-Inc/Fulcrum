using System;
using System.IO;
using FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumModels.LogFileModels.FulcrumModels
{
    /// <summary>
    /// Class which holds information about a log file path from the local machine
    /// </summary>
    public class FulcrumLogFileModel : LogFile
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields
        #endregion //Fields

        #region Properties

        // Public facing properties holding information about the file and the contents of it
        public new bool LogFileExists => File.Exists(this.LogFilePath);
        public new string LogFileSize => this.LogFileExists ? new FileInfo(this.LogFilePath).Length.ToFileSize() : "N/A";

        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // --------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Spawns a new log file object instance and configures fields/properties of it
        /// </summary>
        /// <param name="InputLogPath">The path to the input log file object</param>
        public FulcrumLogFileModel(string InputLogPath) : base(InputLogPath)
        {
            // Validate our input object is a file type 
            if (InputLogPath == null)
                throw new NullReferenceException("Error! Path provided can not be null!");
            if (!Path.HasExtension(InputLogPath))
                throw new ArgumentException("Error! Path provided must be a file!");
        }
    }
}