using Godot;
using System;
[GlobalClass]
public partial class BMStaticBody3D : StaticBody3D, ISaveData
{
    

    private static readonly string _CollisionMask = "CM";
    private static readonly string _CollisionLayer = "CL";


    public JsonValue SerializeSaveData()
    {
        JsonValue data = new JsonValue();

        data[_CollisionMask].Set(CollisionMask);
        data[_CollisionLayer].Set(CollisionLayer);

        return data;
    }


    public void DeserializeSaveData(JsonValue data)
    {
        CollisionMask = data[_CollisionMask].AsUInt();
        CollisionLayer = data[_CollisionLayer].AsUInt();
    }

}
