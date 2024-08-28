using System;
using Godot;

public static class SNC
{
    public static readonly StringName X = new StringName("x");
    public static readonly StringName I = new StringName("i");
    public static readonly StringName Escape = new StringName("escape");
    public static readonly StringName Shift = new StringName("shift");
    public static readonly StringName Up = new StringName("up");
    public static readonly StringName Down = new StringName("down");
    public static readonly StringName Left = new StringName("left");
    public static readonly StringName Right = new StringName("right");
    public static readonly StringName Tilde = new StringName("~");

    public static readonly StringName[] Groups =
    {
        new StringName("Autoload"),
        new StringName("SelfOnly"),
        new StringName("NotPersistent"),
        new StringName("IgnoreChildren"),
        new StringName("IgnoreChildrenSave"),
        new StringName("IgnoreChildrenNetwork"),
        new StringName("Outside"),
        new StringName("Level")
    };

    public static readonly StringName[] Meta =
    {
        new StringName("UniqueId"),
        new StringName("OwnerId"),
        new StringName("LevelPartitionName"),
        new StringName("UniqueItemId")
    };

    public static readonly string[] SaveDest = 
    {
        "Level",
        "Player",
        "ECS",
        "Resource",
    };

}