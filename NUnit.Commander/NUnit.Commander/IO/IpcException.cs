using System;

namespace NUnit.Commander.IO
{
    public class IpcException : Exception
    {
        public IpcException(string message) : base(message) { }
    }
}
