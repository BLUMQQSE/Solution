using Godot;
using System;
[GlobalClass]
public partial class BMBoxContainer : BoxContainer, INetworkData
{
    public bool NetworkUpdate { get; set; } = true;

    private static readonly string _AlignmentMode = "AM";
    private static readonly string _Vertical = "V";

    public JsonValue SerializeNetworkData(bool forceReturn = false, bool ignoreThisUpdateOccurred = false)
    {
        if (!this.ShouldUpdate(forceReturn))
            return null;

        JsonValue data = new JsonValue();
        data[_AlignmentMode].Set((int)Alignment);
        data[_Vertical].Set(Vertical);
        return this.CalculateNetworkReturn(data, ignoreThisUpdateOccurred);
    }

    public void DeserializeNetworkData(JsonValue data)
    {
        Alignment = (AlignmentMode)data[_AlignmentMode].AsInt();
        Vertical = data[_Vertical].AsBool();
    }
}
