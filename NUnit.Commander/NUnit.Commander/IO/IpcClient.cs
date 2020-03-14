using AnyConsole;
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
    /// Connects to an IpcServer
    /// </summary>
    public class IpcClient : IDisposable
    {
        private readonly Encoding UseEncoding = Encoding.UTF8;
        // how often to should poll for test event updates
        private const int DefaultPollIntervalMilliseconds = 33;
        private const int MaxMessageBufferSize = 1024 * 1024 * 4; // 4mb max total message size
        private const int MessageBufferSize = 1024 * 256;
        private const ushort StartMessageHeader = 0xA0FF;
        private const ushort EndMessageHeader = 0xA1FF;
        private const byte TotalHeaderLength = sizeof(UInt16) + sizeof(UInt32) + sizeof(UInt16);
        private const byte StringPreambleLength = 0;

        public delegate void MessageReceivedEventHandler(object sender, MessageEventArgs e);
        /// <summary>
        /// Triggered when a message is received
        /// </summary>
        public event MessageReceivedEventHandler OnMessageReceived;


        private readonly byte[] _messageBuffer = new byte[MaxMessageBufferSize];

        private readonly IExtendedConsole _console;
        private readonly ManualResetEvent _dataReadEvent;
        private readonly ManualResetEvent _closeEvent;
        private readonly ApplicationConfiguration _config;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly XmlSerializer _xmlSerializer = new XmlSerializer(typeof(DataEvent));
        private bool _isDisposed;
        private int _messageByteIndex = 0;
        private NamedPipeClientStream _client;
        private Thread _readThread;

        public bool IsWaitingForConnection { get; private set; }

        public IpcClient(ApplicationConfiguration config, IExtendedConsole console)
        {
            _config = config;
            _console = console;
            _dataReadEvent = new ManualResetEvent(false);
            _closeEvent = new ManualResetEvent(false);
        }

        public void Connect(bool showOutput, Action<IpcClient> onSuccessConnect, Action<IpcClient> onFailedConnect)
        {
            _client = new NamedPipeClientStream(".", "TestMonitorExtension", PipeDirection.InOut);
            try
            {
                var timeoutSeconds = (int)TimeSpan.FromSeconds(_config.ConnectTimeoutSeconds).TotalMilliseconds;
                IsWaitingForConnection = true;
                if (timeoutSeconds > 0)
                    _client.Connect(timeoutSeconds);
                else
                    _client.Connect();
                _client.ReadMode = PipeTransmissionMode.Byte;
            }
            catch (TimeoutException)
            {
                IsWaitingForConnection = false;
                onFailedConnect?.Invoke(this);
                return;
            }
            IsWaitingForConnection = false;
            if (_readThread == null)
            {
                _readThread = new Thread(new ThreadStart(ReadThread));
                _readThread.IsBackground = true;
                _readThread.Name = "ReadThread";
                _readThread.Start();
            }
            onSuccessConnect?.Invoke(this);
        }

        private void ReadThread()
        {
            var receiveBuffer = new byte[MessageBufferSize];
            while (!_closeEvent.WaitOne(DefaultPollIntervalMilliseconds))
            {
                if (_client.CanRead && !_dataReadEvent.WaitOne(10))
                {
                    _dataReadEvent.Set();
                    _client.BeginRead(receiveBuffer, 0, receiveBuffer.Length, new AsyncCallback(ReadCallback), receiveBuffer);
                }
            }
        }

        private void ReadCallback(IAsyncResult ar)
        {
            if (_isDisposed) return;
            try
            {
                var receiveBuffer = ar.AsyncState as byte[];
                var bytesRead = 0;
                try
                {
                    bytesRead = _client.EndRead(ar);
                    Debug.WriteLine($"{bytesRead} bytes read.");
                }
                catch (InvalidOperationException ex)
                {
                    // server disconnected?
                    Debug.WriteLine($"ERROR|{nameof(ReadCallback)}|{ex.Message} {ex.StackTrace}");
                }

                if (bytesRead > 0)
                {
                    _lock?.Wait();
                    try
                    {
                        // copy data into the main message buffer for processing
                        Array.Copy(receiveBuffer, 0, _messageBuffer, _messageByteIndex, bytesRead);
                        _messageByteIndex += bytesRead;

                        var dataToProcess = true;
                        var loopCounter = 0;
                        while (dataToProcess)
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
                                        switch (_config.EventFormatType)
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
                                    Debug.WriteLine($"IPCREAD: {e.Event.Event} {(e.Event.TestName ?? e.Event.TestSuite)}");

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
                _dataReadEvent.Reset();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{nameof(ReadCallback)}|ERROR|{ex.GetBaseException().Message}|{ex.StackTrace}");
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            _isDisposed = true;
            if (isDisposing)
            {
                _closeEvent?.Set();
                _lock.Wait();
                try
                {
                    if (_readThread?.Join(5 * 1000) == false)
                        _readThread.Abort();
                    _closeEvent?.Dispose();
                    _readThread = null;
                    _console?.Dispose();
                }
                finally
                {
                    _lock.Release();
                    _lock.Dispose();
                }
            }
        }
    }

    public class IpcClientException : Exception
    {
        public IpcClientException(string message) : base(message) { }
    }

    public class MessageEventArgs : EventArgs
    {
        public EventEntry EventEntry { get; set; }
        public MessageEventArgs(EventEntry eventEntry)
        {
            EventEntry = eventEntry;
        }
    }
}
