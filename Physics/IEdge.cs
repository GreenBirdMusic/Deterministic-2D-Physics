using System;

namespace Physics;

public interface IEdge
{
    Vec2 Start {get;}
    Vec2 End {get;}
    bool IsInCap(Vec2 p);
    public Vec2 ClosestPoint(Vec2 p);
    public Vec2 NormalAt(Vec2 p);
    public bool IsBothSolid();
    IEdge Transformed(Transform t);

    double AreaPiece();
}