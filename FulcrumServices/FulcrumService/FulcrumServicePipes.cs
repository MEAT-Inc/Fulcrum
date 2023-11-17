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
using FulcrumService.FulcrumServiceModels;
using FulcrumSupport;
using Newtonsoft.Json;
using SharpLogging;

namespace FulcrumService
{
    /// <summary>
    /// Class object holding our definition and logic for a service pipe
    /// </summary>
    public class FulcrumServicePipe : IDisposable
    {
        #region Custom Events

        // Public facing events for pipe action routines
        public EventHandler<FulcrumServicePipeAction> PipeActionInvoked;               // Fired when an action execution is started
        public EventHandler<FulcrumServicePipeAction> PipeActionCompleted;             // Fired when an action is executed on our pipe object

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

        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Disposes a pipe object once it's not longer being used
        /// </summary>
        public void Dispose()
        {
            // Log out we're disposing the pipe object here and cancel our reader task
            this._servicePipeLogger?.WriteLog($"DISPOSING {this.ServicePipeType.ToDescriptionString()} PIPE {this.ServicePipeName}...", LogType.WarnLog);
            this._servicePipeTaskTokenSource?.Cancel();
            
            // Dispose our pipe objects and exit out
            _servicePipeLogger?.Dispose();
            _serviceInstance?.Dispose();
            _servicePipeWriter?.Dispose();
            _servicePipeReader?.Dispose();
            _clientPipe?.Dispose();
            _servicePipe?.Dispose();
            _servicePipeTask?.Dispose();
            _servicePipeTaskTokenSource?.Dispose();
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// CTOR for a new service pipe instance. Spawns the given pipe type for the service instance provided
        /// </summary>
        /// <param name="ServiceInstance">The service instance we're building pipes for</param>
        internal FulcrumServicePipe(FulcrumServiceBase ServiceInstance)
        {
            // Spawn our pipe logger and build the pipe name here
            this._serviceInstance = ServiceInstance;
            this.ServicePipeName = $"{this._serviceInstance.ServiceType.ToDescriptionString()}Pipe";
            this._servicePipeLogger = new SharpLogger(LoggerActions.UniversalLogger, $"{this.ServicePipeName}Logger");
            this._servicePipeLogger.WriteLog($"SPAWNING NEW SERVICE PIPE NAMED {this.ServicePipeName}...", LogType.WarnLog);

            // Store our pipe type here and initialize our pipe objects
            this.ServicePipeType = this._serviceInstance.IsServiceClient
                ? ServicePipeTypes.CLIENT_PIPE
                : ServicePipeTypes.HOST_PIPE;

            // Build a new set of stream readers and writers for our pipe object along with a command queue
            if (!this._spawnServicePipes())
                throw new InvalidOperationException($"Error! Failed to configure pipe objects for pipe {this.ServicePipeName}!");
            if (this.ServicePipeType == ServicePipeTypes.HOST_PIPE && !this._initializeServicePipe())
                throw new InvalidOperationException($"Error! Failed to start pipe service task for pipe {this.ServicePipeName}!");

            // Log out that our pipe has been created and setup correctly 
            this._servicePipeLogger.WriteLog($"{this.ServicePipeType.ToDescriptionString()} PIPE {this.ServicePipeName} HAS BEEN CONFIGURED CORRECTLY!", LogType.InfoLog); 
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Queues a new action to store on our pipe action queue and prepares to execute it
        /// </summary>
        /// <param name="PipeAction">The action to invoke on our pipe host</param>
        /// <returns>True if the pipe action is queued. False if not</returns>
        /// <exception cref="InvalidOperationException">Thrown when we try to queue a pipe action for our host pipe</exception>
        public bool QueuePipeAction(FulcrumServicePipeAction PipeAction)
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

            // Write our pipe action as a JSON string to our host pipe here
            string PipeActionJson = JsonConvert.SerializeObject(PipeAction);
            this._servicePipeWriter.WriteLine(PipeActionJson);
            this._servicePipeWriter.Flush();

            // Return passed once we've invoked our pipe routine
            return true;
        }
        /// <summary>
        /// Waits for a queued action to complete for the given time
        /// </summary>
        /// <param name="ActionGuid">The GUID of the action we're looking for</param>
        /// <param name="TimeoutTime">The time to wait for our action to complete</param>
        /// <param name="PipeAction">The action queued and responded to</param>
        /// <returns>True if an action response is found. False if not</returns>
        public bool WaitForAction(Guid ActionGuid, out FulcrumServicePipeAction PipeAction, int TimeoutTime = 5000)
        {
            // Wait for a new event to fie for our pipe reader
            Stopwatch TimeoutTimer = new Stopwatch();
            TimeoutTimer.Start();

            // While our elapsed time is less than our timeout time try and read our content
            while (TimeoutTimer.ElapsedMilliseconds < TimeoutTime)
            {
                // Pull in the JSON content of our action object returned
                string NextActionString = this._servicePipeReader.ReadLine();
                if (string.IsNullOrWhiteSpace(NextActionString)) continue; 

                // Once we've got a valid JSON entry for our pipe content, process it and check the GUID
                PipeAction = JsonConvert.DeserializeObject<FulcrumServicePipeAction>(NextActionString);
                if (PipeAction?.PipeActionGuid != ActionGuid) continue;

                // Invoke an action completed event to pull in our new pipe action object
                PipeAction.IsExecuted = true;
                this.PipeActionCompleted?.Invoke(this, PipeAction);
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
                if (this.ServicePipeType == ServicePipeTypes.CLIENT_PIPE)
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
                while (!this._servicePipeTaskCancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        // If the pipe is not connected, wait for a new connection to be processed
                        if (!this._servicePipe.IsConnected) this._servicePipe.WaitForConnection(); 

                        // Read content in from the server pipe and store the result
                        string NextActionString = this._servicePipeReader.ReadLine();
                        if (string.IsNullOrWhiteSpace(NextActionString)) continue;
                        if (NextActionString.StartsWith("\"") && NextActionString.EndsWith("\""))
                            NextActionString = NextActionString.Substring(1, NextActionString.Length - 2);

                        // Use our sanitized JSON content to build our Pipe Action here
                        FulcrumServicePipeAction PipeAction = JsonConvert.DeserializeObject<FulcrumServicePipeAction>(NextActionString);

                        // Execute the pipe action and fire event handlers as needed
                        bool ActionInvoked = this._executePipeAction(PipeAction);
                        this.PipeActionInvoked?.Invoke(this, PipeAction);

                        // If the invoke routine passed, serialize and return out our pipe action content here
                        if (!ActionInvoked) this._servicePipeLogger.WriteLog("WARNING! PIPE ACTION FAILED TO EXECUTE!", LogType.WarnLog);
                        
                        // Serialize our pipe action object and write it out to our client object
                        string PipeActionResult = JsonConvert.SerializeObject(PipeAction);
                        this._servicePipeWriter.WriteLine(PipeActionResult);
                        this._servicePipeWriter.Flush();
                    }
                    catch (Exception ReadPipeDataEx)
                    {
                        // Log out the failure to read from the pipe and continue on
                        this._servicePipeLogger.WriteLog($"ERROR! FAILED TO READ OR RESPOND TO DATA FROM PIPE {this.ServicePipeName}!", LogType.ErrorLog);
                        this._servicePipeLogger.WriteException("EXCEPTION DURING ACTION EXECUTION ROUTINE IS BEING LOGGED BELOW", ReadPipeDataEx);

                        // Disconnect our pipe server here
                        this._servicePipe.Disconnect();
                    }
                }
            }, this._servicePipeTaskCancellationToken);

            // Boot the pipe task and return the result
            this._servicePipeTask.Start();
            Thread.Sleep(250);
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
        private bool _executePipeAction(FulcrumServicePipeAction PipeAction)
        {
            // Find our execution type here and determine what routine needs to be done.
            this._servicePipeLogger.WriteLog($"INVOKING PIPE ACTION {PipeAction.PipeActionName} NOW...", LogType.InfoLog);
            switch (PipeAction.ReflectionType)
            {
                // For method invocation types
                case FulcrumServicePipeAction.ReflectionTypes.METHOD_TYPE:
                    {
                        // For method types, find and invoke our method object here
                        MethodInfo PipeActionMethod = this._serviceInstance
                            .GetType().GetMethods()
                            .Where(MethodObj => MethodObj.Name == PipeAction.PipeActionName)
                            .FirstOrDefault(MethodObj => MethodObj.GetParameters().Length == PipeAction.PipeMethodArguments.Length);

                        // Make sure our pipe action is not null here 
                        if (PipeActionMethod == null)
                            throw new MissingMethodException($"Error! Could not find method {PipeAction.PipeActionName} for service type {this._serviceInstance.ServiceType}!");

                        try
                        {
                            // Invoke our method object for the pipe action and store the result
                            object ActionResult = PipeActionMethod.Invoke(this._serviceInstance, PipeAction.PipeMethodArguments);
                            PipeAction.PipeCommandResult = ActionResult;
                            PipeAction.IsExecuted = true;

                            // Log out that we've invoked our pipe action correctly and exit out
                            return true;
                        }
                        catch (Exception InvokeActionEx)
                        {
                            // Log out that we failed to invoke our method action and return false
                            this._servicePipeLogger.WriteLog($"ERROR! FAILED TO INVOKE PIPE ACTION {PipeAction.PipeActionName}!", LogType.ErrorLog);
                            this._servicePipeLogger.WriteException("EXCEPTION THROWN FROM INVOKE ROUTINE IS LOGGING BELOW", InvokeActionEx);
                            return false;
                        }
                    }

                // For getting or setting member objects
                case FulcrumServicePipeAction.ReflectionTypes.GET_MEMBER:
                case FulcrumServicePipeAction.ReflectionTypes.SET_MEMBER:
                    {
                        // For getting members find and invoke our member information object here 
                        MemberInfo PipeActionMember = this._serviceInstance
                            .GetType().GetMembers()
                            .FirstOrDefault(MemberObj => MemberObj.Name == PipeAction.PipeActionName);

                        // If no member information can be found, throw a new missing member exception
                        if (PipeActionMember == null)
                            throw new MissingMemberException($"Error! Could not find member {PipeAction.PipeActionName} for service type {this._serviceInstance.ServiceType}!");

                        try
                        {
                            // Now switch based on if we're using a field or property object here
                            switch (PipeActionMember)
                            {
                                // For fields, set them here using the field setter
                                case FieldInfo PipeField:
                                    {
                                        // For getting members, pull our value
                                        if (PipeAction.ReflectionType == FulcrumServicePipeAction.ReflectionTypes.GET_MEMBER)
                                            PipeAction.PipeCommandResult = PipeField.GetValue(this);

                                        // For setting members, set our value
                                        if (PipeAction.ReflectionType == FulcrumServicePipeAction.ReflectionTypes.SET_MEMBER)
                                            PipeField.SetValue(this, PipeAction.PipeMethodArguments[0]);

                                        // Break out once we've pulled or set our value
                                        break;
                                    }

                                // For properties, set them here using the property setter
                                case PropertyInfo PipeProperty:
                                    {
                                        // For getting members, pull our value
                                        if (PipeAction.ReflectionType == FulcrumServicePipeAction.ReflectionTypes.GET_MEMBER)
                                            PipeAction.PipeCommandResult = PipeProperty.GetValue(this);

                                        // For setting members, set our value
                                        if (PipeAction.ReflectionType == FulcrumServicePipeAction.ReflectionTypes.SET_MEMBER)
                                            PipeProperty.SetValue(this, PipeAction.PipeMethodArguments[0]);

                                        // Break out once we've pulled or set our value
                                        break;
                                    }
                            }

                            // Break out once we've set or pulled our member value
                            PipeAction.IsExecuted = true;
                            return true;
                        }
                        catch (Exception InvokeActionEx)
                        {
                            // Log out that we failed to invoke our method action and return false
                            this._servicePipeLogger.WriteLog($"ERROR! FAILED TO INVOKE PIPE ACTION {PipeAction.PipeActionName}!", LogType.ErrorLog);
                            this._servicePipeLogger.WriteException("EXCEPTION THROWN FROM INVOKE ROUTINE IS LOGGING BELOW", InvokeActionEx);
                            return false;
                        }
                    }

                // If no reflection type is specified, throw a new exception out
                default:
                    this._servicePipeLogger.WriteLog($"ERROR! NO REFLECTION TYPE WAS PROVIDED FOR PIPE ACTION {PipeAction.PipeActionName}!", LogType.ErrorLog);
                    this._servicePipeLogger.WriteLog("ENSURE PIPE ACTION TYPES ARE SPECIFIED BEFORE SENDING THEM TO HOST SERVICES!", LogType.ErrorLog);
                    throw new InvalidOperationException($"Error! No reflection type set for pipe method {PipeAction.PipeActionName}!");
            }
        }
    }
}
