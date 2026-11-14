using System;

namespace Physics;

public readonly record struct Circle(Vec2 Center, double Radius)
{
    public Circle Transformed(Transform t)
    {
        return new Circle(t.Apply(Center), Radius);
    }

    public double RotationalInertia()
    {
        return Math.PI * (Radius * Radius * Radius * Radius / 2 + Radius * Radius * Center.LengthSquared);
    }
}