using System;

namespace Physics;

public readonly record struct Hit(double T, Vec2 Point, Vec2 Normal);