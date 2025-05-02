using System;
using Google.Protobuf;

namespace SuperSocket.ProtoBuf
{
    /// <summary>
    /// Container class for protocol buffer messages and their metadata.
    /// </summary>
    /// <remarks>
    /// This class is used both for encoding messages for transmission and for 
    /// representing decoded messages received from the network.
    /// </remarks>
    public class ProtobufPackageInfo
    {
        /// <summary>
        /// Gets or sets the parsed protobuf message
        /// </summary>
        /// <remarks>
        /// This represents the actual protocol buffer message instance that 
        /// has been deserialized from the binary data or will be serialized.
        /// </remarks>
        public IMessage Message { get; set; }
        
        /// <summary>
        /// Gets or sets the type of the message
        /// </summary>
        /// <remarks>
        /// Used for type identification and handling of different message types.
        /// </remarks>
        public Type MessageType { get; set; }

        /// <summary>
        /// Gets or sets the type identifier of the message
        /// </summary>
        /// <remarks>
        /// A unique integer identifier that is used to map between message types and their identifiers.
        /// This ID is transmitted as part of the message header to allow the receiver to identify 
        /// how to deserialize the message.
        /// </remarks>
        public int TypeId { get; set; }
    }
}