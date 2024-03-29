using Godot;
using System;

public partial class player : CharacterBody2D
{
    private const int _SPEED = 100;
    private Vector2 _dir;
    private float _gravity = 0.8f;
    private float ymove = 0;

    [Export]
    public float JumpHeight;
    [Export]
    public float JumpTimeToPeak;
    [Export]
    public float JumpTimeToDescent;
    private float _jumpVelocity;
    private float _jumpGravity;
    private float _fallGravity;

    [Signal]
    public delegate void EnemyCollisionEventHandler();

    public override void _Ready()
    {
        _jumpVelocity = ((2.0f * JumpHeight) / JumpTimeToPeak) * -1;
        _jumpGravity = ((-2.0f * JumpHeight) / (JumpTimeToPeak * JumpTimeToPeak)) * -1;
        _fallGravity = ((-2.0f * JumpHeight) / (JumpTimeToDescent * JumpTimeToDescent)) * -1;
    }

    public override void _PhysicsProcess(double delta)
    {
        SetPlayerAnimation();

        _dir.Y += GetGravity() * (float)delta;
        _dir.X = GetInputVelocity();

        if (IsOnFloor())
        {
            _dir.Y = 0;
            if (Input.IsActionPressed("jump")) Jump();
        }

        if (IsOnCeiling()) _dir.Y += _fallGravity * (float)delta;

        _dir.X = _dir.X * _SPEED;
        Velocity = _dir;
        MoveAndSlide();
    }

    private void SetPlayerAnimation()
    {
        var anim = GetNode<AnimatedSprite2D>("AnimatedSprite2D");

        int xVelocity = GetInputVelocity();
        if (xVelocity == 0)
        {
            anim.Play("idle");
            return;
        }

        anim.Play("walk");
        if (xVelocity == -1) anim.FlipH = true;
        else anim.FlipH = false;
    }

    private int GetInputVelocity()
    {
        if (Input.IsActionPressed("left")) return -1;
        else if (Input.IsActionPressed("right")) return 1;
        return 0;
    }

    private float GetGravity()
    {
        if (_dir.Y < 0f) return _jumpGravity;
        else return _fallGravity;
    }

    private void Jump() => _dir.Y = _jumpVelocity;

    public void OnHitboxAreaEntered(Area2D area)
    {
        if (area.IsInGroup("enemies")) EmitSignal(SignalName.EnemyCollision);        
    }
}

