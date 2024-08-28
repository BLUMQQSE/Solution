using Godot;
using System;
using Steamworks;
using Steamworks.Data;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class SteamManager : NetworkManager
{
    private static SteamManager instance;
    public static SteamManager Instance { get { return instance; } }
    private uint gameAppID = 480;
    public Lobby ActiveLobby { get; private set; }
    public bool InLobby { get; private set; } = false;
    /// <summary>List of available lobbies for the player to join.</summary>
    private List<Lobby> AvailableLobbies { get; set; } = new List<Lobby>();
    /// <summary>Manager for server callbacks from steam and messaging to clients.</summary>
    public SteamSocketManager SocketManager { get; private set; }
    /// <summary>Manager for client callbacks from steam and messaging to the server.</summary>
    public SteamConnectionManager ConnectionManager { get; private set; }

    #region SteamConnections
    public Dictionary<Friend, uint> FriendToConnection { get; private set; } = new Dictionary<Friend, uint>();
    
    public Dictionary<SteamId, uint> SteamIdToConnection { get; private set; } = new Dictionary<SteamId, uint>();
    private Friend friendToAdd = new Friend(0);
    public Friend friendToRemove = new Friend(0);

    public event Action<Friend> PlayerConnected_Server;

    private void AddFriendConnectionPair(uint connectionID)
    {
        if (friendToAdd.Id != 0)
        {
            FriendToConnection.Add(friendToAdd, connectionID);
            SteamIdToConnection.Add(friendToAdd.Id, connectionID);
            PlayerConnected_Server?.Invoke(friendToAdd);

            GD.Print(friendToAdd.Name + " has connected");
            friendToAdd.Id = 0;
        }
        else
            GD.Print(PlayerName + " has connected");
    }

    public void RemoveFriendConnectionPair(Friend friend)
    {
        friendToRemove = friend;
        FriendToConnection.Remove(friend);
        SteamIdToConnection.Remove(friend.Id);
    }

    #endregion

    /// <summary> Should only be called by SteamSceneManager when creating a new save while not connected to steam. </summary>
    public void SetSteamInfo(ulong id, string name)
    {
        PlayerId = id;
        PlayerName = name;
    }

    public event Action<List<Lobby>> OnLobbyRefreshCompleted;
    public event Action<Friend> OnPlayerJoinLobby;
    public event Action<Friend> OnPlayerLeftLobby;
    public event Action OnLobbyCreated;


    public SteamManager() 
    { 
        instance = this;
        SteamClient.Shutdown();
        try
        {
            SteamClient.Init(gameAppID, true);


            if (!SteamClient.IsValid)
            {
                GD.Print("Something went wrong! Steam Client is not valid");
                throw new Exception();
            }

            PlayerName = SteamClient.Name;
            PlayerId = SteamClient.SteamId;
            ConnectedToNetwork = true;
        }
        catch (System.Exception e)
        {
            ConnectedToNetwork = false;
            GD.Print("Error Connecting to steam: " + e.Message);
            throw;
        }

        if (ConnectedToNetwork)
        {
            JsonValue obj = new JsonValue();
            obj["SteamId"].Set(PlayerId.ToString());
            obj["SteamName"].Set(PlayerName);
            FileManager.SaveToFile(obj, "SteamInfo.json");
        }
        else
        {
            if (FileManager.FileExists("SteamInfo.json"))
            {
                JsonValue data = FileManager.LoadFromFile("SteamInfo.json");
                PlayerId = Convert.ToUInt64(data["SteamId"].AsString());
                PlayerName = data["SteamName"].AsString();
            }
            else
            {
                // TODO: Replace with a graphical overlay telling the player the game must be ran once while connected to internet
                GD.Print("User has not loaded the game with internet access. Can not play :(");
                GetTree().Quit();
            }
        }

    }

    public override void _Ready()
    {
        base._Ready();
        AddToGroup("AutoLoad");
        
        SteamMatchmaking.OnLobbyGameCreated += OnLobbyGameCreatedCallback;
        SteamMatchmaking.OnLobbyCreated += OnLobbyCreatedCallback;
        SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoinedCallback;
        SteamMatchmaking.OnLobbyMemberDisconnected += OnLobbyMemberDisconnectedCallback;
        SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeaveCallback;
        SteamMatchmaking.OnLobbyEntered += OnLobbyEnteredCallback;

    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        SteamClient.RunCallbacks();
        try
        {
            if (SocketManager != null)
                SocketManager.Receive();
            if (ConnectionManager != null && ConnectionManager.Connected)
                ConnectionManager.Receive();
        }
        catch (System.Exception e)
        {
            GD.Print("Error recieving data " + e.Message);
            throw;
        }
    }


    #region SteamMatchmakingCallbacks

    private void OnLobbyGameCreatedCallback(Lobby lobby, uint arg2, ushort arg3, SteamId id)
    {
    }
    private void OnLobbyCreatedCallback(Result result, Lobby lobby)
    {
        CreateSteamSocketServer();
    }
    // called on server
    private void OnLobbyMemberJoinedCallback(Lobby lobby, Friend friend)
    {
        if (IsServer)
            friendToAdd = friend;
        GD.Print(friend.Name + " has joined " + lobby.GetData("LobbyName"));
        OnPlayerJoinLobby.Invoke(friend);
    }
    // called on server
    private void OnLobbyMemberDisconnectedCallback(Lobby lobby, Friend friend)
    {
        RemoveFriendConnectionPair(friend);
        OnPlayerLeftLobby.Invoke(friend);
    }
    // called on server
    private void OnLobbyMemberLeaveCallback(Lobby lobby, Friend friend)
    {
        OnPlayerLeftLobby.Invoke(friend);
        RemoveFriendConnectionPair(friend);
    }

    // called on client
    private void OnLobbyEnteredCallback(Lobby lobby)
    {
        if (lobby.MemberCount > 1)
        {
            IsServer = false;
            GD.Print("Setting active lobby to: " + lobby.GetData("LobbyName"));
            ActiveLobby = lobby;
            InLobby = true;
        }
        else
        {
            IsServer = true;
        }
        JoinSteamSocketServer();
    }

    #endregion

    public async Task<bool> CreateLobby(string lobbyName)
    {
        try
        {
            Lobby? createLobbyOutput = await SteamMatchmaking.CreateLobbyAsync(MaxConnections);

            if (!createLobbyOutput.HasValue)
            {
                GD.Print("lobby created but didnt instance correctly");
                throw new Exception();
            }

            ActiveLobby = createLobbyOutput.Value;
            ActiveLobby.SetPublic();
            ActiveLobby.SetJoinable(true);
            ActiveLobby.SetData("BMOwnerName", PlayerName);
            ActiveLobby.SetData("BMLobbyName", lobbyName);
            InLobby = true;

            GD.Print("Lobby Created");
            OnLobbyCreated?.Invoke();
            return true;

        }
        catch (System.Exception e)
        {
            GD.Print("Failed to create lobby " + e.Message);
            return false;
        }
    }
    /// <summary>Retrieves active lobbies being hosted. Does not return Lobbies with empty names and set to
    /// private.</summary>
    public async Task<bool> GetMultiplayerLobbies()
    {
        AvailableLobbies.Clear();
        try
        {
            Lobby[] lobbies = await SteamMatchmaking.LobbyList.RequestAsync();

            foreach (Lobby lobby in lobbies)
            {
                string lobbyName = lobby.GetData("BMLobbyName");
                if (lobbyName.Length == 0)
                    continue;

                if (!AvailableLobbies.Contains(lobby))
                    AvailableLobbies.Add(lobby);
            }
            OnLobbyRefreshCompleted.Invoke(AvailableLobbies);
            return true;
        }
        catch (System.Exception e)
        {
            GD.Print("Error fetching lobbies " + e.Message);
            return false;
        }
    }

    public override void _Notification(int what)
    {
        base._Notification(what);
        if (what == MainLoop.NotificationCrash || what == MainLoop.NotificationPredelete)
        {
            ActiveLobby.Leave();
            InLobby = false;
            SteamClient.Shutdown();
            GetTree().Quit();
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        ActiveLobby.Leave();
        InLobby = false;

        SteamClient.Shutdown();
        GetTree().Quit();
    }

    /// <summary>Creates the servers SocketManager and ConnectionManager objects.</summary>
    private void CreateSteamSocketServer()
    {
        if (SocketManager != null)
            SocketManager.AddFriendConnectionPair -= AddFriendConnectionPair;

        SocketManager = SteamNetworkingSockets.CreateRelaySocket<SteamSocketManager>(0);
        ConnectionManager = SteamNetworkingSockets.ConnectRelay<SteamConnectionManager>(PlayerId);

        SocketManager.AddFriendConnectionPair += AddFriendConnectionPair;
        GD.Print("Created Socket Server");

    }

    /// <summary>Sets the clients ConnectionManager to the ActiveLobby's lobby owner. </summary>
    private void JoinSteamSocketServer()
    {
        if (IsServer) return;

        GD.Print("We are not the server... setting up connect server to " + ActiveLobby.Owner.Name);

        ConnectionManager = SteamNetworkingSockets.ConnectRelay<SteamConnectionManager>(
            ActiveLobby.Owner.Id, 0);

    }

}


// is run on client
public class SteamConnectionManager : ConnectionManager
{
    public event Action PlayerConnected_Client;
    public event Action<JsonValue> ConnectionRecievedData;

    public override void OnConnected(ConnectionInfo info)
    {
        base.OnConnected(info);
        GD.Print("On Connection");
        PlayerConnected_Client?.Invoke();
    }

    public override void OnConnecting(ConnectionInfo info)
    {
        base.OnConnecting(info);
    }

    public override void OnDisconnected(ConnectionInfo info)
    {
        base.OnDisconnected(info);
        GD.Print("On Disconnection");
    }

    public override void OnMessage(IntPtr data, int size, long messageNum, long recvTime, int channel)
    {
        base.OnMessage(data, size, messageNum, recvTime, channel);
        //GD.Print("Client Recieved message...");
        JsonValue info = DataConverter.ParseData(data, size);

        ConnectionRecievedData?.Invoke(info);
        EventBus.Instance.Invoke(EventId.OnConnectionRecievedData, info, this);
    }

    /// <summary>
    /// Override to send message to Connection
    /// </summary>
    public void SendMessage(string message, bool realiable = true)
    {
        SendType s = SendType.Reliable;
        if (!realiable)
            s = SendType.Unreliable;
        Connection.SendMessage(DataConverter.SealData(message), s);
    }


}

// is run on server
public class SteamSocketManager : SocketManager
{
    public event Action<uint> AddFriendConnectionPair;
    public event Action<JsonValue> SocketRecievedData; 
    public override void OnConnected(Connection connection, ConnectionInfo info)
    {
        base.OnConnected(connection, info);
        AddFriendConnectionPair?.Invoke(connection.Id);
        SteamManager.Instance.SocketConnections += 1;
    }

    public override void OnConnecting(Connection connection, ConnectionInfo info)
    {
        base.OnConnecting(connection, info);
    }

    public override void OnDisconnected(Connection connection, ConnectionInfo info)
    {
        base.OnDisconnected(connection, info);
        //GD.Print(SteamManager.Instance.friendToRemove + " has disconnected");

        SteamManager.Instance.SocketConnections -= 1;
        /*
         * Need to find the player and save it
         * Need to remove the player from the scene
         * Need to remove from ToConnection dictionaries
         */
        foreach (var item in SteamManager.Instance.SteamIdToConnection)
        {
            if (item.Value == connection)
            {
                if (NetworkDataManager.Instance.GetOwnerIdToPlayer(item.Key) == null)
                {
                    return;
                }
                LevelManager.Instance.TerminatePlayer(item.Key);
            }
        }

        foreach (var item in SteamManager.Instance.FriendToConnection)
        {
            if (item.Value == connection)
            {
                SteamManager.Instance.RemoveFriendConnectionPair(item.Key);
            }
        }
    }

    public override void OnMessage(Connection connection, NetIdentity identity,
        IntPtr data, int size, long messageNum, long recvTime, int channel)
    {
        base.OnMessage(connection, identity, data, size, messageNum, recvTime, channel);
        JsonValue info = DataConverter.ParseData(data, size);
        SocketRecievedData?.Invoke(info);
        EventBus.Instance.Invoke(EventId.OnSocketRecievedData, info, this);
    }
    public void SendBroadcast(string data, bool realiable = true)
    {
        SendType s = SendType.Reliable;
        if (!realiable)
            s = SendType.Unreliable;
        foreach (var item in Connected.Skip(1).ToArray())
        {
            item.SendMessage(DataConverter.SealData(data), s);
        }
    }

    /// <summary>
    /// Need to improve performance of this function
    /// </summary>
    /// <param name="client"></param>
    /// <param name="data"></param>
    public void SendMessage(SteamId client, string data, bool realiable = true)
    {
        uint connectionId = SteamManager.Instance.SteamIdToConnection[client];
        if (connectionId == 0) return;

        SendType s = SendType.Reliable;
        if (!realiable)
            s = SendType.Unreliable;
        foreach (var item in Connected.Skip(1).ToArray())
        {
            if (connectionId == item.Id)
            {
                item.SendMessage(DataConverter.SealData(data), s);
            }
        }
    }


}