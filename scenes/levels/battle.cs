using System;
using Godot;

public partial class battle : Node2D
{

    public string[] Enemies;
    public int EnemiesLeft = 99;
    private player _myPlayer;
    private int _selectedUnityIndex;
    private int _selectedMoveIndex;
    private bool _selectingTarget;
    private bool _selectLabelFlash = false;
    private Tween tweenSelectTargetLabel;
    private bool _battleFinished = false;

    // Camera motion
    RandomNumberGenerator camRng = new RandomNumberGenerator();
    private int maxOffset = 10;
    private double delta;
    private bool _tweeningCam = false;
    private Tween tweenCameraOffset;
    public override void _Ready()
    {
        Global.StartGame();
        Global.InBattle = true;

        GetNode<GodotObject>("PhantomCamera2D").Call("set_tween_transition", 5);
        GetNode<GodotObject>("PhantomCamera2D").Call("set_tween_duration", 1.2f);
        GetNode<GodotObject>("PhantomCamera2DZoomed").Call("set_priority", 0);
        GetNode<GodotObject>("PhantomCamera2D").Call("set_priority", 1);

        _myPlayer = GetNode<player>("Player");
        UpdatePlayerStats();
        for (int i = 0; i < _myPlayer.moveNames.Length; i++)
            GetNode<Button>("%Move" + (i + 1)).Text = _myPlayer.moveNames[i];

        EnemiesLeft = Enemies.Length;
        Global.UnitsInBattle = EnemiesLeft;

        for (int i = 0; i < Enemies.Length; i++)
            SpawnEnemy(Enemies[i], i);

        camRng.Randomize();

    }

    private void CameraMotion()
    {
        if (GetNode<camera>("./Camera2D").ShakeStrength > 1)
        {
            if (tweenCameraOffset.IsRunning()) tweenCameraOffset.Stop();
            _tweeningCam = false;
            return;
        }

        if (_tweeningCam) return;
        _tweeningCam = true;

        tweenCameraOffset = CreateTween();
        tweenCameraOffset.SetEase(Tween.EaseType.InOut);
        tweenCameraOffset.TweenProperty(GetNode<Camera2D>("./Camera2D"), "offset", new Vector2(camRng.RandfRange(-maxOffset, maxOffset), camRng.RandfRange(-maxOffset, maxOffset)), 1);

        Callable CamTweenFinished = new Callable(this, "CamTweenFinished");
        if (!tweenCameraOffset.IsConnected("finished", CamTweenFinished))
            tweenCameraOffset.Connect("finished", CamTweenFinished);
    }

    public void CamTweenFinished() => _tweeningCam = false;

    public override void _Process(double delta)
    {
        CameraMotion();
        this.delta = delta;

        if (EnemiesLeft > 0)
        {
            if (_selectingTarget)
                SelectTarget(_selectedMoveIndex);

            if (!IsInstanceValid(GetNodeOrNull<GameEntity>("./enemy" + (Global.TurnIndex - 1))) && Global.TurnIndex != 0)
                Global.NextTurn();

            Global.UpdateTurn();

            UpdatePlayerStats();
            return;
        }

        if (_battleFinished) return;
        Global.EndGame();
        FinishBattle();
        _battleFinished = true;
    }

    private async void FinishBattle()
    {
        await ToSignal(GetTree().CreateTimer(1), "timeout");
        GetNode<scene_changer>("/root/SceneChanger").HandleWorldChange("battle", new string[0]);
    }

    public void UpdatePlayerStats()
    {
        GetNode<ProgressBar>("%HealthBar").Value = _myPlayer.Health;

        bool disableButtons = false;
        if (Global.TurnIndex != 0 || _myPlayer.HasMoved || _selectingTarget || _myPlayer.HasMoved) disableButtons = true;

        for (int i = 0; i < _myPlayer.moveNames.Length; i++)
            GetNode<Button>("%Move" + (i + 1)).Disabled = disableButtons;
    }

    public void SpawnEnemy(string enemy, int enemyIndex)
    {
        string scenePath = "";
        switch (enemy)
        {
            case "bat":
                scenePath = "res://scenes/enemies/bat.tscn";
                break;
        }
        PackedScene enemyScene = GD.Load<PackedScene>(scenePath);
        Enemy newEnemy = enemyScene.Instantiate<Enemy>();

        newEnemy.GlobalPosition = GetNode<Marker2D>("EnemySpawners/Marker2D" + enemyIndex).GlobalPosition;
        newEnemy.Name = "enemy" + enemyIndex;
        newEnemy.EnemyIndex = enemyIndex;

        Callable EnemySelected = new Callable(this, "EnemySelected");
        newEnemy.Connect("EnemySelected", EnemySelected);
        AddChild(newEnemy);
    }

    private void EnemySelected(int EnemyIndex)
    {

        tweenSelectTargetLabel.Stop();
        GetNode<Label>("%selectTargetLabel").Modulate = new Color(Colors.White, 0);
        _selectLabelFlash = false;
        _selectingTarget = false;
        _selectedUnityIndex = EnemyIndex;
        UseMove(_myPlayer, _myPlayer.moveTypes[_selectedMoveIndex], _myPlayer.movePowers[_selectedMoveIndex], GetNode<GameEntity>("./enemy" + _selectedUnityIndex));
    }

    public async void UseMove(GameEntity user, string moveType, double power, GameEntity target)
    {
        user.HasMoved = true;
        Vector2 userPos = user.GlobalPosition;
        string pCamPath = "./Player/PhantomCamera2D";

        if (user is Enemy myEnemy)
        {
            pCamPath = "./EnemiesPhantomCamera2D";
            userPos = GetNode<Marker2D>("EnemySpawners/Marker2D" + myEnemy.EnemyIndex).GlobalPosition;
        }

        GodotObject userPcam = GetNode<GodotObject>(pCamPath);
        userPcam.Call("set_tween_transition", 0);
        userPcam.Call("set_tween_duration", .4);
        userPcam.Call("set_priority", 10);

        switch (moveType)
        {
            case "attack":
                user.UpdateDamage(target, power);
                if (user is Enemy) user.GetNode<AnimatedSprite2D>("AnimatedSprite2D").Play("attack");
                else user.GetNode<AnimationPlayer>("AnimationPlayer").Play("attack");
                break;
            case "heal":
                user.Heal(power);
                EmitHealParticles(userPos);
                await ToSignal(GetTree().CreateTimer(1), "timeout");
                CentralizeCamera();
                await ToSignal(GetTree().CreateTimer(1.2), "timeout");
                Global.NextTurn();
                break;
            case "defend":
                if (user.IsDefending) GD.Print("Already Defending");
                user.IsDefending = true;
                break;
        }
    }

    public void CentralizeCamera()
    {
        GetNode<GodotObject>("./Player/PhantomCamera2D").Call("set_priority", 0);
        GetNode<GodotObject>("./EnemiesPhantomCamera2D").Call("set_priority", 0);

        GodotObject pcam = GetNode<GodotObject>("PhantomCamera2D");
        pcam.Call("set_tween_duration", .5);
        pcam.Call("set_priority", 10);
    }

    private void EmitHealParticles(Vector2 targetPosition)
    {
        GetNode<GpuParticles2D>("%HealParticles").GlobalPosition = targetPosition;
        GetNode<GpuParticles2D>("%HealParticles").Emitting = true;
    }

    private void SelectTarget(int moveIndex)
    {
        if (!_selectLabelFlash)
        {
            _selectLabelFlash = true;
            tweenSelectTargetLabel = CreateTween().SetLoops();
            tweenSelectTargetLabel.TweenProperty(GetNode<Label>("%selectTargetLabel"), "modulate", new Color(Colors.White, 1), .4);
            tweenSelectTargetLabel.TweenProperty(GetNode<Label>("%selectTargetLabel"), "modulate", new Color(Colors.White, 0), .4);
        }

        if (Input.IsActionPressed("left")) _selectedUnityIndex = Mathf.Clamp(_selectedUnityIndex - 1, 0, Global.UnitsInBattle - 1);
        if (Input.IsActionPressed("right")) _selectedUnityIndex = Mathf.Clamp(_selectedUnityIndex + 1, 0, Global.UnitsInBattle - 1);

        if (Input.IsActionPressed("select") && IsInstanceValid(GetNodeOrNull<GameEntity>("./enemy" + _selectedUnityIndex)))
        {
            _selectingTarget = false;
            UseMove(_myPlayer, _myPlayer.moveTypes[moveIndex], _myPlayer.movePowers[moveIndex], GetNode<GameEntity>("./enemy" + _selectedUnityIndex));
        }

        if (!_selectingTarget)
        {
            tweenSelectTargetLabel.Stop();
            GetNode<Label>("%selectTargetLabel").Modulate = new Color(Colors.White, 0);
        }
    }

    public void OnMove1Pressed()
    {
        if (_myPlayer.moveTypes[0] != "heal")
        {
            GD.Print("player: " + _myPlayer.moveNames[0]);
            _selectedMoveIndex = 0;
            _selectingTarget = true;
            return;
        }

        UseMove(_myPlayer, _myPlayer.moveTypes[0], _myPlayer.movePowers[0], GetNode<GameEntity>("./enemy" + _selectedUnityIndex));
    }
    public void OnMove2Pressed()
    {
        if (_myPlayer.moveTypes[1] != "heal")
        {
            GD.Print("player: " + _myPlayer.moveNames[1]);
            _selectedMoveIndex = 1;
            _selectingTarget = true;
            return;
        }

        UseMove(_myPlayer, _myPlayer.moveTypes[1], _myPlayer.movePowers[1], GetNode<GameEntity>("./enemy" + _selectedUnityIndex));
    }
    public void OnMove3Pressed()
    {
        if (_myPlayer.moveTypes[2] != "heal")
        {
            GD.Print("player: " + _myPlayer.moveNames[2]);
            _selectedMoveIndex = 2;
            _selectingTarget = true;
            return;
        }

        UseMove(_myPlayer, _myPlayer.moveTypes[2], _myPlayer.movePowers[2], GetNode<GameEntity>("./enemy" + _selectedUnityIndex));
    }
}
