using System;

namespace Physics;

public static class Overlap
{
    /// <summary>
    /// Returns whether or not a given Vec2 point is in a given circle
    /// </summary>
    public static bool PointInCircle(Vec2 p, Circle c)
    {
        return (p - c.Center).LengthSquared < c.Radius * c.Radius;
    }
    /// <summary>
    /// Returns whether or not two given circles overlap
    /// </summary>
    public static bool CircleCircle(Circle a, Circle b)
    {
        return (b.Center - a.Center).LengthSquared < (a.Radius + b.Radius) * (a.Radius + b.Radius);
    }
    /// <summary>
    /// Returns whether or not a given circle and a given line segment overlap
    /// </summary>
    public static bool CircleSegment(Circle c, Segment s)
    {
        Vec2 closestPointToCenter = s.ClosestPoint(c.Center);
        return PointInCircle(closestPointToCenter, c);
    }
}