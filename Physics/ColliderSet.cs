using System;
using System.Collections.Generic;

namespace Physics;

public class ColliderSet
{
    private double density;
    public double Density
    {
        get => density;
        set
        {
            if (value < 0) {throw new ArgumentException("Density must be nonnegative");}
            density = value;
            if (Owner != null) {Owner.RecalculateMass();}
        }
    }
    public CollisionGroup Group;
    private readonly Circle[] localCircles;
    public IReadOnlyList<Circle> Circles => localCircles;
    private readonly Circle[] worldCircles;
    public IReadOnlyList<Circle> WorldCircles => worldCircles;
    public ulong[] CircleTags;
    private readonly EdgeList[] edgeLists;
    public IReadOnlyList<EdgeList> EdgeLists => edgeLists;

    internal PhysicsObject? Owner = null;

    internal bool active;
    public bool Active
    {
        get => active;
        set
        {
            if (value == active) return;
            Owner?.CheckActivation(this, value);
            active = value;
            Owner?.OnSetChanged(this);
        }
    }

    private readonly double area;
    public double Area => area;

    public double Reach {get;}

    private readonly Vec2 moment;
    public Vec2 Moment => moment;

    private readonly Vec2 center;
    public Vec2 Center => center;

    private readonly double rotationalInertia;
    public double RotationalInertia => rotationalInertia;

    public AlignedBoundingBox Bounds {get; private set;}

    public AlignedBoundingBox RotationalBounds {get; internal set;}

    public AlignedBoundingBox SweptBounds {get; internal set;}

    public ColliderSet(CollisionGroup group, double density, Circle[] circles, EdgeList[] edgeLists, ulong[]? circleTags = null, bool active = true)
    {
        if (edgeLists.Length + circles.Length == 0) {throw new ArgumentException("Must provide at least one collider");}
        Density = density;
        if (circleTags == null)
        {
            circleTags = new ulong[circles.Length];
        }
        else if (circleTags.Length != circles.Length)
        {
            throw new ArgumentException("Provided circle tag count must match circle count");
        }
        CircleTags = (ulong[])circleTags.Clone();
        localCircles = (Circle[])circles.Clone();
        worldCircles = (Circle[])circles.Clone();
        Active = active;
        Group = group;
        this.edgeLists = (EdgeList[])edgeLists.Clone();
        area = 0;
        moment = new Vec2();
        rotationalInertia = 0;
        foreach (Circle circle in circles)
        {
            double testReach = circle.Center.Length + circle.Radius;
            if (testReach > Reach) {Reach = testReach;}
            area += circle.Radius * circle.Radius;
            moment += circle.Radius * circle.Radius * circle.Center;
            rotationalInertia += circle.RotationalInertia();
        }
        area *= Math.PI; // Intentionally reducing how many times I multiply by pi
        moment *= Math.PI;
        foreach (EdgeList edges in edgeLists)
        {
            if (edges.Reach > Reach) {Reach = edges.Reach;}
            area += edges.Area;
            moment += edges.Moment;
            rotationalInertia += edges.RotationalInertia;
        }
        if (area != 0)
        {
            center = moment/area;
        }
        else
        {
            center = new Vec2(0, 0);
        }
    }

    public void UpdateWorld(Transform t)
    {
        AlignedBoundingBox? bestBounds = null;
        for (int i = 0; i < localCircles.Length; i++)
        {
            worldCircles[i] = localCircles[i].Transformed(t);
            double r = worldCircles[i].Radius;
            if (bestBounds == null)
            {
                bestBounds = new AlignedBoundingBox(worldCircles[i].Center, worldCircles[i].Center);
            }
            bestBounds = bestBounds.Value.CheckPoint(worldCircles[i].Center - new Vec2(r, r));
            bestBounds = bestBounds.Value.CheckPoint(worldCircles[i].Center + new Vec2(r, r));
        }
        foreach (EdgeList edges in edgeLists)
        {
            edges.UpdateWorld(t);
            bestBounds = edges.CompareBounds(bestBounds);
        }
        if (bestBounds != null) {Bounds = bestBounds.Value;}
    }
}