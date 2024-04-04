using Godot;

public partial class level : Node2D
{
    [Signal]
    public delegate void LevelChangedEventHandler(string currentWorldName, string[] area);
    [Export]
    public string CurrentWorldName = "level1";
    player myPlayer;

    public override void _Ready()
    {
        GetTree().Paused = false;
        Global.InBattle = false;
        myPlayer = GetNode<player>("./Player");
    }

    public override void _Process(double delta)
    {
        if (Global.InBattle) return;
        if (Input.IsActionPressed("reset")) ResetGame();

        GetNode<GodotObject>("PhantomCamera2D").Call("set_follow_target_node", this);
        GetNode<GodotObject>("PhantomCamera2DZoom").Call("set_follow_target_node", this);

        if (!IsInstanceValid(myPlayer) || myPlayer == null)
        {
            myPlayer = GetNodeOrNull<player>("./Player");
            GD.Print(myPlayer);
            return;
        }

        GetNode<GodotObject>("PhantomCamera2D").Call("set_follow_target_node", myPlayer);
        GetNode<GodotObject>("PhantomCamera2DZoom").Call("set_follow_target_node", myPlayer);

        Callable callable = new Callable(this, "OnPlayerCollidedWithEnemy");
        if (myPlayer.IsConnected("EnemyCollision", callable)) return;
        myPlayer.Connect("EnemyCollision", callable);

    }

    public async void OnPlayerCollidedWithEnemy(string[] enemies)
    {
        GetNode<GodotObject>("PhantomCamera2DZoom").Call("set_tween_transition", 8);
        GetNode<GodotObject>("PhantomCamera2DZoom").Call("set_tween_duration", 1.3f);
        GetNode<GodotObject>("PhantomCamera2DZoom").Call("set_priority", 10);
        GetNode<TextureRect>("%SpeedFilter").Visible = true;

        GetTree().Paused = true;

        await ToSignal(GetTree().CreateTimer(.3f), "timeout");
        
        EmitSignal(SignalName.LevelChanged, CurrentWorldName, enemies);
    }

    private void ResetGame() => GetTree().ReloadCurrentScene();
}
