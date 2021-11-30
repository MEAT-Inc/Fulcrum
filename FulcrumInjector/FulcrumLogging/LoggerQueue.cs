using System.Collections.Generic;
using System.Linq;
using FulcrumInjector.FulcrumLogging.LoggerObjects;
using FulcrumInjector.FulcrumLogging.LoggerSupport;

namespace FulcrumInjector.FulcrumLogging
{
    /// <summary>
    /// Class which holds all built loggers for this active instace.
    /// </summary>
    public class WatchdogLoggerQueue
    {
        // List of all logger items in the pool.
        private List<BaseLogger> LoggerPool = new List<BaseLogger>();

        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// Adds a logger item to the pool of all loggers.
        /// </summary>
        /// <param name="LoggerItem">Item to add to the pool.</param>
        public void AddLoggerToPool(BaseLogger LoggerItem)
        {
            // Find existing loggers that may have the same name as this logger obj.
            if (LoggerPool.Any(LogObj => LogObj.LoggerGuid == LoggerItem.LoggerGuid))
            {
                // Update current.
                int IndexOfExisting = LoggerPool.IndexOf(LoggerItem);
                LoggerPool[IndexOfExisting] = LoggerItem;
                return;
            }
            
            // If the logger didnt get added (no dupes) do it not.
            LoggerPool.Add(LoggerItem); 
        }
        /// <summary>
        /// Removes the logger passed from the logger queue
        /// </summary>
        /// <param name="LoggerItem">Logger to yank</param>
        public void RemoveLoggerFromPool(BaseLogger LoggerItem)
        {
            // Pull out all the dupes.
            var NewLoggers = LoggerPool.Where(LogObj => 
                LogObj.LoggerGuid != LoggerItem.LoggerGuid).ToList();

            // Check if new logger is in loggers filtered or not and store it.
            if (NewLoggers.Contains(LoggerItem)) NewLoggers.Remove(LoggerItem);
            this.LoggerPool = NewLoggers;
        }

        
        /// <summary>
        /// Gets all loggers that exist currently.
        /// </summary>
        /// <returns></returns>
        public List<BaseLogger> GetLoggers()
        {
            // Get them and return.
            return this.LoggerPool;
        }
        /// <summary>
        /// Gets loggers based on a given type of logger.
        /// </summary>
        /// <param name="TypeOfLogger">Type of logger to get.</param>
        /// <returns>List of all loggers for this type.</returns>
        public List<BaseLogger> GetLoggers(LoggerActions TypeOfLogger)
        {
            // Logger object to populate
            return this.LoggerPool.Where(LogObj => LogObj.LoggerType == TypeOfLogger).ToList();
        }
    }
}
