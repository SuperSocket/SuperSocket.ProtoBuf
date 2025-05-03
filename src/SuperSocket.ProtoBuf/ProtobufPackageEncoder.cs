using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using Google.Protobuf;
using SuperSocket.ProtoBase;

namespace SuperSocket.ProtoBuf
{
    /// <summary>
    /// Provides encoding functionality for protobuf messages, transforming them into network-ready binary packets.
    /// </summary>
    /// <remarks>
    /// The encoder prepends each message with an 8-byte header consisting of:
    /// - 4 bytes for message size (big-endian)
    /// - 4 bytes for message type ID (big-endian)
    /// </remarks>
    public abstract class ProtobufPackageEncoder<TPackageInfo> : IPackageEncoder<TPackageInfo>
    {
        private readonly ProtobufTypeRegistry _typeRegistry;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProtobufPackageEncoder{TPackageInfo}"/> class.
        /// </summary>
        /// <param name="typeRegistry">The protobuf type registry to use for encoding</param>
        public ProtobufPackageEncoder(ProtobufTypeRegistry typeRegistry)
        {
            _typeRegistry = typeRegistry ?? throw new ArgumentNullException(nameof(typeRegistry));
        }

        /// <summary>
        /// Encodes a ProtobufPackageInfo into a binary format suitable for network transmission
        /// </summary>
        /// <param name="writer">The buffer writer to write the encoded package to</param>
        /// <param name="package">The protobuf package to encode</param>
        /// <returns>The total number of bytes written</returns>
        /// <exception cref="ArgumentNullException">Thrown when package is null</exception>
        /// <exception cref="ArgumentException">Thrown when package.Message is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when the message type is not registered</exception>
        public int Encode(IBufferWriter<byte> writer, TPackageInfo package)
        {
            if (package == null)
                throw new ArgumentNullException(nameof(package));

            var message = GetProtoBufMessage(package);

            // Get the message type ID from the package directly or from the registry
            int typeId = GetProtoBufMessageTypeId(package);
            
            // If type ID is not set, try to get it from the registry
            if (typeId == 0)
            {
                typeId = GetMessageTypeId(message.GetType());
            }

            // Calculate the message size
            int messageSize = message.CalculateSize();
            
            // Reserve space for the header (length + type ID)
            var headerBuffer = writer.GetSpan(8);
            
            // Write the message size (excluding the type ID field)
            BinaryPrimitives.WriteInt32BigEndian(headerBuffer, messageSize);
            
            // Write the message type ID
            BinaryPrimitives.WriteInt32BigEndian(headerBuffer.Slice(4), typeId);
            
            // Advance the writer past the header
            writer.Advance(8);

            // Write the actual message
            var messageBuffer = writer.GetSpan(messageSize);
            message.WriteTo(messageBuffer);
            writer.Advance(messageSize);

            // Return the total bytes written
            return messageSize + 8;
        }

        /// <summary>
        /// Tries to get the message type ID from the registry.
        /// </summary>
        /// <param name="messageType">The message type.</param>
        /// <exception cref="InvalidOperationException">Thrown when the message type is not registered</exception>
        /// <returns>The type ID.</returns>
        protected int GetMessageTypeId(Type messageType)
        {
            if (!_typeRegistry.TryGetTypeId(messageType, out var typeId))
            {
                throw new InvalidOperationException($"Message type {messageType.FullName} is not registered with a type ID");
            }

            return typeId;
        }

        /// <summary>
        /// Converts a package info into a ProtoBuf message.
        /// </summary>
        /// <param name="package">The package.</param>
        protected abstract IMessage GetProtoBufMessage(TPackageInfo package);

        /// <summary>
        /// Gets the ProtoBuf message type ID from the package.
        /// </summary>
        /// <param name="package">The package.</param>
        protected virtual int GetProtoBufMessageTypeId(TPackageInfo package)
        {
            return 0;
        }
    }

    /// <summary>
    /// A concrete implementation of ProtobufPackageEncoder for IMessage.
    /// </summary>
    public class ProtobufPackageEncoder : ProtobufPackageEncoder<IMessage>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProtobufPackageEncoder"/> class.
        /// </summary>
        /// <param name="typeRegistry">The protobuf type registry to use for encoding</param>
        public ProtobufPackageEncoder(ProtobufTypeRegistry typeRegistry)
            : base(typeRegistry)
        {
        }

        /// <inheritdoc/>
        protected override IMessage GetProtoBufMessage(IMessage package)
        {
            return package;
        }

        /// <inheritdoc/>
        protected override int GetProtoBufMessageTypeId(IMessage package)
        {
            return GetMessageTypeId(package.GetType());
        }
    }
}