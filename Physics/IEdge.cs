using System;

namespace Physics;

public interface IEdge
{
    Vec2 Start {get;}
    Vec2 End {get;}
    bool IsInCap(Vec2 p);
}