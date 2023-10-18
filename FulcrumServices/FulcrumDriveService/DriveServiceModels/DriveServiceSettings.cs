using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FulcrumEncryption;
using FulcrumService;

namespace FulcrumDriveService.DriveServiceModels
{
    /// <summary>
    /// Class object holding our configuration for the settings section to control a drive service instance
    /// </summary>
    public class DriveServiceSettings : FulcrumServiceSettings
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields
        #endregion // Fields

        #region Properties

        // Public facing properties holding configuration for our drive service
        public string ApplicationName { get; set; } 
        [EncryptedValue] public string GoogleDriveId { get; set; }

        // Public facing properties holding configuration for drive authorization
        public DriveConfiguration ExplorerConfiguration { get; set; }
        public DriveAuthorization ExplorerAuthorization { get; set; }

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes
    }
}
