using Godot;
using System;
[GlobalClass]
public partial class BMNavAgent3D : NavigationAgent3D, ISaveData
{
    
    private static readonly string _NavigationLayers = "NL";
    private static readonly string _PathDesiredDistance = "PDD";
    private static readonly string _TargetDesiredDistance = "TD";
    private static readonly string _PathMaxDistance = "PM";

    private static readonly string _DebugEnabled = "DE";
    private static readonly string _UseCustom = "UC";
    private static readonly string _PathCustomColor = "PCC";
    private static readonly string _PathSize = "PS";

    public Vector3 GetMovementDirection()
    {
        if (IsNavigationFinished())
            return Vector3.Zero;
        
        return GetParent<Node3D>().GlobalPosition.DirectionTo(GetNextPathPosition());
    }

    public JsonValue SerializeSaveData()
    {
        JsonValue data = new JsonValue();
        data[_NavigationLayers].Set(NavigationLayers);
        data[_PathDesiredDistance].Set(PathDesiredDistance);
        data[_TargetDesiredDistance].Set(TargetDesiredDistance);
        data[_PathMaxDistance].Set(PathMaxDistance);
        
        data[_DebugEnabled].Set(DebugEnabled);
        data[_UseCustom].Set(DebugUseCustom);
        data[_PathCustomColor].Set(new Vector3(DebugPathCustomColor.R, DebugPathCustomColor.G, DebugPathCustomColor.B));
        data[_PathSize].Set(DebugPathCustomPointSize);

        return data;
    }

    public void DeserializeSaveData(JsonValue data)
    {
        NavigationLayers = data[_NavigationLayers].AsUInt();
        PathDesiredDistance = data[_PathDesiredDistance].AsFloat();
        TargetDesiredDistance = data[_TargetDesiredDistance].AsFloat();
        PathMaxDistance = data[_PathMaxDistance].AsFloat();

        DebugEnabled = data[_DebugEnabled].AsBool();
        DebugUseCustom = data[_UseCustom].AsBool();
        Vector3 x = data[_PathCustomColor].AsVector3();
        Color c = new Color(x.X,x.Y,x.Z);
        DebugPathCustomColor = c;
        DebugPathCustomPointSize = data[_PathSize].AsFloat();
    }
}
