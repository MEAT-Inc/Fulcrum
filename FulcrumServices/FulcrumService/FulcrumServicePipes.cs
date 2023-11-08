using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Pipes;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FulcrumSupport;
using Newtonsoft.Json;
using SharpLogging;

namespace FulcrumService
{
    /// <summary>
    /// Class object holding our definition and logic for a service pipe
    /// </summary>
    public class FulcrumServicePipe
    {
        #region Custom Events

        // Public facing events for pipe action routines
        public EventHandler<ServicePipeAction> PipeActionInvoked;               // Fired when an action execution is started
        public EventHandler<ServicePipeAction> PipeActionCompleted;             // Fired when an action is executed on our pipe object

        #endregion // Custom Events

        #region Fields
        
        // Public facing fields holding information about our service pipe
        public readonly string ServicePipeName;                         // Name of the pipe service being run here
        public readonly ServicePipeTypes ServicePipeType;               // The type of service this class represents

        // Private facing readonly fields for our service instance and pipe logger
        private readonly SharpLogger _servicePipeLogger;                // Logger instance for our pipe objects
        private readonly FulcrumServiceBase _serviceInstance;           // The service instance being consumed for this pipe

        // Private fields for our service pipe objects                
        private StreamWriter _servicePipeWriter;                        // The service pipe writer for sending commands to a service or responding with output
        private StreamReader _servicePipeReader;                        // The service pipe reader for reading input commands
        private NamedPipeClientStream _clientPipe;                      // The client pipe stream for our client service
        private NamedPipeServerStream _servicePipe;                     // The host pipe stream for our host service instance

        // Private fields used to run our pipe in the background for our service
        private Task _servicePipeTask;                                  // Task holding our background pipe operation
        private CancellationTokenSource _servicePipeTaskTokenSource;    // CancellationTokenSource for our pipe operation
        private CancellationToken _servicePipeTaskCancellationToken;    // CancellationToken object for our pipe operation

        #endregion // Fields

        #region Properties
        #endregion // Properties

        #region Structs and Classes

        /// <summary>
        /// Enumeration holding our different pipe types for service communication
        /// </summary>
        public enum ServicePipeTypes
        {
            [Description("UNDEFINED")] UNDEFINED,    // Undefined pipe types
            [Description("HOST")] HOST_PIPE,         // Host pipe (Service Instance)
            [Description("CLIENT")] CLIENT_PIPE,     // Client pipe (Consumers)
        }

        /// <summary>
        /// Class object holding a routine to execute on our service pipes.
        /// This should ONLY be used when we're using a client service to control the host
        /// </summary>
        public class ServicePipeAction
        {
            #region Custom Events
            #endregion // Custom Events

            #region Fields

            // Public facing readonly fields for our pipe actions
            public readonly Guid PipeActionGuid;
            public readonly string PipeMethodName;
            public readonly object[] PipeMethodArguments;
            public readonly FulcrumServiceBase.ServiceTypes PipeServiceType;

            #endregion // Fields

            #region Properties

            // Public facing properties holding information about our command execution results
            public bool IsExecuted { get; internal set; }
            public object PipeCommandResult { get; internal set; }

            #endregion // Properties

            #region Structs and Classes
            #endregion // Structs and Classes

            // ------------------------------------------------------------------------------------------------------------------------------------------

            /// <summary>
            /// Spawns a new pipe action routine to be invoked to our service host
            /// </summary>
            /// <param name="ServiceType">The type of service we're invoking this method on</param>
            /// <param name="PipeCommand">Text of the command being issued for our pipe instance</param>
            /// <param name="CommandArguments">The arguments passed into our pipe command object</param>
            public ServicePipeAction(FulcrumServiceBase.ServiceTypes ServiceType, string PipeCommand, params object[] CommandArguments)
            {
                // Store our new pipe GUID value here 
                this.PipeActionGuid = Guid.NewGuid();

                // Store our pipe command name and arguments
                this.PipeMethodName = PipeCommand;
                this.PipeServiceType = ServiceType;
                this.PipeMethodArguments = CommandArguments;
            }
        }

        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// CTOR for a new service pipe instance. Spawns the given pipe type for the service instance provided
        /// </summary>
        /// <param name="ServiceInstance">The service instance we're building pipes for</param>
        internal FulcrumServicePipe(FulcrumServiceBase ServiceInstance)
        {
            // Spawn our pipe logger and build the pipe name here
            this._serviceInstance = ServiceInstance;
            this.ServicePipeName = $"{this._serviceInstance.ServiceName.ToDescriptionString()}Pipe";
            this._servicePipeLogger = new SharpLogger(LoggerActions.UniversalLogger, $"{this.ServicePipeName}Logger");
            this._servicePipeLogger.WriteLog($"SPAWNING NEW SERVICE PIPE NAMED {this.ServicePipeName}...", LogType.WarnLog);

            // Store our pipe type here and initialize our pipe objects
            this.ServicePipeType = this._serviceInstance.IsServiceClient
                ? ServicePipeTypes.CLIENT_PIPE
                : ServicePipeTypes.HOST_PIPE;

            // Build a new set of stream readers and writers for our pipe object along with a command queue
            if (!this._spawnServicePipes())
                throw new InvalidOperationException($"Error! Failed to configure pipe objects for pipe {this.ServicePipeName}!");
            if (!this._initializeServicePipe())
                throw new InvalidOperationException($"Error! Failed to start pipe service task for pipe {this.ServicePipeName}!");
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Queues a new action to store on our pipe action queue and prepares to execute it
        /// </summary>
        /// <param name="PipeAction">The action to invoke on our pipe host</param>
        /// <returns>True if the pipe action is queued. False if not</returns>
        /// <exception cref="InvalidOperationException">Thrown when we try to queue a pipe action for our host pipe</exception>
        public bool QueuePipeAction(ServicePipeAction PipeAction)
        {
            // Make sure we've got a client pipe object first
            if (this.ServicePipeType != ServicePipeTypes.CLIENT_PIPE)
            {
                // Make sure we've got a client pipe before queueing. If we're not, exit out
                this._servicePipeLogger.WriteLog("ERROR! CAN NOT QUEUE PIPE ACTIONS ON OUR HOST PIPE INSTANCE!", LogType.ErrorLog);
                this._servicePipeLogger.WriteLog("SERVICE PIPES CAN ONLY QUEUE ACTIONS FOR CLIENT PIPE TYPES!", LogType.ErrorLog);
                throw new InvalidOperationException("Error! Unable to queue pipe actions for host pipes!");
            }
            if (PipeAction.PipeServiceType != this._serviceInstance.ServiceType)
            {
                // Log out that we've got an incompatible pipe action type here
                this._servicePipeLogger.WriteLog($"ERROR! CAN NOT INVOKE PIPE ACTION TYPE {PipeAction.PipeServiceType} FOR A {this._serviceInstance.ServiceType} SERVICE!", LogType.ErrorLog);
                this._servicePipeLogger.WriteLog("SERVICES AND ACTIONS MUST HAVE THE SAME TYPE TO BE INVOKED!", LogType.ErrorLog);
                throw new InvalidOperationException("Error! Pipe action type and service type do not match!");
            }

            // Queue the command for our pipe and store it if it's a new unique action
            string PipeActionGuid = PipeAction.PipeActionGuid.ToString("D").ToUpper();
            this._servicePipeLogger.WriteLog($"QUEUEING AND SENDING ACTION {PipeActionGuid} TO PIPE HOST NOW...");

            // Write our pipe action as a JSON string to our host pipe here
            string PipeActionJson = JsonConvert.SerializeObject(PipeActionGuid);
            this._servicePipeWriter.WriteLine(PipeActionJson);
            this._servicePipeWriter.Flush();

            // Log out that we've sent this action out to be invoked and fire an event for it
            this._servicePipeLogger.WriteLog($"QUEUED PIPE ACTION {PipeActionGuid} CORRECTLY! EXECUTING WHEN READY!", LogType.InfoLog);
            return true;
        }
        /// <summary>
        /// Waits for a queued action to complete for the given time
        /// </summary>
        /// <param name="ActionGuid">The GUID of the action we're looking for</param>
        /// <param name="TimeoutTime">The time to wait for our action to complete</param>
        /// <param name="PipeAction">The action queued and responded to</param>
        /// <returns>True if an action response is found. False if not</returns>
        public bool WaitForAction(Guid ActionGuid, out ServicePipeAction PipeAction, int TimeoutTime = 5000)
        {
            // Wait for a new event to fie for our pipe reader
            Stopwatch TimeoutTimer = new Stopwatch();
            TimeoutTimer.Start();

            // While our elapsed time is less than our timeout time try and read our content
            while (TimeoutTimer.ElapsedMilliseconds < TimeoutTime)
            {
                // Pull in the JSON content of our action object returned
                string NextActionString = this._servicePipeReader.ReadLine();
                PipeAction = JsonConvert.DeserializeObject<ServicePipeAction>(NextActionString);
                if (PipeAction.PipeActionGuid != ActionGuid) continue;

                // Invoke an action completed event to pull in our new pipe action object
                PipeAction.IsExecuted = true;
                this.PipeActionCompleted?.Invoke(this, PipeAction);
                this._servicePipeLogger.WriteLog($"PROCESSED PIPE ACTION {ActionGuid.ToString("D").ToUpper()} RESPONSE CORRECTLY!", LogType.InfoLog);
                return true; 
            }
            
            // If we got here without getting a valid response, return false back 
            this._servicePipeLogger.WriteLog($"ERROR! FAILED TO FIND A RESPONSE FOR ACTION {ActionGuid.ToString("D").ToUpper()} IN OUR GIVEN TIMEOUT!", LogType.ErrorLog);
            PipeAction = null;
            return false;
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Helper method used to spawn pipe instances for communication to and from this service outside our instance
        /// </summary>
        /// <returns>True if the needed pipes are built. False if not</returns>
        private bool _spawnServicePipes()
        {
            // Log out our pipe name and configuration here
            this._servicePipeLogger.WriteLog($"{this.ServicePipeType.ToDescriptionString()} SERVICE IDENTIFIED!", LogType.InfoLog);
            this._servicePipeLogger.WriteLog($"SPAWNING A {this.ServicePipeType.ToDescriptionString()} PIPE OBJECT NOW...", LogType.InfoLog);
            this._servicePipeLogger.WriteLog($"BUILDING NEW {this.ServicePipeType.ToDescriptionString()} SERVICE PIPE CONNECTION NAMED {this.ServicePipeName}...");

            // Spawn our pipe based on the pipe type here
            try
            {
                // Try and build our pipe instance and setup new readers/writers for it
                if (this._serviceInstance.IsServiceClient)
                {
                    // Spawn a client pipe instance for client connections
                    this._clientPipe = new NamedPipeClientStream(this.ServicePipeName);
                    this._servicePipeReader = new StreamReader(this._clientPipe);
                    this._servicePipeWriter = new StreamWriter(this._clientPipe);

                    // Connect our client pipe and exit out 
                    this._clientPipe.Connect();
                }
                else
                {
                    // Spawn a host pipe instance for service instances
                    this._servicePipe = new NamedPipeServerStream(this.ServicePipeName);
                    this._servicePipeReader = new StreamReader(this._servicePipe);
                    this._servicePipeWriter = new StreamWriter(this._servicePipe);

                    // Initialize our host pipe and exit out
                    Task.Run(() =>
                    {
                        // Wait for a new connection to our host pipe and log once found
                        this._servicePipe.WaitForConnection();
                        this._servicePipeLogger.WriteLog($"FOUND NEW CLIENT CONNECTION FOR PIPE {this.ServicePipeName}!", LogType.InfoLog);
                    });
                }

                // Log out our pipe type and state then return passed
                this._servicePipeLogger.WriteLog($"BUILT NEW {this.ServicePipeType.ToDescriptionString()} PIPE AND STREAM READER/WRITER FOR PIPE {this.ServicePipeName} CORRECTLY!", LogType.InfoLog);
                return true;
            }
            catch (Exception SetupPipeEx)
            {
                // Log our failure for building our pipe and return out false
                this._servicePipeLogger.WriteLog($"ERROR! FAILED TO BUILD NEW {this.ServicePipeType.ToDescriptionString()} PIPE NAMED {this.ServicePipeName}!", LogType.ErrorLog);
                this._servicePipeLogger.WriteException("CONFIGURATION EXCEPTION IS BEING LOGGED BELOW", SetupPipeEx);
                return false;
            }
        }
        /// <summary>
        /// Helper method used to spawn a new pipe task to run our pipe operations in the background
        /// </summary>
        /// <returns>True if our pipe service is booted correctly. False if not</returns>
        private bool _initializeServicePipe()
        {
            // Try and build a new task for our pipe service here 
            this._servicePipeLogger.WriteLog($"SPAWNING NEW PIPE TASK FOR {this.ServicePipeName} NOW...", LogType.WarnLog);

            // Configure cancellation token source and token instance here
            this._servicePipeTaskTokenSource = new CancellationTokenSource();
            this._servicePipeTaskCancellationToken = this._servicePipeTaskTokenSource.Token;

            // Build a new task and boot it here
            this._servicePipeTask = new Task(() =>
            {
                // Check if we've got a host or client pipe setup here first
                this._servicePipeLogger.WriteLog($"STARTING {this.ServicePipeName} SERVICE TASK NOW...", LogType.WarnLog);
                if (this.ServicePipeType != ServicePipeTypes.HOST_PIPE) return;

                // For host pipes, we boot a new reader to pull our lines in and build pipe commands for them
                while (!this._servicePipeTaskCancellationToken.IsCancellationRequested)
                {
                    // Read content in from the server pipe and store the result
                    string NextActionString = this._servicePipeReader.ReadLine();
                    ServicePipeAction PipeAction = JsonConvert.DeserializeObject<ServicePipeAction>(NextActionString);

                    // Execute the pipe action and fire event handlers as needed
                    bool ActionInvoked = this._executePipeAction(PipeAction);
                    this.PipeActionInvoked?.Invoke(this, PipeAction);

                    // If the invoke routine passed, serialize and return out our pipe action content here
                    if (!ActionInvoked) this._servicePipeLogger.WriteLog("WARNING! PIPE ACTION FAILED TO EXECUTE!", LogType.WarnLog);
                    this._servicePipeLogger.WriteLog("RESPONDING TO PIPE ACTION REQUEST WITH UPDATED PIPE OBJECT VALUES NOW...", LogType.InfoLog);
                    string PipeActionResult = JsonConvert.SerializeObject(PipeAction);
                    this._servicePipeWriter.WriteLine(PipeActionResult);
                    this._servicePipeWriter.Flush();
                }
            }, this._servicePipeTaskCancellationToken);

            // Boot the pipe task and return the result
            this._servicePipeTask.Start();
            bool IsRunning = this._servicePipeTask.Status == TaskStatus.Running;
            if (!IsRunning) this._servicePipeLogger.WriteLog($"ERROR! FAILED TO BOOT PIPE TASK FOR PIPE {this.ServicePipeName}!", LogType.ErrorLog);
            else this._servicePipeLogger.WriteLog($"PIPE SERVICE {this.ServicePipeName} HAS BEEN BOOTED AND IS RUNNING CORRECTLY!", LogType.InfoLog);

            // Return out based on our pipe state
            return IsRunning;
        }
        /// <summary>
        /// Helper method which is used to find and invoke methods on our service instance
        /// </summary>
        /// <param name="PipeAction"></param>
        private bool _executePipeAction(ServicePipeAction PipeAction)
        {
            // Log out that we're invoking a new pipe action here and find our method to invoke
            this._servicePipeLogger.WriteLog($"INVOKING PIPE ACTION {PipeAction.PipeMethodName} NOW...", LogType.InfoLog);
            MethodInfo PipeActionMethod = this._serviceInstance
                .GetType().GetMethods()
                .Where(MethodObj => MethodObj.Name == PipeAction.PipeMethodName)
                .FirstOrDefault(MethodObj => MethodObj.GetParameters().Length == PipeAction.PipeMethodArguments.Length);

            // Make sure our pipe action is not null here 
            if (PipeActionMethod == null)
                throw new MissingMethodException($"Error! Could not find method {PipeAction.PipeMethodName} for service type {this._serviceInstance.ServiceType}!");

            try
            {
                // Invoke our method object for the pipe action and store the result
                object ActionResult = PipeActionMethod.Invoke(this._serviceInstance, PipeAction.PipeMethodArguments);
                PipeAction.IsExecuted = true;
                PipeAction.PipeCommandResult = ActionResult;

                // Log out that we've invoked our pipe action correctly and exit out
                this._servicePipeLogger.WriteLog($"INVOKED PIPE ACTION {PipeAction.PipeMethodName} CORRECTLY!", LogType.InfoLog);
                return true;
            }
            catch (Exception InvokeActionEx)
            {
                // Log out that we failed to invoke our method action and return false
                this._servicePipeLogger.WriteLog($"ERROR! FAILED TO INVOKE PIPE ACTION {PipeAction.PipeMethodName}!", LogType.ErrorLog);
                this._servicePipeLogger.WriteException("EXCEPTION THROWN FROM INVOKE ROUTINE IS LOGGING BELOW", InvokeActionEx);
                return false;
            }
        }
    }
}
