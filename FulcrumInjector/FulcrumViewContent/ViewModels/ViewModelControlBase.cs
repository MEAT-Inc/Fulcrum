using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewContent.ViewModels
{
    /// <summary>
    /// Base class for Model objects on the UI
    /// </summary>
    internal class ViewModelControlBase : INotifyPropertyChanged
    {
        #region Custom Events

        // Public facing event for hooking into property changed calls
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Event handler for when a property is updated on this view model controller
        /// </summary>
        /// <param name="PropertyName">Name of the property being updated</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string PropertyName = null)
        {
            // Invoke the event for our property if the event handler is configured
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        }

        #endregion //Custom Events

        #region Fields

        // Private fields used to help configure our view model base object
        public UserControl BaseViewControl;         // The base view content we're controlling with this view model
        protected SharpLogger ViewModelLogger;      // The logger object which will be used on all view models

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
        public ViewModelControlBase(UserControl ViewContent)
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
        /// Updates the globals with the new values configured into this object 
        /// </summary>
        /// <param name="ViewModelObject">Object to update</param>
        private void _updateSingletonProperty(ViewModelControlBase ViewModelObject)
        {
            // Get the types on the globals first.
            var AppViewStoreType = typeof(FulcrumConstants);
            var ViewModelTypeName = Type.GetType(ViewModelObject.ToString());
            if (AppViewStoreType == null) { throw new NullReferenceException($"THE TYPE {typeof(FulcrumConstants).ToString()} COULD NOT BE FOUND!"); }

            // Gets all the members and sets one to update
            var AppStoreMembers = AppViewStoreType.GetMembers();
            if (FulcrumConstants.FulcrumMainWindow == null) { return; }

            // If the main window isn't null keep going.
            var MemberToUpdate = AppStoreMembers.FirstOrDefault((MemberObj) =>
            {
                // Remove the viewmodel text and search.
                string ComponentTypeRemoved = ViewModelTypeName.Name.Replace("ViewModel", string.Empty);
                return MemberObj.Name.StartsWith(ComponentTypeRemoved) && MemberObj.Name.Contains("ViewModel");
            });

            // Find if the VM is a core option or misc singleton
            bool IsViewModelType =
                ViewModelTypeName.Name.Contains("CoreViewModels") ||
                ViewModelTypeName.Name.Contains("OptionViewModels") ||
                ViewModelTypeName.Name.Contains("MiscViewModels");

            // Switch for fields vs properties
            switch (MemberToUpdate.MemberType)
            {
                // For Field based objects
                case MemberTypes.Field:
                    FieldInfo MemberAsField = (FieldInfo)MemberToUpdate;
                    try
                    {
                        // Check for singleton content object
                        if (!IsViewModelType)
                        {
                            // Try setting value inside this block in case VM value has no public setter.
                            MemberAsField.SetValue(null, ViewModelObject);
                            return;
                        }

                        // Try and find Object for our singleton instance and store a value to it. If this fails, default back to no singleton.
                        var PulledSingleton = FulcrumSingletonContent<UserControl, ViewModelControlBase>.LocateSingletonViewInstance(ViewModelObject.GetType());
                        FulcrumSingletonContent<UserControl, ViewModelControlBase>.RegisterAsSingleton(PulledSingleton.SingletonUserControl, ViewModelObject);
                        return;
                    }
                    catch { return; }

                // For Property Based objects
                case MemberTypes.Property:
                    PropertyInfo MemberAsProperty = (PropertyInfo)MemberToUpdate;
                    try
                    {
                        // Check for singleton content object
                        if (!IsViewModelType)
                        {
                            // Try setting value inside this block in case VM value has no public setter.
                            MemberAsProperty.SetValue(null, ViewModelObject);
                            return;
                        }

                        // Try and find Object for our singleton instance and store a value to it. If this fails, default back to no singleton.
                        var PulledSingleton = FulcrumSingletonContent<UserControl, ViewModelControlBase>.LocateSingletonViewInstance(ViewModelObject.GetType());
                        FulcrumSingletonContent<UserControl, ViewModelControlBase>.RegisterAsSingleton(PulledSingleton.SingletonUserControl, ViewModelObject);
                        return;
                    }
                    catch { return; }

                // If neither field or property fail out
                default: throw new InvalidEnumArgumentException($"THE REQUESTED MEMBER {nameof(ViewModelObject)} COULD NOT BE FOUND!");
            }
        }
        /// <summary>
        /// Property Changed without model binding
        /// </summary>
        /// <param name="PropertyName">Name of property to emit change for</param>
        /// <param name="NotifierObject">Object sending this out</param>
        private void _updateBackingField(object NotifierObject, string PropertyName, object NewPropValue)
        {
            // Store the type of the sender
            var InputObjType = NotifierObject.GetType();

            // Loop all fields, find the private value and store it
            var MembersFound = InputObjType.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var MemberObject = MembersFound.FirstOrDefault(FieldObj =>
                FieldObj.Name.Contains("_") &&
                FieldObj.Name.Substring(1).ToUpper() == PropertyName.ToUpper());

            // Try serialization here. Set if failed.
            switch (MemberObject.MemberType)
            {
                // For Field Objects
                case MemberTypes.Field:
                    FieldInfo InvokerField = (FieldInfo)MemberObject;
                    InvokerField.SetValue(NotifierObject, NewPropValue);
                    break;

                // For Property objects
                case MemberTypes.Property:
                    PropertyInfo InvokerProperty = (PropertyInfo)MemberObject;
                    InvokerProperty.SetValue(NotifierObject, NewPropValue);
                    break;

                // Throw if value to modify is not found
                default: throw new NotImplementedException($"THE INVOKED MEMBER {PropertyName} COULD NOT BE FOUND!");
            }
        }
    }
}
