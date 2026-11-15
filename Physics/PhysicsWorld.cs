using System;
using System.Collections.Generic;

namespace Physics;

public class PhysicsWorld
{
    public GroupTable CollisionGroups {get;} // Intentionally modifiable, not replaceable
    
    public double TicksPerSecond {get;}
    public long TickCount {get; private set;}
    private readonly double tickLength;

    public Vec2 Gravity;

    private bool inStep = false;

    private readonly Queue<PhysicsObject> addQueue;

    private readonly Queue<PhysicsObject> removeQueue;

    private readonly List<PhysicsObject> objects;
    public IReadOnlyList<PhysicsObject> Objects => objects;


    public PhysicsWorld(GroupTable groups, double ticksPerSecond, Vec2 gravity)
    {
        TicksPerSecond = ticksPerSecond;
        CollisionGroups = groups;
        TickCount = 0;
        tickLength = 1.0 / ticksPerSecond;
        Gravity = gravity;
        addQueue = new Queue<PhysicsObject>();
        removeQueue = new Queue<PhysicsObject>();
        objects = [];
    }

    public void Add(PhysicsObject obj)
    {
        if (obj.World != null) {throw new ArgumentException("An object can only be in one world at a time");}
        CheckMass(obj);
        if (inStep)
        {
            addQueue.Enqueue(obj);
            obj.World = this;
            obj.IsRemoved = false;
            obj.UpdateWorld();
        }
        else
        {
            objects.Add(obj);
            obj.World = this;
            obj.IsRemoved = false;
            obj.UpdateWorld();
        }
    }

    public void Remove(PhysicsObject obj)
    {
        if (obj.World != this) {throw new ArgumentException("Cannot remove an object from a world it is not in");}
        if (inStep)
        {
            removeQueue.Enqueue(obj);
            obj.World = null;
            obj.IsRemoved = true;
        }
        else
        {
            objects.Remove(obj);
            obj.World = null;
            obj.IsRemoved = true;
        }
    }

    internal void CheckMass(PhysicsObject obj)
    {
        if (obj.Mass <= 0 && obj.MovementType == MovementKind.Dynamic)
        {
            foreach (ColliderSet collider in obj.ColliderSets)
            {
                if (CollisionGroups.IsSolid(collider.Group))
                {
                    throw new ArgumentException("A dynamic, solid object must have positive, nonzero mass");
                }
            }
        }
    }

    private readonly List<ColliderSet> activeSets = [];
    public IReadOnlyList<ColliderSet> ActiveSets => activeSets;
    private readonly List<CollisionPair> potentialCollisions = [];
    public IReadOnlyList<CollisionPair> PotentialCollisions => potentialCollisions;

    private readonly List<FCollisionPair> filteredCollisions = [];
    public IReadOnlyList<FCollisionPair> FilteredCollisions => filteredCollisions;

    private AlignedBoundingBox SweepBounds(PhysicsObject obj, ColliderSet collider)
    {
        AlignedBoundingBox bounds = collider.RotationalBounds;
        return AlignedBoundingBox.Union(bounds, bounds + obj.Velocity * tickLength);
    }

    private AlignedBoundingBox RotateBounds(PhysicsObject obj, ColliderSet collider)
    {
        AlignedBoundingBox bounds = collider.Bounds;
        if (obj.AngularVelocity != 0)
        {
            Vec2 r = new Vec2(collider.Reach, collider.Reach);
            bounds = new AlignedBoundingBox(
                obj.Position - r,
                obj.Position + r
            );
        }
        return bounds;
    }

    public void Step()
    {
        TickCount++; // Let me know if I should move this to the end
        inStep = true;

        static Vec2 ignoredVelocity(PhysicsObject obj)
        {
            return obj.MovementType == MovementKind.Static? Vec2.Zero : obj.Velocity;
        }

        try
        {
            activeSets.Clear();
            potentialCollisions.Clear();
            filteredCollisions.Clear();
            // Phase 1: update world positions, apply gravity, create swept boxes
            foreach (PhysicsObject obj in objects)
            {
                if (removeQueue.Contains(obj)) {continue;}
                if (obj.MovementType != MovementKind.Static) {
                    obj.UpdateWorld();
                    if (obj.MovementType == MovementKind.Dynamic)
                    {
                        obj.Velocity += (obj.GravityOverride ?? Gravity) * obj.GravityScale * tickLength;
                    }
                    foreach (ColliderSet collider in obj.ColliderSets)
                    {
                        if (collider.Active)
                        {
                            activeSets.Add(collider);
                            collider.RotationalBounds = RotateBounds(obj, collider);
                            collider.SweptBounds = SweepBounds(obj, collider);
                        }
                    }
                }
                else
                {
                    // static
                    foreach (ColliderSet collider in obj.ColliderSets)
                    {
                        if (collider.Active)
                        {
                            activeSets.Add(collider);
                            collider.RotationalBounds = collider.Bounds;
                            collider.SweptBounds = collider.Bounds;
                        }
                    }
                }
            }

            // Phase 2: iterate over active colliders and check for overlapping broad boxes
            for (int i = 0; i < activeSets.Count - 1; i++)
            {
                ColliderSet a = activeSets[i];
                if (removeQueue.Contains(a.Owner!)) {continue;}
                for (int j = i + 1; j < activeSets.Count; j++)
                {
                    ColliderSet b = activeSets[j];
                    if (removeQueue.Contains(b.Owner!)) {continue;}
                    if (a.Owner == b.Owner) {continue;}
                    if (a.Owner!.MovementType == MovementKind.Static && b.Owner!.MovementType == MovementKind.Static) {continue;}
                    CollisionResponse response = CollisionGroups.Get(a.Group, b.Group);
                    if (response == CollisionResponse.Ignore) {continue;}
                    if (AlignedBoundingBox.BoxesOverlap(a.SweptBounds, b.SweptBounds))
                    {
                        potentialCollisions.Add(new CollisionPair(a, b, response));
                    }
                }
            }

            // Phase 3: iterate over potential collisions and determine actual collisions
            foreach (CollisionPair pair in potentialCollisions)
            {
                ColliderSet a = pair.A;
                ColliderSet b = pair.B;
                if (removeQueue.Contains(a.Owner!) || removeQueue.Contains(b.Owner!)) {continue;}
                if (AlignedBoundingBox.FindOverlapTime(a.RotationalBounds, b.RotationalBounds, (ignoredVelocity(a.Owner!) - ignoredVelocity(b.Owner!)) * tickLength) is
                    (double tEnter, double tExit))
                {
                    filteredCollisions.Add(new FCollisionPair(a, b, pair.Response, tEnter, tExit));
                }
            }

            // Phase 4?: move all non-static objects that exist
            foreach (PhysicsObject obj in objects)
            {
                if (removeQueue.Contains(obj)) {continue;}
                if (obj.MovementType != MovementKind.Static) {
                    obj.Position += obj.Velocity * tickLength;
                    obj.Angle += obj.AngularVelocity * tickLength;
                }
            }
        }
        finally
        {
            inStep = false;
            while (addQueue.Count > 0)
            {
                objects.Add(addQueue.Dequeue());
            }
            while (removeQueue.Count > 0)
            {
                objects.Remove(removeQueue.Dequeue());
            }
        }
    }
}