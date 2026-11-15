using System;
using System.Collections.Generic;

namespace Physics;

public class EdgeList
{
    public ulong[] Tags {get;}
    
    private readonly IReadOnlyList<IEdge> localEdges;
    public IReadOnlyList<IEdge> LocalEdges => localEdges;

    private readonly IEdge[] worldEdges;
    public IReadOnlyList<IEdge> WorldEdges => worldEdges;

    public bool Closed {get;}

    public double Reach {get;}

    private readonly double area;
    public double Area => area;

    private readonly Vec2 moment;
    public Vec2 Moment => moment;

    private readonly double rotationalInertia;
    public double RotationalInertia => rotationalInertia;

    public EdgeList(IEdge[] edges, bool closed, ulong[]? tags = null)
    {
        if (edges.Length == 0) {throw new ArgumentException("Must provide at least one edge");}
        if (tags == null)
        {
            tags = new ulong[edges.Length];
        }
        else if (tags.Length != edges.Length)
        {
            throw new ArgumentException("Provided tag count must match edge count");
        }
        Tags = (ulong[])tags.Clone();
        if (closed)
        {
            for (int i = 0; i < edges.Length; i++)
            {
                if ((edges[i].End - edges[(i + 1) % edges.Length].Start).LengthSquared > Vec2.Epsilon * Vec2.Epsilon)
                {
                    throw new ArgumentException("Closed shape must actually be closed");
                }
            }
        }
        Closed = closed;
        localEdges = (IEdge[])edges.Clone();
        worldEdges = (IEdge[])edges.Clone();
        area = 0;
        moment = new Vec2(0, 0);
        rotationalInertia = 0;
        foreach (IEdge edge in edges)
        {
            double testReach = edge switch
            {
                Segment s => Math.Sqrt(Math.Max(s.Start.LengthSquared, s.End.LengthSquared)),
                Arc a => a.Center.Length + a.Radius,
                Curve c => Math.Sqrt(Math.Max(Math.Max(c.Start.LengthSquared, c.Control.LengthSquared), c.End.LengthSquared)),
                _ => throw new NotImplementedException()
            };
            if (testReach > Reach) {Reach = testReach;}

            if (closed) {
                area += edge.AreaPiece();
                moment += edge.Moment();
                rotationalInertia += edge.RotationalInertia();
            }
        }
    }

    public void UpdateWorld(Transform t)
    {
        for (int i = 0; i < localEdges.Count; i++)
        {
            worldEdges[i] = localEdges[i].Transformed(t);
        }
    }

    public AlignedBoundingBox? CompareBounds(AlignedBoundingBox? bestBounds)
    {
        for (int i = 0; i < worldEdges.Length; i++)
        {
            bestBounds = worldEdges[i].CompareBounds(bestBounds);
        }
        return bestBounds;
    }
}