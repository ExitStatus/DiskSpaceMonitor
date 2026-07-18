using System;

namespace DiskSpaceMonitor.Drives
{
    /// <summary>
    /// Pure gauge maths, kept free of any UI type so it can be unit tested: the
    /// used fraction of a drive and the fill level that drives its colour.
    /// </summary>
    public static class DiskGauge
    {
        /// <summary>Used space as a fraction of capacity, clamped to [0, 1].</summary>
        public static double UsedFraction(long usedBytes, long totalBytes)
        {
            if (totalBytes <= 0)
                return 0;

            return Math.Clamp((double)usedBytes / totalBytes, 0, 1);
        }

        /// <summary>Default free fraction below which the ring turns to the "low" colour.</summary>
        public const double DefaultLowThreshold = 0.40;

        /// <summary>Default free fraction below which the ring turns to the "critical" colour.</summary>
        public const double DefaultCriticalThreshold = 0.15;

        /// <summary>Fill level from the free fraction (1 - used) using the default thresholds.</summary>
        public static DiskFillLevel LevelForFree(double freeFraction) =>
            LevelForFree(freeFraction, DefaultLowThreshold, DefaultCriticalThreshold);

        /// <summary>
        /// Fill level from the free fraction (1 - used): above <paramref name="lowThreshold"/> is
        /// healthy, above <paramref name="criticalThreshold"/> is low (warning), else critical.
        /// </summary>
        public static DiskFillLevel LevelForFree(double freeFraction, double lowThreshold, double criticalThreshold)
        {
            if (freeFraction > lowThreshold)
                return DiskFillLevel.Healthy;
            if (freeFraction > criticalThreshold)
                return DiskFillLevel.Warning;
            return DiskFillLevel.Critical;
        }
    }
}
