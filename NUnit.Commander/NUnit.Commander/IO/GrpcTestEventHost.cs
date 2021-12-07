using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Commander.IO.Services;
using TestEventService1;
using Grpc.Reflection;
using Grpc.Reflection.V1Alpha;
using System.Threading.Tasks;
using System.Threading;
using NUnit.Commander.Configuration;
using System;

namespace NUnit.Commander.IO
{
    /// <summary>
    /// Hosts a Grpc TestEvent service
    /// </summary>
    internal class GrpcTestEventHost
    {
        private const string GrpcServer = "127.0.0.1";
        private readonly TimeSpan MaxShutdownWaitTime = TimeSpan.FromSeconds(5);
        private readonly HostBuilder _builder;
        private readonly ApplicationConfiguration _configuration;
        private ServiceProvider _serviceProvider;

        public delegate void TestEventHandler(object sender, MessageEventArgs e);
        /// <summary>
        /// Fired when a test event is received
        /// </summary>
        public event TestEventHandler TestEventReceived;

        public GrpcTestEventHost(ApplicationConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _builder = new HostBuilder();
            
            _builder.ConfigureServices(services =>
            {
                services.AddGrpc();
            });
            var stuff = _builder.ConfigureServices((hostContext, services) =>
            {
                // setup the reflection support for service discovery
                var reflectionService = new ReflectionServiceImpl(TestEvent.Descriptor, ServerReflection.Descriptor);
                var testEventService = new TestEventService(_configuration);
                testEventService.TestEventReceived += TestEventService_TestEventReceived;
                // create a Grpc server
                var server = new Server
                {
                    Services = { TestEvent.BindService(testEventService), ServerReflection.BindService(reflectionService) },
                    Ports = { new ServerPort(GrpcServer, _configuration.Port, ServerCredentials.Insecure) },
                };
                var gprcHostedService = new GrpcHostedService(server);
                services.AddSingleton(server);
                services.AddSingleton(gprcHostedService);
                services.AddSingleton<IHostedService, GrpcHostedService>(serviceProvider => gprcHostedService);
                _serviceProvider = services.BuildServiceProvider();
            });
        }

        private void TestEventService_TestEventReceived(object sender, MessageEventArgs e)
        {
            TestEventReceived?.Invoke(sender, e);
        }

        /// <summary>
        /// Run the Grpc Host
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            await _builder.RunConsoleAsync(cancellationToken);
        }

        /// <summary>
        /// Task that waits until shutdown is complete
        /// </summary>
        /// <returns></returns>
        public void WaitForShutdown()
        {
            var service = _serviceProvider.GetRequiredService<GrpcHostedService>();
            service.ShutdownComplete.WaitOne(MaxShutdownWaitTime);
        }
    }
}
