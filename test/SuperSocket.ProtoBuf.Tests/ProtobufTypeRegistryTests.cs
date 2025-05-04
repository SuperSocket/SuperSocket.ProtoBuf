using System;
using Xunit;
using FluentAssertions;
using Google.Protobuf;
using SuperSocket.ProtoBuf;
using SuperSocket.ProtoBuf.Tests;

namespace SuperSocket.ProtoBuf.Tests
{
    public class ProtobufTypeRegistryTests
    {
        [Fact]
        public void RegisterMessageType_ShouldStoreTypeInformation()
        {
            // Arrange
            var registry = new ProtobufTypeRegistry();
            var messageType = typeof(TestMessage);
            var parser = TestMessage.Parser;
            const int typeId = 1;

            // Act
            registry.RegisterMessageType(typeId, messageType, parser);

            // Assert
            registry.TryGetTypeId(messageType, out var retrievedTypeId).Should().BeTrue();
            retrievedTypeId.Should().Be(typeId);

            registry.TryGetMessageType(typeId, out var retrievedType).Should().BeTrue();
            retrievedType.Should().Be(messageType);

            registry.TryGetParser(typeId, out var retrievedParser).Should().BeTrue();
            retrievedParser.Should().Be(parser);
        }

        [Fact]
        public void RegisterMessageType_WithDuplicateTypeId_ShouldThrow()
        {
            // Arrange
            var registry = new ProtobufTypeRegistry();
            registry.RegisterMessageType(1, typeof(TestMessage), TestMessage.Parser);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => 
                registry.RegisterMessageType(1, typeof(AnotherTestMessage), AnotherTestMessage.Parser));
        }

        [Fact]
        public void RegisterMessageType_WithDuplicateType_ShouldThrow()
        {
            // Arrange
            var registry = new ProtobufTypeRegistry();
            registry.RegisterMessageType(1, typeof(TestMessage), TestMessage.Parser);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => 
                registry.RegisterMessageType(2, typeof(TestMessage), TestMessage.Parser));
        }

        [Fact]
        public void GetTypeId_ForUnregisteredType_ShouldThrow()
        {
            // Arrange
            var registry = new ProtobufTypeRegistry();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => 
                registry.GetTypeId(typeof(TestMessage)));
        }

        [Fact]
        public void GetMessageType_ForUnregisteredTypeId_ShouldThrow()
        {
            // Arrange
            var registry = new ProtobufTypeRegistry();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => 
                registry.GetMessageType(1));
        }

        [Fact]
        public void GetParser_ForUnregisteredTypeId_ShouldThrow()
        {
            // Arrange
            var registry = new ProtobufTypeRegistry();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => 
                registry.GetParser(1));
        }

        [Fact]
        public void RegisterMessageType_WithNullType_ShouldThrow()
        {
            // Arrange
            var registry = new ProtobufTypeRegistry();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                registry.RegisterMessageType(1, null!, TestMessage.Parser));
        }

        [Fact]
        public void RegisterMessageType_WithNullParser_ShouldThrow()
        {
            // Arrange
            var registry = new ProtobufTypeRegistry();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                registry.RegisterMessageType(1, typeof(TestMessage), null!));
        }
    }
}