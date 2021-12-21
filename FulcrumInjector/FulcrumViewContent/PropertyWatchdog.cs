using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace FulcrumInjector.FulcrumViewContent
{
    /// <summary>
    /// Refreshes and checks a property value on a given value object
    /// </summary>
    public class PropertyWatchdog : INotifyPropertyChanged
    {
        // Timer to use and the update time
        public TimeSpan UpdateInterval;
        public DispatcherTimer PropertyUpdateTimer;

        // Property Changed Setup
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // ---------------------------------------------------------------------------------------------

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
            OnPropertyChanged();
            PropertyUpdateTimer.Start();
        }
    }
}
