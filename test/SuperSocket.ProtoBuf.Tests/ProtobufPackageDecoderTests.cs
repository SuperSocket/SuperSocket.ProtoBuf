using System;
using System.Buffers;
using System.IO.Pipelines;
using Xunit;
using FluentAssertions;
using Google.Protobuf;
using SuperSocket.ProtoBase;
using SuperSocket.ProtoBuf;

namespace SuperSocket.ProtoBuf.Tests
{
    public class ProtobufPackageDecoderTests
    {
        private readonly ProtobufTypeRegistry _registry;
        private readonly ProtobufPackageDecoder _decoder;
        private readonly TestMessage _testMessage;
        private readonly int _testTypeId = 1;

        public ProtobufPackageDecoderTests()
        {
            _registry = new ProtobufTypeRegistry();
            _registry.RegisterMessageType(_testTypeId, typeof(TestMessage), TestMessage.Parser);
            _decoder = new ProtobufPackageDecoder(_registry);

            _testMessage = new TestMessage
            {
                Id = 42,
                Name = "Test Message",
                Data = Google.Protobuf.ByteString.CopyFromUtf8("Sample Data")
            };
        }

        [Fact]
        public void Decode_ShouldCorrectlyDecodeMessage()
        {
            // Arrange
            var pipe = new Pipe();
            var messageSize = _testMessage.CalculateSize();
            
            // Write length field (4 bytes)
            var headerSpan = pipe.Writer.GetSpan(8);
            System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(headerSpan, messageSize);
            
            // Write type id (4 bytes)
            System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(headerSpan.Slice(4), _testTypeId);
            pipe.Writer.Advance(8);
            
            // Write the message content using byte array instead of span
            byte[] messageBytes = new byte[messageSize];
            _testMessage.WriteTo(messageBytes);
            pipe.Writer.Write(messageBytes);
            
            pipe.Writer.FlushAsync().GetAwaiter().GetResult();
            pipe.Writer.Complete();
            
            var result = pipe.Reader.ReadAsync().GetAwaiter().GetResult();
            var buffer = result.Buffer;

            // Act
            var decodedMessage = _decoder.Decode(ref buffer, null);
            
            // Assert
            decodedMessage.Should().NotBeNull();
            decodedMessage.Should().BeOfType<TestMessage>();
            var typedMessage = decodedMessage as TestMessage;
            
            typedMessage!.Id.Should().Be(_testMessage.Id);
            typedMessage.Name.Should().Be(_testMessage.Name);
            typedMessage.Data.Should().Equal(_testMessage.Data);
            
            pipe.Reader.Complete();
        }

        [Fact]
        public void Decode_WithUnregisteredTypeId_ShouldThrow()
        {
            // Arrange
            var pipe = new Pipe();
            var messageSize = _testMessage.CalculateSize();
            var unknownTypeId = 999;
            
            // Write length field (4 bytes)
            var headerSpan = pipe.Writer.GetSpan(8);
            System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(headerSpan, messageSize);
            
            // Write unregistered type id (4 bytes)
            System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(headerSpan.Slice(4), unknownTypeId);
            pipe.Writer.Advance(8);
            
            // Write the message content using byte array instead of span
            byte[] messageBytes = new byte[messageSize];
            _testMessage.WriteTo(messageBytes);
            pipe.Writer.Write(messageBytes);
            
            pipe.Writer.FlushAsync().GetAwaiter().GetResult();
            pipe.Writer.Complete();
            
            var result = pipe.Reader.ReadAsync().GetAwaiter().GetResult();
            var buffer = result.Buffer;

            // Act & Assert
            Assert.Throws<ProtocolException>(() => _decoder.Decode(ref buffer, null));
            
            pipe.Reader.Complete();
        }

        [Fact]
        public void Constructor_WithNullRegistry_ShouldThrow()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ProtobufPackageDecoder(null!));
        }
    }
}