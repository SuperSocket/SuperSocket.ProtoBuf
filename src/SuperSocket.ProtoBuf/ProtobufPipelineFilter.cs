using System;
using System.Buffers;
using System.Collections.Generic;
using Google.Protobuf;
using SuperSocket.ProtoBase;

namespace SuperSocket.ProtoBuf
{
    /// <summary>
    /// A pipeline filter that processes incoming data streams and extracts protocol buffer messages.
    /// </summary>
    /// <remarks>
    /// This filter expects messages with an 8-byte header:
    /// - 4 bytes for message size (big-endian)
    /// - 4 bytes for message type ID (big-endian)
    /// Followed by the protocol buffer encoded message body.
    /// </remarks>
    public class ProtobufPipelineFilter : FixedHeaderPipelineFilter<ProtobufPackageInfo>
    {
        private readonly Dictionary<int, MessageParser> _parsers = new Dictionary<int, MessageParser>();
        private readonly Dictionary<int, Type> _messageTypes = new Dictionary<int, Type>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ProtobufPipelineFilter"/> class.
        /// </summary>
        /// <remarks>
        /// Sets the header size to 8 bytes (4 bytes for message length + 4 bytes for message type identifier).
        /// </remarks>
        public ProtobufPipelineFilter()
            : base(8) // 4 bytes for message length + 4 bytes for message type identifier
        {
        }

        /// <summary>
        /// Register a message type with its type identifier
        /// </summary>
        /// <param name="typeId">The message type identifier</param>
        /// <param name="parser">The protobuf message parser</param>
        /// <param name="messageType">The message type</param>
        /// <exception cref="ArgumentNullException">Thrown when parser or messageType is null</exception>
        public void RegisterMessageType(int typeId, MessageParser parser, Type messageType)
        {
            if (parser == null)
                throw new ArgumentNullException(nameof(parser));
                
            if (messageType == null)
                throw new ArgumentNullException(nameof(messageType));

            _parsers[typeId] = parser;
            _messageTypes[typeId] = messageType;
        }

        /// <summary>
        /// Extracts the message body length from the header.
        /// </summary>
        /// <param name="buffer">The buffer containing the header data</param>
        /// <returns>The length of the message body</returns>
        protected override int GetBodyLengthFromHeader(ref ReadOnlySequence<byte> buffer)
        {
            var reader = new SequenceReader<byte>(buffer);
            reader.TryReadBigEndian(out int bodyLength);
            return bodyLength;
        }

        /// <summary>
        /// Decodes the protocol buffer message from the data buffer.
        /// </summary>
        /// <param name="buffer">The buffer containing the message data</param>
        /// <returns>A <see cref="ProtobufPackageInfo"/> containing the decoded message</returns>
        /// <exception cref="ProtocolException">Thrown when no parser or message type is registered for the type ID</exception>
        protected override ProtobufPackageInfo DecodePackage(ref ReadOnlySequence<byte> buffer)
        {
            var reader = new SequenceReader<byte>(buffer);
            
            // Skip the length field that we already processed
            reader.Advance(4);
            
            // Read the message type identifier
            reader.TryReadBigEndian(out int messageTypeId);

            // Get the appropriate parser and message type
            if (!_parsers.TryGetValue(messageTypeId, out var parser))
                throw new ProtocolException($"No message parser registered for type id: {messageTypeId}");

            if (!_messageTypes.TryGetValue(messageTypeId, out var messageType))
                throw new ProtocolException($"No message type registered for type id: {messageTypeId}");

            // Use the remaining buffer (actual protobuf message data)
            var messageBuffer = buffer.Slice(8);
            var message = parser.ParseFrom(messageBuffer);
            
            return new ProtobufPackageInfo
            {
                Message = message,
                MessageType = messageType,
                TypeId = messageTypeId
            };
        }
    }
}