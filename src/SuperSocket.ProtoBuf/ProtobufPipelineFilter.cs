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
    public class ProtobufPipelineFilter<TPackageInfo> : FixedHeaderPipelineFilter<TPackageInfo>
        where TPackageInfo : class
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="ProtobufPipelineFilter{TPackageInfo}"/> class.
        /// </summary>
        /// <param name="decoder">The package decoder to use for decoding messages</param>
        /// <exception cref="ArgumentNullException">Thrown when typeRegistry is null</exception>
        public ProtobufPipelineFilter(IPackageDecoder<TPackageInfo> decoder)
            : base(8) // 4 bytes for message length + 4 bytes for message type identifier
        {
            Decoder = decoder ?? throw new ArgumentNullException(nameof(decoder));
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
    }
}