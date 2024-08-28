using Godot;
using System;

public partial class Main : Node3D, INetworkData
{
	private static readonly string _SunRotation;
	private static Main instance;
	public static Main Instance {  get { return instance; } }

	public BMDirLight3D Sun { get; set; }
	private bool SunHidden = false;
	public bool NetworkUpdate { get; set; } = true;

    public Main() 
	{
		if(instance == null)
			instance = this;
	}
	public override void _Ready()
	{
        Sun = this.GetChildOfType<BMDirLight3D>();
        Sun.Visible = true;

		NetworkDataManager.Instance.AddNetworkNodes(this);

		MainMenu mainMenu = GD.Load<PackedScene>(ReferenceManager.Instance.GetScenePath("MainMenu")).Instantiate<MainMenu>();
		MenuManager.Instance.AddLocalMenu(mainMenu, true);

		LevelManager.Instance.PlayerChangeLevel += OnPlayerChangeLevel;
		TimeManager.Instance.GameTime.GameMinuteChanged += OnMinutePassed;
    }

    private void OnMinutePassed()
    {
		NetworkUpdate = true;
		Sun.RotateZ(Mathf.DegToRad(4));
    }

	// only called on server
	private void OnPlayerChangeLevel(Player player, Level node)
	{
		if (player != Helper.Instance.LocalPlayer)
		{

			return;
		}
		if (node.IsOutside)
		{
			if (player == Helper.Instance.LocalPlayer)
				ShowSun();
			else
				NetworkDataManager.Instance.RpcClient(player.GetOwnerId(), this, "ShowSun");
		}
		else
		{
            if (player == Helper.Instance.LocalPlayer)
                HideSun();
            else
                NetworkDataManager.Instance.RpcClient(player.GetOwnerId(), this, "HideSun");
        }
	}
	
	public void HideSun()
	{
		Sun.Visible = false;
		SunHidden = true;
	}
	public void ShowSun()
	{
		Sun.Visible = true;
		SunHidden = false;
	}

    public JsonValue SerializeNetworkData(bool forceReturn = false, bool ignoreThisUpdateOccurred = false)
    {
		if (!this.ShouldUpdate(forceReturn))
			return null;

		JsonValue data = new JsonValue();
		data[_SunRotation].Set(Sun.Rotation);

		return this.CalculateNetworkReturn(data, ignoreThisUpdateOccurred);
    }

    public void DeserializeNetworkData(JsonValue data)
    {
		Sun.Rotation = data[_SunRotation].AsVector3();
    }
}
