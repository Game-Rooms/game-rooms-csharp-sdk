using System;
using System.Threading;
using System.Threading.Tasks;

namespace GameRooms.Sdk.Realtime;

public interface IGameRoomsSocketTransport : IAsyncDisposable
{
    Task ConnectAsync(Uri uri, CancellationToken cancellationToken);
    Task SendTextAsync(string payload, CancellationToken cancellationToken);
    Task<string?> ReceiveTextAsync(CancellationToken cancellationToken);
    Task CloseAsync(CancellationToken cancellationToken);
}
