using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

public partial class SaveManager : Node
{
    private static SaveManager instance;
    public static SaveManager Instance { get { return instance; } }
    public SaveManager()
    {
        if (instance == null)
            instance = this;
        AddToGroup(SNC.Groups[(int)Globals.Groups.AutoLoad]);
    }

    private static readonly string _RealTimeStamp = "RTS";
    private static readonly string _GameTimeStamp = "GTS";

    /// <summary> Game starts up on 'static', this is the save file for no active game. </summary>
    public string CurrentSave { get; private set; } = null;
    /// <summary> Level a player spawns into for first time. </summary>
    public string PlayerStartLevel { get; private set; } = "StartUp";

    public event Action<string> SaveCreated;
    public event Action SaveLoaded;
    public event Action GameSaved;
    public event Action<string> ChangedSave;
    public event Action<Node> LoadedPlayer;
    public event Action<Node> LoadedLevel;
    /// <summary>
    /// Destination folder for different save items. This can be added to for new save locations. The enum will be converted
    /// to a string folder name when saving.
    /// IMPORTANT NOTE: Any modification here should be reflected in SNC.SaveDest
    /// </summary>
    public enum SaveDest
    {
        Level,
        Player,
        ECS,
        Resource
    }

    public override void _Ready()
    {
        base._Ready();
    }

    public bool CreateSave(string saveName)
    {
        string priorSave = CurrentSave;
        if (FileManager.DirExists("saves/" + saveName))
        {
            FileManager.RemoveDir("/saves/" + saveName);
            /* could modify to return false, etc
            */    
        }
        CurrentSave = saveName;
        InstantiateScenesDir();
        InstantiateNewPlayerFile(NetworkManager.Instance.PlayerId.ToString(), NetworkManager.Instance.PlayerName);
        
        SaveCreated?.Invoke(saveName);

        ChangedSave?.Invoke(priorSave);
        return true;
    }

    public void SaveGame()
    {
        GameSaved?.Invoke();  
    }

    public bool LoadSave(string saveName)
    {
        if (!FileManager.DirExists("saves/" + saveName))
            return false;

        string priorSave = CurrentSave;

        CurrentSave = saveName;
        ChangedSave?.Invoke(priorSave);
        SaveLoaded?.Invoke();  
        return true;
    }

    public void Save(Node rootNode, SaveDest dest)
    {
        JsonValue data = ConvertNodeToJson(rootNode);
        string name = rootNode.Name;
        if(dest == SaveDest.Player)
            name = rootNode.GetMeta(SNC.Meta[(int)Globals.Meta.OwnerId]).ToString();
        
        SaveData(name, data, dest);
    }
    public void SaveData(string saveName, JsonValue data, SaveDest dest)
    {
        string folderName = SNC.SaveDest[(int)dest];

        data[_RealTimeStamp].Set(TimeManager.Instance.SerializeRealTime());
        data[_GameTimeStamp].Set(TimeManager.Instance.SerializeGameTime());
        
        FileManager.SaveToFileFormattedAsync(data, $"saves/{CurrentSave}/{folderName}/{saveName}");
    }
    public Node Load(string fileName, SaveDest dest)
    {
        JsonValue data = LoadData(fileName, dest);
        Node node = ConvertJsonToNode(data);
        // now can apply time
        TimeManager.Instance.ApplyRealTimeProgress(node, data[_RealTimeStamp]);
        TimeManager.Instance.ApplyGameTimeProgress(node, data[_GameTimeStamp]);

        if (dest == SaveDest.Player)
            LoadedPlayer?.Invoke(node);
        else if (dest == SaveDest.Level)
            LoadedLevel?.Invoke(node);

        return node;
    }
    public JsonValue LoadData(string fileName, SaveDest dest)
    {
        string fileHolder = SNC.SaveDest[(int)dest];

        return FileManager.LoadFromFile($"saves/{CurrentSave}/{fileHolder}/{fileName}");
    }

    private void InstantiateNewPlayerFile(string fileName, string playerName)
    {
        Node player = GD.Load<PackedScene>(ReferenceManager.Instance.GetScenePath("Player")).Instantiate();
        player.Name = playerName;
        player.SetMeta(SNC.Meta[(int)Globals.Meta.OwnerId], fileName);
        player.SetMeta(SNC.Meta[(int)Globals.Meta.LevelPartitionName], PlayerStartLevel);

        Save(player, SaveDest.Player);
    }

    private void InstantiateScenesDir()
    {
        foreach (KeyValuePair<string, string> filePath in ReferenceManager.Instance.LevelPaths)
        {
            Node root = GD.Load<PackedScene>(filePath.Value).Instantiate();
            if(root is not Level)
            {
                GD.Print(root.Name + " IS NOT A LEVEL NODE. FIX.");
                throw new Exception();
            }
            
            root.AddToGroup(SNC.Groups[(int)Globals.Groups.Level]);

            NetworkDataManager.Instance.ApplyNextAvailableUniqueId(root);

            JsonValue sceneData = SaveManager.ConvertNodeToJson(root);

            sceneData[_RealTimeStamp].Set(TimeManager.Instance.SerializeRealTime());
            sceneData[_GameTimeStamp].Set(TimeManager.Instance.SerializeGameTime());
            AddHash(ref sceneData);

            FileManager.SaveToFileFormatted(sceneData, $"saves/{CurrentSave}/{SNC.SaveDest[(int)SaveDest.Level]}/{root.Name}");

            root.QueueFree();
        }
    }

    #region HASH
    static string GetHash(string inputString)
    {
        byte[] hashBytes;
        using (HashAlgorithm algorithm = SHA256.Create())
            hashBytes = algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));

        return BitConverter
                .ToString(hashBytes)
                .Replace("-", String.Empty);
    }

    static private void AddHash(ref JsonValue obj)
    {
        obj.Remove("hash");
        string hash = GetHash(obj.ToString());
        obj["hash"].Set(hash);
    }
    private bool HashMatches(JsonValue obj)
    {
        string hashStored = obj["hash"].AsString();
        obj.Remove("hash");

        return hashStored == GetHash(obj.ToString());
    }

    #endregion


    #region Converting Data

    private static readonly string _Name = "N";
    private static readonly string _Type = "T";
    private static readonly string _DerivedType = "DT";
    private static readonly string _Position = "P";
    private static readonly string _Rotation = "R";
    private static readonly string _Scale = "S";
    private static readonly string _Size = "SZ";
    private static readonly string _ZIndex = "ZI";
    private static readonly string _ZIsRelative = "ZIR";
    private static readonly string _YSortEnabled = "YSE";
    private static readonly string _Meta = "M";
    private static readonly string _Group = "G";
    private static readonly string _Children = "C";
    private static readonly string _ISaveData = "ISD";

    private static readonly string _MinSize = "MS";
    private static readonly string _AnchorLeft = "AL";
    private static readonly string _AnchorRight = "AR";
    private static readonly string _AnchorBottom = "AB";
    private static readonly string _AnchorTop = "AT";
    private static readonly string _AnchorPreset = "AP";
    private static readonly string _OffsetLeft = "OL";
    private static readonly string _OffsetRight = "OR";
    private static readonly string _OffsetTop = "OT";
    private static readonly string _OffsetBottom = "OB";
    private static readonly string _PivotOffset = "PO";
    private static readonly string _Theme = "THM";
    private static readonly string _LayoutMode = "LM";
    private static readonly string _LayoutDirection = "LD";

    public static JsonValue ConvertNodeToJson(Node node)
    {
        JsonValue val = CollectNodeData(node);
        return val;
    }

    private static JsonValue CollectNodeData(Node node)
    {
        JsonValue jsonNode = new JsonValue();

        if (node.IsInGroup(SNC.Groups[(int)Globals.Groups.NotPersistent]) || node.IsInGroup(SNC.Groups[(int)Globals.Groups.SelfOnly]))
            return new JsonValue();
        if (node.GetParent().IsValid())
            if (node.GetParent().IsInGroup(SNC.Groups[(int)Globals.Groups.IgnoreChildren]) ||
                node.GetParent().IsInGroup(SNC.Groups[(int)Globals.Groups.IgnoreChildrenSave]))
                return new JsonValue();

        jsonNode[_Name].Set(node.Name);
        jsonNode[_Type].Set(Globals.RemoveNamespace(node.GetType().ToString()));
        jsonNode[_DerivedType].Set(node.GetClass());

        if (node is Node2D)
        {
            Node2D node2d = (Node2D)node;
            jsonNode[_ZIsRelative].Set(node2d.ZAsRelative);
            jsonNode[_YSortEnabled].Set(node2d.YSortEnabled);
            jsonNode[_ZIndex].Set(node2d.ZIndex);

            jsonNode[_Position].Set(node2d.Position);
            jsonNode[_Rotation].Set(node2d.Rotation);
            jsonNode[_Scale].Set(node2d.Scale);
        }
        else if (node is Control c)
        {
            jsonNode[_Position].Set(c.Position);
            jsonNode[_Rotation].Set(c.Rotation);
            jsonNode[_Scale].Set(c.Scale);
            jsonNode[_Size].Set(c.Size);

            jsonNode[_ZIndex].Set(c.ZIndex);
            jsonNode[_ZIsRelative].Set(c.ZAsRelative);
            jsonNode[_MinSize].Set(c.CustomMinimumSize);
            jsonNode[_LayoutMode].Set(c.LayoutMode);
            jsonNode[_LayoutDirection].Set((int)c.LayoutDirection);
            jsonNode[_AnchorLeft].Set(c.AnchorLeft);
            jsonNode[_AnchorRight].Set(c.AnchorRight);
            jsonNode[_AnchorBottom].Set(c.AnchorBottom);
            jsonNode[_AnchorTop].Set(c.AnchorTop);
            jsonNode[_AnchorPreset].Set(c.AnchorsPreset);
            jsonNode[_OffsetLeft].Set(c.OffsetLeft);
            jsonNode[_OffsetRight].Set(c.OffsetRight);
            jsonNode[_OffsetTop].Set(c.OffsetTop);
            jsonNode[_OffsetBottom].Set(c.OffsetBottom);
            jsonNode[_PivotOffset].Set(c.PivotOffset);
            if (c.Theme.IsValid())
            {
                jsonNode[_Theme].Set(c.Theme.ResourcePath.RemovePathAndFileType());
            }
        }
        else if (node is Node3D)
        {
            Node3D node3d = (Node3D)node;

            jsonNode[_Position].Set(node3d.Position);
            jsonNode[_Rotation].Set(node3d.Rotation);
            jsonNode[_Scale].Set(node3d.Scale);
        }

        foreach (string meta in node.GetMetaList())
        {
            if (meta == SNC.Meta[(int)Globals.Meta.UniqueId])
                continue;
            jsonNode[_Meta][meta].Set(node.GetMeta(meta).AsString());
        }
        foreach (string group in node.GetGroups()) // not accessible outside main
            jsonNode[_Group].Append(group);

        for (int i = 0; i < node.GetChildCount(); i++) // not accessible outside main
            jsonNode[_Children].Append(CollectNodeData(node.GetChild(i)));

        if (node is ISaveData)
            jsonNode[_ISaveData].Set((node as ISaveData).SerializeSaveData());

        return jsonNode;
    }

    public static Node ConvertJsonToNode(JsonValue data)
    {
        Node node = (Node)ClassDB.Instantiate(data[_DerivedType].AsString());
        // Set Basic Node Data
        node.Name = data[_Name].AsString();
        if (node is Node2D node2d)
        {
            node2d.Position = data[_Position].AsVector2();
            node2d.Rotation = data[_Rotation].AsFloat();
            node2d.Scale = data[_Scale].AsVector2();

            node2d.ZIndex = data[_ZIndex].AsInt();
            node2d.ZAsRelative = data[_ZIsRelative].AsBool();
            node2d.YSortEnabled = data[_YSortEnabled].AsBool();
        }
        else if (node is Control c)
        {
            c.Position = data[_Position].AsVector2();
            c.Scale = data[_Scale].AsVector2();
            c.Rotation = data[_Rotation].AsFloat();
            c.Size = data[_Size].AsVector2();

            c.ZIndex = data[_ZIndex].AsInt();
            c.ZAsRelative = data[_ZIsRelative].AsBool();
            c.CustomMinimumSize = data[_MinSize].AsVector2();
            c.LayoutMode = data[_LayoutMode].AsInt();
            c.LayoutDirection = (Control.LayoutDirectionEnum)data[_LayoutDirection].AsInt();
            c.AnchorLeft = data[_AnchorLeft].AsFloat();
            c.AnchorRight = data[_AnchorRight].AsFloat();
            c.AnchorBottom = data[_AnchorBottom].AsFloat();
            c.AnchorTop = data[_AnchorTop].AsFloat();
            c.AnchorsPreset = data[_AnchorPreset].AsInt();
            c.OffsetLeft = data[_OffsetLeft].AsFloat();
            c.OffsetRight = data[_OffsetRight].AsFloat();
            c.OffsetBottom = data[_OffsetBottom].AsFloat();
            c.OffsetTop = data[_OffsetTop].AsFloat();
            c.PivotOffset = data[_PivotOffset].AsVector2();

            if (data[_Theme].IsValue)
            {
                c.Theme = GD.Load<Theme>(ReferenceManager.Instance.GetResourcePath(data[_Theme].AsString()));
            }
        }
        else if (node is Node3D node3d)
        {
            node3d.Position = data[_Position].AsVector3();
            node3d.Rotation = data[_Rotation].AsVector3();
            node3d.Scale = data[_Scale].AsVector3();
        }

        // Save node instance id to re-reference after setting script
        ulong nodeID = node.GetInstanceId();
        // if type != derived-type, a script is attached
        if (!data[_Type].AsString().Equals(data[_DerivedType].AsString()))
        {
            if(ReferenceManager.Instance.Scripts.ContainsKey(data[_Type].AsString()))
                node.SetScript(ReferenceManager.Instance.Scripts[data[_Type].AsString()]);
            else
                node.SetScript(GD.Load<Script>(ReferenceManager.Instance.GetScriptPath(data[_Type].AsString())));
        }

        node = GodotObject.InstanceFromId(nodeID) as Node;

        foreach (KeyValuePair<string, JsonValue> meta in data[_Meta].Object)
            node.SetMeta(meta.Key, meta.Value.AsString());
        foreach (JsonValue group in data[_Group].Array)
            node.AddToGroup(group.AsString());

        foreach (JsonValue child in data[_Children].Array)
            node.AddChild(ConvertJsonToNode(child));

        if (node is ISaveData isd)
            isd.DeserializeSaveData(data[_ISaveData]);
        return node;
    }

    #endregion

}

public interface ISaveData
{
    public JsonValue SerializeSaveData();
    public void DeserializeSaveData(JsonValue data);
}
