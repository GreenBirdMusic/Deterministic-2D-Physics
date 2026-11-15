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
        }
        else
        {
            objects.Add(obj);
            obj.World = this;
            obj.IsRemoved = false;
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

    public void Step()
    {
        TickCount++; // Let me know if I should move this to the end
        inStep = true;

        try
        {
            foreach (PhysicsObject obj in objects)
            {
                if (removeQueue.Contains(obj)) {continue;}
                if (obj.MovementType != MovementKind.Static) {
                    obj.UpdateWorld();
                    if (obj.MovementType == MovementKind.Dynamic)
                    {
                        obj.Velocity += (obj.GravityOverride ?? Gravity) * obj.GravityScale * tickLength;
                    }
                    // Note: would use the solver here to account for collision later
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