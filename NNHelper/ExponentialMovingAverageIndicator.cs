namespace NNHelper
{
    public class ExponentialMovingAverageIndicator
    {
        private bool isInitialized;
        private readonly int lookback;
        private readonly double weightingMultiplier;
        private double previousAverage;

        public double Average { get; private set; }
        public double Slope { get; private set; }

        public ExponentialMovingAverageIndicator(int lookback)
        {
            this.lookback = lookback;
            weightingMultiplier = 2.0 / (lookback + 1);
        }

        public void AddDataPoint(double dataPoint)
        {
            if (!isInitialized)
            {
                Average = dataPoint;
                Slope = 0;
                previousAverage = Average;
                isInitialized = true;
                return;
            }

            Average = (dataPoint - previousAverage) * weightingMultiplier + previousAverage;
            Slope = Average - previousAverage;

            //update previous average
            previousAverage = Average;
        }
    }
}
