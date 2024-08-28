using Godot;
using System;
[GlobalClass]
public partial class BMMeshInstance3D : MeshInstance3D, INetworkData, ISaveData
{
    
    public bool NetworkUpdate { get; set; } = true;
    private static readonly string _Mesh = "MSH";
    private static readonly string _Radius = "RAD";
    private static readonly string _Height = "HGT";
    private static readonly string _Size = "SZ";
    private static readonly string _Material = "MT";
    public JsonValue SerializeNetworkData(bool forceReturn, bool ignoreThisUpdateOccurred)
    {
        if (!this.ShouldUpdate(forceReturn))
            return null;
        JsonValue data = new JsonValue();

        //Mesh info
        if (Mesh != null)
        {
            if (Mesh is CapsuleMesh c)
            {
                data[_Mesh].Set("Capsule");
                data[_Radius].Set(c.Radius);
                data[_Height].Set(c.Height);
            }
            else if (Mesh is BoxMesh b)
            {
                data[_Mesh].Set("Box");
                data[_Size].Set(b.Size);
            }
            else if (Mesh is TextMesh)
                data[_Mesh].Set("Text");
            else if (Mesh is PlaneMesh)
                data[_Mesh].Set("Plane");
            else if (Mesh is SphereMesh s)
            {
                data[_Mesh].Set("Sphere");
                data[_Radius].Set(s.Radius);
            }
            else
                data[_Mesh].Set(Mesh.ResourcePath);
        }
        if(MaterialOverride  != null)
        data[_Material].Set(MaterialOverride.ResourcePath);


        return this.CalculateNetworkReturn(data, ignoreThisUpdateOccurred);
    }
    public void DeserializeNetworkData(JsonValue data)
    {
        if (data[_Mesh].IsValue)
        {
            ApplyMesh(data);
        }
        else
            Mesh = null;
        if (data[_Material].IsValue)
            MaterialOverride = GD.Load<Material>(data[_Material].AsString());
        else
            MaterialOverride = null;
    }


    private void ApplyMesh(JsonValue data)
    {
        string meshName = data[_Mesh].AsString();
        if (meshName == "Capsule")
        {
            Mesh = new CapsuleMesh();
            (Mesh as CapsuleMesh).Radius = data[_Radius].AsFloat();
            (Mesh as CapsuleMesh).Height = data[_Height].AsFloat();
        }
        else if (meshName == "Box")
        {
            Mesh = new BoxMesh();
            (Mesh as BoxMesh).Size = data[_Size].AsVector3();
        }
        else if (meshName == "Text")
            Mesh = new TextMesh();
        else if (meshName == "Sphere")
        {
            Mesh = new SphereMesh();
            (Mesh as SphereMesh).Radius = data[_Radius].AsFloat();
        }
        else if (meshName == "Plane")
            Mesh = new PlaneMesh();
        else
            Mesh = GD.Load<Mesh>(meshName);
    }

    public JsonValue SerializeSaveData()
    {
        JsonValue data = new JsonValue();
        if (Mesh != null)
        {
            if (Mesh is CapsuleMesh c)
            {
                data[_Mesh].Set("Capsule");
                data[_Radius].Set(c.Radius);
                data[_Height].Set(c.Height);
            }
            else if (Mesh is BoxMesh b)
            {
                data[_Mesh].Set("Box");
                data[_Size].Set(b.Size);
            }
            else if (Mesh is TextMesh)
                data[_Mesh].Set("Text");
            else if (Mesh is PlaneMesh)
                data[_Mesh].Set("Plane");
            else if (Mesh is SphereMesh s)
            {
                data[_Mesh].Set("Sphere");
                data[_Radius].Set(s.Radius);
            }
            else
                data[_Mesh].Set(Mesh.ResourcePath);
        }
        if (MaterialOverride != null)
            data[_Material].Set(MaterialOverride.ResourcePath);

        return data;
    }

    public void DeserializeSaveData(JsonValue data)
    {
        if (data[_Mesh].IsValue)
        {
            ApplyMesh(data);
        }
        else
            Mesh = null;
        if (data[_Material].IsValue)
            MaterialOverride = GD.Load<Material>(data[_Material].AsString());
        else
            MaterialOverride = null;
    }
}
