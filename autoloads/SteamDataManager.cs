using Godot;
using Steamworks;
using System;

public partial class SteamDataManager : NetworkDataManager
{
    public override void _Ready()
    {
        base._Ready();
        SteamManager.Instance.PlayerConnected_Server += OnPlayerConnected_Server;
        LevelManager.Instance.PlayerInstantiated += OnPlayerJoined;
    }


    private void OnPlayerJoined(Node node)
    {
        CallDeferred(nameof(OnPlayerJoinedDeferred), node);
    }
    private void OnPlayerJoinedDeferred(Node node)
    {
        ulong ownerId = Convert.ToUInt64(node.GetMeta(SNC.Meta[(int)Globals.Meta.OwnerId]).ToString());
        AddOwnerIdToPlayer(ownerId, node);
    }

    public override void SendToClientId(ulong client, JsonValue data, bool reliable = true)
    {
        if (!SteamManager.Instance.IsServer) { return; }

        SteamManager.Instance.SocketManager.SendMessage((SteamId)client, data.ToString(), reliable);
    }

    public override void SendToClients(JsonValue data, bool reliable = true)
    {
        if (!SteamManager.Instance.IsServer) { return; }
        // if only server is connected, return
        if (SteamManager.Instance.SocketConnections < 2) { return; }

        SteamManager.Instance.SocketManager.SendBroadcast(data.ToString(), reliable);
    }

    public override void SendToServer(JsonValue data, bool reliable = true)
    {
        if (SteamManager.Instance.IsServer) { return; }

        SteamManager.Instance.ConnectionManager.SendMessage(data.ToString(), reliable);
    }

    private void OnPlayerConnected_Server(Friend friend)
    {
        if (SteamManager.Instance.IsServer)
        {
            LevelManager.Instance.InstantiatePlayer((ulong)friend.Id, friend.Name);
            JsonValue fullData = FullServerData();
            SendToClientId(friend.Id, fullData);
        }
    }

}
