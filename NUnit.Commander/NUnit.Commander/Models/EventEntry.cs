using System;

namespace NUnit.Commander.Models
{
    public class EventEntry<T>
    {
        public bool IsQueuedForRemoval => RemovalTime != DateTime.MinValue;
        public DateTime RemovalTime { get; set; }
        public DateTime DateAdded { get; }
        public T Event { get; set; }

        public EventEntry(T dataEvent)
        {
            Event = dataEvent;
            DateAdded = DateTime.Now;
        }
    }
}
