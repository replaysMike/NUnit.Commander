using NUnit.Commander.Configuration;
using NUnit.Commander.Models;
using ProtoBuf;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Xml.Serialization;

namespace NUnit.Commander.IO
{
    /// <summary>
    /// Multi-client Interprocess (IPC) server
    /// </summary>
    public partial class IpcServer : IDisposable
    {
        /// <summary>
        /// Maximum number of clients that can be simultaneously connected. Also limits the number of threads used to manage Ipc clients
        /// </summary>
        private const int MaxClients = 32;
        private const int LockWaitMilliseconds = 5 * 1000;
        private readonly Encoding UseEncoding = Encoding.UTF8;
        /// <summary>
        /// how often to should poll for test event updates
        /// </summary>
        private const int DefaultPollIntervalMilliseconds = 33;
        /// <summary>
        /// This value determines how many IPC pipes can share the same name.
        /// It does not indicate the max number of threads used.
        /// </summary>
        private const int MaxNumberOfServerInstances = 64;
        private const int BufferSize = 1024 * 256;
        private const int MaxMessageBufferSize = 1024 * 1024 * 4; // 4mb max total message size
        private const int MessageBufferSize = 1024 * 256;
        private const ushort StartMessageHeader = 0xA0FF;
        private const ushort EndMessageHeader = 0xA1FF;
        private const byte TotalHeaderLength = sizeof(UInt16) + sizeof(UInt32) + sizeof(UInt16);
        private const byte StringPreambleLength = 0;

        private readonly ManualResetEvent _closeEvent;
        private readonly byte[] _messageBuffer = new byte[MaxMessageBufferSize];
        private readonly XmlSerializer _xmlSerializer = new XmlSerializer(typeof(DataEvent));
        private int _connectedClients;
        private ApplicationConfiguration _configuration;
        private byte[] _messageBufferBytes;
        private SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private int _messageByteIndex = 0;
        private bool _isDisposed = false;

        public delegate void MessageReceivedEventHandler(object sender, MessageEventArgs e);

        /// <summary>
        /// Triggered when a message is received from a connected client
        /// </summary>
        public event MessageReceivedEventHandler OnMessageReceived;

        public IpcServer(ApplicationConfiguration configuration)
        {
            _configuration = configuration;
            _closeEvent = new ManualResetEvent(false);
        }

        public void Start()
        {
            CreateConnectionListenerThread();
        }

        private void CreateConnectionListenerThread()
        {
            var thread = new Thread(new ThreadStart(ConnectionThread));
            thread.Name = "Connection Listener";
            thread.Start();
        }

        private void CreateReadThread(ConnectionState state)
        {
            var thread = new Thread(new ParameterizedThreadStart(ReadThread));
            thread.Name = "Read Thread";
            thread.IsBackground = true;
            thread.Start(state);
        }

        private void ConnectionThread()
        {
            var serverStream = new NamedPipeServerStream(
                nameof(Commander),
                PipeDirection.In,
                MaxNumberOfServerInstances,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous,
                BufferSize,
                BufferSize);
            var asyncResult = serverStream.BeginWaitForConnection((ar) => HandleConnection(ar), serverStream);
        }

        private void ReadThread(object state)
        {
            if (state is not ConnectionState connectionState)
                throw new ArgumentNullException($"nameof({ReadThread}) - {nameof(state)} must be of type {nameof(ConnectionState)} and cannot be null!");
            var serverStream = connectionState.ServerStream;
            var dataReadEvent = connectionState.DataReadEvent;
            var receiveBuffer = new byte[MessageBufferSize];
            while (!_closeEvent.WaitOne(DefaultPollIntervalMilliseconds))
            {
                try
                {
                    // if we aren't currently reading data, start reading.
                    if (serverStream.CanRead && !connectionState.DataReadEvent.WaitOne(10))
                    {
                        connectionState.DataReadEvent.Set();
                        var readState = new ReadState(connectionState, receiveBuffer);
                        serverStream.BeginRead(receiveBuffer, 0, receiveBuffer.Length, new AsyncCallback(ReadCallback), readState);
                    }
                }
                catch (ObjectDisposedException)
                {
                    // stream was disposed
                    break;
                }
            }
            Interlocked.Decrement(ref _connectedClients);
            Debug.WriteLine($"[{DateTime.Now}]|INFO|{nameof(HandleConnection)}|Ipc Client '{connectionState.Id}' has disconnected! Current clients: {_connectedClients}{Environment.NewLine}");
        }

        private void ReadCallback(IAsyncResult ar)
        {
            if (_isDisposed) return;
            try
            {
                if (ar.AsyncState is not ReadState readState)
                    throw new ArgumentNullException($"{nameof(ar.AsyncState)} must be of type {nameof(ReadState)} and cannot be null!");
                var receiveBuffer = readState.ReceiveBuffer;
                var serverStream = readState.ServerStream;
                var dataReadEvent = readState.DataReadEvent;
                var bytesRead = 0;
                try
                {
                    bytesRead = serverStream.EndRead(ar);
                    // Debug.WriteLine($"{bytesRead} bytes read.");
                }
                catch (InvalidOperationException ex)
                {
                    // client disconnected?
                    Debug.WriteLine($"ERROR|{nameof(ReadCallback)}|{ex.Message} {ex.StackTrace}");
                    return;
                }

                if (bytesRead > 0)
                {
                    Debug.WriteLine($"[{DateTime.Now}]|INFO|{nameof(HandleConnection)}|Ipc data being read for client {readState.Id}...{Environment.NewLine}");
                    _lock?.Wait();
                    try
                    {
                        // copy data into the main message buffer for processing
                        Array.Copy(receiveBuffer, 0, _messageBuffer, _messageByteIndex, bytesRead);
                        _messageByteIndex += bytesRead;

                        var dataToProcess = true;
                        var loopCounter = 0;
                        while (dataToProcess || loopCounter > 1000)
                        {
                            loopCounter++;
                            if (_messageByteIndex >= TotalHeaderLength)
                            {
                                // we have at least the header, read it and any additional data
                                var startMessageHeader = BitConverter.ToUInt16(_messageBuffer, 0);
                                var totalMessageLength = BitConverter.ToInt32(_messageBuffer, sizeof(UInt16));
                                var endMessageHeader = BitConverter.ToUInt16(_messageBuffer, sizeof(UInt16) + sizeof(UInt32));
                                if (startMessageHeader != StartMessageHeader)
                                    throw new InvalidOperationException($"ERROR|Message start header '{startMessageHeader}' was not the expected value of '{StartMessageHeader}'.");
                                if (endMessageHeader != EndMessageHeader)
                                    throw new InvalidOperationException($"ERROR|Message end header '{endMessageHeader}' was not the expected value of '{EndMessageHeader}'.");
                                // have we received all the data? (message size, plus it's header)
                                if (_messageByteIndex >= totalMessageLength + TotalHeaderLength)
                                {
                                    // all data received for message
                                    // read in the data
                                    var messageBytes = new byte[totalMessageLength - StringPreambleLength];
                                    Array.Copy(_messageBuffer, TotalHeaderLength + StringPreambleLength, messageBytes, 0, messageBytes.Length);
                                    DataEvent dataEvent = null;
                                    string eventStr = null;
                                    try
                                    {
                                        switch (_configuration.EventFormatType)
                                        {
                                            default:
                                            case EventFormatTypes.Json:
                                                eventStr = UseEncoding.GetString(messageBytes);
                                                dataEvent = JsonSerializer.Deserialize<DataEvent>(eventStr);
                                                break;
                                            case EventFormatTypes.Xml:
                                                eventStr = UseEncoding.GetString(messageBytes);
                                                using (var stringReader = new StringReader(eventStr))
                                                {
                                                    dataEvent = _xmlSerializer.Deserialize(stringReader) as DataEvent;
                                                }
                                                break;
                                            case EventFormatTypes.Binary:
                                                using (var stream = new MemoryStream(messageBytes))
                                                {
                                                    dataEvent = Serializer.Deserialize<DataEvent>(stream);
                                                }
                                                break;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        // failed to deserialize json
                                        throw new IpcClientException($"Failed to deserialize event data. Ensure you have the 'EventFormatType' configured correctly. {ex.Message}");
                                    }
                                    var e = new EventEntry(dataEvent);
                                    // Debug.WriteLine($"IPCREAD: {e.Event.Event} {(e.Event.TestName ?? e.Event.TestSuite)}");

                                    // if there is more data received past the initial message size, copy it to the beginning of the buffer
                                    var totalMessageDataLength = TotalHeaderLength + totalMessageLength;
                                    if (_messageByteIndex > totalMessageDataLength)
                                    {
                                        var bufferOverflowBytes = new byte[_messageByteIndex - totalMessageDataLength];
                                        // copy the overflow data
                                        Array.Copy(_messageBuffer, totalMessageDataLength, bufferOverflowBytes, 0, bufferOverflowBytes.Length);
                                        // move the overflow data to the start of the message buffer
                                        Array.Copy(bufferOverflowBytes, 0, _messageBuffer, 0, bufferOverflowBytes.Length);
                                        // reset the buffer position, ready for next message
                                        _messageByteIndex = bufferOverflowBytes.Length;
                                    }
                                    else
                                    {
                                        // reset the buffer position, ready for next message
                                        _messageByteIndex = 0;
                                    }

                                    // let commander know we've received a new message
                                    OnMessageReceived?.Invoke(this, new MessageEventArgs(e));
                                }
                                else
                                {
                                    // more data needed to process the message
                                    dataToProcess = false;
                                }
                            }
                            else
                            {
                                // more data needed to read the header
                                dataToProcess = false;
                            }
                        }
                    }
                    finally
                    {
                        _lock?.Release();
                    }
                }
                // signal ready to read more data
                dataReadEvent.Reset();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{nameof(ReadCallback)}|ERROR|{ex.GetBaseException().Message}|{ex.StackTrace}");
            }
        }

        private void HandleConnection(IAsyncResult c)
        {
            // a client has connected
            try
            {
                if (_connectedClients < MaxClients)
                {
                    // create a new listener on a new thread for more clients to connect
                    CreateConnectionListenerThread();

                    // process the new connection
                    var serverStream = c.AsyncState as NamedPipeServerStream;

                    var waitResult = WaitHandle.WaitAny(new[] { _closeEvent, c.AsyncWaitHandle });
                    // handle connection wait abort, server shutting down

                    // if waitResult = 0, the _closeEvent waithandle was received
                    if (waitResult == 0)
                        return;
                    // if waitResult = 1, the connection waithandle was received and we need to process the new connection
                    _messageBufferBytes = new byte[MaxMessageBufferSize];
                    serverStream.EndWaitForConnection(c);
                    Interlocked.Increment(ref _connectedClients);
                    Debug.WriteLine($"[{DateTime.Now}]|INFO|{nameof(HandleConnection)}|Ipc Client has connected! Current clients: {_connectedClients}{Environment.NewLine}");

                    // create a thread for reading data on this server stream
                    var connectionState = new ConnectionState(Guid.NewGuid(), serverStream);
                    CreateReadThread(connectionState);
                }
            }
            catch (IOException)
            {
                // message "The pipe has ended" received when server stream is forcibly closed
                //WriteLog($"[{DateTime.Now}]|ERROR|{nameof(HandleConnection)}|{ex.GetBaseException().Message}|{ex.StackTrace.ToString()}{Environment.NewLine}");
            }
            catch (ObjectDisposedException)
            {
                WriteLog($"[{DateTime.Now}]|ERROR|{nameof(HandleConnection)}|Timeout waiting for a client to connect!{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                WriteLog($"[{DateTime.Now}]|ERROR|{nameof(HandleConnection)}|Unhandled Exception|{ex.GetBaseException().Message}|{ex.StackTrace.ToString()}{Environment.NewLine}");
            }
        }

        private void WriteLog(string text)
        {
            Console.WriteLine(text);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            try
            {
                if (isDisposing)
                {
                    try
                    {
                        _lock.Wait(LockWaitMilliseconds);
                        try
                        {
                            _closeEvent?.Set();
                        }
                        finally
                        {
                            _lock.Release();
                            _lock.Dispose();
                            _lock = null;
                        }
                    }
                    finally
                    {
                    }
                    _isDisposed = true;
                }
            }
            catch (Exception ex)
            {
                WriteLog($"[{DateTime.Now}]|ERROR|{nameof(IpcServer)}|{nameof(Dispose)}|{ex.GetBaseException().Message}|{ex.StackTrace.ToString()}{Environment.NewLine}");
            }
        }
    }
}
