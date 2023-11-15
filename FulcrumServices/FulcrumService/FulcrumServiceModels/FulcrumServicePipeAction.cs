using System;
using System.ComponentModel;
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
        public string PipeActionName { get; internal set; }
        public Type[] PipeArgumentTypes { get; internal set; }
        public object[] PipeMethodArguments { get; internal set; }
        public ReflectionTypes ReflectionType { get; internal set; }
        public FulcrumServiceBase.ServiceTypes PipeServiceType { get; internal set; }


        // Public facing properties holding information about our command execution results
        public bool IsExecuted { get; internal set; }
        public object PipeCommandResult { get; internal set; }

        #endregion // Properties

        #region Structs and Classes

        /// <summary>
        /// Enumeration holding our different types of reflection we support
        /// </summary>
        public enum ReflectionTypes
        { 
            // Types of reflection we're able to perform on our services
            [Description("N/A")]             NO_TYPE,          // Default value. No reflection type
            [Description("Get Member")]      GET_MEMBER,       // Specifies reflection of a member value
            [Description("Set Member")]      SET_MEMBER,       // Specified setting of a member value
            [Description("Execute Method")]  METHOD_TYPE,      // Specifies reflection of a method action
        }

        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Private JSON Constructor for a Pipe Service Action
        /// </summary>
        internal FulcrumServicePipeAction() { }

        /// <summary>
        /// Spawns a new pipe action routine to be invoked to our service host for pulling field values
        /// </summary>
        /// <param name="ServiceType">The type of service we're invoking this method on</param>
        /// <param name="MemberName">The name of the field or property we're pulling our value for</param>
        public FulcrumServicePipeAction(FulcrumServiceBase.ServiceTypes ServiceType, string MemberName)
        {
            // Store our new pipe GUID value and reflection type here
            this.PipeActionGuid = Guid.NewGuid();
            this.ReflectionType = ReflectionTypes.GET_MEMBER;

            // Store our pipe command name and arguments
            this.PipeActionName = MemberName;
            this.PipeServiceType = ServiceType;
        }
        /// <summary>
        /// Spawns a new pipe action routine to be invoked to our service host for pulling field values
        /// </summary>
        /// <param name="ServiceType">The type of service we're invoking this method on</param>
        /// <param name="MemberName">The name of the field or property we're setting the value for</param>
        /// <param name="MemberValue">The value of the field or property we're setting</param>
        public FulcrumServicePipeAction(FulcrumServiceBase.ServiceTypes ServiceType, string MemberName, object MemberValue)
        {
            // Store our new pipe GUID value and reflection type here
            this.PipeActionGuid = Guid.NewGuid();
            this.ReflectionType = ReflectionTypes.SET_MEMBER;

            // Store our pipe command name and arguments
            this.PipeActionName = MemberName;
            this.PipeServiceType = ServiceType;
            this.PipeMethodArguments = new[] { MemberValue };
            this.PipeArgumentTypes = this.PipeMethodArguments.Select(ArgObj => ArgObj.GetType()).ToArray();
        }
        /// <summary>
        /// Spawns a new pipe action routine to be invoked to our service host for method execution
        /// </summary>
        /// <param name="ServiceType">The type of service we're invoking this method on</param>
        /// <param name="PipeMethodName">Text of the command being issued for our pipe instance</param>
        /// <param name="CommandArguments">The arguments passed into our pipe command object</param>
        public FulcrumServicePipeAction(FulcrumServiceBase.ServiceTypes ServiceType, string PipeMethodName, object[] CommandArguments)
        {
            // Store our new pipe GUID value and reflection type here 
            this.PipeActionGuid = Guid.NewGuid();
            this.ReflectionType = ReflectionTypes.METHOD_TYPE;

            // Store our pipe command name and arguments
            this.PipeActionName = PipeMethodName;
            this.PipeServiceType = ServiceType;
            this.PipeMethodArguments = CommandArguments;
            this.PipeArgumentTypes = this.PipeMethodArguments.Select(ArgObj => ArgObj.GetType()).ToArray();
        }
    }
}
