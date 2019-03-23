using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NNHelper
{
    public class ChaoticSmoothManager
    {
        private const int TIME_TO_SMOOTH_MS = 250;
        private const int HOW_MANY_MOVEMENTS = 4;
        private const float TOLERANCE = 0.01f;
        private readonly List<(float dx, float dy, float time)> lastMovementList = new List<(float, float, float)>();
        private readonly Stopwatch mainTimer = new Stopwatch();
        private readonly List<(long smoothTill, float smooth)> smoothList = new List<(long smoothTill, float smooth)>();

        public ChaoticSmoothManager()
        {
            mainTimer.Start();
        }

        public void AddPoint(float dx, float dy)
        {
            if (Math.Abs(dx) < 1f && Math.Abs(dy) < 1f) return;
            lastMovementList.Add((dx, dy, mainTimer.ElapsedMilliseconds));
            CleanOld();
            Update();
        }

        private void CleanOld()
        {
            if (lastMovementList.Count > HOW_MANY_MOVEMENTS)
            {
                lastMovementList.RemoveAt(0);
            }
            if (smoothList.Count > 0 && mainTimer.ElapsedMilliseconds > smoothList[0].smoothTill)
            {
                smoothList.RemoveAt(0);
            }
        }

        private void Update()
        {
            var angle = CalculateAverageAngle();
            smoothList.Add((mainTimer.ElapsedMilliseconds + TIME_TO_SMOOTH_MS, ApproximateChaosSmoothSimple(angle)));
        }

        private float CalculateAverageAngle()
        {
            float averageAngle = 0;
            if (lastMovementList.Count < Math.Max(3, HOW_MANY_MOVEMENTS / 2))
            {
                return averageAngle;
            }
            for (var index = 0; index < lastMovementList.Count - 1; index++)
            {
                var (a1, a2, _) = lastMovementList[index];
                var (b1, b2, _) = lastMovementList[index + 1];
                var cosAngle = (a1 * b1 + a2 * b2) / (Math.Sqrt(a1 * a1 + a2 * a2) * Math.Sqrt(b1 * b1 + b2 * b2));
                if (cosAngle >= 1f && cosAngle <= 1f + TOLERANCE) cosAngle = 1f;
                if (cosAngle <= -1f && cosAngle >= -1f - TOLERANCE) cosAngle = -1f;
                var angle = (float)Math.Acos(cosAngle);
                if (float.IsNaN(angle)) angle = float.MaxValue;
                averageAngle += angle;
            }
            averageAngle *= 180f / (float)Math.PI;
            averageAngle /= lastMovementList.Count;
            return averageAngle;
        }

        // ReSharper disable once UnusedMember.Local
        private static float ApproximateChaosSmoothSimple(float averageAngle)
        {
            if (averageAngle < 30f)
                return 1f;
            if (averageAngle < 60f)
                return 0.5f;
            return 0.33f;
        }

        /// <summary>
        /// linear fit {{30, 1}, {60, 0.5}, {90, 0.25}
        /// </summary>
        /// <param name="averageAngle"></param>
        /// <returns></returns>
        private static float ApproximateChaosSmoothFull(float averageAngle)
        {
            if (averageAngle < 30) averageAngle = 30;
            if (averageAngle > 90) averageAngle = 90;
            var tmp = (float)(1.33333 - 0.0125 * averageAngle);
            if (tmp < 0.25) tmp = 0.25f;
            if (tmp > 1f) tmp = 1f;
            return tmp;
        }

        public float GetSmooth()
        {
            var minSmooth = 1f;
            foreach (var (smoothTill, smoothCoeff) in smoothList)
            {
                if (mainTimer.ElapsedMilliseconds < smoothTill)
                {
                    minSmooth = Math.Min(minSmooth, smoothCoeff);
                }
            }
            return minSmooth;
        }
    }
}
