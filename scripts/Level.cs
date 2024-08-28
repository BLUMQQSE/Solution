using Godot;
using System;
[GlobalClass]
public partial class Level : Node3D, ISaveData, INetworkData
{
    private static readonly string _IsOutside = "O";


	[Export] public bool IsOutside = false;

    public bool NetworkUpdate { get; set; } = true;

    public JsonValue SerializeNetworkData(bool forceReturn = false, bool ignoreThisUpdateOccurred = false)
    {
        if (!this.ShouldUpdate(forceReturn))
            return null;

        JsonValue data = new JsonValue();
        data[_IsOutside].Set(IsOutside);
        return this.CalculateNetworkReturn(data, ignoreThisUpdateOccurred);
    }

    public void DeserializeNetworkData(JsonValue data)
    {
        IsOutside = data[_IsOutside].AsBool();
    }
    public JsonValue SerializeSaveData()
    {
        JsonValue data = new JsonValue();
        data[_IsOutside].Set(IsOutside);
        return data;
    }

    public void DeserializeSaveData(JsonValue data)
    {
        IsOutside = data[_IsOutside].AsBool();
    }

}
