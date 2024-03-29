using Godot;
using System;

public partial class FlyingEnemy : Area2D
{
    private CharacterBody2D _player;
    private int _speed = 100;

    public override void _Ready() {
        GetNode<AnimatedSprite2D>("AnimatedSprite2D").Play("flying");
        _player = GetNode<CharacterBody2D>("../../Player");
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 dir = GlobalPosition.DirectionTo(_player.GlobalPosition);
        GlobalPosition += dir * _speed * (float)delta;
    }
}
