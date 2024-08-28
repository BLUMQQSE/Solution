using Godot;
using System;
using System.Collections.Generic;

public partial class Helper : Node
{

    private static Helper instance;
    public static Helper Instance {  get { return instance; } }

    public Player LocalPlayer { get; set; }
    public List<Player> AllPlayers { get; set; } = new List<Player>();

    public override void _Ready()
    {
        base._Ready();
        AddToGroup(SNC.Groups[(int)Globals.Groups.AutoLoad]);
        if (instance == null)
            instance = this;
    }

    
    public Godot.Collections.Dictionary MouseRaycast(uint layersToDetect, bool includeAreas, Player player = null)
    {
        if (!LocalPlayer.IsValid() && player != null)
            return new Godot.Collections.Dictionary();

        if (player == null)
            player = LocalPlayer;
        
        var rayOrigin = player.Camera.ProjectRayOrigin(InputManager.Instance.GetMousePosition(player));
        var rayTarget = 
            player.Camera.ProjectRayNormal(InputManager.Instance.GetMousePosition(player)) * 1000f;
        var spaceState = player.Camera.GetWorld3D().DirectSpaceState;
        var rayParam = PhysicsRayQueryParameters3D.Create(rayOrigin, rayOrigin + rayTarget, layersToDetect);
        rayParam.CollideWithAreas = includeAreas;
        var result = spaceState.IntersectRay(rayParam);
        return result;
    }
    
    public Godot.Collections.Dictionary Raycast(World3D world, Vector3 rayOrigin, Vector3 rayTarget, uint layersToDetect,
        bool includeAreas)
    {
        var spaceState = world.DirectSpaceState;
        var rayParam = PhysicsRayQueryParameters3D.Create(rayOrigin, rayTarget, layersToDetect);
        rayParam.CollideWithAreas = includeAreas;
        var result = spaceState.IntersectRay(rayParam);
        return result;
    }

    public void Say(string thingToSay)
    {
        GetTree().Root.GetNode("DebugTts").Call("say", thingToSay);
    }


    /// <summary>
    /// Converts a JsonValue object to a Variant. The JsonValue must be in the same format
    /// as provided from JsonToVarient.
    /// </summary>
    public Variant JsonToVariant(JsonValue value)
    {
        Variant result = new Variant();

        Variant.Type type = StringToVariantType(value["Type"].AsString());

        if (type == Variant.Type.Int)
        {
            int num = Convert.ToInt32(value["Value"].AsString());
            result = num;
        }
        else if (type == Variant.Type.Float)
        {
            double num = Convert.ToDouble(value["Value"].AsString());
            result = num;
        }
        else if (type == Variant.Type.Bool)
        {
            bool boolVal = Convert.ToBoolean(value["Value"].AsString());
            result = boolVal;
        }
        else if (type == Variant.Type.String)
        {
            string strVal = Convert.ToString(value["Value"].AsString());
            result = strVal;
        }
        else if (type == Variant.Type.Vector2)
        {
            Vector2 vec = new Vector2();
            vec.X = (float)JsonToVariant(value["Value"]["X"]);
            vec.Y = (float)JsonToVariant(value["Value"]["Y"]);
            result = vec;
        }
        else if (type == Variant.Type.Vector3)
        {
            Vector3 vec = new Vector3();
            vec.X = (float)JsonToVariant(value["Value"]["X"]);
            vec.Y = (float)JsonToVariant(value["Value"]["Y"]);
            vec.Z = (float)JsonToVariant(value["Value"]["Z"]);
            result = vec;
        }
        else if (type == Variant.Type.Array)
        {
            Godot.Collections.Array ar = new Godot.Collections.Array();
            foreach (JsonValue arrayElement in value["ArrayElements"].Array)
            {
                ar.Add(JsonToVariant(arrayElement));
            }
            result = ar;
        }
        else if (type == Variant.Type.Dictionary)
        {
            Godot.Collections.Dictionary dict = new Godot.Collections.Dictionary();

            foreach (JsonValue dictElement in value["DictElements"].Array)
            {
                dict.Add(JsonToVariant(dictElement["Key"]),
                    JsonToVariant(dictElement["Value"]));
            }
            result = dict;
        }
        else if (type == Variant.Type.Object)
        {
            Node n = NetworkDataManager.Instance.UniqueIdToNode(Convert.ToUInt32(value["UniqueId"].AsString()));
            result = n;
        }

        return result;
    }
    /// <summary>
    /// Converts a Variant to a JsonValue object. 
    /// </summary>
    public JsonValue VariantToJson(Variant variant)
    {
        JsonValue result = new JsonValue();

        switch (variant.VariantType)
        {
            case Variant.Type.Int:
            case Variant.Type.Float:
            case Variant.Type.Bool:
            case Variant.Type.String:
                {
                    result["Type"].Set(VariantTypeToString(variant.VariantType));
                    result["Value"].Set(variant.ToString());
                }
                break;
            case Variant.Type.Vector2:
                {
                    result["Type"].Set(VariantTypeToString(Variant.Type.Vector2));
                    Vector2 vector = (Vector2)variant;
                    result["Value"]["X"]["Value"].Set(vector.X);
                    result["Value"]["X"]["Type"].Set(Variant.Type.Float.ToString());
                    result["Value"]["Y"]["Value"].Set(vector.Y);
                    result["Value"]["Y"]["Type"].Set(Variant.Type.Float.ToString());
                }
                break;
            case Variant.Type.Vector3:
                {
                    result["Type"].Set(VariantTypeToString(Variant.Type.Vector3));
                    Vector3 vector = (Vector3)variant;
                    result["Value"]["X"]["Value"].Set(vector.X);
                    result["Value"]["X"]["Type"].Set(Variant.Type.Float.ToString());
                    result["Value"]["Y"]["Value"].Set(vector.Y);
                    result["Value"]["Y"]["Type"].Set(Variant.Type.Float.ToString());
                    result["Value"]["Z"]["Value"].Set(vector.Z);
                    result["Value"]["Z"]["Type"].Set(Variant.Type.Float.ToString());
                }
                break;
            case Variant.Type.Array:
                {
                    result["Type"].Set(VariantTypeToString(Variant.Type.Array));
                    Godot.Collections.Array ar = (Godot.Collections.Array)variant;
                    foreach (Variant var in ar)
                        result["ArrayElements"].Append(VariantToJson(var));

                }
                break;
            case Variant.Type.Dictionary:
                {
                    result["Type"].Set(VariantTypeToString(Variant.Type.Dictionary));
                    Godot.Collections.Dictionary dict = (Godot.Collections.Dictionary)variant;
                    foreach (KeyValuePair<Variant, Variant> var in dict)
                    {
                        JsonValue dictPart = new JsonValue();
                        dictPart["Key"] = VariantToJson(var.Key);
                        dictPart["Value"] = VariantToJson(var.Value);
                        result["DictElements"].Append(dictPart);
                    }
                }
                break;
            case Variant.Type.Object:
                {
                    Node n = (Node)variant;
                    result["Type"].Set("Node");
                    result["UniqueId"].Set((string)n.GetMeta(SNC.Meta[(int)Globals.Meta.UniqueId]));
                }
                break;
        }

        return result;

    }
    private Variant.Type StringToVariantType(string str)
    {
        if (str == Variant.Type.Int.ToString())
            return Variant.Type.Int;
        if (str == Variant.Type.Float.ToString())
            return Variant.Type.Float;
        if (str == Variant.Type.Bool.ToString())
            return Variant.Type.Bool;
        if (str == Variant.Type.String.ToString())
            return Variant.Type.String;
        if (str == Variant.Type.Array.ToString())
            return Variant.Type.Array;
        if (str == Variant.Type.Dictionary.ToString())
            return Variant.Type.Dictionary;
        if (str == Variant.Type.Vector2.ToString())
            return Variant.Type.Vector2;
        if (str == Variant.Type.Vector3.ToString())
            return Variant.Type.Vector3;
        if (str == "Node")
            return Variant.Type.Object;

        return Variant.Type.Nil;
    }
    private string VariantTypeToString(Variant.Type type)
    {
        switch (type)
        {
            case Variant.Type.Int: return Variant.Type.Int.ToString();
            case Variant.Type.Float: return Variant.Type.Float.ToString();
            case Variant.Type.String: return Variant.Type.String.ToString();
            case Variant.Type.Bool: return Variant.Type.Bool.ToString();
            case Variant.Type.Vector2: return Variant.Type.Vector2.ToString();
            case Variant.Type.Vector3: return Variant.Type.Vector3.ToString();
            case Variant.Type.Dictionary: return Variant.Type.Dictionary.ToString();
            case Variant.Type.Array: return Variant.Type.Array.ToString();
            case Variant.Type.Object: return "Node";

        }
        return Variant.Type.Nil.ToString();
    }

}