using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using GameRooms.Sdk.Realtime;

namespace GameRooms.Sdk.Tests.TestDoubles;

internal sealed class FakeSocketTransport : IGameRoomsSocketTransport
{
    private readonly Channel<string?> _incoming = Channel.CreateUnbounded<string?>();

    public List<string> SentMessages { get; } = new();

    public Task ConnectAsync(Uri uri, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task SendTextAsync(string payload, CancellationToken cancellationToken)
    {
        SentMessages.Add(payload);
        return Task.CompletedTask;
    }

    public async Task<string?> ReceiveTextAsync(CancellationToken cancellationToken)
    {
        return await _incoming.Reader.ReadAsync(cancellationToken);
    }

    public Task CloseAsync(CancellationToken cancellationToken)
    {
        _incoming.Writer.TryWrite(null);
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _incoming.Writer.TryComplete();
        return ValueTask.CompletedTask;
    }

    public void EnqueueIncoming(string payload)
    {
        _incoming.Writer.TryWrite(payload);
    }
}
