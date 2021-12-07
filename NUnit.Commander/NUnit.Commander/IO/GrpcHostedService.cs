using Grpc.Core;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NUnit.Commander.IO
{
    /// <summary>
    /// Grpc Host that supports start/stop
    /// </summary>
    internal class GrpcHostedService : IHostedService
    {
        private Server _server;
        private readonly ManualResetEvent _shutdownComplete = new ManualResetEvent(false);
        public Guid ServerId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Waithandle that signals when the server shutdown is complete
        /// </summary>
        public ManualResetEvent ShutdownComplete => _shutdownComplete;

        public GrpcHostedService(Server server)
        {
            _server = server;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _server.Start();
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            // stop the service
            await _server.ShutdownAsync();
            _shutdownComplete.Set();
        }
    }
}
