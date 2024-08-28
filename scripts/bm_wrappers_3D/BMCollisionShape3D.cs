using Godot;
using System;
using System.Collections.Generic;
[GlobalClass]
public partial class BMCollisionShape3D : CollisionShape3D, ISaveData
{
    private static readonly string _Shape = "SHP";
    private static readonly string _Radius = "RD";
    private static readonly string _Height = "HGT";
    private static readonly string _Size = "SZ";
    enum ShapeType
    {
        Null,
        Capsule,
        Sphere,
        Box
    }
    ShapeType CurrentShapeType {  get; set; }

    public override void _Ready()
    {
        SetShapeType();
    }     

    private void SetShapeType()
    {
        if(Shape is null)
            CurrentShapeType = ShapeType.Null;
        if (Shape is CapsuleShape3D)
            CurrentShapeType = ShapeType.Capsule;
        if (Shape is SphereShape3D)
            CurrentShapeType = ShapeType.Sphere;
        if (Shape is BoxShape3D)
            CurrentShapeType = ShapeType.Box;

    }

    public JsonValue SerializeSaveData()
    {
        JsonValue data = new JsonValue();

        if (CurrentShapeType == ShapeType.Null && Shape != null)
            SetShapeType();
        data[_Shape].Set(CurrentShapeType.ToString());

        if (Shape is CapsuleShape3D cs3)
        {
            data[_Radius].Set(cs3.Radius);
            data[_Height].Set(cs3.Height);
        }
        if (Shape is SphereShape3D ss3)
        {
            data[_Radius].Set(ss3.Radius);
        }
        if (Shape is BoxShape3D bs3)
        {
            data[_Size].Set(bs3.Size);
        }
        return data;
    }

    public void DeserializeSaveData(JsonValue data)
    {
        if (data[_Shape].AsString() == "Null") { return; }
        if (data[_Shape].AsString() != CurrentShapeType.ToString())
        { 
            if (data[_Shape].AsString() == ShapeType.Capsule.ToString())
                Shape = new CapsuleShape3D();
            if (data[_Shape].AsString() == ShapeType.Sphere.ToString())
                Shape = new SphereShape3D();
            if (data[_Shape].AsString() == ShapeType.Box.ToString())
                Shape = new BoxShape3D();
        }
        UpdateShape(data);
}
    private void UpdateShape(JsonValue data)
    {
        if (Shape is CapsuleShape3D cs3)
        {
            cs3.Radius = data[_Radius].AsFloat();
            cs3.Height = data[_Height].AsFloat();
        }
        if (Shape is SphereShape3D ss3)
        {
            ss3.Radius = data[_Radius].AsFloat();
        }
        if (Shape is BoxShape3D bs3)
            bs3.Size = data[_Size].AsVector3();
    }


}
