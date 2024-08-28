using Godot;
using System;
using System.Collections.Generic;

public partial class EventBus : Node
{
    private static EventBus instance = null;

    private EventBus()
    {
        AddToGroup(SNC.Groups[(int)Globals.Groups.AutoLoad]);
        if (instance == null)
            instance = this;
    }

    public static EventBus Instance { get { return instance; } }

    /// <summary>
    /// IMPORTANT! Make sure to UnsubscribeAll on _ExitTree.
    /// </summary>
    public void Subscribe(EventId id, IListener listener) 
    {
        if (AlreadyRegistered(id, listener))
            return;

        Instance.database.Add(id, listener);
    }
    public void Unsubscribe(EventId id, IListener listener) 
    {
        Instance.database.Remove(id, listener);
    }
    public void UnsubscribeAll(IListener listener)
    {
        foreach(EventId id in database.Keys)
        {
            Instance.database.Remove(id, listener);
        }
    }
    public void Invoke(EventId id, object owner)
    {
        DispatchEvent(id, null, owner);
    }
    public void Invoke(EventId id, object data, object owner) 
    {
        DispatchEvent(id, data, owner);
    }

    private bool AlreadyRegistered(EventId id, IListener listener)
    {
        return Instance.database.Contains(id, listener);
    }

    private void DispatchEvent(EventId id, object data, object caller)
    {
        List<IListener> list = Instance.database[id];
        foreach(IListener listener in list)
        { 
            listener.HandleEvent(id, data, caller);
        }
    }

    MultiMap<EventId, IListener> database = new MultiMap<EventId, IListener>();

}

public interface IListener
{
    public void HandleEvent(EventId id, object data, object caller);
}

public enum EventId
{
    OnConnectionRecievedData,
    OnSocketRecievedData,
    OnServerGameLoaded,
}