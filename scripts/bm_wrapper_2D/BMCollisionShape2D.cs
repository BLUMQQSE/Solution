using Godot;
using System;
[GlobalClass]
public partial class BMCollisionShape2D : CollisionShape2D, INetworkData
{
    public bool NetworkUpdate { get; set; } = true;

    private static readonly string _Shape = "S";
    private static readonly string _Radius = "R";
    private static readonly string _Height = "H";
    private static readonly string _Size = "SZ";

    enum ShapeType
    {
        Null,
        Capsule,
        Rectangle,
        Circle
    }
    ShapeType CurrentShapeType { get; set; } 
    public override void _Ready()
	{
        SetShapeType();
    }

    private void SetShapeType()
    {
        if (Shape is null)
            CurrentShapeType = ShapeType.Null;
        if (Shape is CapsuleShape2D)
            CurrentShapeType = ShapeType.Capsule;
        if (Shape is RectangleShape2D)
            CurrentShapeType = ShapeType.Rectangle;
        if (Shape is CircleShape2D)
            CurrentShapeType = ShapeType.Circle;

    }

    private void UpdateShape(JsonValue data)
    {
        if (Shape is CapsuleShape2D cs2)
        {
            cs2.Radius = data[_Radius].AsFloat();
            cs2.Height = data[_Height].AsFloat();
        }
        if (Shape is CircleShape2D cc2)
            cc2.Radius = data[_Radius].AsFloat();
        
        if (Shape is RectangleShape2D rs2)
            rs2.Size = data[_Size].AsVector2();
        
    }


    public JsonValue SerializeNetworkData(bool forceReturn, bool ignoreThisUpdateOccurred)
    {
        if (!this.ShouldUpdate(forceReturn))
            return null;

        JsonValue data = new JsonValue();

        if (CurrentShapeType == ShapeType.Null && Shape != null)
            SetShapeType();

        data[_Shape].Set(CurrentShapeType.ToString());

        if (Shape is CapsuleShape2D cs3)
        {
            data[_Radius].Set(cs3.Radius);
            data[_Height].Set(cs3.Height);
        }
        if (Shape is CircleShape2D ss3)
        {
            data[_Radius].Set(ss3.Radius);
        }
        if (Shape is RectangleShape2D bs3)
        {
            data[_Size].Set(bs3.Size);
        }

        return this.CalculateNetworkReturn(data, ignoreThisUpdateOccurred);
    }

    public void DeserializeNetworkData(JsonValue data)
    {
        if (data[_Shape].AsString() == "Null") { return; }
        if (data[_Shape].AsString() == CurrentShapeType.ToString())
        {
            // just updata
            UpdateShape(data);
        }
        else
        {
            if (data[_Shape].AsString() == ShapeType.Capsule.ToString())
                Shape = new CapsuleShape2D();
            if (data[_Shape].AsString() == ShapeType.Circle.ToString())
                Shape = new CircleShape2D();
            if (data[_Shape].AsString() == ShapeType.Rectangle.ToString())
                Shape = new RectangleShape2D();


            UpdateShape(data);
        }

    }
}
