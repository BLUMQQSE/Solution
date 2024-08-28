using Godot;
using System;
using System.Collections.Generic;

public partial class Player : BMCharacter3D
{
    public override void _Ready()
    {
        base._Ready();

        Camera = GetNode("CameraHolder").GetChildOfType<BMCamera3D>();
        if (this.IsLocalOwned())
        {
            Helper.Instance.LocalPlayer = this;
            Camera.MakeCurrent();
        }
        Helper.Instance.AllPlayers.Add(this);

        if(NetworkManager.Instance.IsServer)
        {
            InputManager.Instance.SetInputState(InputStateType.Gameplay, this);
        }
    }


    public override void _ExitTree()
    {
        base._ExitTree();
        Helper.Instance.AllPlayers.Remove(this);
    }
}
