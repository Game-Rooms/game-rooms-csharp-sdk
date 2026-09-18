using System.Text.Json;
using GameRooms.Sdk.Realtime;
using GameRooms.Sdk.Tests.TestDoubles;

namespace GameRooms.Sdk.Tests;

public sealed class GameRoomsRealtimeClientTests
{
    [Fact]
    public async Task SendCommandAsync_MatchesResponseBySequence()
    {
        var transport = new FakeSocketTransport();
        await using var client = new GameRoomsRealtimeClient(() => transport);
        await client.ConnectAsync(new Uri("wss://example.com/rooms/ABCD/host"));

        var sendTask = client.SendCommandAsync<string>("object:get", new { key = "prompt" });

        Assert.Single(transport.SentMessages);
        var sent = JsonSerializer.Deserialize<RealtimeCommandEnvelope>(transport.SentMessages[0], new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(sent);

        transport.EnqueueIncoming($"{{\"pc\":{sent!.Seq},\"opcode\":\"object:get\",\"result\":\"hello\"}}");
        var result = await sendTask;

        Assert.Equal("hello", result);
    }

    [Fact]
    public async Task ReaderLoop_RaisesEvent_ForUncorrelatedMessages()
    {
        var transport = new FakeSocketTransport();
        await using var client = new GameRoomsRealtimeClient(() => transport);
        await client.ConnectAsync(new Uri("wss://example.com/rooms/ABCD/player"));

        RealtimeEventMessage? received = null;
        var evtSignal = new TaskCompletionSource<RealtimeEventMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        client.EventReceived += (_, evt) =>
        {
            received = evt;
            evtSignal.TrySetResult(evt);
        };

        transport.EnqueueIncoming("{\"opcode\":\"player:joined\",\"result\":{\"id\":\"p1\"}}");
        await evtSignal.Task;

        Assert.NotNull(received);
        Assert.Equal("player:joined", received!.Opcode);
    }

    [Fact]
    public async Task LockObjectAsync_SendsExpectedPayload_AndParsesResponse()
    {
        var transport = new FakeSocketTransport();
        await using var client = new GameRoomsRealtimeClient(() => transport);
        await client.ConnectAsync(new Uri("wss://example.com/rooms/ABCD/host"));

        var lockTask = client.LockObjectAsync("prompt", locked: true);

        Assert.Single(transport.SentMessages);
        var sent = JsonSerializer.Deserialize<RealtimeCommandEnvelope>(transport.SentMessages[0], new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(sent);
        Assert.Equal("object:lock", sent!.Opcode);

        var sentParams = JsonSerializer.Serialize(sent.Params);
        Assert.Contains("\"key\":\"prompt\"", sentParams);
        Assert.Contains("\"locked\":true", sentParams);

        transport.EnqueueIncoming($"{{\"pc\":{sent.Seq},\"opcode\":\"object:lock\",\"result\":true}}");
        var result = await lockTask;

        Assert.True(result);
    }

    [Fact]
    public async Task SendCommandAsync_ThrowsRealtimeRequestException_WhenServerReturnsError()
    {
        var transport = new FakeSocketTransport();
        await using var client = new GameRoomsRealtimeClient(() => transport);
        await client.ConnectAsync(new Uri("wss://example.com/rooms/ABCD/host"));

        var sendTask = client.SendCommandAsync<string>("object:get", new { key = "prompt" });
        var sent = JsonSerializer.Deserialize<RealtimeCommandEnvelope>(transport.SentMessages[0], new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(sent);

        transport.EnqueueIncoming($"{{\"pc\":{sent!.Seq},\"opcode\":\"object:get\",\"re\":{{\"code\":\"ROOM_LOCKED\",\"message\":\"locked\"}}}}");

        var ex = await Assert.ThrowsAsync<RealtimeRequestException>(async () => await sendTask);
        Assert.Equal("ROOM_LOCKED", ex.Error?.Code);
    }
}
