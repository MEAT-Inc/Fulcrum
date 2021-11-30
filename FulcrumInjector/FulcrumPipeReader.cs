using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulcrumInjector
{
    /// <summary>
    /// Enums for pipe types
    /// </summary>
    public enum FulcrumPipe
    {
        PipeAlpha,      // Pipe number 1
        PipeBravo,      // Pipe number 2
    }
    
    /// <summary>
    /// Instance object for reading pipe server data from our fulcrum DLL
    /// </summary>
    public class FulcrumPipeReader
    {
        // Pipe Configurations for the default values.
        private static readonly string FulcrumPipeAlpha = "2CC3F0FB08354929BB453151BBAA5A15";
        private static readonly string FulcrumPipeBravo = "1D16333944F74A928A932417074DD2B3";

        // Pipe configuration information.
        public readonly FulcrumPipe PipeType;
        public readonly string PipeLocation; 

        // ---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new fulcrum pipe listener
        /// </summary>
        /// <param name="PipeId">ID Of the pipe in use for this object</param>
        public FulcrumPipeReader(FulcrumPipe PipeId)
        {
            // Store information about the pipe being configured
            this.PipeType = PipeId;
            this.PipeLocation = this.PipeType == FulcrumPipe.PipeAlpha ? FulcrumPipeAlpha : FulcrumPipeBravo;
        }
    }
}
