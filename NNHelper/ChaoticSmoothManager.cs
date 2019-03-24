using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NNHelper
{
    public class ChaoticSmoothManager
    {
        private const int TIME_TO_SMOOTH_MS = 1000;
        private const int TIME_TO_TRACK_MS = 1000;
        private const float TOLERANCE = 0.01f;
        private readonly List<(float dx, float dy, float validTill)> lastMovementList = new List<(float, float, float)>();
        private readonly Stopwatch mainTimer = new Stopwatch();
        private readonly List<(long smoothTill, float smooth, float angle)> smoothList = new List<(long, float, float)>();

        public ChaoticSmoothManager()
        {
            mainTimer.Start();
        }

        public void AddPoint(float dx, float dy)
        {
            if (Math.Abs(dx) < 1f + TOLERANCE && Math.Abs(dy) < 1f + TOLERANCE) return;
            lastMovementList.Add((dx, dy, mainTimer.ElapsedMilliseconds + TIME_TO_TRACK_MS));
            CleanOld();
            Update();
        }

        private void CleanOld()
        {
            if (lastMovementList.Count > 0 && mainTimer.ElapsedMilliseconds > lastMovementList[0].validTill)
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
            var smooth = ApproximateChaosSmoothFull(angle);
            smoothList.Add((mainTimer.ElapsedMilliseconds + TIME_TO_SMOOTH_MS, smooth, angle));
        }

        private float CalculateAverageAngle()
        {
            float averageAngle = 0;
            if (lastMovementList.Count < 2)
            {
                return averageAngle;
            }
            for (var index = 0; index < lastMovementList.Count - 1; index++)
            {
                var (a1, a2, _) = lastMovementList[index];
                var (b1, b2, _) = lastMovementList[index + 1];
                var cosAngle = (a1 * b1 + a2 * b2) / (Math.Sqrt(a1 * a1 + a2 * a2) * Math.Sqrt(b1 * b1 + b2 * b2));
                if (Math.Abs(cosAngle) >= 1f - TOLERANCE && Math.Abs(cosAngle) <= 1f + TOLERANCE) cosAngle = Math.Sign(cosAngle);
                var angle = (float)Math.Acos(cosAngle);
                averageAngle += angle;
            }
            averageAngle *= 180f / (float)Math.PI;
            averageAngle /= lastMovementList.Count;
            return averageAngle;
        }

        /// <summary>
        /// quadratic fit {{0, 1}, {50, 0.5}, {90, 0.1}}
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private static float ApproximateChaosSmoothFull(float x)
        {
            if (x < 0)
                return 1f;
            if (x > 90)
                return 0.1f;
            var tmp = 1f - 0.01f * x;
            return Math.Min(tmp, 1);
        }

        //private static float ApproximateChaosSmoothSimple(float angle)
        //{
        //    float tmp;
        //    if (angle <= 15)
        //    {
        //        tmp = 1f;
        //    }
        //    else if (angle > 15 && angle <= 30)
        //    {
        //        tmp = 0.75f;
        //    }
        //    else if (angle > 30 && angle <= 45)
        //    {
        //        tmp = 0.5f;
        //    }
        //    else if (angle > 45 && angle <= 90)
        //    {
        //        tmp = 0.33f;
        //    }
        //    else
        //    {
        //        tmp = 0.25f;
        //    }
        //    return tmp;
        //}

        public float GetSmooth()
        {
            return 1f;
            var complexSmooth = 0f;
            var weightSum = 0f;
            foreach (var (smoothTill, smoothCoeff, _) in smoothList)
            {
                var currentTime = mainTimer.ElapsedMilliseconds;
                if (mainTimer.ElapsedMilliseconds >= smoothTill) continue;
                var currentWeight = smoothTill - currentTime;
                if (float.IsNaN(currentWeight) || float.IsInfinity(currentWeight)) continue;
                complexSmooth += currentWeight * smoothCoeff;
                weightSum += currentWeight;
            }
            complexSmooth /= weightSum;
            return Math.Abs(weightSum) < TOLERANCE ? 1f : complexSmooth;
        }
    }
}
