using System;

namespace Physics;

public readonly record struct AlignedBoundingBox(Vec2 Min, Vec2 Max)
{
    public static bool BoxesOverlap(AlignedBoundingBox a, AlignedBoundingBox b)
    {
        return a.Min.X <= b.Max.X && 
        b.Min.X <= a.Max.X && 
        a.Min.Y <= b.Max.Y && 
        b.Min.Y <= a.Max.Y;
    }

    public AlignedBoundingBox Expanded(double margin)
    {
        Vec2 mVec = new Vec2(margin, margin);
        return new AlignedBoundingBox(Min - mVec, Max + mVec);
    }

    public AlignedBoundingBox CheckPoint(Vec2 p)
    {
        if (Min.X <= p.X && Min.Y <= p.Y && Max.X >= p.X && Max.Y >= p.Y) {return this;}
        return new AlignedBoundingBox(
            new Vec2(
                Min.X <= p.X ? Min.X : p.X,
                Min.Y <= p.Y ? Min.Y : p.Y
            ),
            new Vec2(
                Max.X >= p.X ? Max.X : p.X,
                Max.Y >= p.Y ? Max.Y : p.Y
            )
        );
    }

    public static AlignedBoundingBox Union (AlignedBoundingBox left, AlignedBoundingBox right)
    {
        left = left.CheckPoint(right.Min);
        return left.CheckPoint(right.Max);
    }

    public static AlignedBoundingBox operator +(AlignedBoundingBox box, Vec2 p)
    {
        return new AlignedBoundingBox(box.Min + p, box.Max + p);
    }

    public static AlignedBoundingBox operator +(Vec2 p, AlignedBoundingBox box)
    {
        return new AlignedBoundingBox(box.Min + p, box.Max + p);
    }

    public static AlignedBoundingBox operator -(AlignedBoundingBox box, Vec2 p)
    {
        return new AlignedBoundingBox(box.Min - p, box.Max - p);
    }

    public static (double, double)? FindOverlapTime(AlignedBoundingBox a, AlignedBoundingBox b, Vec2 vRel)
    {
        double t1x = -1;
        double t2x = 2;
        if (vRel.X != 0)
        {
            t1x = (b.Min.X - a.Max.X)/vRel.X;
            t2x = (b.Max.X - a.Min.X)/vRel.X;
            if (t1x > t2x) {(t1x, t2x) = (t2x, t1x);}
        }
        else if (a.Max.X < b.Min.X || a.Min.X > b.Max.X) {return null;}
        double t1y = -1;
        double t2y = 2;
        if (vRel.Y != 0)
        {
            t1y = (b.Min.Y - a.Max.Y)/vRel.Y;
            t2y = (b.Max.Y - a.Min.Y)/vRel.Y;
            if (t1y > t2y) {(t1y, t2y) = (t2y, t1y);}
        }
        else if (a.Max.Y < b.Min.Y || a.Min.Y > b.Max.Y) {return null;}
        double tEnter = Math.Max(t1x, t1y);
        double tExit = Math.Min(t2x, t2y);
        if (tEnter > tExit || tExit < 0 || tEnter > 1) {return null;}
        return (Math.Max(tEnter, 0), Math.Min(tExit, 1));
    }
}