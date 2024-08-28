using Godot;
using System;
using System.Collections.Generic;
[GlobalClass]
public partial class BMCamera3D : Camera3D, ISaveData, INetworkData
{
    public string LastDataSent {  get; set; }
    public bool NetworkUpdate { get; set; } = true;
    private static readonly string _Orthogonal = "OR";
    private static readonly string _Size = "SZ";
    private static readonly string _FOV = "FOV";
    private static readonly string _Far = "FAR";


    public JsonValue SerializeNetworkData(bool forceReturn = false, bool ignoreThisUpdateOccurred = false)
    {
        if (!this.ShouldUpdate(forceReturn))
            return null;

        JsonValue data = new JsonValue();

        data[_Orthogonal].Set(Projection == ProjectionType.Orthogonal);
        data[_Size].Set(Size);
        data[_FOV].Set(Fov);
        data[_Far].Set(Far);

        return this.CalculateNetworkReturn(data, ignoreThisUpdateOccurred);
    }

    public void DeserializeNetworkData(JsonValue data)
    {
        if (data[_Orthogonal].AsBool())
            Projection = ProjectionType.Orthogonal;

        Size = data[_Size].AsFloat();
        Fov = data[_FOV].AsFloat();
        Far = data[_Far].AsFloat();
    }

    public JsonValue SerializeSaveData()
    {
        JsonValue data = new JsonValue();

        data[_Orthogonal].Set(Projection == ProjectionType.Orthogonal);
        data[_Size].Set(Size);
        data[_FOV].Set(Fov);
        data[_Far].Set(Far);

        return data;
    }

    public void DeserializeSaveData(JsonValue data)
    {
        if(data[_Orthogonal].AsBool())
            Projection = ProjectionType.Orthogonal;

        Size = data[_Size].AsFloat();
        Fov = data[_FOV].AsFloat();
        Far = data[_Far].AsFloat();

    }

}
