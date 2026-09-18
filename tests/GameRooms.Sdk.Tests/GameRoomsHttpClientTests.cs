using System.Net;
using System.Net.Http;
using System.Text;
using GameRooms.Sdk;
using GameRooms.Sdk.Http;
using GameRooms.Sdk.Models;
using GameRooms.Sdk.Tests.TestDoubles;

namespace GameRooms.Sdk.Tests;

public sealed class GameRoomsHttpClientTests
{
    [Fact]
    public async Task CreateRoomAsync_SendsRequest_AndParsesResponse()
    {
        HttpRequestMessage? capturedRequest = null;

        var handler = new FakeHttpMessageHandler(req =>
        {
            capturedRequest = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"roomId\":\"room-1\",\"code\":\"ABCD\",\"hostUrl\":\"wss://host\"}", Encoding.UTF8, "application/json")
            };
        });

        using var httpClient = new HttpClient(handler);
        var client = new GameRoomsHttpClient(httpClient, new GameRoomsClientOptions { HttpBaseUri = new Uri("https://api.example.com") });

        var result = await client.CreateRoomAsync(new CreateRoomRequest { AppId = "drawful" });

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest!.Method);
        Assert.Equal("https://api.example.com/rooms", capturedRequest.RequestUri!.ToString());
        Assert.Equal("room-1", result.RoomId);
        Assert.Equal("ABCD", result.Code);
    }

    [Fact]
    public async Task GetRoomByCodeAsync_MapsNotFoundError()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("{\"error\":\"ROOM_NOT_FOUND\",\"message\":\"missing\"}", Encoding.UTF8, "application/json")
            });

        using var httpClient = new HttpClient(handler);
        var client = new GameRoomsHttpClient(httpClient, new GameRoomsClientOptions { HttpBaseUri = new Uri("https://api.example.com") });

        var ex = await Assert.ThrowsAsync<GameRoomsApiException>(() => client.GetRoomByCodeAsync("XXXX"));

        Assert.Equal(GameRoomsErrorCode.RoomNotFound, ex.ErrorCode);
    }

    [Fact]
    public async Task GetRoomByCodeAsync_MapsStatusOnlyNotFoundError()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("oops", Encoding.UTF8, "text/plain")
            });

        using var httpClient = new HttpClient(handler);
        var client = new GameRoomsHttpClient(httpClient, new GameRoomsClientOptions { HttpBaseUri = new Uri("https://api.example.com") });

        var ex = await Assert.ThrowsAsync<GameRoomsApiException>(() => client.GetRoomByCodeAsync("XXXX"));

        Assert.Equal(GameRoomsErrorCode.RoomNotFound, ex.ErrorCode);
    }
}
