using Godot;
using System;
[GlobalClass]
public partial class BMDirLight3D : DirectionalLight3D, INetworkData, ISaveData
{
    public string LastDataSent {  get; set; }
    public bool NetworkUpdate { get; set; } = true;
    private static readonly string _LightColor = "LC";
    private static readonly string _LightColorAlpha = "LCA";
    private static readonly string _LightEnergy = "LE";
    private static readonly string _ShadowEnabled = "SWE";
    public JsonValue SerializeNetworkData(bool forceReturn, bool ignoreThisUpdateOccurred)
    {
        if (!this.ShouldUpdate(forceReturn))
            return null;
        JsonValue data = new JsonValue();

        data[_LightColor].Set(new Vector3(LightColor.R, LightColor.G, LightColor.B));
        data[_LightColorAlpha].Set(LightColor.A);
        data[_LightEnergy].Set(LightEnergy);
        data[_ShadowEnabled].Set(ShadowEnabled);

        return this.CalculateNetworkReturn(data, ignoreThisUpdateOccurred);
    }

    public void DeserializeNetworkData(JsonValue data)
    {
        LightColor = new Color(data[_LightColor][0].AsFloat(), data[_LightColor][1].AsFloat(),
            data[_LightColor][2].AsFloat(), data[_LightColorAlpha].AsFloat());
        LightEnergy = data[_LightEnergy].AsFloat();
        ShadowEnabled = data[_ShadowEnabled].AsBool();
    }
    public JsonValue SerializeSaveData()
    {
        JsonValue data = new JsonValue();

        data[_LightColor].Set(new Vector3(LightColor.R, LightColor.G, LightColor.B));
        data[_LightColorAlpha].Set(LightColor.A);
        data[_LightEnergy].Set(LightEnergy);
        data[_ShadowEnabled].Set(ShadowEnabled);

        return data;
    }

    public void DeserializeSaveData(JsonValue data)
    {
        LightColor = new Color(data[_LightColor][0].AsFloat(), data[_LightColor][1].AsFloat(), 
            data[_LightColor][2].AsFloat(), data[_LightColorAlpha].AsFloat());
        LightEnergy = data[_LightEnergy].AsFloat();
        ShadowEnabled = data[_ShadowEnabled].AsBool();
    }

}
