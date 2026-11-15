using System;
using System.Collections.Generic;

namespace Physics;

public class PhysicsObject
{
    public Vec2 Position;
    public Vec2 Velocity;
    private double angle;
    public double Angle
    {
        get => angle;
        set
        {
            angle = Math.IEEERemainder(value, 2 * Math.PI);
        }
    }
    public double AngularVelocity;
    public bool Mirrored;

    private readonly ColliderSet[] colliderSets;
    public IReadOnlyList<ColliderSet> ColliderSets => colliderSets;

    private double mass;
    public double Mass => mass;

    private double inverseMass;
    public double InverseMass => inverseMass;

    private double inverseJ;
    public double InverseJ => inverseJ;

    private Vec2 center;
    public Vec2 Center => center;

    private double rotationalInertia;
    public double RotationalInertia => rotationalInertia;

    private MovementKind movementType;
    public MovementKind MovementType
    {
        get => movementType;
        set
        {
            movementType = value;
            RecalculateMass();
        }
    }

    private bool rotatable;
    public bool Rotatable
    {
        get => rotatable;
        set
        {
            rotatable = value;
            RecalculateMass();
        }
    }

    private double? massOverride;
    public double? MassOverride
    {
        get => massOverride;
        set
        {
            if (value != null && value <= 0) {throw new ArgumentException("Mass override must be a positive non-zero number or null");} 
            massOverride = value;
            RecalculateMass();
        }
    }

    public Vec2? GravityOverride;

    public double GravityScale;

    private ColliderSet[]? massSets;
    public ColliderSet[]? MassSets
    {
        get => massSets;
        set
        {
            if (value == null) {massSets = null;}
            else
            {
                foreach (ColliderSet collision in value)
                {
                    if (collision.Owner != this) {throw new ArgumentException("Mass sets must be included in given collider sets");}
                }
                massSets = (ColliderSet[])value.Clone();
            }
            RecalculateMass();
        }
    }


    public PhysicsObject(Vec2 position, ColliderSet[] colliders, MovementKind movementType, bool rotatable, Vec2 velocity = default, double angle = 0, double angularVelocity = 0, bool mirrored = false, ColliderSet[]? massSets = null, double? massOverride = null, double gravityScale = 1, Vec2? gravityOverride = null)
    {
        colliders = (ColliderSet[])colliders.Clone();
        for (int i = 0; i < colliders.Length; i++)
        {
            ColliderSet collision = colliders[i];
            if (collision.Owner != null) {throw new ArgumentException("Collider sets can only be assigned to one physics object");}
            for (int j = 0; j < i; j++)
            {
                if (j != i && collision.Active && colliders[j].Active && collision.Group.ID == colliders[j].Group.ID) {throw new ArgumentException("There can only be one active collider per collision group in each object");}
            }
        }
        foreach (ColliderSet collision in colliders)
        {
            collision.Owner = this;
        }
        colliderSets = colliders;
        Velocity = velocity;
        Position = position;
        Angle = angle;
        MassSets = massSets;
        AngularVelocity = angularVelocity;
        Mirrored = mirrored;
        this.movementType = movementType;
        this.rotatable = rotatable;
        GravityScale = gravityScale;
        GravityOverride = gravityOverride;
        MassOverride = massOverride;
    }

    public void RecalculateMass()
    {
        IReadOnlyList<ColliderSet> colliders = massSets ?? ColliderSets;
        mass = 0;
        rotationalInertia = 0;
        Vec2 moments = new Vec2(0, 0);
        foreach (ColliderSet collision in colliders)
        {
            if (massSets == null && !collision.Active) {continue;}
            mass += collision.Density * collision.Area;
            rotationalInertia += collision.Density * collision.RotationalInertia;
            moments += collision.Density * collision.Moment;
        }
        center = mass > 0 ? moments / mass : new Vec2(0, 0);
        if (massOverride != null)
        {
            if (mass > 0) {rotationalInertia = rotationalInertia * massOverride.Value / mass;}
            else
            {
                double area = 0;
                moments = new Vec2(0, 0);
                double rInertia = 0;
                foreach (ColliderSet collision in colliders)
                {
                    if (massSets == null && !collision.Active) {continue;}
                    area += collision.Area;
                    moments += collision.Moment;
                    rInertia += collision.RotationalInertia;
                }
                rotationalInertia = area > 0 ? rInertia * massOverride.Value / area : 0;
                center = area > 0 ? moments / area : new Vec2(0, 0);
            }
            mass = massOverride.Value;
        }

        inverseMass = (movementType == MovementKind.Dynamic && mass > 0) ? 1 / mass : 0;
        inverseJ = (movementType == MovementKind.Dynamic && rotatable && rotationalInertia > 0) ? 1 / rotationalInertia : 0;
    }

    public void UpdateWorld()
    {
        Transform t = Transform.From(Position, angle, Mirrored);
        foreach (ColliderSet collision in colliderSets)
        {
            if (collision.Active) {collision.UpdateWorld(t);}
        }
    }

    public bool Swap(ColliderSet off, ColliderSet on, bool keepMomentum, bool ignoreOrder = true)
    {
        if (off.Owner != this || on.Owner != this) {throw new ArgumentException("Both collider sets must be in this object");}
        if (off.Active == false && on.Active == true && ignoreOrder)
        {
            // Special case: If ignoreOrder is true, don't throw an error if the order is reversed.
            (off, on) = (on, off);
        }
        else if (off.Active == false || on.Active == true) {throw new ArgumentException("The passed collider sets do not have the right active status");}
        if (on.Group.ID != off.Group.ID) {throw new ArgumentException("Both colliders must have the same collision group ID");}
        double oldMass = mass;
        double oldRotationalInertia = rotationalInertia;
        off.active = false;
        on.active = true;
        RecalculateMass(); // Modified the internal value so that I only recalculate once
        on.UpdateWorld(Transform.From(Position, angle, Mirrored));
        if (keepMomentum)
        {
            Velocity *= mass > 0 ? oldMass / mass : 1;
            AngularVelocity *= rotationalInertia > 0 ? oldRotationalInertia / rotationalInertia : 1;
        }
        return true;
    }

    internal void CheckActivation(ColliderSet collision, bool active)
    {
        if (active == false) {return;} // Early exit to reduce processing time
        for (int i = 0; i < colliderSets.Length; i++)
        {
            if (collision != colliderSets[i] && colliderSets[i].Active && collision.Group.ID == colliderSets[i].Group.ID) {throw new ArgumentException("There can only be one active collider per collision group in each object");}
        }
    }

    internal void OnSetChanged(ColliderSet collider)
    {
        // mass += collider.Active ? collider.Area * collider.Density : 0;
        RecalculateMass();
        if (collider.Active)
        {
            collider.UpdateWorld(Transform.From(Position, angle, Mirrored));
        }
    }
}