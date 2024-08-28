using Godot;
using System;
[GlobalClass]
public partial class BMGridContainer : GridContainer, INetworkData
{
    public bool NetworkUpdate { get; set; } = true;

    private static readonly string _Columns = "C";

    public JsonValue SerializeNetworkData(bool forceReturn = false, bool ignoreThisUpdateOccurred = false)
    {
        if (!this.ShouldUpdate(forceReturn))
            return null;

        JsonValue data = new JsonValue();
        data[_Columns].Set(Columns);
        return this.CalculateNetworkReturn(data, ignoreThisUpdateOccurred);
    }

    public void DeserializeNetworkData(JsonValue data)
    {
        Columns = data[_Columns].AsInt();
    }
}
