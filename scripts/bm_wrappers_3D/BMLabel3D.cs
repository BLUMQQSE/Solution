using Godot;
using System;
[GlobalClass]
public partial class BMLabel3D : Label3D, INetworkData, ISaveData
{
    public string LastDataSent {  get; set; }
    public bool NetworkUpdate { get; set; } = true;
    private static readonly string _Billboard = "BB";
    private static readonly string _Text = "TXT";
    private static readonly string _PixelSize = "PS";
    private static readonly string _FontSize = "FS";
    private static readonly string _OutlineSize = "OS";


    public JsonValue SerializeNetworkData(bool forceReturn, bool ignoreThisUpdateOccurred)
    {
        if (!this.ShouldUpdate(forceReturn))
            return null;
        JsonValue data = new JsonValue();

        data[_Billboard].Set((int)Billboard);
        data[_Text].Set(Text);
        data[_PixelSize].Set(PixelSize);
        data[_FontSize].Set(FontSize);
        data[_OutlineSize].Set(OutlineSize);

        return this.CalculateNetworkReturn(data, ignoreThisUpdateOccurred);
    }
    public void DeserializeNetworkData(JsonValue data)
    {
        Billboard = (BaseMaterial3D.BillboardModeEnum)data[_Billboard].AsInt();
        Text = data[_Text].AsString();
        PixelSize = data[_PixelSize].AsFloat();
        FontSize = data[_FontSize].AsInt();
        OutlineSize = data[_OutlineSize].AsInt();
    }

    public JsonValue SerializeSaveData()
    {
        JsonValue data = new JsonValue();

        data[_Billboard].Set((int)Billboard);
        data[_Text].Set(Text);
        data[_PixelSize].Set(PixelSize);
        data[_FontSize].Set(FontSize);
        data[_OutlineSize].Set(OutlineSize);

        return data;
    }

    public void DeserializeSaveData(JsonValue data)
    {
        Billboard = (BaseMaterial3D.BillboardModeEnum)data[_Billboard].AsInt();
        Text = data[_Text].AsString();
        PixelSize = data[_PixelSize].AsFloat();
        FontSize = data[_FontSize].AsInt();
        OutlineSize = data[_OutlineSize].AsInt();
    }
}
