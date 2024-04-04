using Godot;

public partial class GameEntity : CharacterBody2D
{
    public double Health = 100;
    public double Damage;
    public bool IsDefending = false;
    private GameEntity _target;

    [ExportGroup("Moves")]
    [Export]
    public string[] moveNames = new string[3];
    [Export]
    public string[] moveTypes = new string[3];
    [Export]
    public double[] movePowers = new double[3];
    public bool HasMoved = false;

    public void UpdateDamage(GameEntity target, double damage)
    {
        _target = target;
        Damage = damage;
    }

    public async void Attack()
    {
        string userPcamPath = "";
        string targetPcamPath = "";

        switch (_target)
        {
            case Enemy:
                userPcamPath = "./PhantomCamera2D";
                targetPcamPath = "../EnemiesPhantomCamera2D";
                break;
            case player:
                userPcamPath = "../EnemiesPhantomCamera2D";
                targetPcamPath = "../Player/PhantomCamera2D";
                break;
        }

        const float tweenDur = .6f;

        GetNode<GodotObject>("../PhantomCamera2D").Call("set_priority", 0);

        GetNode<GodotObject>(targetPcamPath).Call("set_priority", 0);

        GetNode<GodotObject>(userPcamPath).Call("set_tween_transition", 10);
        GetNode<GodotObject>(userPcamPath).Call("set_tween_duration", tweenDur);
        GetNode<GodotObject>(userPcamPath).Call("set_priority", 10);

        await ToSignal(GetTree().CreateTimer(.3f), "timeout");

        GetNode<GodotObject>(userPcamPath).Call("set_priority", 0);

        GetNode<GodotObject>(targetPcamPath).Call("set_tween_transition", 10);
        GetNode<GodotObject>(targetPcamPath).Call("set_tween_duration", tweenDur);
        GetNode<GodotObject>(targetPcamPath).Call("set_priority", 10);

        await ToSignal(GetTree().CreateTimer(.8f), "timeout");

        if (!_target.IsDefending)
            _target.TakeDamage(Damage);
        else
        {
            _target.TakeDamage(Damage / 2);
            _target.IsDefending = false;
        }

        await ToSignal(GetTree().CreateTimer(1.2), "timeout");
        GetNode<battle>("/root/SceneChanger/Battle").CentralizeCamera();

        await ToSignal(GetTree().CreateTimer(1), "timeout");
        Global.NextTurn();
    }

    public void TakeDamage(double damage)
    {
        GetNode<camera>("../Camera2D").ApplyShake(40);
        Health -= damage;

        if (this is Enemy) GetNode<ProgressBar>("HealthBar").Value = Health;
        GetNode<AnimationPlayer>("AnimationPlayer").Play("hit");
        GetNode<GpuParticles2D>("BloodParticles").Emitting = true;
    }

    public void Heal(double damage)
    {
        Health = Mathf.Clamp(Health + damage, 0, 100);
        if (this is Enemy) GetNode<ProgressBar>("HealthBar").Value = Health;
    }
}
