using Godot;
using System;

public partial class BMScrollContainer : ScrollContainer, INetworkData
{
	private static readonly string _HorizontalScroll = "HS";
    public bool NetworkUpdate { get; set; } = true;

    public JsonValue SerializeNetworkData(bool forceReturn = false, bool ignoreThisUpdateOccurred = false)
    {
        if (!this.ShouldUpdate(forceReturn))
            return null;
        
        JsonValue data = new JsonValue();
        data[_HorizontalScroll].Set((int)HorizontalScrollMode);
        return data;
    }
    public void DeserializeNetworkData(JsonValue data)
    {
        HorizontalScrollMode = (ScrollMode)data[_HorizontalScroll].AsInt();
    }

}
