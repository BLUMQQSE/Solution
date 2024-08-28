using Godot;
using System;
using System.Collections.Generic;
using static System.Collections.Specialized.BitVector32;

public partial class InputManager : Node
{
    private static readonly string _Action = "A";
    private static readonly string _Mouse = "M";
    private static readonly string _MousePosition = "MP";
    private static readonly string _InputStateType = "IST";

    private static InputManager instance;
    public static InputManager Instance {  get { return instance; } }
    public InputManager()
    {
        if (instance == null)
            instance = this;
        AddToGroup(SNC.Groups[(int)Globals.Groups.AutoLoad]);
    }

    private InputState localInputState;
    private Dictionary<Player, InputState> inputs = new Dictionary<Player, InputState>();

    public override void _Ready()
    {
        base._Ready();
        
        localInputState = new InputState();

        ProcessPriority = 1000;
        foreach (var item in InputMap.GetActions())
        {
            if (!item.ToString().Contains("ui_"))
            {
                localInputState.ActionStates[item] = PressState.NotPressed;
            }
        }

        localInputState.MouseStates[MouseButton.Left] = PressState.NotPressed;
        localInputState.MouseStates[MouseButton.Middle] = PressState.NotPressed;
        localInputState.MouseStates[MouseButton.Right] = PressState.NotPressed;
        localInputState.InputStateType = InputStateType.UI;

        
        LevelManager.Instance.PlayerInstantiated += OnPlayerLoaded;
        LevelManager.Instance.PlayerTerminated += OnPlayerTerminated;

        MenuManager.Instance.MenuOpened += OnMenuOpened;
        MenuManager.Instance.MenuClosed += OnMenuClosed;
    }


    private void OnMenuOpened(Menu menu, Player player)
    {
        if (IsLocalPlayer(player))
        {
            localInputState.InputStateType = InputStateType.UI;
            ResetAllInput(null);
        }
        else
        {
            inputs[player].InputStateType = InputStateType.UI;
            ResetAllInput(player);
        }
    }


    private void OnMenuClosed(Menu menu, Player player)
    {
        if (MenuManager.Instance.IsInMenu(player))
            return;
        if (IsLocalPlayer(player))
        {
            if (localInputState.InputStateType == InputStateType.UI)
            {
                localInputState.InputStateType = InputStateType.Gameplay;
                ResetAllInput(null);
            }
        }
        else if (inputs[player].InputStateType == InputStateType.UI)
        {
            inputs[player].InputStateType = InputStateType.Gameplay;
            ResetAllInput(player);
        }
    }

    private void CreateInputState(Player player)
    {
        if(inputs.ContainsKey(player))
        {
            inputs.Remove(player);
        }
        InputState state = new InputState();
        foreach (var item in InputMap.GetActions())
        {
            if (!item.ToString().Contains("ui_"))
            {
                state.ActionStates[item] = PressState.NotPressed;
            }
        }

        state.MouseStates[MouseButton.Left] = PressState.NotPressed;
        state.MouseStates[MouseButton.Middle] = PressState.NotPressed;
        state.MouseStates[MouseButton.Right] = PressState.NotPressed;

        // NOTE: This requires input manager being instantiated AFTER Helper
        inputs.Add(player, state);
    }

    public override void _Process(double delta)
    {
        var change = localInputState.Update();
        localInputState.MousePosition = GetTree().Root.GetMousePosition();
        if (!NetworkManager.Instance.IsServer && change != InputUpdateType.None)
        {
            // we need to send update to server
            JsonValue inputData = new JsonValue();

            if (change == InputUpdateType.UpdateAll)
            {
                foreach (var action in localInputState.ActionStates)
                {
                    inputData[_Action][action.Key].Set((int)action.Value);
                }
                foreach (var mouse in localInputState.MouseStates)
                {
                    inputData[_Mouse][((int)mouse.Key).ToString()].Set((int)mouse.Value);
                }
                inputData[_InputStateType].Set((int)localInputState.InputStateType);
            }
            if(change == InputUpdateType.UpdateAll || change == InputUpdateType.UpdateMouse)
                inputData[_MousePosition].Set(localInputState.MousePosition);
            
            NetworkDataManager.Instance.ClientInputUpdate(inputData);
        }
        if (NetworkManager.Instance.IsServer)
        {
            foreach (var input in  inputs)
            {
                // need to update inputs to their next values
                input.Value.UpdateClientInput();
            }
        }
    }
    public void SetInputState(InputStateType type, Player player)
    {
        if(IsLocalPlayer(player))
            localInputState.InputStateType = type;
        else
            inputs[player].InputStateType = type;
        ResetAllInput(null);
    }
    public InputStateType GetInputState(Player player = null)
    {
        if (IsLocalPlayer(player))
            return localInputState.InputStateType;
        else
            return inputs[player].InputStateType;
    }
    public Vector2 GetMousePosition(Player player = null)
    {
        if (player == null)
            return localInputState.MousePosition;
        else if (player.IsLocalOwned())
            return localInputState.MousePosition;
        else if (inputs.ContainsKey(player))
            return inputs[player].MousePosition;

        GD.Print("[Error] Attempting to access input of non-registered player: ");
        return Vector2.Zero;
    }

    private bool CorrectState(InputStateType state, Player player)
    {
        if(player == null)
        {
            return localInputState.InputStateType == state;
        }
        else if (player.IsLocalOwned())
        {
            return localInputState.InputStateType == state;
        }
        return inputs[player].InputStateType == state;
    }
    private bool IsLocalPlayer(Player player)
    {
        if (player == null)
            return true;
        if (player.IsLocalOwned())
            return true;
        return false;
    }
    /// <summary>
    /// Pass null for player to reference local player
    /// </summary>
    public bool ActionJustPressed(StringName action, InputStateType inputType, Player player) 
    {
        if (!CorrectState(inputType, player))
            return false;

        if (player == null)
        {
            return localInputState.ActionStates[action] == PressState.JustPressed;
        }
        else if (player.IsLocalOwned())
        {
            return localInputState.ActionStates[action] == PressState.JustPressed;
        }
        else if (inputs.ContainsKey(player))
            return inputs[player].ActionStates[action] == PressState.JustPressed;

        GD.Print("[Error] Attempting to access input of non-registered player: ");
        return false;
    }
    /// <summary>
    /// Pass null for player to reference local player
    /// </summary>
    public bool ActionPressed(StringName action, InputStateType inputType, Player player) 
    {
        if (!CorrectState(inputType, player))
        {
            return false;
        }
        
        if (IsLocalPlayer(player)) 
            return localInputState.ActionStates[action] == PressState.Pressed;
        else if (inputs.ContainsKey(player))
            return inputs[player].ActionStates[action] == PressState.Pressed;

        GD.Print("[Error] Attempting to access input of non-registered player");
        return false;
    }
    /// <summary>
    /// Pass null for player to reference local player
    /// </summary>
    public bool ActionJustReleased(StringName action, InputStateType inputType, Player player) 
    {
        if (!CorrectState(inputType, player))
            return false;

        if (IsLocalPlayer(player))
            return localInputState.ActionStates[action] == PressState.JustReleased;
        else if (inputs.ContainsKey(player))
            return inputs[player].ActionStates[action] == PressState.JustReleased;

        GD.Print("[Error] Attempting to access input of non-registered player");
        return false;
    }
    /// <summary>
    /// Pass null for player to reference local player
    /// </summary>
    public bool MouseJustPressed(MouseButton mouse, InputStateType inputType, Player player) 
    {
        if (!CorrectState(inputType, player))
            return false;
        
        if (IsLocalPlayer(player))
            return localInputState.MouseStates[mouse] == PressState.JustPressed;
        else if (inputs.ContainsKey(player))
            return inputs[player].MouseStates[mouse] == PressState.JustPressed;

        GD.Print("[Error] Attempting to access input of non-registered player");
        return false;
    }
    /// <summary>
    /// Pass null for player to reference local player
    /// </summary>
    public bool MousePressed(MouseButton mouse, InputStateType inputType, Player player) 
    {
        if (!CorrectState(inputType, player))
            return false;

        if (IsLocalPlayer(player))
            return localInputState.MouseStates[mouse] == PressState.Pressed;
        else if (inputs.ContainsKey(player))
            return inputs[player].MouseStates[mouse] == PressState.Pressed;

        GD.Print("[Error] Attempting to access input of non-registered player");
        return false;
    }
    /// <summary>
    /// Pass null for player to reference local player
    /// </summary>
    public bool MouseJustReleased(MouseButton mouse, InputStateType inputType, Player player) 
    {
        if (!CorrectState(inputType, player))
            return false;

        if (IsLocalPlayer(player))
            return localInputState.MouseStates[mouse] == PressState.JustReleased;
        else if (inputs.ContainsKey(player))
            return inputs[player].MouseStates[mouse] == PressState.JustReleased;

        GD.Print("[Error] Attempting to access input of non-registered player");
        return false;
    }
    /// <summary>
    /// Pass null for player to reference local player
    /// </summary>
    public void ResetActionInput(Player player)
    {
        if (IsLocalPlayer(player))
            foreach (var action in localInputState.ActionStates.Keys)
                localInputState.ActionStates[action] = PressState.Reset;
        else if (inputs.ContainsKey(player))
            foreach (var action in inputs[player].ActionStates.Keys)
                inputs[player].ActionStates[action] = PressState.Reset;
        else
            GD.Print("[Error] Attempting to reset actions for non-registered player");
    }
    /// <summary>
    /// Pass null for player to reference local player
    /// </summary>
    public void ResetMouseInput(Player player)
    {
        if (IsLocalPlayer(player))
            foreach (var mouse in localInputState.MouseStates.Keys)
                localInputState.MouseStates[mouse] = PressState.Reset;
        else if (inputs.ContainsKey(player))
            foreach (var mouse in inputs[player].MouseStates.Keys)
                inputs[player].MouseStates[mouse] = PressState.Reset;
        else
            GD.Print("[Error] Attempting to reset mouse for non-registered player");
    }
    /// <summary>
    /// Pass null for player to reference local player
    /// </summary>
    public void ResetAllInput(Player player)
    {
        ResetActionInput(player);
        ResetMouseInput(player);
    }

    public void HandleClientInputUpdate(Player player, JsonValue data)
    {
        
        InputState input = inputs[player];
        foreach(var action in data[_Action].Object)
        {
            // here modify to ONLY set value if is JustPressed or JustReleased
            PressState s = (PressState)action.Value.AsInt();
            if(s == PressState.JustPressed || s == PressState.JustReleased)
                input.ActionStates[action.Key] = s;
        }
        foreach(var mouse in data[_Mouse].Object)
        {
            MouseButton m = (MouseButton)int.Parse(mouse.Key);
            PressState s = (PressState)mouse.Value.AsInt();
            if (s == PressState.JustPressed || s == PressState.JustReleased)
                input.MouseStates[m] = s;
        }

        if (data[_InputStateType].IsValue)
        {
            InputStateType ist = (InputStateType)data[_InputStateType].AsInt();
            if (ist == input.InputStateType)
                return;
            if (ist == InputStateType.UI)
            {
                input.InputStateType = InputStateType.UI;
                ResetAllInput(player);
            }
            else if (ist == InputStateType.Gameplay)
            {
                // first verify no server side UI is open (like inventory?)
                if (!MenuManager.Instance.IsInMenu(player))
                {
                    input.InputStateType = InputStateType.Gameplay;
                    ResetAllInput(player);
                }
            }


        }
        inputs[player].MousePosition = data[_MousePosition].AsVector2();
        inputs[player] = input;
    }

    private void OnPlayerLoaded(Player player)
    {
        if (player.IsLocalOwned())
            return;
        CreateInputState(player);
    }

    private void OnPlayerTerminated(Player player)
    {
        if (player.IsLocalOwned())
            return;

        inputs.Remove(player);
    }
}
public enum InputUpdateType
{
    None,
    UpdateAll,
    UpdateMouse
}
public enum InputStateType
{
    UI, 
    Gameplay
}
public class InputState
{
    public Dictionary<StringName, PressState> ActionStates { get; set; } = new Dictionary<StringName, PressState>();
    public Dictionary<MouseButton, PressState> MouseStates { get; set; } = new Dictionary<MouseButton, PressState>();
    public Vector2 MousePosition { get; set; } = new Vector2();
    public InputStateType InputStateType { get; set; }
    private InputStateType priorState { get; set; }

    public InputUpdateType Update()
    {
        bool change = false;
        bool updateAll = false;
        foreach (var key in ActionStates.Keys)
        {
            if (Input.IsActionPressed(key))
            {
                updateAll = true;
                if (ActionStates[key] == PressState.NotPressed)
                {
                    ActionStates[key] = PressState.JustPressed;
                    change = true;
                }
                else if (ActionStates[key] == PressState.JustPressed)
                {
                    ActionStates[key] = PressState.Pressed;
                    change = true;
                }
            }
            else
            {
                if (ActionStates[key] == PressState.Reset)
                {
                    ActionStates[key] = PressState.NotPressed;
                    change = true;
                }
                if (ActionStates[key] == PressState.JustPressed)
                {
                    ActionStates[key] = PressState.JustReleased;
                    change = true;
                }
                else if (ActionStates[key] == PressState.Pressed)
                {
                    ActionStates[key] = PressState.JustReleased;
                    change = true;
                }
                else if (ActionStates[key] == PressState.JustReleased)
                {
                    ActionStates[key] = PressState.NotPressed;
                    change = true;
                }
            }
        }

        foreach (var key in MouseStates.Keys)
        {
            if (Input.IsMouseButtonPressed(key))
            {
                updateAll = true;
                if (MouseStates[key] == PressState.NotPressed)
                {
                    MouseStates[key] = PressState.JustPressed;
                    change = true;
                }
                else if (MouseStates[key] == PressState.JustPressed)
                {
                    MouseStates[key] = PressState.Pressed;
                    change = true;
                }
            }
            else
            {
                if (MouseStates[key] == PressState.Reset)
                {
                    MouseStates[key] = PressState.NotPressed;
                    change = true;
                }
                if (MouseStates[key] == PressState.JustPressed)
                {
                    MouseStates[key] = PressState.JustReleased;
                    change = true;
                }
                else if (MouseStates[key] == PressState.Pressed)
                {
                    MouseStates[key] = PressState.JustReleased;
                    change = true;
                }
                else if (MouseStates[key] == PressState.JustReleased)
                {
                    MouseStates[key] = PressState.NotPressed;
                    change = true;
                }
            }
        }
        if(priorState != InputStateType)
        {
            updateAll = true;
            priorState = InputStateType;
        }

        if(change)
            return InputUpdateType.UpdateAll;
        if (updateAll)
            return InputUpdateType.UpdateMouse;
        return InputUpdateType.None;
    }

    public void UpdateClientInput()
    {
        foreach (var key in ActionStates.Keys)
        {
            if (ActionStates[key] == PressState.JustPressed)
                ActionStates[key] = PressState.Pressed;
            else if (ActionStates[key] == PressState.JustReleased)
                ActionStates[key] = PressState.NotPressed;
        }
        foreach (var key in MouseStates.Keys)
        {
            if (MouseStates[key] == PressState.JustPressed)
                MouseStates[key] = PressState.Pressed;
            else if (MouseStates[key] == PressState.JustReleased)
                MouseStates[key] = PressState.NotPressed;
        }
    }

}
public enum PressState
{
    NotPressed,
    JustPressed,
    Pressed,
    JustReleased,
    Reset
}