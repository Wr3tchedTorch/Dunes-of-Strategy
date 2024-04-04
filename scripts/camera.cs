using Godot;
using System;

public partial class camera : Camera2D
{

    [Export]
    public float ShakeFade = 5;

    RandomNumberGenerator rng = new RandomNumberGenerator();

    public float ShakeStrength = 0;
    private bool _resetingCameraPos = false;

    public override void _Process(double delta)
    {
        if (ShakeStrength > 1)
        {
            ShakeStrength = Mathf.Lerp(ShakeStrength, 0, ShakeFade * (float)delta);
            Offset += RandomOffset();
        }
        // else if (!_resetingCameraPos)
        // {
        //     Tween tweenOffset = CreateTween();
        //     tweenOffset.TweenProperty(this, "offset", Vector2.Zero, .3f);
        //     tweenOffset.Play();
        //     _resetingCameraPos = true;
        // }
    }

    public void ApplyShake(float RandomStrength = 6)
    {
        ShakeStrength = RandomStrength;
        _resetingCameraPos = false;
    }

    public Vector2 RandomOffset()
    {
        return new Vector2(rng.RandfRange(-ShakeStrength, ShakeStrength), rng.RandfRange(-ShakeStrength, ShakeStrength));
    }
}
