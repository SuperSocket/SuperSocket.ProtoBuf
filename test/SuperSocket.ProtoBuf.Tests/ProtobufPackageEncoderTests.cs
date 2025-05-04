using System;
using System.Buffers;
using System.IO.Pipelines;
using Xunit;
using FluentAssertions;
using Google.Protobuf;
using SuperSocket.ProtoBuf;

namespace SuperSocket.ProtoBuf.Tests
{
    public class ProtobufPackageEncoderTests
    {
        private readonly ProtobufTypeRegistry _registry;
        private readonly ProtobufPackageEncoder _encoder;
        private readonly TestMessage _testMessage;
        private readonly int _testTypeId = 1;

        public ProtobufPackageEncoderTests()
        {
            _registry = new ProtobufTypeRegistry();
            _registry.RegisterMessageType(_testTypeId, typeof(TestMessage), TestMessage.Parser);
            _encoder = new ProtobufPackageEncoder(_registry);

            _testMessage = new TestMessage
            {
                Id = 42,
                Name = "Test Message",
                Data = Google.Protobuf.ByteString.CopyFromUtf8("Sample Data")
            };
        }

        [Fact]
        public void Encode_ShouldCorrectlyEncodeMessageWithHeader()
        {
            // Arrange
            var pipe = new Pipe();
            var expectedMessageSize = _testMessage.CalculateSize();

            // Act
            var bytesWritten = _encoder.Encode(pipe.Writer, _testMessage);
            pipe.Writer.FlushAsync().GetAwaiter().GetResult();
            pipe.Writer.Complete();
            
            // Assert
            bytesWritten.Should().Be(expectedMessageSize + 8); // message size + 8-byte header

            var result = pipe.Reader.ReadAsync().GetAwaiter().GetResult();
            var buffer = result.Buffer;
            
            // Verify header
            var reader = new SequenceReader<byte>(buffer);
            reader.TryReadBigEndian(out int encodedSize);
            reader.TryReadBigEndian(out int encodedTypeId);
            
            encodedSize.Should().Be(expectedMessageSize);
            encodedTypeId.Should().Be(_testTypeId);
            
            // Verify message content
            var messageBuffer = buffer.Slice(8);
            var decodedMessage = TestMessage.Parser.ParseFrom(messageBuffer);
            decodedMessage.Id.Should().Be(_testMessage.Id);
            decodedMessage.Name.Should().Be(_testMessage.Name);
            decodedMessage.Data.ToStringUtf8().Should().Be("Sample Data");
            
            pipe.Reader.Complete();
        }

        [Fact]
        public void Encode_WithNullPackage_ShouldThrow()
        {
            // Arrange
            var pipe = new Pipe();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _encoder.Encode(pipe.Writer, null!));
        }

        [Fact]
        public void Encode_WithUnregisteredMessageType_ShouldThrow()
        {
            // Arrange
            var registry = new ProtobufTypeRegistry();
            var encoder = new ProtobufPackageEncoder(registry);
            var pipe = new Pipe();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => encoder.Encode(pipe.Writer, _testMessage));
        }

        [Fact]
        public void Constructor_WithNullRegistry_ShouldThrow()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ProtobufPackageEncoder(null!));
        }
    }
}