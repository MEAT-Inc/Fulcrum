using System;

namespace FulcrumInjector.FulcrumLogic.InjectorPipes.PipeEvents
{
    /// <summary>
    /// Event object fired when there's new data processed onto our pipe reader
    /// </summary>
    public class FulcrumPipeDataReadEventArgs : EventArgs
    {
        // Properties of the pipe data we processed in.
        public DateTime TimeProcessed;
        public uint ByteDataLength;
        public byte[] PipeByteData;
        public string PipeDataString;

        /// <summary>
        /// Builds new event arguments for a pipe reader processing state
        /// </summary>
        public FulcrumPipeDataReadEventArgs() { }
    }
}
