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
        client.EventReceived += (_, evt) => received = evt;

        transport.EnqueueIncoming("{\"opcode\":\"player:joined\",\"result\":{\"id\":\"p1\"}}");
        await Task.Delay(20);

        Assert.NotNull(received);
        Assert.Equal("player:joined", received!.Opcode);
    }
}
