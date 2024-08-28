using Godot;
using System;

public partial class NetworkTransform : Node, INetworkData
{
    private Vector3 SyncPos;
    private Vector3 SyncRot;

    public bool NetworkUpdate { get; set; }

    public override void _Process(double delta)
    {
        base._Process(delta);
        NetworkUpdate = true;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (!NetworkManager.Instance.IsServer)
        {
            if (GetParent<Node3D>().Position.DistanceSquaredTo(SyncPos) > Mathf.Pow(10, 2)) //just transport
                GetParent<Node3D>().Position = NetworkTransformComponent.SyncPos;
            else
                GetParent<Node3D>().Position = GetParent<Node3D>().Position.Lerp(SyncPos, 5 * (float)delta);
            GetParent<Node3D>().Rotation = SyncRot;
        }
        else
        {
            SyncPos = GetParent<Node3D>().Position;
            SyncRot = GetParent<Node3D>().Rotation;
        }
    }

    public JsonValue SerializeNetworkData(bool forceReturn = false, bool ignoreThisUpdateOccurred = false)
    {
        if (!this.ShouldUpdate(forceReturn))
            return null;

        JsonValue data = new JsonValue();
        data["P"].Set(SyncPos);
        data["R"].Set(SyncRot);

        return this.CalculateNetworkReturn(data, ignoreThisUpdateOccurred);
    }
    public void DeserializeNetworkData(JsonValue data)
    {
        SyncPos = data["P"].AsVector3();
        SyncRot = data["R"].AsVector3();
    }

}
