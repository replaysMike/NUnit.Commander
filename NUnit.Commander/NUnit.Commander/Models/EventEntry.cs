using System;
using System.Collections.Generic;
using System.Text;

namespace NUnit.Commander.Models
{
    public class EventEntry<T>
    {
        public bool IsQueuedForRemoval => RemovalTime != DateTime.MinValue;
        public DateTime RemovalTime { get; set; }
        public DateTime DateAdded { get; }
        public T Event { get; }

        public EventEntry(T dataEvent)
        {
            Event = dataEvent;
            DateAdded = DateTime.Now;
        }
    }
}
