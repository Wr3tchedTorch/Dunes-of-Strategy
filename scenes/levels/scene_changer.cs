using Godot;
using System;

public partial class scene_changer : Node2D
{
    private Node2D _currentWorld;
    private Node2D _nextWorld;
    private AnimationPlayer _anim;
    private bool _createdNextWorld = false;
    private string _savePath = "user://savegame.save";

    public override void _Ready()
    {
        _currentWorld = GetNode<Node2D>("Level1");
        _anim = GetNode<AnimationPlayer>("AnimationPlayer");

        Callable callable = new Callable(this, "HandleWorldChange");
        _currentWorld.Connect("LevelChanged", callable);
    }

    public override void _Process(double delta)
    {        
        if (IsInstanceValid(_currentWorld) || _createdNextWorld) return;

        AddChild(_nextWorld);
        if (_nextWorld is level) 
            LoadGame();

        _currentWorld = _nextWorld;
        _currentWorld.ZIndex = 0;

        _nextWorld = null;
        _anim.Play("fadeOut");
        _createdNextWorld = true;
    }

    public void HandleWorldChange(string currentWorldName, string[] enemiesList)
    {
        if (currentWorldName == "level_1")
            SaveGame();

        _createdNextWorld = false;

        string nextWorldName = "";
        switch (currentWorldName)
        {
            case "level_1": nextWorldName = "battle"; break;
            case "battle": nextWorldName = "level_1"; break;
        }

        PackedScene nextWorldScene = GD.Load<PackedScene>("res://scenes/levels/" + nextWorldName + ".tscn");
        _nextWorld = nextWorldScene.Instantiate<Node2D>();
        
        if (_nextWorld is battle BattleLevel)
        {
            BattleLevel.Enemies = enemiesList;
            Global.InBattle = false;
        }
        else if (_nextWorld is level level)
        {
            Callable callable = new Callable(this, "HandleWorldChange");
            level.Connect("LevelChanged", callable);
        }
        _nextWorld.ZIndex = -1;
        _anim.Play("fadeIn");
    }

    public void OnAnimationFinished(String _anim_name)
    {
        if (_anim_name != "fadeIn") return;
        _currentWorld.QueueFree();
    }

    public void SaveGame()
    {
        using var saveGame = FileAccess.Open(_savePath, FileAccess.ModeFlags.Write);

        var saveNodes = GetTree().GetNodesInGroup("persist");
        foreach (Node saveNode in saveNodes)
        {
            if (string.IsNullOrEmpty(saveNode.SceneFilePath))
            {
                GD.Print($"persistent node '{saveNode.Name}' is not an instanced scene, skipped");
                continue;
            }

            if (!saveNode.HasMethod("Save"))
            {
                GD.Print($"persistent node '{saveNode.Name}' is missing a Save() function, skipped");
                continue;
            }

            var nodeData = saveNode.Call("Save");
            var jsonString = Json.Stringify(nodeData);

            saveGame.StoreLine(jsonString);
        }
    }
    
    public void LoadGame()
    {
        if (!FileAccess.FileExists(_savePath))        
            return; // Error! We don't have a save to load.        
        
        var saveNodes = GetTree().GetNodesInGroup("persist");
        foreach (Node saveNode in saveNodes)
        {
            saveNode.QueueFree();
        }

        using var saveGame = FileAccess.Open(_savePath, FileAccess.ModeFlags.Read);
        while (saveGame.GetPosition() < saveGame.GetLength())
        {
            var jsonString = saveGame.GetLine();
            var json = new Json();

            var parseResult = json.Parse(jsonString);
            if (parseResult != Error.Ok)
            {
                GD.Print($"JSON Parse Error: {json.GetErrorMessage()} in {jsonString} at line {json.GetErrorLine()}");
                continue;
            }

            var nodeData = new Godot.Collections.Dictionary<string, Variant>((Godot.Collections.Dictionary)json.Data);
            var newObjectScene = GD.Load<PackedScene>(nodeData["Filename"].ToString());
            var newObject = newObjectScene.Instantiate<Node>();

            GetNode(nodeData["Parent"].ToString()).AddChild(newObject);
            newObject.Set(Node2D.PropertyName.Position, new Vector2((float)nodeData["PosX"], (float)nodeData["PosY"]));
            if (newObject is Enemy)
            {
                newObject.Name = "Bat";
                if ((bool)nodeData["IsAlive"] == false)
                {
                    newObject.GetNode<CollisionShape2D>("CollisionShape2D").Disabled = true;
                    newObject.GetNode<AnimationPlayer>("AnimationPlayer").Play("death");
                }
            }

            foreach (var (key, value) in nodeData)
            {
                if (key == "Filename" || key == "Parent" || key == "PosX" || key == "PosY")
                {
                    continue;
                }
                newObject.Set(key, value);
            }
        }
    }
}
