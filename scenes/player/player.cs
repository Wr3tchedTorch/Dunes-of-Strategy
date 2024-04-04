using System.Text.RegularExpressions;
using Godot;

public partial class player : GameEntity
{
    private Vector2 _dir;
    private const int _SPEED = 100;
    private float _gravity = 0.8f;
    private float ymove = 0;

    [ExportGroup("Physics")]
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
    public delegate void EnemyCollisionEventHandler(string overlappingEnemies);


    public override void _Ready()
    {
        _jumpVelocity = ((2.0f * JumpHeight) / JumpTimeToPeak) * -1;
        _jumpGravity = ((-2.0f * JumpHeight) / (JumpTimeToPeak * JumpTimeToPeak)) * -1;
        _fallGravity = ((-2.0f * JumpHeight) / (JumpTimeToDescent * JumpTimeToDescent)) * -1;

        GetNode<AnimatedSprite2D>("AnimatedSprite2D").Play("idle");
    }

    public override void _Process(double delta)
    {
        if (Name != "Player")
            Name = "Player";

        if (!Global.InBattle) return;
        if (Global.TurnIndex != 0) HasMoved = false;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Global.InBattle)
            return;

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

    public void OnHitboxBodyEntered(Node2D body)
    {
        if (body is not Enemy) return;
        else if (body is Enemy myEnemy && myEnemy.IsAlive == false) return;

        Godot.Collections.Array<Node2D> enemiesList = GetNode<Area2D>("OverlapRange").GetOverlappingBodies();
        int enemiesNumber = Mathf.Clamp(enemiesList.Count, 0, 3);

        string[] enemiesTypes = new string[enemiesNumber];
        for (int i = 0; i < enemiesNumber; i++)
        {
            Enemy enemy = (Enemy)enemiesList[i];
            enemy.IsAlive = false;
            enemiesTypes[i] = GetEnemyType(enemiesList[i]);
            GD.Print(GetEnemyType(enemiesList[i]));
        }

        Global.InBattle = true;
        EmitSignal(SignalName.EnemyCollision, enemiesTypes);
    }

    private string GetEnemyType(Node2D enemy) => Regex.Replace(enemy.Name, @"[\d-]", string.Empty).ToLower();

    public Godot.Collections.Dictionary<string, Variant> Save()
    {
        return new Godot.Collections.Dictionary<string, Variant>()
    {
        { "Filename", SceneFilePath },
        { "Parent", GetParent().GetPath() },
        { "PosX", Position.X }, // Vector2 is not supported by JSON
        { "PosY", Position.Y },
        {"Health", Health}
    };
    }
}

