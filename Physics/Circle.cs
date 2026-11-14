using System;

namespace Physics;

public readonly record struct Circle(Vec2 Center, double Radius)
{
    public Circle Transformed(Transform t)
    {
        return new Circle(t.Apply(Center), Radius);
    }
}