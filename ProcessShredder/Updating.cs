using System;

namespace Updating
{
    /// <summary>
    /// An object with the capability to auto update after
    /// certain periods of time
    /// </summary>
    interface IUpdatable
    {
        /// <summary>
        /// A method used to update parts of your object,
        /// when used with GUIs or thread sensative objects
        /// you should invoke their thread to run code
        /// </summary>
        void TimedUpdate();
    }
    
    /// <summary>
    /// A class that sends timed update messages
    /// to updatable objects
    /// </summary>
    class Updater
    {
        /// <summary>The object being updated</summary>
        public IUpdatable Obj { get; set; }
        
        /// <summary>The time between update events</summary>
        public int Interval { get; set; }
        
        private System.Timers.Timer timer = null;
        
        /// <summary>Constructs a updater for the given object</summary>
        /// <param name="obj">The object that will recieve updates</param>
        public Updater(IUpdatable obj)
        {
            Obj = obj;
            Interval = 1000;
        }

        /// <summary>Begins sending update messages</summary>
        /// <param name="interval">The frequency (in miliseconds) of updates</param>
        /// <returns>Whether the operation was a success</returns>
        public bool StartTimedUpdates(int interval)
        {
            if ( interval > 0 )
            {
                Interval = interval;
                timer = new System.Timers.Timer(Interval);
                timer.Elapsed += OnTimedEvent;
                timer.AutoReset = false;
                timer.Enabled = true;
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Called every 'interval' miliseconds after StartTimedUpdates
        /// succeeds; calls TimedUpdate() for the stored object
        /// </summary>
        /// <param name="source">unused</param>
        /// <param name="e">unused</param>
        private void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            if ( Obj != null )
                Obj.TimedUpdate();

            if ( timer != null )
                timer.Interval = Interval;
        }
    }
}
