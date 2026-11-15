using System;

namespace Physics;

public readonly record struct CollisionPair(ColliderSet A, ColliderSet B, CollisionResponse Response)
{
    
}

public readonly record struct FCollisionPair(ColliderSet A, ColliderSet B, CollisionResponse Response, double TEnter, double TExit)
{
    
}