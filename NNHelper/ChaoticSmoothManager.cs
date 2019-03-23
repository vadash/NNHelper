using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NNHelper
{
    public class ChaoticSmoothManager
    {
        private const int TIME_TO_SMOOTH_MS = 1000;
        private const int HOW_MANY_MOVEMENTS = 6;
        private const float TOLERANCE = 0.01f;
        private readonly List<(float dx, float dy, float time)> lastMovementList = new List<(float, float, float)>();
        private readonly Stopwatch mainTimer = new Stopwatch();
        private readonly List<(long smoothTill, float smooth, float angle)> smoothList = new List<(long smoothTill, float smooth, float angle)>();

        public ChaoticSmoothManager()
        {
            mainTimer.Start();
        }

        public void AddPoint(float dx, float dy)
        {
            if (Math.Abs(dx) < 3f + TOLERANCE && Math.Abs(dy) < 3f + TOLERANCE) return;
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

            if (smoothList.Count > 10)
            {
                
            }
        }

        private void Update()
        {
            var angle = CalculateAverageAngle();
            var smooth = ApproximateChaosSmoothFull(angle);
            if (smooth < 0.9f)
            {
                smoothList.Add((mainTimer.ElapsedMilliseconds + TIME_TO_SMOOTH_MS, smooth, angle));
            }
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
        //private static float ApproximateChaosSmoothSimple(float averageAngle)
        //{
        //    if (averageAngle < 15f)
        //        return 1f;
        //    if (averageAngle < 25f)
        //        return 0.5f;
        //    if (averageAngle < 35f)
        //        return 0.33f;
        //    if (averageAngle < 45f)
        //        return 0.25f;
        //    if (averageAngle < 55f)
        //        return 0.2f;
        //    return 0.15f;
        //}

        /// <summary>
        /// linear fit {{15, 1}, {30, 0.5}, {45, 0.33}, {90, 0.2}}
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        private static float ApproximateChaosSmoothFull(float angle)
        {
            float tmp;
            if (angle <= 15)
            {
                tmp = 1f;
            }
            else if (angle > 15 && angle <= 30)
            {
                tmp = 1.5f - 0.0333333f * angle;
            }
            else if (angle > 30 && angle <= 45)
            {
                tmp = 0.84f - 0.0113333f * angle;
            }
            else if (angle > 45 && angle <= 90)
            {
                tmp = 0.46f - 0.00288889f * angle;
            }
            else
            {
                tmp = 0.2f;
            }
            return tmp;
        }

        public float GetSmooth()
        {
            //var minSmooth = 1f;
            //var aveSmooth = 0f;
            var complexSmooth = 0f;
            var weightSum = 0f;
            var i = 0;
            foreach (var (smoothTill, smoothCoeff, _) in smoothList)
            {
                var currentTime = mainTimer.ElapsedMilliseconds;
                if (mainTimer.ElapsedMilliseconds < smoothTill)
                {
                    //minSmooth = Math.Min(minSmooth, smoothCoeff);
                    //aveSmooth += smoothCoeff;
                    var currentWeight = (smoothTill - currentTime) / (float)TIME_TO_SMOOTH_MS;
                    complexSmooth += currentWeight * smoothCoeff;
                    weightSum += currentWeight;
                    i++;
                }
            }

            //aveSmooth /= i;
            complexSmooth /= weightSum;

            return i == 0 ? 1f : complexSmooth;
        }
    }
}
