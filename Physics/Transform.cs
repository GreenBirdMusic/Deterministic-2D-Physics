using System;

namespace Physics;

public readonly record struct Transform(Vec2 Position, double Cos, double Sin, bool Mirrored)
{
    public static Transform From(Vec2 position, double angle, bool mirrored)
    {
        return new Transform(position, Math.Cos(angle), Math.Sin(angle), mirrored);
    }

    public Vec2 Apply(Vec2 p)
    {
        return ApplyDirection(p) + Position;
    }

    public Vec2 ApplyDirection(Vec2 d)
    {
        double x = d.X;
        double y = d.Y;
        if (Mirrored)
        {
            x = -x;
        }
        return new Vec2(x * Cos + y * -Sin, x * Sin + y * Cos);
    }
}