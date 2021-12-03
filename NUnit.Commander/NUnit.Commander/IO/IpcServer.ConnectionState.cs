using System;
using System.IO.Pipes;
using System.Threading;

namespace NUnit.Commander.IO
{
    public partial class IpcServer
    {
        private class ConnectionState
        {
            /// <summary>
            /// Unique connection id
            /// </summary>
            public Guid Id { get; protected set; }

            /// <summary>
            /// Data being read waithandle
            /// </summary>
            public ManualResetEvent DataReadEvent { get; protected set; } = new ManualResetEvent(false);

            /// <summary>
            /// The unique Ipc server stream for the client
            /// </summary>
            public NamedPipeServerStream ServerStream { get; protected set; }

            public ConnectionState(Guid id, NamedPipeServerStream namedPipeServerStream)
            {
                Id = id;
                ServerStream = namedPipeServerStream;
            }
        }
    }
}
