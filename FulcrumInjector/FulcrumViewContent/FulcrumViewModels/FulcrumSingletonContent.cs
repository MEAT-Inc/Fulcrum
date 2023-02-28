using System;
using System.Linq;
using System.Reflection;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewContent.FulcrumViewModels
{
    /// <summary>
    /// Singleton instance builder for user controls.
    /// This forces us to only pass in ViewModelControl base instance objects.
    /// </summary>
    internal class FulcrumSingletonContent<TViewType, TViewModelType> where TViewModelType : FulcrumViewModelBase
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Private static fields used to configure a singleton content instance
        private static readonly SharpLogger _singletonLogger = new(LoggerActions.UniversalLogger);
        private static FulcrumSingletonContent<TViewType, TViewModelType>[] _fulcrumSingletons = 
            Array.Empty<FulcrumSingletonContent<TViewType, TViewModelType>>();

        // Private instance fields holding information about our singleton instance 
        protected readonly DateTime TimeCreated;                // Time the instance was built.
        public readonly TViewType SingletonUserControl;         // User control content for this control input.
        public readonly TViewModelType SingletonViewModel;      // Base view model control object used to build this singleton

        #endregion //Fields

        #region Properties

        // Public facing collection of all built singleton instances for the injector application
        public static FulcrumSingletonContent<TViewType, TViewModelType>[] FulcrumSingletons
        {
            get => _fulcrumSingletons;
            private set => _fulcrumSingletons = value;
        }

        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new instance of a singleton object helper type.
        /// </summary>
        /// <param name="SingletonUserControlContent">The user control to register on our singleton</param>
        /// <param name="SingletonViewModelContent">The view model to register on our singleton</param>
        private FulcrumSingletonContent(TViewType SingletonUserControlContent, TViewModelType SingletonViewModelContent)
        {
            // Store time information.
            this.TimeCreated = DateTime.Now;
            FulcrumSingletons ??= Array.Empty<FulcrumSingletonContent<TViewType, TViewModelType>>();
            _singletonLogger.WriteLog($"INSTANCE HAS BEEN CREATED AND TIMESTAMPED! TIME BUILT: {this.TimeCreated:s}", LogType.TraceLog);

            // Log building new singleton instance object
            this.SingletonUserControl = SingletonUserControlContent;
            this.SingletonViewModel = SingletonViewModelContent;
            _singletonLogger.WriteLog($"STORED NEW SINGLETON INSTANCE OBJECT FOR TYPE {typeof(TViewType)}!", LogType.InfoLog);
        }
        /// <summary>
        /// Deconstruction routine for singleton helper class object 
        /// </summary>
        ~FulcrumSingletonContent()
        {
            // Log building new removed list and remove the object from static contents.
            _singletonLogger.WriteLog($"DECONSTRUCTING A SINGLETON USER CONTROL OBJECT FOR TYPE {typeof(TViewType)}...", LogType.WarnLog);
            _singletonLogger.WriteLog($"INSTANCE HAS BEEN ALIVE FOR A TOTAL OF {(DateTime.Now - this.TimeCreated):g}", LogType.TraceLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new lazy instance based on the values provided in the input args
        /// </summary>
        /// <typeparamref name="TViewType">Type of user control for the view model content</typeparamref>
        /// <typeparamref name="TViewModelType">Type of view model for the view model content</typeparamref>   
        /// <param name="ViewType">The type of the view to spawn</param>
        /// <param name="ViewModelType">The type of the view model to spawn</param>
        /// <returns>True if built ok. False if not.</returns>
        public static FulcrumSingletonContent<TViewType, TViewModelType> CreateSingletonInstance(Type ViewType, Type ViewModelType)
        {
            // Build new instance of this singleton helper and return it
            _singletonLogger.WriteLog($"TRYING TO BUILD NEW SINGLETON INSTANCE FOR VIEW TYPE {ViewType.Name}...", LogType.WarnLog);
            _singletonLogger.WriteLog($"VIEWMODEL TYPE ASSOCIATED WITH CONTENT IS: {ViewModelType.Name}", LogType.InfoLog);
            var LocatedSingleton = LocateSingletonViewInstance(ViewType);
            if (LocatedSingleton != null)
            {
                // Log found existing instance and return it out
                _singletonLogger.WriteLog("FOUND EXISTING INSTANCE OBJECT ENTRY! RETURNING IT NOW...", LogType.InfoLog);
                _singletonLogger.WriteLog($"EXISTING INSTANCE HAS BEEN BUILT AND DEFINED SINCE: {LocatedSingleton.TimeCreated:s}", LogType.TraceLog);

                // Return current instance.
                return LocatedSingleton;
            }

            // Build a new instance object type here and store values.
            var CreatedViewContent = Activator.CreateInstance(ViewType);
            var LocatedViewModel = CreatedViewContent.GetType()
                .GetRuntimeProperties()
                .FirstOrDefault(PropObj =>
                    PropObj.PropertyType == typeof(TViewModelType) ||
                    PropObj.PropertyType.BaseType == typeof(TViewModelType))
                .GetValue(CreatedViewContent);

            // Ensure neither object value pulled in is null at this point
            if (CreatedViewContent == null) throw new NullReferenceException($"Error! Failed to find view content for type {ViewType.FullName}!");
            if (LocatedViewModel == null) throw new NullReferenceException($"Error! Failed to find view model content for type {ViewModelType.FullName}!");

            // Cast our view and view model to the generic types provided and store them now
            TViewType ViewContent = (TViewType)CreatedViewContent;
            TViewModelType ViewModelContent = (TViewModelType)LocatedViewModel;
            
            // Once we've found our view and view model content, store it on our singleton collection and exit out
            _singletonLogger.WriteLog("BUILT NEW INSTANCE FOR VIEW AND VIEW MODEL CONTENT OK!", LogType.WarnLog);
            var NewSingletonInstance = new FulcrumSingletonContent<TViewType, TViewModelType>(ViewContent, ViewModelContent);
            FulcrumSingletons = FulcrumSingletons.Append(NewSingletonInstance).ToArray();
        
            // Log information and return.
            _singletonLogger.WriteLog("STORED NEW SINGLETON INSTANCE ON STATIC LIST OK!", LogType.InfoLog);
            return NewSingletonInstance;
        }
        /// <summary>
        /// Registers an existing view and view model as a new lazy instance based on the values provided in the input args
        /// </summary>
        /// <typeparamref name="TViewType">Type of user control for the view model content</typeparamref>
        /// <typeparamref name="TViewModelType">Type of view model for the view model content</typeparamref>   
        /// <param name="ViewObject">The view we need to register on our singleton</param>
        /// <param name="ViewModelObject">The view model we need to register on our singleton</param>
        /// <returns>True if the singleton is registered correctly. False if not.</returns>
        public static FulcrumSingletonContent<TViewType, TViewModelType> RegisterAsSingleton(TViewType ViewObject, TViewModelType ViewModelObject)
        {
            // Build new instance of this singleton helper and return it
            _singletonLogger.WriteLog($"TRYING TO REGISTER NEW SINGLETON INSTANCE FOR VIEW TYPE {ViewObject.GetType().Name}...", LogType.WarnLog);
            _singletonLogger.WriteLog($"VIEWMODEL TYPE ASSOCIATED WITH CONTENT IS: {ViewModelObject.GetType().Name}", LogType.InfoLog);
            var LocatedSingleton = LocateSingletonViewInstance(ViewObject.GetType());
            if (LocatedSingleton != null)
            {
                // Log found existing instance and return it out
                _singletonLogger.WriteLog("FOUND EXISTING INSTANCE OBJECT ENTRY! RETURNING IT NOW...", LogType.InfoLog);
                _singletonLogger.WriteLog($"EXISTING INSTANCE HAS BEEN BUILT AND DEFINED SINCE: {LocatedSingleton.TimeCreated:s}", LogType.TraceLog);
                _singletonLogger.WriteLog("UPDATING DEFINITIONS FOR THIS OBJECT NOW...", LogType.WarnLog);

                // Pull the singleton from our list and replace the content.
                int IndexOfSingleton = FulcrumSingletons.ToList().IndexOf(LocatedSingleton);
                FulcrumSingletons[IndexOfSingleton] = new FulcrumSingletonContent<TViewType, TViewModelType>(ViewObject, ViewModelObject);
                _singletonLogger.WriteLog("UPDATED CONTENTS OF OUR SINGLETON VIEW OBJECT OK!", LogType.InfoLog);

                // Return current instance.
                return FulcrumSingletons[IndexOfSingleton];
            }

            // Build new instance and return output.
            _singletonLogger.WriteLog("BUILT NEW INSTANCE FOR VIEW AND VIEW MODEL CONTENT OK!", LogType.WarnLog);
            var NewSingletonInstance = new FulcrumSingletonContent<TViewType, TViewModelType>(ViewObject, ViewModelObject);
            FulcrumSingletons = FulcrumSingletons.Append(NewSingletonInstance).ToArray();

            // Log information and return.
            _singletonLogger.WriteLog("STORED NEW SINGLETON INSTANCE ON STATIC LIST OK!", LogType.InfoLog);
            return NewSingletonInstance;
        }

        /// <summary>
        /// Returns the first or only instance object for our current instances of singletons matching this view type
        /// </summary>
        /// <typeparamref name="TViewType">Type of user control for the view model content</typeparamref>
        /// <typeparamref name="TViewModelType">Type of view model for the view model content</typeparamref>   
        /// <param name="ViewTypeToLocate">The type of the view to search for in our singleton content</param>
        /// <returns>The singleton content object holding our view and view model if one is found</returns>
        public static FulcrumSingletonContent<TViewType, TViewModelType> LocateSingletonViewInstance(Type ViewTypeToLocate)
        {
            // If not type of view model control base, then dump out.
            if (ViewTypeToLocate != typeof(TViewType) && ViewTypeToLocate.BaseType != typeof(TViewType))
            {
                // IF this failed, try using VMs before failing out.
                var ViewModelFallbackValue = LocateSingletonViewModelInstance(ViewTypeToLocate);
                if (ViewModelFallbackValue != null) return ViewModelFallbackValue;
                throw new InvalidCastException($"CAN NOT USE A NON USER CONTROL BASE TYPE FOR SINGLETON LOOKUPS!");
            }
            
            // Find first object with the type matching the given viewmodel type
            var PulledSingleton = FulcrumSingletons.FirstOrDefault(ViewObj => ViewObj.SingletonUserControl.GetType() == ViewTypeToLocate);
            if (PulledSingleton == null) _singletonLogger.WriteLog($"NO MATCHING SINGLETON INSTANCE WAS FOUND FOR TYPE {ViewTypeToLocate.Name}!", LogType.ErrorLog);
            return PulledSingleton;
        }
        /// <summary>
        /// Returns the first or only instance object for our current instances of singletons matching this view model type
        /// </summary>
        /// <typeparamref name="TViewType">Type of user control for the view model content</typeparamref>
        /// <typeparamref name="TViewModelType">Type of view model for the view model content</typeparamref>   
        /// <param name="ViewModelTypeToLocate">The type of the view model to search for in our singleton content</param>
        /// <returns>The singleton content object holding our view and view model if one is found</returns>
        public static FulcrumSingletonContent<TViewType, TViewModelType> LocateSingletonViewModelInstance(Type ViewModelTypeToLocate)
        {
            // If not type of view model control base, then dump out.
            if (ViewModelTypeToLocate != typeof(TViewModelType) && ViewModelTypeToLocate.BaseType != typeof(TViewModelType))
            { 
                // IF this failed, try using view objects before failing out.
                var UserControlFallbackValue = LocateSingletonViewInstance(ViewModelTypeToLocate);
                if (UserControlFallbackValue != null) return UserControlFallbackValue;
                throw new InvalidCastException($"FAILED TO LOCATE A USER CONTROL OR VIEW MODEL MATCHING TYPE: {ViewModelTypeToLocate.Name}!");
            }

            // Find first object with the type matching the given viewmodel type
            var PulledSingleton = FulcrumSingletons.FirstOrDefault(ViewObj => ViewObj.SingletonViewModel.GetType() == ViewModelTypeToLocate);
            if (PulledSingleton == null) _singletonLogger.WriteLog("FAILED TO LOCATE VALID SINGLETON INSTANCE!", LogType.ErrorLog);
            return PulledSingleton;
        }
    }
}
