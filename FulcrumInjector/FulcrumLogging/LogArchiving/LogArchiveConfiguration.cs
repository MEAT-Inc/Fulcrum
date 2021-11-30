using System.IO.Compression;

namespace FulcrumInjector.FulcrumLogging.LogArchiving
{
    /// <summary>
    /// Class which contains configuration type info for an archive session
    /// </summary>
    public class LogArchiveConfiguration
    {
        // Configuration values
        public bool ProgressToConsole;
        public string LogArchivePath;

        // Archive set info.
        public int ArchiveFileSetSize;
        public int ArchiveOnFileCount;

        // Compression configuration
        [JsonIgnore] public CompressionLevel CompressionLevel;
        [JsonIgnore] public CompressionType CompressionStyle;
        [JsonProperty("CompressionLevel")] private string _compressionLevel => CompressionLevel.ToString();
        [JsonProperty("CompressionStyle")] private string _compressionStyle => CompressionStyle.ToString();

        // -----------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new instance of a log archive configuration object.
        /// </summary>
        public LogArchiveConfiguration() { }
    }
}
