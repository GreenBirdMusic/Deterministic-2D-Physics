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
}