using Godot;
using System;

public partial class level : Node2D
{
    [Signal]
    public delegate void LevelChangedEventHandler(String currentWorldName);
    [Export]
    public String CurrentWorldName = "battle";

    public override void _Process(double delta)
    {
        if (Input.IsActionPressed("reset")) ResetGame();
    }

    public void OnPlayerCollidedWithEnemy() {        
        EmitSignal(SignalName.LevelChanged, CurrentWorldName);
    }

    private void ResetGame() => GetTree().ReloadCurrentScene();

}
