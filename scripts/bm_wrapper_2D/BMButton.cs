using Godot;
using System;

[GlobalClass]
public partial class BMButton : Button, INetworkData
{
    private static readonly string _Text = "TX";
    private static readonly string _ToggleMode = "TM";
    private static readonly string _ActionMode = "AM";
    private static readonly string _ButtonMask = "BM";
    private static readonly string _Alignment = "AL";

    private static readonly string _OnPressed = "OnPress";
    private static readonly string _ButtonToggled = "OnToggled";

    public event Action<BMButton> Clicked;
    public event Action<BMButton> ButtonToggled;

    public Menu ParentMenu { get; private set; }

    public bool NetworkUpdate { get; set; } = true;

    public override void _Ready()
    {
        base._Ready();

        ParentMenu = this.FindParentOfType<Menu>();

        Pressed += OnPress;
        Toggled += OnToggled;
    }

    public void SetText(string text)
    {
        Text = text;
        NetworkUpdate = true;
    }

    private void OnPress()
    {
        Clicked?.Invoke(this);
        EventBus.Instance.Invoke(EventId.BMButton_Clicked, this);

        InputManager.Instance.ResetMouseInput(ParentMenu.OwningPlayer);
        if (!NetworkManager.Instance.IsServer)
            NetworkDataManager.Instance.RpcServer(this, _OnPressed);
    }

    private void OnToggled(bool toggledOn)
    {
        ButtonToggled?.Invoke(this);
        EventBus.Instance.Invoke(EventId.BMButton_ButtonToggled, this);

        InputManager.Instance.ResetMouseInput(ParentMenu.OwningPlayer);
        if (!NetworkManager.Instance.IsServer)
            NetworkDataManager.Instance.RpcServer(this, _ButtonToggled);
    }

    public JsonValue SerializeNetworkData(bool forceReturn, bool ignoreThisUpdateOccurred)
    {
        if (!this.ShouldUpdate(forceReturn))
            return null;
        JsonValue data = new JsonValue();

        data[_Text].Set(Text);
        data[_ToggleMode].Set(ToggleMode);
        data[_ActionMode].Set((int)ActionMode);
        data[_ButtonMask].Set((int)ButtonMask);
        data[_Alignment].Set((int)Alignment);

        return this.CalculateNetworkReturn(data, ignoreThisUpdateOccurred);
    }

    public void DeserializeNetworkData(JsonValue data)
    {
        Text = data[_Text].AsString();
        ToggleMode = data[_ToggleMode].AsBool();
        ActionMode = (ActionModeEnum)data[_ActionMode].AsInt();
        ButtonMask = (MouseButtonMask)data[_ButtonMask].AsInt();
        Alignment = (HorizontalAlignment)data[_Alignment].AsInt();
    }

}
