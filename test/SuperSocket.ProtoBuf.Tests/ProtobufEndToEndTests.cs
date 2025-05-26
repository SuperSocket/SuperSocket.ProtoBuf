using System;
using System.Threading.Tasks;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using Xunit;
using FluentAssertions;
using Google.Protobuf;
using SuperSocket.ProtoBase;
using SuperSocket.Server;
using SuperSocket.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SuperSocket.Server.Host;
using SuperSocket.Server.Abstractions;
using SuperSocket.Server.Abstractions.Session;

namespace SuperSocket.ProtoBuf.Tests
{
    public class ProtobufEndToEndTests
    {
        private readonly IPEndPoint _serverEndPoint;
        private readonly ProtobufTypeRegistry _registry;
        private readonly int _testTypeId = 1;
        private readonly int _anotherTestTypeId = 2;

        public ProtobufEndToEndTests()
        {
            _serverEndPoint = new IPEndPoint(IPAddress.Loopback, 4040);

            // Create type registry
            _registry = new ProtobufTypeRegistry();
            _registry.RegisterMessageType(_testTypeId, typeof(TestMessage), TestMessage.Parser);
            _registry.RegisterMessageType(_anotherTestTypeId, typeof(AnotherTestMessage), AnotherTestMessage.Parser);
        }

        private IHost CreateServer(ProtobufTypeRegistry registry, Func<IAppSession, IMessage, ValueTask> packageHandler)
        {
            return SuperSocketHostBuilder.Create<IMessage>()
                .UsePackageDecoder<ProtobufPackageDecoder>()
                .UsePackageEncoder<ProtobufPackageEncoder>()
                .UsePipelineFilter<ProtobufPipelineFilter>()
                .ConfigureSuperSocket(options =>
                {
                    options.Name = "ProtobufTestServer";
                    options.Listeners = new List<ListenOptions>
                    {
                        new ListenOptions
                        {
                            Ip = _serverEndPoint.Address.ToString(),
                            Port = _serverEndPoint.Port
                        }
                    };
                })
                .UseSessionHandler(async (session) =>
                {
                    // Session connected handler
                    Console.WriteLine($"Session connected: {session.SessionID}");
                    await Task.CompletedTask;
                })
                .UsePackageHandler(packageHandler)
                .ConfigureLogging(loggingBuilder =>
                {
                    loggingBuilder.AddConsole();
                    loggingBuilder.SetMinimumLevel(LogLevel.Information);
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton(registry);
                })
                .Build();
        }

        [Fact]
        public async Task SendAndReceiveMessageTest()
        {
            using var server = CreateServer(_registry, async (session, package) =>
            {
                var encoder = session.Server.ServiceProvider.GetRequiredService<IPackageEncoder<IMessage>>();
                // Handle received package
                // Echo back any received message
                Console.WriteLine($"Received message from session {session.SessionID}");
                await session.SendAsync(encoder, package);
            });

            // Create client
            var pipelineFilter = new ProtobufPipelineFilter(new ProtobufPackageDecoder(_registry));
            var encoder = new ProtobufPackageEncoder(_registry);

            var client = new EasyClient<IMessage>(pipelineFilter).AsClient();
            
            // Create a test message to send
            var testMessage = new TestMessage
            {
                Id = 42,
                Name = "Integration Test",
                Data = ByteString.CopyFromUtf8("Client-Server Test")
            };

            // Connect to the server
            var connected = await client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, _serverEndPoint.Port));
            connected.Should().BeTrue();

            // Send the message
            await client.SendAsync(encoder, testMessage);

            // Receive the echo response
            var response = await client.ReceiveAsync();
            
            // Verify the response
            response.Should().NotBeNull();
            response.Should().BeOfType<TestMessage>();
            var typedResponse = response as TestMessage;
            
            typedResponse!.Id.Should().Be(testMessage.Id);
            typedResponse.Name.Should().Be(testMessage.Name);
            typedResponse.Data.Should().Equal(testMessage.Data);
            
            // Cleanup
            await client.CloseAsync();
        }

        [Fact]
        public async Task SendAndReceiveMultipleMessagesTest()
        {
            using var server = CreateServer(_registry, async (session, package) =>
            {
                var encoder = session.Server.ServiceProvider.GetRequiredService<IPackageEncoder<IMessage>>();
                // Handle received package
                // Echo back any received message
                Console.WriteLine($"Received message from session {session.SessionID}");
                await session.SendAsync(encoder, package);
            });

            // Create client
            var pipelineFilter = new ProtobufPipelineFilter(new ProtobufPackageDecoder(_registry));
            var encoder = new ProtobufPackageEncoder(_registry);

            var client = new EasyClient<IMessage>(pipelineFilter).AsClient();
            
            // Connect to the server
            var connected = await client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, _serverEndPoint.Port));
            connected.Should().BeTrue();

            // Send first message type
            var testMessage = new TestMessage
            {
                Id = 42,
                Name = "Integration Test 1",
                Data = ByteString.CopyFromUtf8("First Message")
            };
            await client.SendAsync(encoder, testMessage);
            
            // Receive first response
            var response1 = await client.ReceiveAsync();
            response1.Should().BeOfType<TestMessage>();
            var typedResponse1 = response1 as TestMessage;
            typedResponse1!.Name.Should().Be("Integration Test 1");

            // Send second message type
            var anotherMessage = new AnotherTestMessage
            {
                Message = "Second Test Message",
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            await client.SendAsync(encoder, anotherMessage);
            
            // Receive second response
            var response2 = await client.ReceiveAsync();
            response2.Should().BeOfType<AnotherTestMessage>();
            var typedResponse2 = response2 as AnotherTestMessage;
            typedResponse2!.Message.Should().Be("Second Test Message");
            
            // Cleanup
            await client.CloseAsync();
        }
    }
}
