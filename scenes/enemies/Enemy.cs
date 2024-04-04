using Godot;
using System;

public partial class Enemy : GameEntity
{
    private Random rng;
    public player _target;
    public bool IsAlive = true;
    public int EnemyIndex = -1;
    private bool _isHovering = false;
    [Signal]
    public delegate void EnemySelectedEventHandler(int index);
    public override void _Ready()
    {
        GetNode<ProgressBar>("HealthBar").Value = Health;
        rng = new Random();
    }

    public override void _Process(double delta)
    {
        if (Health <= 0)
        {
            GetNode<AnimationPlayer>("AnimationPlayer").Play("death");
            return;
        }

        if (!Global.InBattle)
        {
            GetNode<ProgressBar>("HealthBar").Visible = false;
            return;
        }

        GetNode<ProgressBar>("HealthBar").Visible = true;

        if (_isHovering && Input.IsActionPressed("click") && Global.TurnIndex == 0)
        {            
            EmitSignal(SignalName.EnemySelected, EnemyIndex);
        }

        if (Global.TurnIndex != EnemyIndex + 1) HasMoved = false;

        if (Global.TurnIndex == EnemyIndex + 1 && !HasMoved)
        {
            ChooseNextMove();
            HasMoved = true;
        }
    }

    private void ChooseNextMove()
    {
        int moveIndex = (int)Math.Round(rng.NextDouble() * (moveNames.Length - 1));
        GetNode<battle>("/root/SceneChanger/Battle").UseMove(this, moveTypes[moveIndex], movePowers[moveIndex], GetNode<GameEntity>("../Player"));
    }

    public void SelfDestroy()
    {
        QueueFree();
        if (Global.InBattle) GetNode<battle>("/root/SceneChanger/Battle").EnemiesLeft--;
    }

    public void OnAnimatedSprite2DAnimationFinished()
    {
        Attack();
        GetNode<AnimatedSprite2D>("AnimatedSprite2D").Play("flying");
    }

    public void OnMouseEntered()
    {
        GD.Print("hover");
        _isHovering = true;
    }
    public void OnMouseExited()
    {
        GD.Print("hover exited");
        _isHovering = false;
    }
    public Godot.Collections.Dictionary<string, Variant> Save()
    {
        return new Godot.Collections.Dictionary<string, Variant>()
    {
        { "Filename", SceneFilePath },
        { "Parent", GetParent().GetPath() },
        { "PosX", Position.X }, // Vector2 is not supported by JSON
        { "PosY", Position.Y },
        { "IsAlive", IsAlive },
    };
    }
}
