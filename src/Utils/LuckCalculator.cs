
using System;
using MegaCrit.Sts2.Core.Entities.Creatures;
using GamblingMod.Powers;

namespace GamblingMod.Utils;

public static class LuckCalculator
{
    // Globale Kurvenparameter
    private const double A = 1.2;
    private const double B = 2.2;
    private const double W = 25.0;

    // Länge des glatten Übergangs zur linearen Steigung
    private const double Transition = 20.0;

    /// <summary>
    /// Berechnet eine Wahrscheinlichkeit abhängig von LuckPower.
    /// Wahrscheinlichkeiten werden als Prozentwerte (0-100) angegeben.
    /// </summary>
    public static decimal Calculate(
        decimal startChance,
        decimal luck = 0m,
        decimal minChance = 0m,
        decimal maxChance = 100m,
        double curveFactor = 1.0,
        double linearSlopeFactor = 1.0)
    {
        // Parameter überprüfen
        if (minChance < 0m ||
            maxChance > 100m ||
            minChance > startChance ||
            startChance > maxChance)
        {
            throw new ArgumentOutOfRangeException(
                nameof(startChance),
                "Es muss gelten: 0 <= minChance <= startChance <= maxChance <= 100.");
        }

        if (!double.IsFinite(curveFactor) || curveFactor <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(curveFactor));

        if (!double.IsFinite(linearSlopeFactor) ||
            linearSlopeFactor < 0)
            throw new ArgumentOutOfRangeException(
                nameof(linearSlopeFactor));

        double p0 = (double)startChance;
        double pMin = (double)minChance;
        double pMax = (double)maxChance;
        double L = (double)luck;

        // Krümmung abhängig von der Startwahrscheinlichkeit
        double distance = (p0 - 50.0) / W;

        double k = curveFactor *
            (A + B * Math.Exp(-0.5 * distance * distance));

        double result;

        if (L < -100.0)
        {
            // Negative lineare Fortsetzung
            double slope = GetSlope(p0 - pMin, k);

            double extension = LinearExtension(
                -100.0 - L,
                slope,
                linearSlopeFactor
            );

            result = pMin - extension;
        }
        else if (L < 0.0)
        {
            // Negative exponentielle Kurve
            double factor = NormalizedCurve(-L / 100.0, k);

            result = p0 - (p0 - pMin) * factor;
        }
        else if (L <= 100.0)
        {
            // Positive exponentielle Kurve
            double factor = NormalizedCurve(L / 100.0, k);

            result = p0 + (pMax - p0) * factor;
        }
        else
        {
            // Positive lineare Fortsetzung
            double slope = GetSlope(pMax - p0, k);

            double extension = LinearExtension(
                L - 100.0,
                slope,
                linearSlopeFactor
            );

            result = pMax + extension;
        }

        // Absolute Begrenzung auf 0-100 %
        return (decimal)Math.Clamp(result, 0.0, 100.0);
    }

    /// <summary>
    /// Berechnet die Wahrscheinlichkeit und liest LuckPower
    /// automatisch von der Creature.
    /// </summary>
    public static decimal Calculate(
        Creature creature,
        decimal startChance,
        decimal minChance = 0m,
        decimal maxChance = 100m,
        double curveFactor = 1.0,
        double linearSlopeFactor = 1.0)
    {
        decimal luck =
            creature.GetPower<LuckPower>()?.Amount ?? 0m;

        return Calculate(
            startChance: startChance,
            luck: luck,
            minChance: minChance,
            maxChance: maxChance,
            curveFactor: curveFactor,
            linearSlopeFactor: linearSlopeFactor
        );
    }

    /// <summary>
    /// Normierte exponentielle Kurve.
    /// F(0) = 0 und F(1) = 1.
    /// </summary>
    private static double NormalizedCurve(double x, double k)
    {
        if (k < 1e-7)
            return x;

        return (1.0 - Math.Exp(-k * x))
             / (1.0 - Math.Exp(-k));
    }

    /// <summary>
    /// Berechnet den Betrag der Tangentensteigung bei ±100 Luck.
    /// </summary>
    private static double GetSlope(double span, double k)
    {
        if (k < 1e-7)
            return span / 100.0;

        return span * k * Math.Exp(-k)
             / (100.0 * (1.0 - Math.Exp(-k)));
    }

    /// <summary>
    /// Glatte Fortsetzung nach ±100 Luck.
    /// Nach 20 Luck-Punkten ist die neue lineare
    /// Steigung vollständig erreicht.
    /// </summary>
    private static double LinearExtension(
        double distance,
        double slope,
        double linearSlopeFactor)
    {
        double r = linearSlopeFactor;
        double t = distance;
        double T = Transition;

        if (t <= T)
        {
            // Knickfreier Übergang über 20 Luck-Punkte
            return slope * (
                t + (r - 1.0) *
                (t * t / T - t * t * t / (3.0 * T * T))
            );
        }

        // Danach exakt linear
        double transitionValue = slope * T *
            (1.0 + 2.0 * (r - 1.0) / 3.0);

        return transitionValue + slope * r * (t - T);
    }
}
