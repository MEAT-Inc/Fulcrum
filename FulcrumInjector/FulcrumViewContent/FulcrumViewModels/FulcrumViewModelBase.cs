using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewContent.FulcrumViewModels
{
    /// <summary>
    /// Base class for Model objects on the UI
    /// </summary>
    internal class FulcrumViewModelBase : INotifyPropertyChanged, INotifyCollectionChanged
    {
        #region Custom Events

        // Public facing event for hooking into property or collection changed calls
        public event PropertyChangedEventHandler PropertyChanged;
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        
        /// <summary>
        /// Event handler for when a property is updated on this view model controller
        /// </summary>
        /// <param name="PropertyName">Name of the property being updated</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string PropertyName = null)
        {
            // Invoke the event for our property if the event handler is configured
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        }
        /// <summary>
        /// Event handler for when a collection is updated on this view model controller
        /// </summary>
        /// <param name="CollectionAction">The action for the collection changed or updated</param>
        /// <param name="CollectionChanged">The collection which is being updated</param>
        protected virtual void OnCollectionChanged(NotifyCollectionChangedAction CollectionAction, IList CollectionChanged)
        {
            // Invoke the event for our property if the event handler is configured
            this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(CollectionAction, CollectionChanged));
        }

        #endregion //Custom Events

        #region Fields

        // Private fields used to help configure our view model base object
        protected SharpLogger ViewModelLogger;               // The logger object which will be used on all view models
        public readonly UserControl BaseViewControl;         // The base view content we're controlling with this view model

        #endregion //Fields

        #region Properties
        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Constructs a new instance of a ViewModelControl base object
        /// </summary>
        /// <param name="ViewContent">The view which this VMC Base object will be consuming</param>
        public FulcrumViewModelBase(UserControl ViewContent)
        {
            // Store the base view content on this instance and exit out
            this.BaseViewControl = ViewContent;
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Updates the property on this view model and sets a prop notify event
        /// </summary>
        /// <param name="PropertyName">Name of property </param>
        /// <param name="Value">Value being used</param>
        public void PropertyUpdated(object Value, [CallerMemberName] string PropertyName = null)
        {
            // Property Changed Event
            this.OnPropertyChanged(PropertyName);

            // Update VM Value and Global value
            this._updateSingletonProperty(this);
            this._updateBackingField(this, PropertyName, Value);
        }
        /// <summary>
        /// Updates the Collection on this view model and sets a collection changed notify event
        /// </summary>
        /// <param name="Value">Value being used</param>
        /// <param name="CollectionAction">The action of the collection being updated</param>
        /// <param name="CollectionName">The name of the collection being updated</param>
        public void CollectionUpdated(IList Value, NotifyCollectionChangedAction CollectionAction, [CallerMemberName] string CollectionName = null)
        {
            // Collection Changed Event
            this.OnCollectionChanged(CollectionAction, Value);

            // Update the VM and Global value
            this._updateSingletonProperty(this);
            this._updateBackingField(this, CollectionName, Value);
        }

        /// <summary>
        /// Updates the globals with the new values configured into this object
        /// The member being stored/updated is accessed from the static FulcrumConstants class which is why we don't need objects
        /// to reflect onto when setting values
        /// </summary>
        /// <param name="ViewModelObject">Object to update</param>
        private void _updateSingletonProperty(FulcrumViewModelBase ViewModelObject)
        {
            // Get the type of the view model and make sure the main window instance is built
            var ViewModelType = ViewModelObject.GetType();
            if (ViewModelType.FullName == null) { return; }
            if (FulcrumConstants.FulcrumMainWindow == null) { return; }
            
            // Find the member information object used to perform our update routine
            string ViewModelNameCleaned = ViewModelType.Name.Replace("ViewModel", string.Empty);
            var MemberToUpdate = typeof(FulcrumConstants).GetMembers()
                .FirstOrDefault(MemberObj => MemberObj.Name.StartsWith(ViewModelNameCleaned));

            // If the member type is null, and make sure it's a field or property type here
            if (MemberToUpdate == null) return;
            if (MemberToUpdate.MemberType != MemberTypes.Field && MemberToUpdate.MemberType != MemberTypes.Property) return;

            // Find if the VM is a core option or misc singleton. If it's none of those types, exit out
            string[] SupportedTypes = { "CoreViewModels", "OptionViewModels", "MiscViewModels" };
            if (!SupportedTypes.Any(SupportedType => ViewModelType.FullName.Contains(SupportedType))) return;

            try
            {
                // Check if we've got a field or property and update accordingly
                switch (MemberToUpdate)
                {
                    // For fields, set them here using the field setter
                    case FieldInfo FieldToUpdate:
                        FieldToUpdate.SetValue(null, ViewModelObject);
                        break;

                    // For properties, set them here using the property setter
                    case PropertyInfo PropertyToUpdate:
                        PropertyToUpdate.SetValue(null, ViewModelObject);
                        break;
                }

                // Try and find Object for our singleton instance and store a value to it. If this fails, default back to no singleton.
                var PulledSingleton = FulcrumSingletonContent<UserControl, FulcrumViewModelBase>.LocateSingletonViewInstance(ViewModelObject.GetType());
                FulcrumSingletonContent<UserControl, FulcrumViewModelBase>.RegisterAsSingleton(PulledSingleton.SingletonUserControl, ViewModelObject, false);
            }
            catch (Exception UpdateMemberEx)
            {
                // Log the thrown exception here and return out
                this.ViewModelLogger.WriteException("ERROR! FAILED TO UPDATE A SINGLETON FIELD!", UpdateMemberEx, LogType.TraceLog);
            }
        }
        /// <summary>
        /// Property Changed without model binding
        /// </summary>
        /// <param name="PropertyName">Name of property to emit change for</param>
        /// <param name="NotifierObject">Object sending this out</param>
        private void _updateBackingField(object NotifierObject, string PropertyName, object NewPropValue)
        {
            // Store the type of the sender and get the field being setup now
            var InputObjType = NotifierObject.GetType();
            var MemberToUpdate = InputObjType
                .GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(FieldObj =>
                    FieldObj.Name.Contains("_") && 
                    string.Equals(FieldObj.Name.Substring(1), PropertyName, StringComparison.CurrentCultureIgnoreCase));

            // If the member type is null, and make sure it's a field or property type here
            if (MemberToUpdate == null) return;
            if (MemberToUpdate.MemberType != MemberTypes.Field && MemberToUpdate.MemberType != MemberTypes.Property) return;

            try
            {
                // Check if we've got a field or property and update accordingly
                switch (MemberToUpdate)
                {
                    // For fields, set them here using the field setter
                    case FieldInfo FieldToUpdate:
                        FieldToUpdate.SetValue(null, NewPropValue);
                        break;

                    // For properties, set them here using the property setter
                    case PropertyInfo PropertyToUpdate:
                        PropertyToUpdate.SetValue(null, NewPropValue);
                        break;
                }
            }
            catch (Exception UpdateMemberEx)
            {
                // Log the thrown exception here and return out
                this.ViewModelLogger.WriteException("ERROR! FAILED TO UPDATE A BACKING FIELD!", UpdateMemberEx, LogType.TraceLog);
            }
        }
    }
}
