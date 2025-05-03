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

// Create a registry for your message types
var typeRegistry = new ProtobufTypeRegistry();
// Register for both encoding and decoding
typeRegistry.RegisterMessageType(1, typeof(LoginRequest), LoginRequest.Parser);
typeRegistry.RegisterMessageType(2, typeof(LoginResponse), LoginResponse.Parser);

// Create your decoder with the type registry
var decoder = new MyProtobufPackageDecoder(typeRegistry);
// Create your encoder with the same type registry
var encoder = new MyProtobufPackageEncoder(typeRegistry);

// Configure the SuperSocket host
var host = SuperSocketHostBuilder.Create<MyProtobufPackageInfo>()
    .ConfigurePackageHandler(async (session, package) =>
    {
        // Handle your protobuf messages based on their types
        if (package.MessageType == typeof(LoginRequest))
        {
            var request = package.Message as LoginRequest;
            // Process login request
            
            // Create a response
            var response = new LoginResponse
            {
                Success = true,
                Message = $"Welcome {request.Username}!"
            };
            
            // Send the response using the encoder
            await session.SendAsync(new MyProtobufPackageInfo 
            { 
                Message = response, 
                TypeId = 2 
            });
        }
    })
    .UsePipelineFilter(serviceProvider => 
    {
        return new ProtobufPipelineFilter<MyProtobufPackageInfo>(decoder);
    })
    .UsePackageEncoder(encoder)
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
typeRegistry.RegisterMessageType(1, typeof(LoginRequest), LoginRequest.Parser);
typeRegistry.RegisterMessageType(2, typeof(LoginResponse), LoginResponse.Parser);

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
    // Register a message type with parser for decoding and encoding
    public void RegisterMessageType(int typeId, Type messageType, MessageParser parser);
    
    // Try to get the parser for a given type ID
    public bool TryGetParser(int typeId, out MessageParser parser);
    
    // Try to get the message type for a given type ID
    public bool TryGetMessageType(int typeId, out Type messageType);
    
    // Try to get the type ID for a given message type
    public bool TryGetTypeId(Type messageType, out int typeId);
    
    // Get the type ID for a message type
    public int GetTypeId(Type messageType);
    
    // Get the message type for a type ID
    public Type GetMessageType(int typeId);
    
    // Get the message parser for a type ID
    public MessageParser GetParser(int typeId);
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

## Using IMessage Directly

SuperSocket.ProtoBuf provides concrete implementations of `ProtobufPipelineFilter`, `ProtobufPackageDecoder`, and `ProtobufPackageEncoder` that work directly with `IMessage` as the package type.

### Server Example with Direct IMessage Handling

```csharp
// Create a registry for your message types
var typeRegistry = new ProtobufTypeRegistry();
// Register for both encoding and decoding
typeRegistry.RegisterMessageType(1, typeof(LoginRequest), LoginRequest.Parser);
typeRegistry.RegisterMessageType(2, typeof(LoginResponse), LoginResponse.Parser);

// Use the concrete ProtobufPackageDecoder and Encoder that work directly with IMessage
var decoder = new ProtobufPackageDecoder(typeRegistry);
var encoder = new ProtobufPackageEncoder(typeRegistry);

// Configure the SuperSocket host using IMessage as the package type
var host = SuperSocketHostBuilder.Create<IMessage>()
    .ConfigurePackageHandler(async (session, message) =>
    {
        // Handle messages based on their type
        if (message is LoginRequest loginRequest)
        {
            // Process login request
            Console.WriteLine($"Login request received for: {loginRequest.Username}");
            
            // Create a response
            var response = new LoginResponse
            {
                Success = true,
                Message = $"Welcome {loginRequest.Username}!"
            };
            
            // Send the response directly
            await session.SendAsync(response);
        }
    })
    .UsePipelineFilter(serviceProvider => 
    {
        // Use the concrete ProtobufPipelineFilter for IMessage
        return new ProtobufPipelineFilter(decoder);
    })
    .UsePackageEncoder(encoder)
    .Build();

await host.RunAsync();
```

### Client Example with Direct IMessage Handling

```csharp
// Create a registry for your message types
var typeRegistry = new ProtobufTypeRegistry();
typeRegistry.RegisterMessageType(1, typeof(LoginRequest), LoginRequest.Parser);
typeRegistry.RegisterMessageType(2, typeof(LoginResponse), LoginResponse.Parser);

// Use the concrete ProtobufPackageEncoder that works directly with IMessage
var encoder = new ProtobufPackageEncoder(typeRegistry);

// Connect to the server
var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
await client.ConnectAsync(serverEndPoint);

// Create a login request message
var loginRequest = new LoginRequest
{
    Username = "username",
    Password = "password"
};

// Encode and send the message directly
var buffer = new ArrayBufferWriter<byte>();
encoder.Encode(buffer, loginRequest);
await client.SendAsync(buffer.WrittenMemory, SocketFlags.None);

// For receiving responses, you would typically set up a client using the ProtobufPipelineFilter
// with the concrete ProtobufPackageDecoder and handle messages accordingly
```

## License

This project is licensed under the terms of the [LICENSE](../../LICENSE) file included in the repository.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.