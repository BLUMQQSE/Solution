using Godot;
using System;

public partial class AppManager : Node
{
	public event Action GameStarted;
    public event Action<double> AppUpdate;
    public event Action<double> AppFixedUpdate;


    private static AppManager instance;
	public static AppManager Instance { get { return instance; } }
	
	public AppManager() 
	{
        if (instance == null)
            instance = this;
        AddToGroup(SNC.Groups[(int)Globals.Groups.AutoLoad]);
    }

    public override void _Process(double delta)
    {
        AppUpdate?.Invoke(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        AppFixedUpdate?.Invoke(delta);
    }

    public void InvokeGameStart()
	{
		GameStarted?.Invoke();
	}

}
