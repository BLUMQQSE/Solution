using Godot;
using System;

public partial class NetworkManager : Node
{
    private static NetworkManager instance;
    public static NetworkManager Instance { get { return instance; } }

    public NetworkManager()
    {
        if (instance == null)
            instance = this;
        AddToGroup(SNC.Groups[(int)Globals.Groups.AutoLoad]);
    }

    public string PlayerName { get; protected set; }
    private ulong playerId;
    public ulong PlayerId
    {
        get
        {
            return playerId;
        }
        set 
        { 
            playerId = value;
            PlayerIdString = playerId.ToString();
        }
    }
    public string PlayerIdString { get; protected set; }
    public bool ConnectedToNetwork { get; protected set; }
    public bool IsServer { get; protected set; } = true;
    public int SocketConnections { get; set; } = 0;
    public int MaxConnections { get; protected set; } = 5;

}
