# SuperSocket.ProtoBuf

## Overview

SuperSocket.ProtoBuf is an extension library for [SuperSocket](https://github.com/kerryjiang/SuperSocket) that provides seamless integration with Google Protocol Buffers (protobuf). This library enables efficient binary serialization and deserialization of network messages with built-in support for message type identification, making it ideal for high-performance network applications requiring cross-platform and cross-language compatibility.

## Features

- Efficient binary serialization and deserialization using Google Protocol Buffers
- Built-in message type identification and routing
- Support for strongly-typed message handling
- Easy integration with existing SuperSocket applications
- High-performance network communication

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
var host = SuperSocketHostBuilder.Create<ProtobufPackageInfo>()
    .ConfigurePackageHandler(async (session, package) =>
    {
        // Handle your protobuf messages based on their types
        if (package.MessageType == typeof(LoginRequest))
        {
            var request = package.Message as LoginRequest;
            // Process login request
        }
    })
    .UsePipelineFilter<ProtobufPipelineFilter>()
    .Build();

// Register message types
var filter = host.ServiceProvider.GetRequiredService<ProtobufPipelineFilter>();
filter.RegisterMessageType(1, LoginRequest.Parser, typeof(LoginRequest));
filter.RegisterMessageType(2, LoginResponse.Parser, typeof(LoginResponse));

await host.RunAsync();
```

### Step 3: Send protobuf messages from client

```csharp
var encoder = new ProtobufPackageEncoder();
encoder.RegisterMessageType(typeof(LoginRequest), 1);

var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
await client.ConnectAsync(serverEndPoint);

var loginRequest = new LoginRequest
{
    Username = "username",
    Password = "password"
};

var package = new ProtobufPackageInfo
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

### ProtobufPackageInfo

Container class for protobuf messages and their metadata:

```csharp
public class ProtobufPackageInfo
{
    // The parsed protobuf message
    public IMessage Message { get; set; }
    
    // The type of the message
    public Type MessageType { get; set; }

    // The type identifier of the message
    public int TypeId { get; set; }
}
```

### ProtobufPipelineFilter

Processes incoming network data into structured protobuf messages:

```csharp
// Register message types to handle incoming messages
filter.RegisterMessageType(messageTypeId, messageParser, messageType);
```

### ProtobufPackageEncoder

Encodes protobuf messages for network transmission:

```csharp
// Register message types for encoding
encoder.RegisterMessageType(messageType, messageTypeId);

// Encode a message
encoder.Encode(bufferWriter, packageInfo);
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