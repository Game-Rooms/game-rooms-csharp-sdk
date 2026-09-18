# Game Rooms C# SDK

C# SDK for the Game Rooms protocol, targeting **netstandard2.1** for Unity compatibility.

## Features

- HTTP client for:
  - Creating rooms
  - Looking up rooms by 4-letter code
  - Fetching app configuration
- Realtime WebSocket client with request/response sequence tracking
- Typed helpers for object operations:
  - `object:create`
  - `object:update`
  - `object:get`
  - `object:lock`
  - `object:relay`

## Install

Add the project/package to your Unity or .NET solution.

Target framework minimum:

- `netstandard2.1`

## Quick Start

```csharp
using GameRooms.Sdk;
using GameRooms.Sdk.Models;

var options = new GameRoomsClientOptions
{
    HttpBaseUri = new Uri("https://your-game-rooms-host")
};

using var http = new HttpClient();
var sdk = new GameRoomsSdkClient(http, options);

var room = await sdk.Http.CreateRoomAsync(new CreateRoomRequest
{
    AppId = "your-app-id"
});

var roomInfo = await sdk.Http.GetRoomByCodeAsync(room.Code!);

await sdk.Realtime.ConnectAsync(new Uri(roomInfo.HostUrl!));

var prompt = await sdk.Realtime.GetObjectAsync<string>("prompt");
await sdk.Realtime.UpdateObjectAsync("prompt", "New prompt");
```

## Error Handling

HTTP failures throw `GameRoomsApiException` with:

- `StatusCode`
- `ErrorCode` (`RoomNotFound`, `RoomLocked`, `RoomFull`, etc.)

Realtime request failures throw `RealtimeRequestException`.
