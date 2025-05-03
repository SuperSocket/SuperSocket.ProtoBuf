using System;
using System.Buffers;
using Google.Protobuf;
using SuperSocket.ProtoBase;

namespace SuperSocket.ProtoBuf
{
    /// <summary>
    /// Provides decoding functionality for binary data into protobuf messages
    /// </summary>
    public abstract class ProtobufPackageDecoder<TPackageInfo> : IPackageDecoder<TPackageInfo>
    {
        private readonly ProtobufTypeRegistry _typeRegistry;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProtobufPackageDecoder{TPackageInfo}"/> class.
        /// </summary>
        /// <param name="typeRegistry">The protobuf type registry to use for decoding</param>
        /// <exception cref="ArgumentNullException">Thrown when typeRegistry is null</exception>
        public ProtobufPackageDecoder(ProtobufTypeRegistry typeRegistry)
        {
            _typeRegistry = typeRegistry ?? throw new ArgumentNullException(nameof(typeRegistry));
        }

        /// <summary>
        /// Decodes a binary packet into a TPackageInfo instance.
        /// </summary>
        /// <param name="buffer">The buffer containing the binary packet</param>
        /// <param name="context">The context object (not used in this implementation)</param>
        /// <exception cref="ProtocolException">Thrown when the message cannot be decoded</exception>
        public TPackageInfo Decode(ref ReadOnlySequence<byte> buffer, object context)
        {
            var reader = new SequenceReader<byte>(buffer);
            
            // Skip the length field that was already processed by the filter
            reader.Advance(4);
            
            // Read the message type identifier
            reader.TryReadBigEndian(out int messageTypeId);

            if (!_typeRegistry.TryGetParser(messageTypeId, out var parser))
                throw new ProtocolException($"No message parser registered for type id: {messageTypeId}");

            if (!_typeRegistry.TryGetMessageType(messageTypeId, out var messageType))
                throw new ProtocolException($"No message type registered for type id: {messageTypeId}");

            // Use the remaining buffer (actual protobuf message data)
            var messageBuffer = buffer.Slice(8);
            var message = parser.ParseFrom(messageBuffer);
            
            return CreatePackageInfo(message, messageType, messageTypeId);
        }

        /// <summary>
        /// Creates a package info object from the decoded message
        /// </summary>
        /// <param name="message">The decoded message</param>
        /// <param name="messageType">The type of the message</param>
        /// <param name="typeId">The type identifier</param>
        protected abstract TPackageInfo CreatePackageInfo(IMessage message, Type messageType, int typeId);
    }

    /// <summary>
    /// A concrete implementation of ProtobufPackageDecoder for IMessage.
    /// </summary>
    public class ProtobufPackageDecoder : ProtobufPackageDecoder<IMessage>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProtobufPackageDecoder"/> class.
        /// </summary>
        /// <param name="typeRegistry">The protobuf type registry to use for decoding</param>
        /// <exception cref="ArgumentNullException">Thrown when typeRegistry is null</exception>
        public ProtobufPackageDecoder(ProtobufTypeRegistry typeRegistry)
            : base(typeRegistry)
        {
        }

        /// <inheritdoc/>
        protected override IMessage CreatePackageInfo(IMessage message, Type messageType, int typeId)
        {
            return message;
        }
    }
}