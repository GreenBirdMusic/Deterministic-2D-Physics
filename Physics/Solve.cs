using System;

namespace Physics;

public static class Solve
{
    /// <summary>
    /// Solves a cubic of the form ax^3 + bx^2 + cx + d
    /// </summary>
    public static double[] SolveCubic(double A, double B, double C, double D)
    {
        double b = B / A;
        double c = C / A;
        double d = D / A;
        double p = c - b * b / 3;
        double q = 2 * b * b * b / 27 - b * c / 3 + d;
        if (Math.Abs(p) <= Vec2.Epsilon) {return new[] {Math.Cbrt(-1 * q) - b / 3};}
        double delta = (q / 2) * (q / 2) + Math.Pow(p / 3, 3);
        if (delta > Vec2.Epsilon * (q / 2) * (q / 2))
        {
            // One answer
            return new[] {Math.Cbrt(q / -2 + Math.Sqrt(delta)) + Math.Cbrt(q / -2 - Math.Sqrt(delta)) - b / 3};
        }
        else
        {
            // Three answers
            double m = 2 * Math.Sqrt(p / -3);
            double theta = Math.Acos(Math.Clamp(3 * q / (2 * p) * Math.Sqrt(-3 / p), -1, 1)) / 3;
            double x1 = m * Math.Cos(theta) - b / 3;
            double x2 = m * Math.Cos(theta - 2 * Math.PI / 3) - b / 3;
            double x3 = m * Math.Cos(theta - 4 * Math.PI / 3) - b / 3;
            return new[] {x1, x2, x3};
        }
    }
}