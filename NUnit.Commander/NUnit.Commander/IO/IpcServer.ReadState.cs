namespace NUnit.Commander.IO
{
    public partial class IpcServer
    {
        private class ReadState : ConnectionState
        {
            /// <summary>
            /// Receive buffer for reading data
            /// </summary>
            public byte[] ReceiveBuffer { get; }

            public ReadState(ConnectionState connectionState, byte[] receiveBuffer) : base(connectionState.Id, connectionState.ServerStream)
            {
                DataReadEvent = connectionState.DataReadEvent;
                ReceiveBuffer = receiveBuffer;
            }
        }
    }
}
