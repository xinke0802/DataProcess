using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataProcess.Utils
{
    class FloatPointUtils
    {
        public const double EPS = 1e-8;

        public static bool Equals(double x, double y, double eps = EPS)
        {
            return Math.Abs(x - y) < eps;
        }

        public static bool LessThan(double x, double y, double eps = EPS)
        {
            return x + eps < y;
        }

        public static bool GreaterThan(double x, double y, double eps = EPS)
        {
            return x - eps > y;
        }
    }
}
