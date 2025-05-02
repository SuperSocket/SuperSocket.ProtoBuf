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
    public class ProtobufPackageEncoder : IPackageEncoder<ProtobufPackageInfo>
    {
        private readonly Dictionary<Type, int> _typeToIdMapping = new Dictionary<Type, int>();

        /// <summary>
        /// Register a message type with its type identifier
        /// </summary>
        /// <param name="messageType">The message type</param>
        /// <param name="typeId">The message type identifier</param>
        /// <exception cref="ArgumentNullException">Thrown when messageType is null</exception>
        public void RegisterMessageType(Type messageType, int typeId)
        {
            if (messageType == null)
                throw new ArgumentNullException(nameof(messageType));

            _typeToIdMapping[messageType] = typeId;
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
        public int Encode(IBufferWriter<byte> writer, ProtobufPackageInfo package)
        {
            if (package == null)
                throw new ArgumentNullException(nameof(package));

            if (package.Message == null)
                throw new ArgumentException("Message cannot be null", nameof(package));

            var message = package.Message;
            var messageType = message.GetType();

            // Get the message type ID from the package directly or from the mapping
            int typeId = package.TypeId;
            
            // If type ID is not set, try to get it from the mapping
            if (typeId == 0)
            {
                if (!_typeToIdMapping.TryGetValue(messageType, out typeId))
                    throw new InvalidOperationException($"Message type {messageType.FullName} is not registered with a type ID");
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
    }
}