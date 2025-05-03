# SuperSocket.ProtoBuf

## Overview

SuperSocket.ProtoBuf is an extension library for [SuperSocket](https://github.com/kerryjiang/SuperSocket) that provides seamless integration with Google Protocol Buffers (protobuf). This library enables efficient binary serialization and deserialization of network messages with built-in support for message type identification, making it ideal for high-performance network applications requiring cross-platform and cross-language compatibility.

## Features

- Efficient binary serialization and deserialization using Google Protocol Buffers
- Built-in message type identification and routing
- Support for strongly-typed message handling with generic APIs
- Easy integration with existing SuperSocket applications
- High-performance network communication
- Multi-platform support including Android, iOS, macOS, and tvOS
- Compatible with .NET 6.0, 7.0, 8.0, and 9.0

## Installation

### NuGet Package

```bash
dotnet add package SuperSocket.ProtoBuf
```

### Project Reference

```xml
<PackageReference Include="SuperSocket.ProtoBuf" Version="[version]" />
```

## Quick Start

### Step 1: Define your protobuf messages

```protobuf
syntax = "proto3";

message LoginRequest {
    string username = 1;
    string password = 2;
}

message LoginResponse {
    bool success = 1;
    string message = 2;
}
```

### Step 2: Configure SuperSocket with protobuf support

```csharp
// Define your package info class to hold the message data
public class MyProtobufPackageInfo
{
    public IMessage Message { get; set; }
    public Type MessageType { get; set; }
    public int TypeId { get; set; }
}

// Create a custom decoder that implements ProtobufPackageDecoder<MyProtobufPackageInfo>
public class MyProtobufPackageDecoder : ProtobufPackageDecoder<MyProtobufPackageInfo>
{
    public MyProtobufPackageDecoder(ProtobufTypeRegistry typeRegistry) 
        : base(typeRegistry) { }

    protected override MyProtobufPackageInfo CreatePackageInfo(IMessage message, Type messageType, int typeId)
    {
        return new MyProtobufPackageInfo
        {
            Message = message,
            MessageType = messageType,
            TypeId = typeId
        };
    }
}

// Create a registry for your message types
var typeRegistry = new ProtobufTypeRegistry();
typeRegistry.Register(1, LoginRequest.Parser, typeof(LoginRequest));
typeRegistry.Register(2, LoginResponse.Parser, typeof(LoginResponse));

// Create your decoder with the type registry
var decoder = new MyProtobufPackageDecoder(typeRegistry);

// Configure the SuperSocket host
var host = SuperSocketHostBuilder.Create<MyProtobufPackageInfo>()
    .ConfigurePackageHandler(async (session, package) =>
    {
        // Handle your protobuf messages based on their types
        if (package.MessageType == typeof(LoginRequest))
        {
            var request = package.Message as LoginRequest;
            // Process login request
        }
    })
    .UsePipelineFilter(serviceProvider => 
    {
        return new ProtobufPipelineFilter<MyProtobufPackageInfo>(decoder);
    })
    .Build();

await host.RunAsync();
```

### Step 3: Send protobuf messages from client

```csharp
// Create a custom encoder that extends ProtobufPackageEncoder<MyProtobufPackageInfo>
public class MyProtobufPackageEncoder : ProtobufPackageEncoder<MyProtobufPackageInfo>
{
    public MyProtobufPackageEncoder(ProtobufTypeRegistry typeRegistry) 
        : base(typeRegistry) { }

    protected override IMessage GetProtoBufMessage(MyProtobufPackageInfo package)
    {
        return package.Message;
    }

    protected override int GetProtoBufMessageTypeId(MyProtobufPackageInfo package)
    {
        return package.TypeId;
    }
}

var typeRegistry = new ProtobufTypeRegistry();
typeRegistry.Register(typeof(LoginRequest), 1);
typeRegistry.Register(typeof(LoginResponse), 2);

var encoder = new MyProtobufPackageEncoder(typeRegistry);

var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
await client.ConnectAsync(serverEndPoint);

var loginRequest = new LoginRequest
{
    Username = "username",
    Password = "password"
};

var package = new MyProtobufPackageInfo
{
    Message = loginRequest,
    TypeId = 1
};

// Encode and send the message
var buffer = new ArrayBufferWriter<byte>();
encoder.Encode(buffer, package);
await client.SendAsync(buffer.WrittenMemory, SocketFlags.None);
```

## API Reference

### ProtobufTypeRegistry

Registry for mapping between message types and their identifiers:

```csharp
public class ProtobufTypeRegistry
{
    // Register a message type with parser for decoding
    public void Register(int typeId, MessageParser parser, Type messageType);
    
    // Register a message type for encoding
    public void Register(Type messageType, int typeId);
    
    // Try to get the parser for a given type ID
    public bool TryGetParser(int typeId, out MessageParser parser);
    
    // Try to get the message type for a given type ID
    public bool TryGetMessageType(int typeId, out Type messageType);
    
    // Try to get the type ID for a given message type
    public bool TryGetTypeId(Type messageType, out int typeId);
}
```

### ProtobufPipelineFilter<TPackageInfo>

Processes incoming network data into structured protobuf messages:

```csharp
public class ProtobufPipelineFilter<TPackageInfo> : FixedHeaderPipelineFilter<TPackageInfo>
    where TPackageInfo : class
{
    // Initialize the filter with a decoder
    public ProtobufPipelineFilter(IPackageDecoder<TPackageInfo> decoder);
}
```

### ProtobufPackageDecoder<TPackageInfo>

Decodes binary data into structured package information:

```csharp
public abstract class ProtobufPackageDecoder<TPackageInfo> : IPackageDecoder<TPackageInfo>
{
    // Initialize with a type registry
    public ProtobufPackageDecoder(ProtobufTypeRegistry typeRegistry);
    
    // Decode a binary buffer into a package
    public TPackageInfo Decode(ref ReadOnlySequence<byte> buffer, object context);
    
    // Abstract method to create package info from decoded message
    protected abstract TPackageInfo CreatePackageInfo(IMessage message, Type messageType, int typeId);
}
```

### ProtobufPackageEncoder<TPackageInfo>

Encodes protobuf messages for network transmission:

```csharp
public abstract class ProtobufPackageEncoder<TPackageInfo> : IPackageEncoder<TPackageInfo>
{
    // Initialize with a type registry
    public ProtobufPackageEncoder(ProtobufTypeRegistry typeRegistry);
    
    // Encode a package into binary format
    public int Encode(IBufferWriter<byte> writer, TPackageInfo package);
    
    // Abstract method to extract the protobuf message from a package
    protected abstract IMessage GetProtoBufMessage(TPackageInfo package);
    
    // Virtual method to get message type ID from package
    protected virtual int GetProtoBufMessageTypeId(TPackageInfo package);
}
```

## Protocol Format

Messages are sent with an 8-byte header followed by the protobuf message payload:
- First 4 bytes: Message size in big-endian format
- Next 4 bytes: Message type ID in big-endian format
- Remaining bytes: The serialized protobuf message

## License

This project is licensed under the terms of the [LICENSE](../../LICENSE) file included in the repository.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.