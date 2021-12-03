using System;

namespace NUnit.Commander.IO
{
    public class IpcClientException : Exception
    {
        public IpcClientException(string message) : base(message) { }
    }
}
