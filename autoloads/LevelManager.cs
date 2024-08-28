using Godot;
using Steamworks.Ugc;
using System;
using System.Collections.Generic;

public partial class LevelManager : Node
{

    public event Action<Player, Level> PlayerChangeLevel;

    private static LevelManager instance;
    public static LevelManager Instance {  get { return instance; } }

    public LevelManager()
    {
        if (instance == null)
            instance = this;
        AddToGroup(SNC.Groups[(int)Globals.Groups.AutoLoad]);
        PositionsOccupied = new bool[MaxConcurrentLevels];
    }

    /// <summary> Contains a list of all levels which exist in the current save. </summary>
    private List<string> AllLevels = new List<string>();

    private Dictionary<string, LevelPartition> ActiveLevels = new Dictionary<string, LevelPartition>();

    bool[] PositionsOccupied;
    /// <summary> Max number of levels open at the same time. Limiting this number will improve performance. </summary>
    private int MaxConcurrentLevels { get; set; } = 4;
    private bool UseOffsets { get; set; } = true;

    public event Action<Player> PlayerInstantiated;
    public event Action<Player> PlayerTerminated;
    public override void _Ready()
    {
        base._Ready();
        SaveManager.Instance.GameSaved += OnGameSaved;
        SaveManager.Instance.ChangedSave += OnChangedSave;
        SaveManager.Instance.SaveLoaded += OnSaveLoaded;
    }


    public override void _ExitTree()
    {
        base._ExitTree();
        SaveManager.Instance.GameSaved -= OnGameSaved;
        SaveManager.Instance.ChangedSave -= OnChangedSave;
        SaveManager.Instance.SaveLoaded -= OnSaveLoaded;
    }

    public bool LevelExists(string name)
    {
        return AllLevels.Contains(name);
    }
    public bool LevelActive(string name)
    {
        return ActiveLevels.ContainsKey(name);
    }

    public void SaveAllLevels()
    {
        foreach (var val in ActiveLevels.Keys)
            SaveLevelPartition(val);
    }


    public void SaveLevelPartition(string levelName)
    {
        if (!ActiveLevels.ContainsKey(levelName)) { return; }

        var level = ActiveLevels[levelName];
        level.Root.Position -= level.Offset;


        SaveManager.Instance.Save(level.Root, SaveManager.SaveDest.Level);
        level.Root.Position += level.Offset;

        foreach (Player p in level.LocalPlayers)
        {
            p.Position -= level.Offset;

            SaveManager.Instance.Save(p, SaveManager.SaveDest.Player);

            p.Position += level.Offset;
        }
    }
    public void LoadLevelPartition(string levelName)
    {
        if (ActiveLevels.ContainsKey(levelName))
            return;

        if (ActiveLevels.Count >= MaxConcurrentLevels)
        {
            GD.Print("Attempting to load too many scenes, reach MaxConcurrentScenes limit of " + MaxConcurrentLevels);
            return;
        }
        Level level = SaveManager.Instance.Load(levelName, SaveManager.SaveDest.Level) as Level;

        NetworkDataManager.Instance.ApplyNextAvailableUniqueId(level);
        Vector3 offset = Vector3.Zero;
        // only apply a offset if this scene is not a Control and we want to use offsets
        int offsetIndex = -1;
        

        LevelPartition lp = new LevelPartition(level, offset);
        lp.PositionIndex = offsetIndex;

        ActiveLevels.Add(levelName, lp);

        level.Position += offset;
        NetworkDataManager.Instance.AddServerNode(Main.Instance, level);
    }

    public void SaveAndCloseLevelPartition(string levelName)
    {
        if (!ActiveLevels.ContainsKey(levelName)) { return; }
        SaveLevelPartition(levelName);
        CloseLevel(levelName);
    }

    public void CloseLevel(string levelName)
    {
        if (!ActiveLevels.ContainsKey(levelName)) { return; }
        if (ActiveLevels[levelName].PositionIndex != -1)
            PositionsOccupied[ActiveLevels[levelName].PositionIndex] = false;
        NetworkDataManager.Instance.RemoveServerNode(ActiveLevels[levelName].Root);
        ActiveLevels.Remove(levelName);
    }

    public bool HasLocalPlayers(string sceneName)
    {
        if (ActiveLevels.ContainsKey(sceneName))
            return ActiveLevels[sceneName].LocalPlayers.Count > 0;

        return false;
    }

    /// <summary>
    /// Converts a location in local units to scene's true position.
    /// Eg. Local Pos: (0, 20, 0), Scene Pos: (5000, 20, 0)
    /// </summary>
    /// <returns></returns>
    public Vector3 LocalPos2ScenePos(Vector3 position, string scene)
    {
        return position + ActiveLevels[scene].Offset;
    }
    /// <summary>
    /// Moves a player from one level to another. Will remove any positional offsets from the first level and apply the offset of 
    /// the new level.
    /// </summary>
    /// <param name="firstLoad">If true, this is the first time the player is being loaded into the game.</param>
    public void TransferPlayer(Player player, string level, bool firstLoad)
    {
        if (!ActiveLevels.ContainsKey(level))
        {
            LoadLevelPartition(level);
            
            if (!ActiveLevels.ContainsKey(level))
            {
                GD.Print("Level: " + level + " could not be loaded");
                return;
            }
        }

        string currentLevel = player.GetMeta(SNC.Meta[(int)Globals.Meta.LevelPartitionName]).ToString();

        if (firstLoad)
        {
            
            ActiveLevels[level].AddPlayer(player);
            player.Position += ActiveLevels[currentLevel].Offset;

            NetworkDataManager.Instance.AddServerNode(Main.Instance, player);

            PlayerChangeLevel?.Invoke(player, ActiveLevels[level].Root);

            return;
        }

        player.Position -= ActiveLevels[currentLevel].Offset;
        ActiveLevels[currentLevel].RemovePlayer(player);
        ActiveLevels[level].AddPlayer(player);
        player.Position = ActiveLevels[level].Offset;

        PlayerChangeLevel?.Invoke(player, ActiveLevels[level].Root);

        if (ActiveLevels[currentLevel].LocalPlayers.Count == 0)
        {
            SaveAndCloseLevelPartition(currentLevel);
        }
    }
    /// <summary>
    /// Instantiates a Player into the game. If the player does not have a save file, a new player will be created and saved.
    /// </summary>
    public Node InstantiatePlayer(ulong ownerId, string name)
    {
        if (FileManager.FileExists("saves/" + SaveManager.Instance.CurrentSave + "/" + SaveManager.SaveDest.Player + "/" + ownerId.ToString()))
        {
            Player myPlayer = SaveManager.Instance.Load(ownerId.ToString(), SaveManager.SaveDest.Player) as Player;
            NetworkDataManager.Instance.ApplyNextAvailableUniqueId(myPlayer);
            PlayerInstantiated?.Invoke(myPlayer);
            TransferPlayer(myPlayer, myPlayer.GetLevelName(), true);
            return myPlayer;
        }

        // everything below is only called once on client first join

        string levelPartition = SaveManager.Instance.PlayerStartLevel;
        Player player = GD.Load<PackedScene>(ReferenceManager.Instance.GetScenePath("Player")).Instantiate<Player>();
        player.SetMeta(SNC.Meta[(int)Globals.Meta.OwnerId], ownerId.ToString());
        player.SetMeta(SNC.Meta[(int)Globals.Meta.LevelPartitionName], levelPartition);
        player.Name = name;

        SaveManager.Instance.Save(player, SaveManager.SaveDest.Player);


        if (!player.HasMeta(SNC.Meta[(int)Globals.Meta.LevelPartitionName]))
            player.SetMeta(SNC.Meta[(int)Globals.Meta.LevelPartitionName], SaveManager.Instance.PlayerStartLevel);

        NetworkDataManager.Instance.ApplyNextAvailableUniqueId(player);
        string scenePartition = player.GetMeta(SNC.Meta[(int)Globals.Meta.LevelPartitionName]).ToString();

        PlayerInstantiated?.Invoke(player); 
        TransferPlayer(player, scenePartition, true);

        return player;
    }

    public void TerminatePlayer(ulong ownerId)
    {
        Node player = NetworkDataManager.Instance.GetOwnerIdToPlayer(ownerId);
        if (player.IsValid())
            PlayerTerminated?.Invoke(player as Player);
        SaveManager.Instance.Save(player, SaveManager.SaveDest.Player);
        NetworkDataManager.Instance.RemoveServerNode(player);

        NetworkDataManager.Instance.RemoveOwnerIdToPlayer(ownerId);
    }

    public void CheckAddNode(Node owner, Node node)
    {
        if (node.HasMeta(SNC.Meta[(int)Globals.Meta.OwnerId]))
        {
            if (!node.HasMeta(SNC.Meta[(int)Globals.Meta.LevelPartitionName]))
                node.SetMeta(SNC.Meta[(int)Globals.Meta.LevelPartitionName], SaveManager.Instance.PlayerStartLevel);

            string sceneName = node.GetMeta(SNC.Meta[(int)Globals.Meta.LevelPartitionName]).ToString();

            if (node is Node2D n2)
                n2.Position += new Vector2(ActiveLevels[sceneName].Offset.X, ActiveLevels[sceneName].Offset.Y);
            if (node is Node3D n3)
                n3.Position += ActiveLevels[sceneName].Offset;

            ActiveLevels[sceneName].AddPlayer(node as Player);
        }
    }

    public void CheckRemoveNode(Node node)
    {
        if (node.HasMeta(SNC.Meta[(int)Globals.Meta.OwnerId]))
        {
            string sceneName = node.GetMeta(SNC.Meta[(int)Globals.Meta.LevelPartitionName]).ToString();

            if (node is Node2D n2)
                n2.Position -= new Vector2(ActiveLevels[sceneName].Offset.X, ActiveLevels[sceneName].Offset.Y);
            if (node is Node3D n3)
                n3.Position -= ActiveLevels[sceneName].Offset;

            SaveManager.Instance.Save(node, SaveManager.SaveDest.Player);

            ActiveLevels[sceneName].RemovePlayer(node as Player);
        }
    }

    private void CollectAllLevels()
    {
        AllLevels.Clear();
        List<string> list = FileManager.GetFiles("saves/" + SaveManager.Instance.CurrentSave +"/"+ SaveManager.SaveDest.Level);
        for (int i = 0; i < list.Count; i++)
        {
            int lastIndex = list[i].Find(".");
            list[i] = list[i].Substring(0, lastIndex);
        }
        AllLevels = list;
    }


    private void OnChangedSave(string obj)
    {
        List<string> activeLevels = new List<string>(ActiveLevels.Keys.Count);
        foreach(var level in ActiveLevels.Keys)
            activeLevels.Add(level);

        foreach(var level in activeLevels)
            SaveAndCloseLevelPartition(level);
    }

    private void OnSaveLoaded()
    {
        CollectAllLevels();
    }

    private void OnGameSaved()
    {
        SaveAllLevels();
    }

}

public class LevelPartition
{
    public LevelPartition(Level root, Vector3 offset)
    {
        Root = root;
        Offset = offset;
        PositionIndex = (int)(offset.X / 5000f);
    }

    public Level Root { get; private set; }
    public Vector3 Offset { get; private set; }
    public List<Player> LocalPlayers { get; private set; } = new List<Player>();

    public void AddPlayer(Player player)
    {
        if(!LocalPlayers.Contains(player)) 
            LocalPlayers.Add(player);
        player.SetMeta(SNC.Meta[(int)Globals.Meta.LevelPartitionName], Root.Name);
    }
    public void RemovePlayer(Player player)
    {
        LocalPlayers.Remove(player);
    }
    public int PositionIndex { get; set; } = -1;

}