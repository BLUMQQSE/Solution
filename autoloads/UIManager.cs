using Godot;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

public partial class UIManager : Node
{
	private static UIManager instance;
	public static UIManager Instance {  get { return instance; } }
	public UIManager()
	{
		if (instance == null)
			instance = this;
		AddToGroup(SNC.Groups[(int)Globals.Groups.AutoLoad]);
	}
    public Dictionary<Player, PlayerUIContainer> PlayerUIContainers { get; set; } = new Dictionary<Player, PlayerUIContainer>();
    private Node LocalUI { get; set; }
	private Dictionary<uint, UI> LocalUIs = new Dictionary<uint, UI>();
	private Node ServerUI { get; set; }
	private Dictionary<uint, UI> ServerUIs = new Dictionary<uint, UI>();

    public override void _Ready()
    {
        base._Ready();
        Node uiContainer = Main.Instance.GetNode("UIContainer");
        if (NetworkManager.Instance.IsServer)
        {
            LevelManager.Instance.PlayerInstantiated += OnPlayerLoaded;
            LevelManager.Instance.PlayerTerminated += OnPlayerTerminated;

            ServerUI = new Node();
            ServerUI.Name = "ServerUI";
            NetworkDataManager.Instance.AddServerNode(uiContainer, ServerUI);
        }
        LocalUI = new Node();
        LocalUI.Name = "LocalUI";
        NetworkDataManager.Instance.AddSelfNode(uiContainer, LocalUI);
    }



    #region Player UI

    public uint AddUI(UI ui, Player player, bool active = false)
    {
        uint result = 0;
        if (PlayerUIContainers.ContainsKey(player))
        {
            result = PlayerUIContainers[player].AddUI(ui);
            SetActive(ui.GetUniqueId(), active, player);
        }
        return result;
    }
    public void RemoveUI(uint id, Player player)
    {
        if (PlayerUIContainers.ContainsKey(player))
        {
            if (PlayerUIContainers[player].UI[id].Active)
            {
                PlayerUIContainers[player].UI[id].Active = false;
            }
            PlayerUIContainers[player].RemoveUI(id);
        }
    }

    public void SetActive(uint id, bool active, Player player)
    {
        if (PlayerUIContainers.ContainsKey(player))
            PlayerUIContainers[player].SetActive(id, active);
    }
    #endregion

    #region Local UI

    public void AddLocalUI(UI ui, bool active = false)
    {
        ui.LocalUI = true;
        ui.UIOwnerId = NetworkManager.Instance.PlayerId.ToString();
        
        NetworkDataManager.Instance.AddSelfNode(LocalUI, ui);

        uint id = ui.GetUniqueId();
        LocalUIs.Add(id, ui); 
        SetLocalActive(id, active);
    }

    public void RemoveLocalUI(uint id)
    {
        if (LocalUIs.ContainsKey(id))
        {
            if (LocalUIs[id].Active)
            {
                LocalUIs[id].Active = false;
            }
            NetworkDataManager.Instance.RemoveSelfNode(LocalUIs[id]);
            LocalUIs.Remove(id);
        }
    }
    
    public void SetLocalActive(uint id, bool active)
    {
        if (LocalUIs.ContainsKey(id))
        {
            if (LocalUIs[id].Active != active) // only toggle active if not already in that state
            {
                LocalUIs[id].Active = active;
            }
        }
    }

    #endregion

    #region Server UI

    public void AddServerUI(UI ui, bool active = false)
    {
        ui.LocalUI = false;
        ui.ServerUI = true;
        ui.UIOwnerId = NetworkManager.Instance.PlayerId.ToString();
        NetworkDataManager.Instance.AddServerNode(ServerUI, ui);

        uint id = ui.GetUniqueId();
        ServerUIs.Add(id, ui);
        SetServerActive(id, active);
    }

    public void RemoveServerUI(uint id)
    {
        if (ServerUIs.ContainsKey(id))
        {
            if (ServerUIs[id].Active)
            {
                ServerUIs[id].Active = false;
            }
            NetworkDataManager.Instance.RemoveServerNode(ServerUIs[id]);
            ServerUIs.Remove(id);
        }
    }

    public void SetServerActive(uint id, bool active)
    {
        if (ServerUIs.ContainsKey(id))
        {
            if (ServerUIs[id].Active != active) // only toggle active if not already in that state
            {
                ServerUIs[id].Active = active;
            }
        }
    }

    #endregion

    private bool IsLocalPlayer(Player player)
    {
        if (player == null)
            return true;
        if (player.IsLocalOwned())
            return true;
        return false;
    }

    private void OnPlayerLoaded(Player player)
    {
        PlayerUIContainer playerUI = new PlayerUIContainer();
        playerUI.Player = player;
        PlayerUIContainers.Add(player, playerUI);
        playerUI.Name = player.GetMeta(SNC.Meta[(int)Globals.Meta.OwnerId]).ToString();

        NetworkDataManager.Instance.AddServerNode(Main.Instance.GetNode("UIContainer"), playerUI);
    }

    private void OnPlayerTerminated(Player player)
    {
        if (player.IsLocalOwned())
            return;
        Node n = GetNode(player.GetMeta(SNC.Meta[(int)Globals.Meta.OwnerId]).ToString());
        PlayerUIContainers.Remove(player);
        NetworkDataManager.Instance.RemoveServerNode(n);
    }
}
