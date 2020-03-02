using System;

namespace NUnit.Commander.Models
{
    public class EventEntry
    {
        public bool IsQueuedForRemoval => RemovalTime != DateTime.MinValue;
        public DateTime RemovalTime { get; set; }
        public DateTime DateAdded { get; }
        public DataEvent Event { get; set; }

        public EventEntry(DataEvent dataEvent)
        {
            Event = dataEvent;
            DateAdded = DateTime.Now;
        }

        public EventEntry(EventEntry eventEntry)
        {
            RemovalTime = eventEntry.RemovalTime;
            DateAdded = eventEntry.DateAdded;
            Event = new DataEvent(eventEntry.Event);
        }

        public override string ToString()
        {
            return $"{Event.ToString()}";
        }
    }
}
