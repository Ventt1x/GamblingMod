
using System;

namespace GamblingMod.Helpers;

public static class LuckCalculator
{
    public static decimal CalculateChance(
        decimal startChance,
        decimal endChance,
        decimal luck,
        double strength = 5.0,
        double curve = 2.0,
        double flatten = 0.2,
        double knee = 5.0)
    {
        double start = (double)Math.Clamp(startChance, 0m, 100m);
        double end = (double)Math.Clamp(endChance, 0m, 100m);
        double distance = Math.Abs(end - start);

        if (distance == 0 || luck <= 0)
            return (decimal)start;

        // Luck wirkt bei 50 % Grundchance am stärksten.
        double normalized = start / 100.0;

        double weight = Math.Pow(
            4.0 * normalized * (1.0 - normalized),
            Math.Max(0.0, curve)
        );

        double earlyLuck = Math.Min(
            (double)luck,
            Math.Max(0.0, knee)
        );

        double lateLuck = Math.Max(
            0.0,
            (double)luck - Math.Max(0.0, knee)
        );

        // Stärke definiert den Gesamtzuwachs
        // bis zum Knickpunkt bei 50 % Startchance.
        double desiredGain = Math.Max(0.0, strength) * weight;
        double earlyRate = 0.0;

        if (knee > 0)
        {
            double fraction = Math.Clamp(
                desiredGain / distance, 0.0, 1.0
            );

            earlyRate = 1.0 - Math.Pow(
                1.0 - fraction, 1.0 / knee
            );
        }

        // Nach dem Knickpunkt langsameres Wachstum.
        double lateRate = earlyRate *
            Math.Clamp(flatten, 0.0, 1.0);

        double result = end + (start - end)
            * Math.Pow(1.0 - earlyRate, earlyLuck)
            * Math.Pow(1.0 - lateRate, lateLuck);

        return (decimal)Math.Clamp(result, 0.0, 100.0);
    }
}
