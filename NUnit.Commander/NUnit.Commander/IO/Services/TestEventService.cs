using Grpc.Core;
using NUnit.Commander.Configuration;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TestEventService1;

namespace NUnit.Commander.IO.Services
{
    /// <summary>
    /// Grpc TestEvent implementation
    /// </summary>
    public class TestEventService : TestEvent.TestEventBase
    {
        private readonly XmlSerializer _xmlSerializer = new XmlSerializer(typeof(Models.DataEvent));
        private readonly Empty _empty = new Empty();
        private readonly ApplicationConfiguration _config;

        public delegate void TestEventHandler(object sender, MessageEventArgs e);
        /// <summary>
        /// Fired when a test event is received
        /// </summary>
        public event TestEventHandler TestEventReceived;

        public TestEventService(ApplicationConfiguration config)
        {
            _config = config;
        }

        public override Task<Empty> WriteTestEvent(TestEventRequest request, ServerCallContext context)
        {
            System.Diagnostics.Debug.WriteLine($"Grpc Received TestEvent: {request.Event}");
            var dataEvent = Deserialize(request.Event);
            TestEventReceived?.Invoke(this, new MessageEventArgs(dataEvent));
            return Task.FromResult(_empty);
        }

        private Models.EventEntry Deserialize(string eventStr)
        {
            Models.DataEvent dataEvent = null;
            try
            {
                switch (_config.EventFormatType)
                {
                    default:
                    case EventFormatTypes.Json:
                        dataEvent = JsonSerializer.Deserialize<Models.DataEvent>(eventStr);
                        break;
                    case EventFormatTypes.Xml:
                        using (var stringReader = new StringReader(eventStr))
                        {
                            dataEvent = _xmlSerializer.Deserialize(stringReader) as Models.DataEvent;
                        }
                        break;
                        //case EventFormatTypes.Binary:
                        //    using (var stream = new MemoryStream(messageBytes))
                        //    {
                        //        dataEvent = Serializer.Deserialize<Models.DataEvent>(stream);
                        //    }
                        //    break;
                }
            }
            catch (Exception ex)
            {
                // failed to deserialize json
                throw new IpcException($"Failed to deserialize event data. Ensure you have the 'EventFormatType' configured correctly. {ex.Message}");
            }
            return new Models.EventEntry(dataEvent);
        }
    }
}