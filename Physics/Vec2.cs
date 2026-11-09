using System;

namespace Physics;

public readonly struct Vec2
{
    public readonly double X = 0;
    public readonly double Y = 0;

    /// <summary>
    /// Gives the Zero Vec2 vector
    /// </summary>
    public static readonly Vec2 Zero = new Vec2(0, 0);

    /// <summary>
    /// Stores the length of a Vec2 vector
    /// </summary>
    public double Length => Math.Sqrt(LengthSquared);
    /// <summary>
    /// Stores the squared length of a Vec2 vector
    /// </summary>
    public double LengthSquared => X * X + Y * Y;

    /// <summary>
    /// creates a Vec2 object with set x and y
    /// </summary>
    public Vec2(double x, double y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Adds two Vec2 vectors together
    /// </summary>
    public static Vec2 operator +(Vec2 left, Vec2 right)
    {
        return new Vec2(left.X + right.X, left.Y + right.Y);
    }

    /// <summary>
    /// Subtracts one Vec2 vector from another
    /// </summary>
    public static Vec2 operator -(Vec2 left, Vec2 right)
    {
        return new Vec2(left.X - right.X, left.Y - right.Y);
    }

    /// <summary>
    /// Negates a Vec2 vector
    /// </summary>
    public static Vec2 operator -(Vec2 input)
    {
        return new Vec2(-input.X, -input.Y);
    }

    /// <summary>
    /// Multiplies a Vec2 vector by a double
    /// </summary>
    public static Vec2 operator *(Vec2 left, double right)
    {
        return new Vec2(left.X * right, left.Y * right);
    }
    /// <summary>
    /// Multiplies a double by a Vec2 vector
    /// </summary>
    public static Vec2 operator *(double left, Vec2 right)
    {
        return new Vec2(left * right.X, left * right.Y);
    }

    /// <summary>
    /// Divides a Vec2 vector by a double
    /// </summary>
    public static Vec2 operator /(Vec2 left, double right)
    {
        return new Vec2(left.X / right, left.Y / right);
    }

    const double minNormalizeSize = 1e-8;
    /// <summary>
    /// Normalizes a Vec2 vector
    /// </summary>
    public Vec2 Normalized()
    {
        if (Math.Abs(X) > minNormalizeSize || Math.Abs(Y) > minNormalizeSize)
        {
            return this/Length;
        }
        else
        {
            return Zero;
        }
    }

    /// <summary>
    /// Gets the dot product between two Vec2 vectors
    /// </summary>
    public static double Dot(Vec2 a, Vec2 b)
    {
        return a.X * b.X + a.Y * b.Y;
    }

    /// <summary>
    /// Gets the cross product between two Vec2 vectors
    /// Positive if b turns clockwise from a on screen (Y points down).
    /// </summary>
    public static double Cross(Vec2 a, Vec2 b)
    {
        return a.X * b.Y - a.Y * b.X;
    }

    /// <summary>
    /// Gets the Vec2 vector perpendicular to this Vec2 vector
    /// Turned 90 degrees clockwise on screen (Y points down).
    /// </summary>
    public Vec2 Perp()
    {
        return new Vec2(-Y, X);
    }

    /// <summary>
    /// Gets a position t of the way from a to b
    /// </summary>
    public static Vec2 Lerp(Vec2 a, Vec2 b, double t)
    {
        return a + (b - a) * t;
    }
}