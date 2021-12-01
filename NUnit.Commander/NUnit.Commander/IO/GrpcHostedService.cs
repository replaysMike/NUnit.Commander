using Grpc.Core;
using Microsoft.Extensions.Hosting;
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
        }
    }
}
