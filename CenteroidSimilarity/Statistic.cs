using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CenteroidSimilarity
{
    public class Statistic
    {
        private readonly double[] data;
        private bool isCalculated = false;
        private double _sum, _min, _max, _mean, _stdDev;

        public Statistic(int[] intData)
        {
            data = new double[intData.Length];
            Array.Copy(intData, data, intData.Length);
        }

        public Statistic(float[] floatData)
        {
            data = new double[floatData.Length];
            Array.Copy(floatData, data, floatData.Length);
        }

        public Statistic(double[] doubleData)
        {
            data = new double[doubleData.Length];
            Array.Copy(doubleData, data, doubleData.Length);
        }

        private void Calculate()
        {
            _min = double.MaxValue;
            _max = double.MinValue;
            _sum = 0;

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] > _max)
                    _max = data[i];
                if (data[i] < _min)
                    _min = data[i];
                _sum += data[i];
            }
            _mean = _sum / data.Length;

            double _variance = 0;
            for (int i = 0; i < data.Length; i++)
            {
                _variance += Math.Pow(data[i] - _mean, 2);
            }
            _variance /= data.Length;
            _stdDev = Math.Sqrt(_variance);

            isCalculated = true;
        }

        public double Min
        {
            get
            {
                if (!isCalculated)
                    Calculate();
                return _min;
            }
        }

        public double Max
        {
            get
            {
                if (!isCalculated)
                    Calculate();
                return _max;
            }
        }

        public double Mean
        {
            get
            {
                if (!isCalculated)
                    Calculate();
                return _mean;
            }
        }

        public double Sum
        {
            get
            {
                if (!isCalculated)
                    Calculate();
                return _sum;
            }
        }
        public double StdDev
        {
            get
            {
                if (!isCalculated)
                    Calculate();
                return _stdDev;
            }
        }
    }
}
