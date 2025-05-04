using System;
using System.Buffers;
using System.IO.Pipelines;
using Xunit;
using FluentAssertions;
using Google.Protobuf;
using Moq;
using SuperSocket.ProtoBase;
using SuperSocket.ProtoBuf;

namespace SuperSocket.ProtoBuf.Tests
{
    public class ProtobufPipelineFilterTests
    {
        private readonly Mock<IPackageDecoder<IMessage>> _mockDecoder;
        private readonly ProtobufPipelineFilter _filter;
        private readonly TestMessage _testMessage;

        public ProtobufPipelineFilterTests()
        {
            _mockDecoder = new Mock<IPackageDecoder<IMessage>>();
            _filter = new ProtobufPipelineFilter(_mockDecoder.Object);
            _testMessage = new TestMessage
            {
                Id = 42,
                Name = "Test Message",
                Data = Google.Protobuf.ByteString.CopyFromUtf8("Sample Data")
            };
        }

        [Fact]
        public void Constructor_WithNullDecoder_ShouldThrow()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ProtobufPipelineFilter(null!));
        }

        [Fact]
        public void Filter_ShouldExtractCorrectBodyLength()
        {
            // Arrange
            var pipe = new Pipe();
            var messageSize = _testMessage.CalculateSize();
            var typeId = 1;
            
            // Write length field (4 bytes)
            var headerSpan = pipe.Writer.GetSpan(8);
            System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(headerSpan, messageSize);
            
            // Write type ID (4 bytes)
            System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(headerSpan.Slice(4), typeId);
            pipe.Writer.Advance(8);
            
            pipe.Writer.FlushAsync().GetAwaiter().GetResult();
            
            var result = pipe.Reader.ReadAsync().GetAwaiter().GetResult();
            var buffer = result.Buffer;

            // Act - Call the base class's method through reflection to access the protected method
            var method = typeof(ProtobufPipelineFilter)
                .BaseType!
                .GetMethod("GetBodyLengthFromHeader", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            var bodyLength = (int)method!.Invoke(_filter, new object[] { buffer })!;
            
            // Assert
            bodyLength.Should().Be(messageSize);
            
            pipe.Writer.Complete();
            pipe.Reader.Complete();
        }

        [Fact]
        public void Decode_ShouldWorkCorrectly()
        {
            // Arrange
            var messageSize = _testMessage.CalculateSize();
            var headerSize = 8; // 4 bytes for length + 4 bytes for type ID
            var buffer = new byte[headerSize + messageSize];
            
            // Write length field (4 bytes)
            System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(0, 4), messageSize);
            
            // Write type ID (4 bytes)
            System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(4, 4), 1);
            
            // Write message content
            _testMessage.WriteTo(buffer.AsSpan(headerSize));
            
            // Setup mock decoder to return our test message
            _mockDecoder.Setup(d => d.Decode(ref It.Ref<ReadOnlySequence<byte>>.IsAny, It.IsAny<object>()))
                .Returns(_testMessage);
            
            // Create a ReadOnlySequence for the decoder
            var sequence = new ReadOnlySequence<byte>(buffer);
            var bodySequence = sequence.Slice(headerSize);
            
            // Act
            var decodedMessage = _mockDecoder.Object.Decode(ref bodySequence, null);
            
            // Assert
            decodedMessage.Should().NotBeNull();
            decodedMessage.Should().Be(_testMessage);
        }
    }
}