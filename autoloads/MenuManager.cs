using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

public partial class MenuManager : Node, INetworkData
{
    public event Action<Menu, Player> MenuOpened;
    public event Action<Menu, Player> MenuClosed;

    private static readonly string _Nothing = "N";

    private static MenuManager instance;
    public static MenuManager Instance { get { return instance; } }
    public MenuManager()
    {
        if (instance == null)
            instance = this;
        AddToGroup(SNC.Groups[(int)Globals.Groups.AutoLoad]);
    }
    public Dictionary<Player, PlayerMenuContainer> PlayerMenuContainers { get; private set; } = new Dictionary<Player, PlayerMenuContainer>();
    private Node LocalMenuNode { get; set; }
    private List<Menu> LocalTopMenus { get; set; } = new List<Menu>();
    private Dictionary<uint, Menu> LocalSubMenus { get; set; } = new Dictionary<uint, Menu>();
    public bool NetworkUpdate { get; set; } = true;

    public override void _Ready()
    {
        base._Ready();

        if (NetworkManager.Instance.IsServer)
        {
            LevelManager.Instance.PlayerInstantiated += OnPlayerLoaded;
            LevelManager.Instance.PlayerTerminated += OnPlayerTerminated;
        
            NetworkDataManager.Instance.AddNetworkNodes(this);
        }

        LocalMenuNode = new Node();
        LocalMenuNode.Name = "LocalMenus";
        NetworkDataManager.Instance.AddSelfNode(Main.Instance.GetNode("MenuContainer"), LocalMenuNode);
    }

    #region Player Menu

    public uint AddMenu(Menu menu, Player player, bool active = false)
    {
        uint result = 0;
        if (PlayerMenuContainers.ContainsKey(player))
        {
            result = PlayerMenuContainers[player].AddMenu(menu);
            PlayerMenuContainers[player].SetActive(menu.GetUniqueId(), active);
            if (active)
                MenuOpened?.Invoke(menu, player);
        }
        return result;
    }

    public void RegisterSubMenu(Menu menu, Player player, bool active = false)
    {
        if (PlayerMenuContainers.ContainsKey(player))
        {
            PlayerMenuContainers[player].RegisterSubMenu(menu);
            PlayerMenuContainers[player].SetActive(menu.GetUniqueId(), active);
        }
    }

    public void RemoveMenu(uint id, Player player)
    {
        if (PlayerMenuContainers.ContainsKey(player))
        {
            Menu m = PlayerMenuContainers[player].RemoveMenu(id);
            if(m != null)
                MenuClosed?.Invoke(m, player);
        }
    }
    public void UnregisterSubMenu(uint id, Player player)
    {
        if (PlayerMenuContainers.ContainsKey(player))
        {
            PlayerMenuContainers[player].UnregisterSubMenu(id);
        }
    }
    public void SetActive(uint id, bool active, Player player)
    {
        if (PlayerMenuContainers.ContainsKey(player))
        {
            Menu result = PlayerMenuContainers[player].SetActive(id, active);
            if (result != null)
            {
                if(active)
                    MenuOpened?.Invoke(result, player);
                else
                    MenuClosed?.Invoke(result, player);
            }
        }
    }

    #endregion

    #region Local Menu

    #region Menu
    public uint AddLocalMenu(Menu menu, bool active = false)
    {
        menu.LocalUI = true;
        menu.ZIndex = 10;
        menu.OwningPlayer = Helper.Instance.LocalPlayer;
        LocalTopMenus.Add(menu);
        NetworkDataManager.Instance.AddSelfNode(LocalMenuNode, menu);

        uint id = menu.GetUniqueId();
        SetLocalActive(id, active);
        return id;
    }

    public void RemoveLocalMenu(uint id)
    {
        Menu m = LocalTopMenus.FirstOrDefault(cus => cus.GetUniqueId() == id);
        if (m != null)
        {
            if (m.Active)
            {
                m.Active = false;
                MenuClosed?.Invoke(m, null);
            }
            NetworkDataManager.Instance.RemoveSelfNode(m);
            LocalTopMenus.Remove(m);
        }
    }

    #endregion

    #region SubMenu

    public void RegisterLocalSubMenu(Menu menu, bool active = false)
    {
        menu.LocalUI = true;
        menu.ZIndex = 10;
        menu.OwningPlayer = Helper.Instance.LocalPlayer;
        uint id = menu.GetUniqueId();
        LocalSubMenus.Add(id, menu);
        SetLocalActive(id, active);
    }

    public void UnregisterLocalSubMenu(uint id)
    {
        if (LocalSubMenus.ContainsKey(id))
        {
            if (LocalSubMenus[id].Active)
            {
                LocalSubMenus[id].Active = false;
            }
            LocalSubMenus.Remove(id);
        }
    }

    public void SetLocalActive(uint id, bool active)
    {
        Menu m = LocalTopMenus.FirstOrDefault(cus => cus.GetUniqueId() == id);
        if (m != null)
        {
            if (m.Active != active)
            {
                m.Active = active;

                if (active)
                {
                    MenuOpened?.Invoke(m, Helper.Instance.LocalPlayer);
                }
                else
                {
                    MenuClosed?.Invoke(m, Helper.Instance.LocalPlayer);
                }
            }
        }
        else if (LocalSubMenus.ContainsKey(id))
        {
            if (LocalSubMenus[id].Active != active) // only toggle active if not already in that state
            {
                LocalSubMenus[id].Active = active;
            }
        }
    }
    #endregion





    public bool IsInMenu(Player player = null)
    {
        bool res = false;
        if (IsLocalPlayer(player))
        {
            foreach (var item in LocalTopMenus)
            {
                if (item.Active)
                    res = true;
            }
        }
        if(!res && player != null)
        {
            res = PlayerMenuContainers[player].IsInMenu();
        }
        return res;
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
        PlayerMenuContainer playerUI = new PlayerMenuContainer();
        playerUI.Player = player;
        PlayerMenuContainers.Add(player,  playerUI);
        playerUI.Name = player.GetMeta(SNC.Meta[(int)Globals.Meta.OwnerId]).ToString();

        NetworkDataManager.Instance.AddServerNode(Main.Instance.GetNode("MenuContainer"), playerUI);

        NetworkUpdate = true;
    }
    private void OnPlayerTerminated(Player player)
    {
        if (player.IsLocalOwned())
            return;
        Node n = GetNode(player.GetMeta(SNC.Meta[(int)Globals.Meta.OwnerId]).ToString());
        PlayerMenuContainers.Remove(player);
        NetworkDataManager.Instance.RemoveServerNode(n);

        NetworkUpdate = true;
    }

    public JsonValue SerializeNetworkData(bool forceReturn = false, bool ignoreThisUpdateOccurred = false)
    {
        if (!this.ShouldUpdate(forceReturn))
            return null;

        JsonValue data = new JsonValue(); 
        data[_Nothing].Set(0);
        return this.CalculateNetworkReturn(data, ignoreThisUpdateOccurred);
    }

    public void DeserializeNetworkData(JsonValue data)
    {
        PlayerMenuContainers.Clear();
        Node mc = Main.Instance.GetNode("MenuContainer");

        foreach (var child in mc.GetChildren())
        {
            if(child.Name != "LocalMenus")
            {
                Node owningPlayer = NetworkDataManager.Instance.GetOwnerIdToPlayer(ulong.Parse(child.Name));
                PlayerMenuContainers.Add(owningPlayer as Player, child as PlayerMenuContainer);
            }
        }
    }
}
