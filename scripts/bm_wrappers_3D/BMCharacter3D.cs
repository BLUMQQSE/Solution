using Godot;
using System;
using System.Diagnostics;

[GlobalClass]
public partial class BMCharacter3D : CharacterBody3D, ISaveData
{
    private static readonly string _CollisionLayer = "CL";
    private static readonly string _CollisionMask = "CM";
    private static readonly string _Speed = "S";
    [Export]public float Speed { get; set; } = 5.0f;

	private float gravity = 9.8f;
	public NetworkTransform NetworkTransform { get; protected set; }
    Vector3 Direction {  get; set; }
	bool IsPlayer = false;
	Vector3 PrevPos { get; set; }
	Vector3 PrevRot { get; set; }
    public override void _Ready()
    {
        base._Ready();
		if (HasMeta(SNC.Meta[(int)Globals.Meta.OwnerId]))
            IsPlayer = true;
		PrevPos = Position;

        NetworkTransform = this.GetChildOfType<NetworkTransform>();

    }

    public override void _PhysicsProcess(double delta)
	{
		if (NetworkManager.Instance.IsServer)
		{
            Vector3 vel = new Vector3();

            if (Direction != Vector3.Zero)
            {
                vel.X = Direction.X * Speed * 100;
                vel.Y = Velocity.Y;
                vel.Z = Direction.Z * Speed * 100;
            }
            else
            {
                vel.X = 0;
                vel.Y = Velocity.Y;
                vel.Z = 0;

            }
            // we multiply by delta because this delta includes our GameTime timescale
            // without this GameTimeScale wouldn't affect movement
            vel *= (float)delta;
            Velocity = vel;
            MoveAndSlide();
        }
		
		Direction = Vector3.Zero;
	}

	/// <summary>
	/// Moves the character with relation to their transform basis.
	/// </summary>
	public void Move(Vector2 direction)
	{
        Move(new Vector3(direction.X, 0, direction.Y));
    }
    public void Move(Vector3 direction)
    {
        Direction = direction.Normalized();
    }

    public JsonValue SerializeSaveData()
    {
        JsonValue data = new JsonValue();

        data[_CollisionLayer].Set(CollisionLayer);
        data[_CollisionMask].Set(CollisionMask);
        data[_Speed].Set(Speed);

		return data;
    }
	
    public void DeserializeSaveData(JsonValue data)
    {
		CollisionLayer = data[_CollisionLayer].AsUInt();
		CollisionMask = data[_CollisionMask].AsUInt();
        Speed = data[_Speed].AsFloat();
    }
}
