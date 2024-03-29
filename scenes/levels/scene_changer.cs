using Godot;
using System;

public partial class scene_changer : Node2D
{
    private Node2D _currentWorld;
    private AnimationPlayer _anim;
    private Node2D _nextWorld;

    public override void _Ready()
    {
        _currentWorld = GetNode<Node2D>("Level");
        _anim = GetNode<AnimationPlayer>("AnimationPlayer");

        Callable callable = new Callable(this, "HandleWorldChange");
        _currentWorld.Connect("LevelChanged", callable);
    }

    private void HandleWorldChange(String currentWorldName)
    {
        String nextWorldName = "";

        switch (currentWorldName)
        {
            case "level": nextWorldName = "battle"; break;
            case "battle": nextWorldName = "level"; break;
        }

        PackedScene nextWorldScene = GD.Load<PackedScene>("res://scenes/levels/" + nextWorldName + ".tscn");
        _nextWorld = nextWorldScene.Instantiate<Node2D>();
        _nextWorld.ZIndex = -1;
        _anim.Play("fadeIn");
    }

    public void OnAnimationFinished(String _anim_name)
    {
        if (_anim_name != "fadeIn") return;
        
        _currentWorld.QueueFree();
        AddChild(_nextWorld);
        _currentWorld = _nextWorld;
        _currentWorld.ZIndex = 0;
        _nextWorld = null;
        _anim.Play("fadeOut");
    }
}
