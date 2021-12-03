using NUnit.Commander.Models;
using System;

namespace NUnit.Commander.IO
{
    public class MessageEventArgs : EventArgs
    {
        public EventEntry EventEntry { get; set; }
        public MessageEventArgs(EventEntry eventEntry)
        {
            EventEntry = eventEntry;
        }
    }
}
