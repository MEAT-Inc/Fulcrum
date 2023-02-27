using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters
{
    /// <summary>
    /// Refreshes and checks a property value on a given value object
    /// </summary>
    internal class PropertyWatchdog : INotifyPropertyChanged
    {
        #region Custom Events

        // Property Changed Event for when a property change is found
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Event handler for when the watched property is updated
        /// </summary>
        /// <param name="propertyName">Name of the property which is changed</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            // Invoke the event if it's been configured
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion //Custom Events

        #region Fields

        // Timer to use and the update time
        public readonly TimeSpan UpdateInterval;
        public readonly DispatcherTimer PropertyUpdateTimer;

        #endregion //Fields

        #region Properties
        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new property update timer.
        /// </summary>
        public PropertyWatchdog(int UpdateTime = 100)
        {
            // Make timer and interval updates
            PropertyUpdateTimer = new DispatcherTimer();
            UpdateInterval = TimeSpan.FromMilliseconds(UpdateTime);
            PropertyUpdateTimer.Interval = UpdateInterval;
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Update on property changed for the main output and notify using a prop changed event
        /// </summary>
        /// <param name="TimerTriggerAction">Action to trigger on the timer input object</param>
        public void StartUpdateTimer(Action<object, EventArgs> TimerTriggerAction)
        {
            // Build new trigger and assign it to our timer
            var EventTrigger = new EventHandler((sender, args) => TimerTriggerAction(sender, args));
            PropertyUpdateTimer.Tick += EventTrigger;

            // Start Timer and Trigger Prop Changed
            Task.Run(() =>
            {
                OnPropertyChanged();
                PropertyUpdateTimer.Start();
            });
        }
    }
}
