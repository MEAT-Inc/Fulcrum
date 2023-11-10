using System;
using System.Linq;
using Newtonsoft.Json;
using FulcrumService.JsonConverters;

namespace FulcrumService.FulcrumServiceModels
{
    /// <summary>
    /// Class object holding a routine to execute on our service pipes.
    /// This should ONLY be used when we're using a client service to control the host
    /// </summary>
    [JsonConverter(typeof(FulcrumServicePipeActionJsonConverter))]
    public class FulcrumServicePipeAction
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields
        #endregion // Fields

        #region Properties
        
        // Public facing properties holding information about our pipe actions
        public Guid PipeActionGuid { get; internal set; }
        public string PipeMethodName { get; internal set; }
        public Type[] PipeArgumentTypes { get; internal set; }
        public object[] PipeMethodArguments { get; internal set; }
        public FulcrumServiceBase.ServiceTypes PipeServiceType { get; internal set; }


        // Public facing properties holding information about our command execution results
        public bool IsExecuted { get; internal set; }
        public object PipeCommandResult { get; internal set; }

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Private JSON Constructor for a Pipe Service Action
        /// </summary>
        internal FulcrumServicePipeAction() { }
        /// <summary>
        /// Spawns a new pipe action routine to be invoked to our service host
        /// </summary>
        /// <param name="ServiceType">The type of service we're invoking this method on</param>
        /// <param name="PipeCommand">Text of the command being issued for our pipe instance</param>
        /// <param name="CommandArguments">The arguments passed into our pipe command object</param>
        public FulcrumServicePipeAction(FulcrumServiceBase.ServiceTypes ServiceType, string PipeCommand, params object[] CommandArguments)
        {
            // Store our new pipe GUID value here 
            this.PipeActionGuid = Guid.NewGuid();

            // Store our pipe command name and arguments
            this.PipeMethodName = PipeCommand;
            this.PipeServiceType = ServiceType;
            this.PipeMethodArguments = CommandArguments;
            this.PipeArgumentTypes = this.PipeMethodArguments.Select(ArgObj => ArgObj.GetType()).ToArray();
        }
    }
}
