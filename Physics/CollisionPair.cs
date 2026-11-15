using System;

namespace Physics;

public readonly record struct CollisionPair(ColliderSet A, ColliderSet B, CollisionResponse Response)
{
    
}