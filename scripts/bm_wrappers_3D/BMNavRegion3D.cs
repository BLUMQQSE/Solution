using Godot;
using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
[GlobalClass]
public partial class BMNavRegion3D : NavigationRegion3D, ISaveData
{
    private static readonly string _NavigationMesh = "NM";
    public override void _Ready()
    {
        base._Ready();
        if (!NetworkManager.Instance.IsServer) return;

        BakeNavigationMesh();
    }
    public JsonValue SerializeSaveData()
    {
        JsonValue data = new JsonValue();
        data[_NavigationMesh].Set(NavigationMesh.ResourcePath.RemovePathAndFileType());
        return data;
    }

    public void DeserializeSaveData(JsonValue data)
    {
        NavigationMesh nm = GD.Load<NavigationMesh>(ReferenceManager.Instance.GetResourcePath(data[_NavigationMesh].AsString()));

        //mapRid = NavigationServer3D.MapCreate();
        //NavigationServer3D.MapSetCellHeight(mapRid, nm.CellHeight);
        //NavigationServer3D.MapSetCellSize(mapRid, nm.CellSize);
        //NavigationServer3D.MapSetActive(mapRid, true);

        NavigationMesh = nm;

        //SetNavigationMap(mapRid);
        
    }
}