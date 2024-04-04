using Godot;
using System;

public partial class FlyingEnemy : Enemy
{
    private CharacterBody2D _player;
    [Export]
    public int _speed = 130;

    public override void _Ready()
    {
        base._Ready();

        GetNode<AnimatedSprite2D>("AnimatedSprite2D").Play("flying");
        if (Global.InBattle) return;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsAlive) return;

        if (Global.InBattle || !IsInstanceValid(GetNodeOrNull<CharacterBody2D>("/root/SceneChanger/Level1/Player")) || !IsAlive) return;

        _player = GetNode<CharacterBody2D>("/root/SceneChanger/Level1/Player");
        Vector2 dir = GlobalPosition.DirectionTo(_player.GlobalPosition);
        GlobalPosition += dir * _speed * (float)delta;
    }
}
