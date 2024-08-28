using Godot;
using System;

[GlobalClass]
public partial class BMLabel2D : Label, INetworkData
{
    private static readonly string _Text = "T";
    private static readonly string _Size = "S";
    private static readonly string _VerticalAlignment = "VA";
    private static readonly string _HorizontalAlignment = "HA";
    public bool NetworkUpdate { get; set; } = true;

    public void SetText(string text)
    {
        Text = text;
        NetworkUpdate = true;
    }

    public JsonValue SerializeNetworkData(bool forceReturn, bool ignoreThisUpdateOccurred)
    {
        if (!this.ShouldUpdate(forceReturn))
            return null;
        JsonValue data = new JsonValue();

        data[_Text].Set(Text);
        data[_Size].Set(Size);
        data[_VerticalAlignment].Set((int)VerticalAlignment);
        data[_HorizontalAlignment].Set((int)HorizontalAlignment);

        return this.CalculateNetworkReturn(data, ignoreThisUpdateOccurred);

    }
    public void DeserializeNetworkData(JsonValue data)
    {
        Text = data[_Text].AsString();
        Size = data[_Size].AsVector2();
        HorizontalAlignment = (HorizontalAlignment)data[_HorizontalAlignment].AsInt();
        VerticalAlignment = (VerticalAlignment)data[_VerticalAlignment].AsInt();

    }
}
