using Godot;
using System;

public partial class BMRayCast3D : RayCast3D, INetworkData, ISaveData
{
    public bool NetworkUpdate { get; set; } = true;

    private static readonly string _CollisionMask = "CM";
    private static readonly string _TargetPosition = "TP";
    private static readonly string _CollideWithAreas = "A";
    private static readonly string _CollideWithBodies = "B";
    private static readonly string _HitFromInside = "HI";
    public JsonValue SerializeNetworkData(bool forceReturn, bool ignoreThisUpdateOccurred)
    {
        if (!this.ShouldUpdate(forceReturn))
            return null;

        JsonValue data = new JsonValue();

        data[_CollisionMask].Set(CollisionMask);
        data[_TargetPosition].Set(TargetPosition); 
        data[_CollideWithAreas].Set(CollideWithAreas);
        data[_CollideWithBodies].Set(CollideWithBodies); 
        data[_HitFromInside].Set(HitFromInside);

        return this.CalculateNetworkReturn(data, ignoreThisUpdateOccurred);
    }
    public void DeserializeNetworkData(JsonValue data)
    {
        CollisionMask = data[_CollisionMask].AsUInt();
        TargetPosition = data[_TargetPosition].AsVector3();
        CollideWithBodies = data[_CollideWithBodies].AsBool();
        CollideWithAreas = data[_CollideWithAreas].AsBool();
        HitFromInside = data[_HitFromInside].AsBool();
    }

    public JsonValue SerializeSaveData()
    {
        JsonValue data = new JsonValue();

        data[_CollisionMask].Set(CollisionMask);
        data[_TargetPosition].Set(TargetPosition);
        data[_CollideWithAreas].Set(CollideWithAreas);
        data[_CollideWithBodies].Set(CollideWithBodies);
        data[_HitFromInside].Set(HitFromInside);

        return data;
    }

    public void DeserializeSaveData(JsonValue data)
    {
        CollisionMask = data[_CollisionMask].AsUInt();
        TargetPosition = data[_TargetPosition].AsVector3();
        CollideWithBodies = data[_CollideWithBodies].AsBool();
        CollideWithAreas = data[_CollideWithAreas].AsBool();
        HitFromInside = data[_HitFromInside].AsBool();
    }

}
