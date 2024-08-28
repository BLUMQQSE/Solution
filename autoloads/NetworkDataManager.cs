using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;
using static Godot.Window;
using static System.Collections.Specialized.BitVector32;

public abstract partial class NetworkDataManager : Node, IListener, INetworkData
{
    private static readonly string _Add = "A";
    private static readonly string _OwnerID = "OID";
    private static readonly string _UniqueID = "UID";

    protected static NetworkDataManager instance;
    public static NetworkDataManager Instance { get { return instance; } }

    private static readonly string _DataType = "DAT";
    private static readonly string _NetworkNodes = "NN";

    public event Action<Node> ServerNodeAdded;
    public event Action<Node> ServerNodeRemoved;
    public event Action<Node> SelfNodeAdded;
    public event Action<Node> SelfNodeRemoved;

    public NetworkDataManager() 
    {
        if (instance == null)
            instance = this;
        AddToGroup(SNC.Groups[(int)Globals.Groups.AutoLoad]);
    }

    protected Dictionary<ulong, Node> OwnerIdToPlayer { get; private set; } = new Dictionary<ulong, Node>();
    public Node GetOwnerIdToPlayer(ulong id)
    {
        if(OwnerIdToPlayer.ContainsKey(id)) 
            return OwnerIdToPlayer[id];
        else
        {
            // attempt to find
            foreach(var child in Main.Instance.GetChildren())
            {
                if(child is Player)
                {
                    ulong m = ulong.Parse(child.GetMeta(SNC.Meta[(int)Globals.Meta.OwnerId]).AsString());
                    if(m == id)
                    {
                        AddOwnerIdToPlayer(id, child);
                        return child;
                    }
                }
            }
            if (!NetworkManager.Instance.IsServer)
                RequestForceUpdate();

            return null;
        }
    }
    public void AddOwnerIdToPlayer(ulong id, Node player)
    {
        if (OwnerIdToPlayer.ContainsKey(id))
            return;
        OwnerIdToPlayer.Add(id, player);

        playersAdded.Add((id, player));
        NetworkUpdate = true;
    }
    public void RemoveOwnerIdToPlayer(ulong id)
    {
        if (!OwnerIdToPlayer.ContainsKey(id))
            return;

        NetworkUpdate = true;
        OwnerIdToPlayer.Remove(id);
    }

    private TimeTracker UpdateTimer { get; set; } = new TimeTracker();
    public float UpdateIntervalInMil { get; private set; } = 33f;
    public bool NetworkUpdate { get; set; } = true;
    private List<(ulong, Node)> playersAdded = new List<(ulong, Node)>();

    #region Flags
    private bool RecievedFullServerData = false;
    #endregion

    #region UniqueId
    public static readonly uint FirstAvailableSelfUniqueId = uint.MaxValue - 10000000;
    private uint nextAvailableSelfUniqueId = FirstAvailableSelfUniqueId;
    private uint nextAvailableUniqueId = 0;

    private Dictionary<uint, Node> uniqueIdToNode = new Dictionary<uint, Node>();

    public event Action NetworkUpdate_Client;


    public Node UniqueIdToNode(uint id)
    {
        if (uniqueIdToNode.ContainsKey(id))
            return uniqueIdToNode[id];
        else if (!NetworkManager.Instance.IsServer)
            RequestForceUpdate();
        return null;
    }

    public bool HasUniqueIdToNode(uint id)
    {
        return uniqueIdToNode.ContainsKey(id);
    }

    public void ApplyNextAvailableUniqueId(Node node)
    {
        if (!node.HasMeta(SNC.Groups[(int)Globals.Meta.UniqueId]))
        {
            uint result = nextAvailableUniqueId;
            nextAvailableUniqueId++;
            node.SetMeta(SNC.Meta[(int)Globals.Meta.UniqueId], result.ToString());

            uniqueIdToNode[result] = node;
        }
        foreach (Node child in node.GetChildren())
        {
            ApplyNextAvailableUniqueId(child);
        }
    }

    public void ApplyNextAvailableSelfUniqueId(Node node)
    {
        node.AddToGroup(SNC.Groups[(int)Globals.Groups.SelfOnly]);
        if (!node.HasMeta(SNC.Meta[(int)Globals.Meta.UniqueId]))
        {
            uint result = nextAvailableSelfUniqueId;
            nextAvailableSelfUniqueId++;
            node.SetMeta(SNC.Meta[(int)Globals.Meta.UniqueId], result.ToString());

            uniqueIdToNode[result] = node;
        }
        foreach (Node child in node.GetChildren())
        {
            ApplyNextAvailableSelfUniqueId(child);
        }
    }

    #endregion

    #region NetworkNodes

    private List<INetworkData> NetworkNodes = new List<INetworkData>();
    public void AddNetworkNodes(Node node)
    {
        if (node is INetworkData nd && !node.IsInGroup(SNC.Groups[(int)Globals.Groups.SelfOnly]))
            if (!NetworkNodes.Contains(nd))
                NetworkNodes.Add(nd);

        foreach (Node child in node.GetChildren())
            AddNetworkNodes(child);

    }
    public void RemoveNetworkNodes(Node node)
    {
        if (node is INetworkData nd)
        {
            if (NetworkNodes.Contains(nd))
                NetworkNodes.Remove(nd);
        }
        foreach (Node child in node.GetChildren())
            RemoveNetworkNodes(child);
    }

    #endregion

    public override void _Ready()
    {
        base._Ready();

        AddNetworkNodes(this);
        UpdateTimer.Start();
        EventBus.Instance.Subscribe(EventId.OnConnectionRecievedData, this);
        EventBus.Instance.Subscribe(EventId.OnSocketRecievedData, this);
        ApplyNextAvailableUniqueId(GetTree().Root);
        
    }
    public override void _ExitTree()
    {
        base._ExitTree();
        EventBus.Instance.UnsubscribeAll(this);
        UpdateTimer.Dispose();
    }
    public override void _Process(double delta)
    {
        base._Process(delta);

        if (UpdateTimer.ElapsedMilliseconds >= UpdateIntervalInMil)
        {
            if (NetworkManager.Instance.IsServer)
            {
                UpdateTimer.Restart();
                if (NetworkManager.Instance.SocketConnections < 2) return;

                JsonValue scenelessData = new JsonValue();
                scenelessData[_DataType].Set((int)Globals.DataType.ServerUpdate);
                foreach (var n in NetworkNodes)
                {
                    scenelessData[_NetworkNodes][(n as Node).GetMeta(SNC.Meta[(int)Globals.Meta.UniqueId]).ToString()]
                    .Set((n).SerializeNetworkData(false));
                }
                if (scenelessData[_NetworkNodes].ToString() != "{}" && scenelessData[_NetworkNodes].ToString() != "null")
                    SendToClients(scenelessData);
            }
            else if(RecievedFullServerData) //only start sending client data after recieving full server data
            {
                NetworkUpdate_Client?.Invoke();
            }
        }
    }

    private void ForceUpdateClients()
    {
        JsonValue scenelessData = new JsonValue();
        scenelessData[_DataType].Set((int)Globals.DataType.ServerUpdate);
        foreach (var n in NetworkNodes)
        {
            scenelessData[_NetworkNodes][(n as Node).GetMeta(SNC.Meta[(int)Globals.Meta.UniqueId]).ToString()]
            .Set((n).SerializeNetworkData(true));
        }
        if (scenelessData[_NetworkNodes].ToString() != "{}" && scenelessData[_NetworkNodes].ToString() != "null")
            SendToClients(scenelessData);
    }

    public abstract void SendToServer(JsonValue data, bool reliable = true);
    public abstract void SendToClientId(ulong client, JsonValue data, bool reliable = true);
    public abstract void SendToClients(JsonValue data, bool reliable = true);

    #region AddRemoveNode

    public void AddSelfNode(Node owner, Node newNode)
    {
        ApplyNextAvailableSelfUniqueId(newNode);
        owner.AddChild(newNode, true);
        SelfNodeAdded?.Invoke(newNode);
    }

    public void RemoveSelfNode(Node node)
    {
        SelfNodeRemoved?.Invoke(node);
        SafeQueueFree(node);
    }

    private void SafeQueueFree(Node node)
    {
        if (!node.IsValid()) return;

        node.QueueFree();
    }

    public void AddServerNode(Node owner, Node newNode, Vector3 positionOverride = new Vector3(), bool persistent = true)
    {
        if (!NetworkManager.Instance.IsServer)
        {
            throw new Exception("ERROR: CLIENT IS TRYING TO CALL AddServerNode");
        }

        bool levelless = false;
        if (!owner.HasMeta(SNC.Meta[(int)Globals.Meta.LevelPartitionName]) && !newNode.HasMeta(SNC.Meta[(int)Globals.Meta.LevelPartitionName]))
        {
            if (!LevelManager.Instance.LevelActive(newNode.Name))
                levelless = true;
        }


        if (!persistent || levelless)
        {
            newNode.AddToGroup(SNC.Groups[(int)Globals.Groups.NotPersistent]);
        }
        if (positionOverride != Vector3.Zero)
        {
            if (newNode is Node2D n2d)
                n2d.Position = new Vector2(positionOverride.X, positionOverride.Y);

            else if (newNode is Control c)
                c.Position = new Vector2(positionOverride.X, positionOverride.Y);
            else if (newNode is Node3D n3d)
                n3d.Position = positionOverride;
        }

        ApplyNextAvailableUniqueId(newNode);
        owner.AddChild(newNode, true);

        LevelManager.Instance.CheckAddNode(owner, newNode);
        AddNetworkNodes(newNode);

        ServerNodeAdded?.Invoke(newNode);

        JsonValue data = new JsonValue();

        data[_DataType].Set((int)Globals.DataType.ServerAdd);

        // need to collect all data about the node and send to clients
        data["Owner"].Set(owner.GetMeta(SNC.Meta[(int)Globals.Meta.UniqueId]).ToString());

        data["Node"].Set(ConvertNodeToJson(newNode));


        SendToClients(data);
    }

    public void RemoveServerNode(Node removeNode)
    {
        if (!NetworkManager.Instance.IsServer)
        {
            throw new Exception("ERROR: CLIENT IS TRYING TO CALL RemoveNode");
        }
        RemoveNetworkNodes(removeNode);

        JsonValue data = new JsonValue();
        data["UniqueId"].Set(removeNode.GetMeta(SNC.Meta[(int)Globals.Meta.UniqueId]).ToString());

        LevelManager.Instance.CheckRemoveNode(removeNode);

        ServerNodeRemoved?.Invoke(removeNode);

        SafeQueueFree(removeNode);
        // tell all clients to queue free this node
        data[_DataType].Set((int)Globals.DataType.ServerRemove);

        SendToClients(data);

    }

    #endregion

    #region Server
    private void OnSocketDataRecieved(JsonValue value)
    {
        Globals.DataType dataType = (Globals.DataType)value[_DataType].AsInt();

        switch (dataType)
        {
            case Globals.DataType.RpcCall:
                HandleRpc(value);
                break;
            case Globals.DataType.ClientInputUpdate:
                {
                    Player player = UniqueIdToNode(uint.Parse(value["O"].AsString())) as Player;
                    InputManager.Instance.HandleClientInputUpdate(player, value);
                }
                break;
            case Globals.DataType.RequestForceUpdate:
                ForceUpdateClients();
                break;
        }
    }

    protected JsonValue FullServerData()
    {
        JsonValue data = new JsonValue();
        data[_DataType].Set((int)Globals.DataType.FullServerData);

        foreach (Node child in Main.Instance.GetChildren())
        {
            JsonValue nodeData = ConvertNodeToJson(child);
            data["Nodes"].Append(nodeData);
            AddNodeToUniqueIdDict(child);
        }

        return data;
    }

    #endregion

    #region Client
    private void OnConnectionDataRecieved(JsonValue value)
    {
        Globals.DataType dataType = (Globals.DataType)value[_DataType].AsInt();

        switch (dataType)
        {
            case Globals.DataType.ServerUpdate:
                HandleServerUpdate(value);
                break;
            case Globals.DataType.RpcCall:
                HandleRpc(value);
                break;
            case Globals.DataType.ServerAdd:
                HandleServerAdd(value);
                break;
            case Globals.DataType.ServerRemove:
                // TODO: Add logic to find node of unique

                string uniqueIdStr = value["UniqueId"].AsString();
                uint uniqueId = uint.Parse(uniqueIdStr);
                Node removeNode = uniqueIdToNode[uniqueId];
                SafeQueueFree(removeNode);
                uniqueIdToNode.Remove(uniqueId);
                break;
            case Globals.DataType.FullServerData:
                HandleFullServerData(value);
                break;
        }
    }


    private void HandleServerAdd(JsonValue data)
    {
        if (!RecievedFullServerData)
            return;
        // Client does not know who owner is, so we'll ignore this add for now
        // currently an issue when player first joins
        if (!uniqueIdToNode.ContainsKey(Convert.ToUInt32(data["Owner"].AsString())))
        {
            GD.Print("HandleServerAdd: We dont know ID: " + data["Owner"].AsString());
            return;
        }

        string uniqueIdStr = data["Node"][_Meta][SNC.Meta[(int)Globals.Meta.UniqueId]].AsString();
        uint uId = Convert.ToUInt32(uniqueIdStr);
        if (uniqueIdToNode.ContainsKey(uId))
        {
            GD.Print("I already know about this node?");
            // client already knows about this object, dont add again
            return;
        }

        Node node = ConvertJsonToNode(data["Node"]);
        Node owner = uniqueIdToNode[Convert.ToUInt32(data["Owner"].AsString())];

        AddNodeToUniqueIdDict(node);

        owner.CallDeferred(_AddChild, node, true);
    }

    public void ClientInputUpdate(JsonValue inputData)
    {
        inputData["O"].Set(Helper.Instance.LocalPlayer.GetMeta(SNC.Meta[(int)Globals.Meta.UniqueId]).ToString());
       
        inputData[_DataType].Set((int)Globals.DataType.ClientInputUpdate);
        SendToServer(inputData);
    }

    private void HandleServerUpdate(JsonValue data)
    {
        if (!RecievedFullServerData) return;
        // first we verify we have all these nodes in our instance
        foreach (var item in data[_NetworkNodes].Object)
        {
            if (!uniqueIdToNode.ContainsKey(Convert.ToUInt32(item.Key)))
            {
                Node n = null;
                bool found = SearchForNode(item.Key, ref n, GetTree().Root);

                if (!found)
                    return;
                else
                    uniqueIdToNode[Convert.ToUInt32(item.Key)] = n;
            }
            
        }

        foreach (var item in data[_NetworkNodes].Object)
        {
            INetworkData n = uniqueIdToNode[Convert.ToUInt32(item.Key)] as INetworkData;

            if (n == null)
            {
                return;
            }
            n.DeserializeNetworkData(item.Value);
        }
    }


    private void HandleFullServerData(JsonValue data)
    {
        RecievedFullServerData = true;

        foreach (var item in data["Nodes"].Array)
        {
            Node node = ConvertJsonToNode(item);
            uint id = Convert.ToUInt32(node.GetMeta(SNC.Meta[(int)Globals.Meta.UniqueId]).ToString());

            /*
             * Steps:
             *  1) Determine if we already know about the node
             *      1a) If true, we need to check about its children
             *      1b) if false, add the node and move on
             */

            if (uniqueIdToNode.ContainsKey(id))
            {
                // recursively find
                FindAndAdd(UniqueIdToNode(id), node);
            }
            else
            {
                // from here we can assume this is a parent node in the main scene
                Main.Instance.AddChild(node);
                AddNodeToUniqueIdDict(node);
            }
        }
    }

    private void FindAndAdd(Node addTo, Node potentialAdd)
    {
        foreach (var child in potentialAdd.GetChildren())
        {
            uint id = Convert.ToUInt32(child.GetMeta(SNC.Meta[(int)Globals.Meta.UniqueId]).ToString());

            if (uniqueIdToNode.ContainsKey(id))
            {
                FindAndAdd(UniqueIdToNode(id), child);
            }
            else
            {
                Node n = child.Duplicate();
                addTo.AddChild(n);
                AddNodeToUniqueIdDict(n);
            }
        }
    }


    #endregion

    #region RPC
    public void RpcServer(Node caller, string methodName, params Variant[] param)
    {
        JsonValue message = new JsonValue();
        message[_DataType].Set((int)Globals.DataType.RpcCall);
        message["Caller"].Set(caller.GetMeta(SNC.Meta[(int)Globals.Meta.UniqueId]).AsString());
        message["MethodName"].Set(methodName);

        foreach (Variant variant in param)
        {
            message["Params"].Append(Helper.Instance.VariantToJson(variant));
        }

        SendToServer(message);
    }

    public void RpcClients(Node caller, string methodName, params Variant[] param)
    {
        JsonValue message = new JsonValue();
        message[_DataType].Set((int)Globals.DataType.RpcCall);
        message["Caller"].Set((string)caller.GetMeta(SNC.Meta[(int)Globals.Meta.UniqueId]));
        message["MethodName"].Set(methodName);

        foreach (Variant variant in param)
        {
            message["Params"].Append(Helper.Instance.VariantToJson(variant));
        }

        SendToClients(message);
    }

    public void RpcClient(ulong ownerId, Node caller, string methodName, params Variant[] param)
    {
        JsonValue message = new JsonValue();
        message[_DataType].Set((int)Globals.DataType.RpcCall);
        message["Caller"].Set((string)caller.GetMeta(SNC.Meta[(int)Globals.Meta.UniqueId]));
        message["MethodName"].Set(methodName);

        foreach (Variant variant in param)
        {
            message["Params"].Append(Helper.Instance.VariantToJson(variant));
        }

        SendToClientId(ownerId, message);
    }

    /// <summary>
    /// Server Handles an Rpc call
    /// </summary>
    private void HandleRpc(JsonValue value)
    {
        Node node = uniqueIdToNode[uint.Parse(value["Caller"].AsString())];
        if (!node.IsValid())
        {
            GD.Print("Server does not recognize node with ID: " + value["Caller"].AsString());
            return;
        }
        string methodName = value["MethodName"].AsString();

        List<Variant> args = new List<Variant>();
        foreach (JsonValue variant in value["Params"].Array)
        {
            args.Add(Helper.Instance.JsonToVariant(variant));
        }

        node.Call(methodName, args.ToArray());
    }
    #endregion


    #region HelperFunctions

    private void RequestForceUpdate()
    {
        JsonValue data = new JsonValue();
        data[_DataType].Set((int)Globals.DataType.RequestForceUpdate);
        SendToServer(data);
    }

    public void AddNodeToUniqueIdDict(Node node)
    {
        string uniqueStr = node.GetMeta(SNC.Meta[(int)Globals.Meta.UniqueId]).ToString();
        uint id = uint.Parse(uniqueStr);
        uniqueIdToNode[id] = node;
        foreach (Node n in node.GetChildren())
            AddNodeToUniqueIdDict(n);
    }
    private bool SearchForNode(string id, ref Node reference, Node searchPoint)
    {
        if (searchPoint.GetMeta(SNC.Meta[(int)Globals.Meta.UniqueId]).ToString() == id)
        {
            reference = searchPoint;
            return true;
        }
        foreach (Node child in searchPoint.GetChildren())
        {
            if (SearchForNode(id, ref reference, child))
                return true;
        }

        return false;
    }

    #endregion

    #region NodeJsonConversion
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
    private static readonly string _INetworkData = "IND";
    private static readonly StringName _AddChild = "add_child";

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
        
        if (node.IsInGroup(SNC.Groups[(int)Globals.Groups.SelfOnly]))
            return new JsonValue();
        if (node.GetParent().IsValid())
            if (node.GetParent().IsInGroup(SNC.Groups[(int)Globals.Groups.IgnoreChildren]) ||
                node.GetParent().IsInGroup(SNC.Groups[(int)Globals.Groups.IgnoreChildrenNetwork]))
                return new JsonValue();

        jsonNode[_Name].Set(node.Name);
        jsonNode[_Type].Set(RemoveNamespace(node.GetType().ToString()));
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

            jsonNode[_MinSize].Set(c.CustomMinimumSize);
            jsonNode[_LayoutMode].Set(c.LayoutMode);
            jsonNode[_LayoutDirection].Set((int)c.LayoutDirection);
            jsonNode[_AnchorLeft].Set(c.AnchorLeft);
            jsonNode[_AnchorRight].Set(c.AnchorRight);
            jsonNode[_AnchorBottom].Set(c.AnchorBottom);
            jsonNode[_AnchorTop].Set(c.AnchorTop);
            jsonNode[_ZIndex].Set(c.ZIndex);
            jsonNode[_ZIsRelative].Set(c.ZAsRelative);
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
            jsonNode[_Meta][meta].Set(node.GetMeta(meta).AsString());
        }
        foreach (string group in node.GetGroups())
            jsonNode[_Group].Append(group);

        for (int i = 0; i < node.GetChildCount(); i++)
            jsonNode[_Children].Append(CollectNodeData(node.GetChild(i)));

        if (node is INetworkData)
            jsonNode[_INetworkData].Set((node as INetworkData).SerializeNetworkData(true, true));

        return jsonNode;
    }

    public static Node ConvertJsonToNode(JsonValue data)
    {


        Node node = (Node)ClassDB.Instantiate(data[_DerivedType].AsString());

        // Set Basic Node Data
        node.Name = data[_Name].AsString();
        if (node is Node2D)
        {
            Node2D node2d = (Node2D)node;

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
            c.Rotation = data[_Rotation].AsFloat();
            c.Scale = data[_Scale].AsVector2();
            c.Size = data[_Size].AsVector2();

            c.ZAsRelative = data[_ZIsRelative].AsBool();
            c.ZIndex = data[_ZIndex].AsInt();
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
        else if (node is Node3D)
        {
            Node3D node3d = (Node3D)node;

            node3d.Position = data[_Position].AsVector3();
            node3d.Rotation = data[_Rotation].AsVector3();
            node3d.Scale = data[_Scale].AsVector3();

        }


        // Save node instance id to re-reference after setting script
        ulong nodeID = node.GetInstanceId();
        // if type != derived-type, a script is attached
        if (!data[_Type].AsString().Equals(data[_DerivedType].AsString()))
        {
            if (ReferenceManager.Instance.Scripts.ContainsKey(data[_Type].AsString()))
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

        if (node is INetworkData ind)
            ind.DeserializeNetworkData(data[_INetworkData]);


        return node;
    }


    private static string RemoveNamespace(string name)
    {
        int index = name.RFind(".");
        if (index < 0)
            return name;
        else
            return name.Substring(index + 1, name.Length - (index + 1));
    }

    #endregion

    public void HandleEvent(EventId id, object data, object owner)
    {
        switch (id)
        {
            case EventId.OnConnectionRecievedData:
                OnConnectionDataRecieved((JsonValue)data);
                break;
            case EventId.OnSocketRecievedData:
                OnSocketDataRecieved((JsonValue)data);
                break;
        }
    }

    public JsonValue SerializeNetworkData(bool forceReturn = false, bool ignoreThisUpdateOccurred = false)
    {
        if(!this.ShouldUpdate(forceReturn))
            return null;

        JsonValue data = new JsonValue();
        foreach(var add in playersAdded)
        {
            JsonValue d = new JsonValue();
            d[_OwnerID].Set(add.Item1.ToString());
            d[_UniqueID].Set(uint.Parse(add.Item2.GetMeta(SNC.Meta[(int)Globals.Meta.UniqueId]).ToString()));
            data[_Add].Append(d);
        }

        playersAdded.Clear();
        return this.CalculateNetworkReturn(data, ignoreThisUpdateOccurred);
    }

    public void DeserializeNetworkData(JsonValue data)
    {
        List<ulong> removals = new List<ulong>();
        foreach(var remove in OwnerIdToPlayer.Keys)
        {
            if (!OwnerIdToPlayer[remove].IsValid())
                removals.Add(remove);
        }
        foreach (var i in removals)
            RemoveOwnerIdToPlayer(i);

        foreach(var add in data[_Add].Array)
        {
            ulong ownerID = ulong.Parse(add[_OwnerID].AsString());
            Node n = UniqueIdToNode(add[_UniqueID].AsUInt());
            AddOwnerIdToPlayer(ownerID, n);
        }
    }
}
public interface INetworkData
{
    public bool NetworkUpdate { get; set; }
    public JsonValue SerializeNetworkData(bool forceReturn = false, bool ignoreThisUpdateOccurred = false);
    public void DeserializeNetworkData(JsonValue data);
}

public static class NetworkExtensions
{
    public static bool ShouldUpdate(this INetworkData data, bool forceUpdate)
    {
        if(forceUpdate) return true;
        if (data.NetworkUpdate) return true;
        return false;
    }

    public static JsonValue CalculateNetworkReturn(this INetworkData data, JsonValue newData, bool ignoreThisUpdateOccured)
    {
        if(newData == null) return null;

        if (!ignoreThisUpdateOccured)
        {
            data.NetworkUpdate = false;
        }
        return newData;
    }       

}