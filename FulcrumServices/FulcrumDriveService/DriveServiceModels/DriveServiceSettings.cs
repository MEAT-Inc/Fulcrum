namespace FulcrumDriveService.DriveServiceModels
{
    /// <summary>
    /// Class object holding our configuration for a drive service instance
    /// </summary>
    public class DriveServiceSettings
    {
        // Public facing properties for our drive service configuration
        public bool DriveEnabled { get; set; }      // Tells us if the service is enabled or not 
        public string ServiceName { get; set; }     // Stores the name of the google drive service
    }
}
