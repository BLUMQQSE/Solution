using Godot;
using System;

[GlobalClass]
public partial class BMArea3D : Area3D, ISaveData
{
    private static readonly string _CollisionMask = "CM";
    private static readonly string _CollisionLayer = "CL";
    private static readonly string _Monitorable = "MB";
    private static readonly string _Monitoring = "MG";

    public JsonValue SerializeSaveData()
    {
        JsonValue data = new JsonValue();

        data[_CollisionMask].Set(CollisionMask);
        data[_CollisionLayer].Set(CollisionLayer);
        data[_Monitoring].Set(Monitoring);
        data[_Monitorable].Set(Monitorable);

        return data;
    }

    public void DeserializeSaveData(JsonValue data)
    {
        CollisionLayer = data[_CollisionLayer].AsUInt();
        CollisionMask = data[_CollisionMask].AsUInt();
        Monitorable = data[_Monitorable].AsBool();
        Monitoring = data[_Monitoring].AsBool();
    }
}
