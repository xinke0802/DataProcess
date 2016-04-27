using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Lava.Visual;
using System.Diagnostics;
using System.Windows.Controls.Primitives;
using System.Windows.Shapes;
using ClipperLib;

namespace DataProcess.Utils
{
    public abstract class AbstractSegment
    {
        public virtual double GetLength()
        {
            double len = 0;
            Point2D lastPoint = null;
            for (double t = 0; t <= 1; t += 0.1)
            {
                var pt = new Point2D(GetXbyT(t), GetYbyT(t));
                if (lastPoint != null)
                {
                    len += GeometryUtils.GetDistance(lastPoint, pt);
                }
                lastPoint = pt;
            }
            return len;
        }

        public virtual double GetTByLength(double length)
        {
            throw new NotImplementedException();
        }

        public virtual Point GetLastPoint()
        {
            throw new NotImplementedException();
        }

        public virtual double GetXbyT(double t)
        {
            throw new NotImplementedException();
        }

        public virtual double GetYbyT(double t)
        {
            throw new NotImplementedException();
        }

        public virtual double GetTbyX(double x)
        {
            throw new NotImplementedException();
        }

        public virtual double GetTbyY(double y)
        {
            throw new NotImplementedException();
        }

        public virtual double GetGradientXbyT(double t)
        {
            throw new NotImplementedException();
        }

        public virtual double GetGradientYbyT(double t)
        {
            throw new NotImplementedException();
        }

        public static AbstractSegment GetAbstractSegment(List<Point> controlPoints)
        {
            if (controlPoints == null || controlPoints.Count < 2 || controlPoints.Count > 4)
            {
                throw new ArgumentException();
            }
            switch (controlPoints.Count)
            {
                case 2:
                    return new Line(controlPoints[0], controlPoints[1]);
                case 3:
                    return new QuadraticBezier(controlPoints);
                case 4:
                    return new CubicBezier(controlPoints);
                default:
                    throw new ArgumentException();
            }
        }
    }

    public class CompositeBezier
    {
        private List<Point> _controlPoints;
        public CompositeBezier(List<Point> controlPoints)
        {
            _controlPoints = controlPoints;
        }

        public double GetStartGradientTheta()
        {
            return GetTheta(_controlPoints[0], _controlPoints[1]);
        }

        public double GetEndGradientTheta()
        {
            return GetTheta(_controlPoints[_controlPoints.Count - 2], _controlPoints.Last());
        }

        private double GetTheta(Point p1, Point p2)
        {
            return Math.Atan((p2.Y - p1.Y)/(p2.X - p1.X));
        }
    }

    public class CubicBezier : AbstractSegment
    {
        public Point Point1 { get; protected set; }
        public Point Point2 { get; protected set; }
        public Point Point3 { get; protected set; }
        public Point Point4 { get; protected set; }

        public CubicBezier(List<Point> pts)
        {
            if (pts == null || pts.Count != 4)
            {
                throw new ArgumentException();
            }
            Point1 = pts[0];
            Point2 = pts[1];
            Point3 = pts[2];
            Point4 = pts[3];
        }

        public CubicBezier(Point p1, Point p2, Point p3, Point p4)
        {
            Point1 = p1;
            Point2 = p2;
            Point3 = p3;
            Point4 = p4;
        }

        
        public override double GetXbyT(double t)
        {
            double oneMinusT = 1 - t;
            return Math.Pow(oneMinusT, 3)*Point1.X + 3*oneMinusT*oneMinusT*t*Point2.X + 3*oneMinusT*t*t*Point3.X +
                   Math.Pow(t, 3)*Point4.X;
        }

        public override double GetYbyT(double t)
        {
            double oneMinusT = 1 - t;
            return Math.Pow(oneMinusT, 3) * Point1.Y + 3 * oneMinusT * oneMinusT * t * Point2.Y + 3 * oneMinusT * t * t * Point3.Y +
                   Math.Pow(t, 3) * Point4.Y;
        }

        public override double GetGradientXbyT(double t)
        {
            double oneMinusT = 1 - t;
            return 3*oneMinusT*oneMinusT*(Point2.X - Point1.X) + 6*oneMinusT*t*(Point3.X - Point2.X) +
                   3*t*t*(Point4.X - Point3.X);
        }

        public override double GetGradientYbyT(double t)
        {
            double oneMinusT = 1 - t;
            return 3 * oneMinusT * oneMinusT * (Point2.Y - Point1.Y) + 6 * oneMinusT * t * (Point3.Y - Point2.Y) +
                   3 * t * t * (Point4.Y - Point3.Y);
        }

        public override double GetTbyX(double x)
        {
            if ((Point4.X - x) * (x - Point1.X) < 0)
            {
                if (x > Point4.X)
                {
                    return 1;
                }
                return 0;
                //throw new Exception("Cannot find t by x!");
            }
            double epsilon = 0.001;
            return Util.BinarySearch(x, GetXbyT, 0, 1, epsilon);
        }

        public override double GetTbyY(double y)
        {
             double epsilon = 0.001;
            if (Math.Abs(y - Point1.Y) < 1e-3)
            {
                return 0;
            }
            if (Math.Abs(y - Point4.Y) < 1e-3)
            {
                return 1;
            }
            return Util.BinarySearch(y, GetYbyT, 0, 1, epsilon);
        }
    }

    public class Line : AbstractSegment
    {
        private Point _p1;
        private Point _p2;
        private double _k, _b;
        public Line(Point p1, Point p2)
        {
            _p1 = p1;
            _p2 = p2;
            GeometryUtils.GetLineParameter(new Point2D(_p1), new Point2D(_p2), out _k, out _b);
        }

        public Line(List<Point> pts)
        {
            if (pts == null || pts.Count != 2)
            {
                throw new ArgumentException();
            }
            _p1 = pts[0];
            _p2 = pts[1];
        }

        public override double GetLength()
        {
            return Maths.GetDistance(_p1.X, _p1.Y, _p2.X, _p2.Y);
        }

        public override double GetTByLength(double length)
        {
            return length/GetLength();
        }

        public override Point GetLastPoint()
        {
            return _p2;
        }

        public override double GetXbyT(double t)
        {
            return Maths.GetIntermediateNumber(_p1.X, _p2.X, t);
        }

        public override double GetYbyT(double t)
        {
            return Maths.GetIntermediateNumber(_p1.Y, _p2.Y, t);
        }

        public override double GetTbyX(double x)
        {
            return Maths.GetLambda(_p1.X, _p2.X, x);
        }

        public override double GetTbyY(double y)
        {
            return Maths.GetLambda(_p1.Y, _p2.Y, y);
        }

        public override double GetGradientXbyT(double t)
        {
            return _p2.X - _p1.X;
        }

        public override double GetGradientYbyT(double t)
        {
            return _p2.Y - _p1.Y;
        }
    }

    public class QuadraticBezier : AbstractSegment
    {
        private Point _p1;
        private Point _p2;
        private Point _p3;

        public Point Point1 { get { return _p1; } }
        public Point Point2 { get { return _p2; } }
        public Point Point3 { get { return _p3; } }

        public QuadraticBezier(Point p1, Point p2, Point p3, bool isModifyControlPoint = false)
        {
            _p1 = p1;
            _p2 = p2;
            _p3 = p3;
            if (isModifyControlPoint)
            {
                if ((_p3.X - _p2.X)*(_p2.X - _p1.X) < 0)
                {
                    double lambda = Maths.GetLambda(_p1.X, _p3.X, _p2.X);
                    double eps = 1e-3;
                    lambda = Maths.Truncate(lambda, eps, 1 - eps);
                    _p2.X = Maths.GetIntermediateNumber(_p1.X, _p3.X, lambda);
                }
                if ((_p3.Y - _p2.Y) * (_p2.Y - _p1.Y) < 0)
                {
                    double lambda = Maths.GetLambda(_p1.Y, _p3.Y, _p2.Y);
                    double eps = 1e-3;
                    lambda = Maths.Truncate(lambda, eps, 1 - eps);
                    _p2.Y = Maths.GetIntermediateNumber(_p1.Y, _p3.Y, lambda);
                }
            }
        }

        public QuadraticBezier(List<Point> points)
        {
            if (points == null || points.Count != 3)
            {
                throw new ArgumentException();
            }
            _p1 = points[0];
            _p2 = points[1];
            _p3 = points[2];
        }

        public override Point GetLastPoint()
        {
            return Point3;
        }

        public void GetPerpendicularLineParameter(double t, out double A, out double B, out double C)
        {
            var x = GetXbyT(t);
            var y = GetYbyT(t);
            var normVec = GetPerpendicularNormVector(t);
            GeometryUtils.GetLineParameter(new Point2D(x, y), Math.Atan2(normVec.Y, normVec.X), out A, out B, out C);
        }

        public Point GetPerpendicularNormVector(double t)
        {
            var gradX = GetGradientXbyT(t);
            var gradY = GetGradientYbyT(t);
            var gradNorm = Math.Sqrt(gradX*gradX + gradY*gradY);
            return new Point(-gradY/gradNorm, gradX/gradNorm);
        }

        public Point GetNearestPoint(Point p, out double nnDistanceSquare, out double nnt)
        {
            double a, b, c, d;
            GetDistanceSquareGradient(p, out a, out b, out c, out d);
            var res = Maths.SolveCubic(a, b, c, d);
            double eps = 1e-6;
            List<double> ts = new SupportClass.EquatableList<double>();
            foreach (var complex in res)
            {
                if (Math.Abs(complex.Imaginary) < 1e-6 && complex.Real <= 1 && complex.Real >= 0)
                {
                    ts.Add(complex.Real);
                }
            }
            if (ts.Count == 0)
            {
                ts.Add(0);
                ts.Add(1);
            }

            double minDis = double.MaxValue;
            double minDisT = double.NaN;
            foreach (var t in ts)
            {
                var nnp = GetPointByT(t);
                var dis = GeometryUtils.GetDistance(nnp, p);
                if (dis < minDis)
                {
                    minDis = dis;
                    minDisT = t;
                }
            }
            nnDistanceSquare = minDis * minDis;
            nnt = minDisT;
            return GetPointByT(minDisT);
        }

        public void GetDistanceSquareGradient(Point p, out double a, out double b,  out double c, out double d)
        {
            double a2 = _p1.X - 2 * _p2.X + _p3.X;
            double a1 = 2 * (_p2.X - _p1.X);
            double a0 = _p1.X - p.X;
            double b2 = _p1.Y - 2 * _p2.Y + _p3.Y;
            double b1 = 2 * (_p2.Y - _p1.Y);
            double b0 = _p1.Y - p.Y;
            a = 4 * (a2 * a2 + b2 * b2);
            b = 6 * (a1 * a2 + b1 * b2);
            c = 2 * (a1 * a1 + 2 * a0 * a2 + b1 * b1 + 2 * b0 * b2);
            d = 2 * (a0 * a1 + b0 * b1);
        }

        public override double GetTByLength(double length)
        {
            return Util.BinarySearch(length, GetLengthByT, 0, 1, 0.01);
        }

        public double GetLengthByT(double t)
        {
            return GetPartialBezier(0, t).GetLength();
        }

        public override double GetLength()
        {
            // closed-form solution to elliptic integral for arc length
           var ax = Point1.X - 2*Point2.X + Point3.X;
           var ay = Point1.Y - 2*Point2.Y + Point3.Y;
           var bx = 2*Point2.X - 2*Point1.X;
           var by = 2*Point2.Y - 2*Point1.Y;
       
           var a = 4*(ax*ax + ay*ay);
           var b = 4*(ax*bx + ay*by);
           var c = bx*bx + by*by;
       
           var abc = 2*Math.Sqrt(a+b+c);
           var a2  = Math.Sqrt(a);
           var a32 = 2*a*a2;
           var c2  = 2*Math.Sqrt(c);
           var ba  = b/a2;

           var res = (a32*abc + a2*b*(abc - c2) + (4*c*a - b*b)*Math.Log((2*a2 + ba + abc)/(ba + c2)))/(4*a32);
            if (double.IsNaN(res))
            {
                return GeometryUtils.GetDistance(Point1, Point3);
            }
            else
            {
                return res;
            }
        }

        /// <summary>
        /// Calculate the length of the partial curve [0, t]
        /// http://stackoverflow.com/questions/11854907/calculate-the-length-of-a-segment-of-a-quadratic-bezier
        /// </summary>
        //public double GetLengthByT(double t)
        //{
        //    double ax = Point1.X - 2 * Point2.X + Point3.X;
        //    double ay = Point1.Y - 2 * Point2.Y + Point3.Y;
        //    double bx = 2 * Point2.X - 2 * Point1.X;
        //    double by = 2 * Point2.Y - 2 * Point1.Y;
        //    double A = 4*(ax*ax + ay*ay);
        //    double B = 4*(ax*bx + ay*by);
        //    double C = bx*bx + by*by;
        //    double b = B/(2*A);
        //    double c = C/A;
        //    double u = t+b;
        //    double k = c - b*b;
        //    double sqrtU2pk = Math.Sqrt(u*u + k);
        //    double sqrtB2pk = Math.Sqrt(b*b + k);
        //    if (A == 0 ||  b + sqrtB2pk == 0)
        //    {
        //        return GeometryUtils.GetDistance(Point1, Point3) * t;
        //    }
        //    return Math.Sqrt(A)/2*(u*sqrtU2pk - b*sqrtB2pk + Math.Log(Math.Abs((u + sqrtU2pk)/(b + sqrtB2pk))));
        //}

        public override double GetTbyX(double x)
        {
            if ((_p3.X - _p2.X) * (_p2.X - _p1.X) < 0)
            {
                throw new Exception("Cannot find t by x!");
            }
            if ((_p3.X - x)*(x - _p1.X) < 0)
            {
                throw new Exception("Cannot find t by x!");
            }
            double epsilon = 0.001*Math.Min(Math.Abs(_p3.X - _p1.X), Math.Abs(_p3.Y - _p1.Y));
            return Util.BinarySearch(x, GetXbyT, 0, 1, epsilon);
        }

        public double GetTbyY(double y)
        {
            double epsilon = 0.001 * Math.Min(Math.Abs(_p3.X - _p1.X), Math.Abs(_p3.Y - _p1.Y));
            return Util.BinarySearch(y, GetYbyT, 0, 1, epsilon);
        }

        public Point GetPointByT(double t)
        {
            return new Point(GetXbyT(t), GetYbyT(t));
        }

        public override double GetXbyT(double t)
        {
            return (1 - t)*(1 - t)*_p1.X + 2*t*(1 - t)*_p2.X + t*t*_p3.X;
        }

        public override double GetYbyT(double t)
        {
            return (1 - t)*(1 - t)*_p1.Y + 2*t*(1 - t)*_p2.Y + t*t*_p3.Y;
        }

        public double GetGradientThetaByT(double t)
        {
            var gradX = GetGradientXbyT(t);
            var gradY = GetGradientYbyT(t);
            return Math.Atan2(gradY, gradX);
        }

        public override double GetGradientXbyT(double t)
        {
            return - 2 * (1 - t) * _p1.X + 2 * (1 - 2 * t) * _p2.X + 2 * t * _p3.X;
        }

        public override double GetGradientYbyT(double t)
        {
            return -2 * (1 - t) * _p1.Y + 2 * (1 - 2 * t) * _p2.Y + 2 * t * _p3.Y;
        }

        public QuadraticBezier GetPartialBezier(double ta, double tb)
        {
            if (ta > tb)
            {
                Util.Swap(ref ta, ref tb);
            }

            var p1x = GetXbyT(ta);
            var p1y = GetYbyT(ta);
            var p3x = GetXbyT(tb);
            var p3y = GetYbyT(tb);
            var p2x = (1 - ta) * (1 - tb) * _p1.X + (ta + tb - 2 * ta * tb) * _p2.X + (ta * tb) * _p3.X;
            var p2y = (1 - ta) * (1 - tb) * _p1.Y + (ta + tb - 2 * ta * tb) * _p2.Y + (ta * tb) * _p3.Y;
            return new QuadraticBezier(new Point(p1x, p1y), new Point(p2x, p2y), new Point(p3x, p3y));
        }
    }

    public class GeometryUtils
    {
        public static List<Point> GetStartFromTopPolygon(List<Point> polygon)
        {
            int maxYIndex = 0;
            double maxY = double.MinValue;

            for (int i = 0; i < polygon.Count; i++)
            {
                var y = polygon[i].Y;
                if (y > maxY)
                {
                    maxY = y;
                    maxYIndex = i;
                }
            }

            List<Point> polygon2 = new List<Point>();
            for (int i = 0; i < polygon.Count; i++)
            {
                polygon2.Add(polygon[(i + maxYIndex)%polygon.Count]);
            }
            return polygon2;
        }

        public static List<List<Point>> GetUnionPolygon(List<List<Point>> polygons)
        {
            if (polygons.Count == 0)
            {
                return new List<List<Point>>();
            }
            else if (polygons.Count == 1)
            {
                return polygons;
            }

            
            var intPolygons =
                polygons.ConvertAll(list => list.ConvertAll(pt => new IntPoint(Math.Round(pt.X), Math.Round(pt.Y))));

            List<List<IntPoint>> solution = new List<List<IntPoint>>();
            Clipper c = new Clipper();
            c.AddPaths(intPolygons, PolyType.ptClip, true);
            var succeed = c.Execute(ClipType.ctUnion, solution, PolyFillType.pftNonZero, PolyFillType.pftNonZero);
            if (!succeed)
            {
                throw new ArgumentException();
            }

            return solution.ConvertAll(list => list.ConvertAll(pt => new Point(pt.X, pt.Y)));
        }

        public static double GetClosedBezierCurveLength(List<Point[]> bezierCurves)
        {
            double length = 0;
            foreach (var bezierCurve in bezierCurves)
            {
                var seg = AbstractSegment.GetAbstractSegment(bezierCurve.ToList());
                length += seg.GetLength();
            }
            return length;
        }

        public static List<Point> ConvertClosedBezierCurveToPolygon(List<Point[]> bezierCurves, int interpolPointCount)
        {
            Dictionary<AbstractSegment, double> seg2LenDict = new Dictionary<AbstractSegment, double>();
            foreach (var bezierCurve in bezierCurves)
            {
                var seg = AbstractSegment.GetAbstractSegment(bezierCurve.ToList());
                seg2LenDict.Add(seg, seg.GetLength());
            }

            var avgLength = seg2LenDict.Sum(kvp => kvp.Value)/interpolPointCount;
            double curLength = 0;
            double prevSegLengthSum = 0;
            List<Point> controlPoints = new List<Point>();
            foreach (var kvp in seg2LenDict)
            {
                var bezier = kvp.Key;
                var segLength = kvp.Value;
                while (curLength - prevSegLengthSum <= segLength)
                {
                    double t = (curLength - prevSegLengthSum)/segLength;
                    controlPoints.Add(new Point(bezier.GetXbyT(t), bezier.GetYbyT(t)));

                    curLength += avgLength;
                }
                prevSegLengthSum += segLength;
            }

            if (controlPoints.Count == interpolPointCount + 1)
            {
                controlPoints.Remove(controlPoints.Last());
            }
            if (controlPoints.Count != interpolPointCount)
            {
                throw new ArgumentException();
            }

            return controlPoints;
        }

        public static List<Point> GetInterpolatePoints(Point startPoint, Point endPoint, int interpolPointCount)
        {
            List<Point> pts = new List<Point>();
            for (int i = 0; i < interpolPointCount; i++)
            {
                var lambda = ((double) i)/(interpolPointCount - 1);
                pts.Add(GeometryUtils.GetIntermediatePoint(startPoint, endPoint, lambda));
            }
            return pts;
        }

        /// <summary>
        /// Assumption: control points' x values or y values changes monotonously 
        /// </summary>
        /// <returns></returns>
        public static List<Point> GetInterpolatePointsNew(List<Point> controlPoints, int interpolPointCount)
        {
            if (controlPoints.Count == 0)
            {
                return new List<Point>();
            }
            if (controlPoints.Count == 1)
            {
                var list = new List<Point>();
                for (int i = 0; i < interpolPointCount; i++)
                {
                    list.Add(controlPoints[0]);
                }
                return list;
            }

            controlPoints = new List<Point>(controlPoints);
            bool isTranspose = ((controlPoints.Max(pt => pt.Y) - controlPoints.Min(pt => pt.Y)) > 1.3 * (controlPoints.Max(pt => pt.X) - controlPoints.Min(pt => pt.X))) 
                && Util.IsMonotonous(controlPoints.ConvertAll(pt=>pt.Y));
            if (isTranspose)
            {
                controlPoints = controlPoints.ConvertAll(pt => new Point(pt.Y, pt.X));
            }
            bool isReverse = controlPoints.Last().X < controlPoints.First().X;
            if (isReverse)
            {
                controlPoints.Reverse();
            }

            var interpolPts = GetInterpolatePoints(controlPoints, interpolPointCount);

            if (isReverse)
            {
                interpolPts.Reverse();
            }
            if (isTranspose)
            {
                interpolPts = interpolPts.ConvertAll(pt => new Point(pt.Y, pt.X));
            }

            return interpolPts;
        }

        /// <summary>
        /// Assumption: control points' x values keep increasing
        /// </summary>
        /// <returns></returns>
        public static List<Point> GetInterpolatePoints(List<Point> controlPoints, int interpolPointCount)
        {
            var segments = GetCurvePathSegments(controlPoints.ConvertAll(p => new Point2D(p)));
            double startX = controlPoints.First().X;
            var endX = controlPoints.Last().X;
            List<double> lambdas = new List<double>();
            for (int i = 0; i < interpolPointCount; i++)
            {
                lambdas.Add(i/(interpolPointCount - 1.0));
            }
            var segmentEnum = segments.GetEnumerator();
            segmentEnum.MoveNext();
            List<Point> newControlPoints = new SupportClass.EquatableList<Point>();
            foreach (var lambda in lambdas)
            {
                double x = Maths.GetIntermediateNumber(startX, endX, lambda);
                while (segmentEnum.Current.Last().X < x)
                {
                    segmentEnum.MoveNext();
                }
                var segment = AbstractSegment.GetAbstractSegment(segmentEnum.Current.ToList());
                var t = segment.GetTbyX(x);
                newControlPoints.Add(new Point(segment.GetXbyT(t), segment.GetYbyT(t)));
            }
            return newControlPoints;
        }

        public static void GetWideLineEndPoints(Point midEndPoint, Point controlPoint, double width, out Point topPoint, out Point bottomPoint)
        {
            double gradX = controlPoint.X - midEndPoint.X;
            double gradY = controlPoint.Y - midEndPoint.Y;
            double norm = Math.Sqrt(gradX*gradX + gradY*gradY);
            double dx = -gradY/norm*width;
            double dy = gradX/norm*width;
            if (dy < 0)
            {
                topPoint = new Point(midEndPoint.X + dx, midEndPoint.Y + dy);
                bottomPoint = new Point(midEndPoint.X - dx, midEndPoint.Y - dy);
            }
            else
            {
                bottomPoint = new Point(midEndPoint.X + dx, midEndPoint.Y + dy);
                topPoint = new Point(midEndPoint.X - dx, midEndPoint.Y - dy);
            }
        }

        public static List<Point[]> GetCurvePathSegments(List<Point> p)
        {

            if (p.Count <= 1)
                return null;

            var startPoint = new Point(p[0].X, p[0].Y);
            List<Point[]> segments = new List<Point[]>();

            if (p.Count == 2)
            {
                segments.Add(new[] { startPoint, new Point(p[1].X, p[1].Y) });
            }
            else if (p.Count == 3)
            {
                segments.Add(new[] { startPoint, new Point(p[1].X, p[1].Y), new Point(p[2].X, p[2].Y) });
            }
            else if (p.Count == 4)
            {

                segments.Add(new[]
                {
                    startPoint,
                    new Point(p[1].X, p[1].Y),
                    new Point(p[2].X, p[2].Y),
                    new Point(p[3].X, p[3].Y)
                });
            }
            else
            {
                if (p.Count == 5)
                {
                    var newp = new Point((p[3].X + p[4].X) / 2, (p[3].Y + p[4].Y) / 2);
                    p.Insert(4, newp);
                }

                double b0x, b0y, b1x, b1y;
                double b2x, b2y, b3x, b3y, b4x, b4y;
                b0x = p[0].X;
                b0y = p[0].Y;
                b1x = p[1].X;
                b1y = p[1].Y;
                b2x = (p[1].X + p[2].X) / 2.0f;
                b2y = (p[1].Y + p[2].Y) / 2.0f;
                b4x = (2.0f * p[2].X + p[3].X) / 3.0f;
                b4y = (2.0f * p[2].Y + p[3].Y) / 3.0f;
                b3x = (b2x + b4x) / 2.0f;
                b3y = (b2y + b4y) / 2.0f;
                segments.Add(new[] { startPoint, new Point(b1x, b1y), new Point(b2x, b2y), new Point(b3x, b3y) });
                for (int i = 2; i < p.Count - 4; i++)
                {
                    b1x = b4x;
                    b1y = b4y;
                    b2x = (p[i].X + 2.0f * p[i + 1].X) / 3.0f;
                    b2y = (p[i].Y + 2.0f * p[i + 1].Y) / 3.0f;
                    b4x = (2.0f * p[i + 1].X + p[i + 2].X) / 3.0f;
                    b4y = (2.0f * p[i + 1].Y + p[i + 2].Y) / 3.0f;
                    b3x = (b2x + b4x) / 2.0f;
                    b3y = (b2y + b4y) / 2.0f;
                    segments.Add(new[] { segments.Last().Last(), new Point(b1x, b1y), new Point(b2x, b2y), new Point(b3x, b3y) });
                }
                Point p1 = p[p.Count - 4];
                Point p2 = p[p.Count - 3];
                Point p3 = p[p.Count - 2];
                b1x = b4x;
                b1y = b4y;
                b2x = (p1.X + 2.0f * p2.X) / 3.0f;
                b2y = (p1.Y + 2.0f * p2.Y) / 3.0f;
                b4x = (p2.X + p3.X) / 2.0f;
                b4y = (p2.Y + p3.Y) / 2.0f;
                b3x = (b2x + b4x) / 2.0f;
                b3y = (b2y + b4y) / 2.0f;
                segments.Add(new[] { segments.Last().Last(), new Point(b1x, b1y), new Point(b2x, b2y), new Point(b3x, b3y) });
                p2 = p3;
                p3 = p[p.Count - 1];
                b1x = b4x;
                b1y = b4y;
                b2x = p2.X;
                b2y = p2.Y;
                b3x = p3.X;
                b3y = p3.Y;
                segments.Add(new[] { segments.Last().Last(), new Point(b1x, b1y), new Point(b2x, b2y), new Point(b3x, b3y) });
            }

            return segments;
        }


        public static List<Point[]> GetCurvePathSegments(List<Point2D> p)
        {

            if (p.Count <= 1)
                return null;

            var startPoint = new Point(p[0].X, p[0].Y);
            List<Point[]> segments = new List<Point[]>();

            if (p.Count == 2)
            {
                segments.Add(new[] {startPoint, new Point(p[1].X, p[1].Y)});
            }
            else if (p.Count == 3)
            {
                segments.Add(new[] {startPoint, new Point(p[1].X, p[1].Y), new Point(p[2].X, p[2].Y)});
            }
            else if (p.Count == 4)
            {

                segments.Add(new[]
                {
                    startPoint,
                    new Point(p[1].X, p[1].Y),
                    new Point(p[2].X, p[2].Y),
                    new Point(p[3].X, p[3].Y)
                });
            }
            else
            {
                if (p.Count == 5)
                {
                    var newp = new Point2D((p[3].X + p[4].X)/2, (p[3].Y + p[4].Y)/2);
                    p.Insert(4, newp);
                }

                double b0x, b0y, b1x, b1y;
                double b2x, b2y, b3x, b3y, b4x, b4y;
                b0x = p[0].X;
                b0y = p[0].Y;
                b1x = p[1].X;
                b1y = p[1].Y;
                b2x = (p[1].X + p[2].X)/2.0f;
                b2y = (p[1].Y + p[2].Y)/2.0f;
                b4x = (2.0f*p[2].X + p[3].X)/3.0f;
                b4y = (2.0f*p[2].Y + p[3].Y)/3.0f;
                b3x = (b2x + b4x)/2.0f;
                b3y = (b2y + b4y)/2.0f;
                segments.Add(new[] {startPoint, new Point(b1x, b1y), new Point(b2x, b2y), new Point(b3x, b3y)});
                for (int i = 2; i < p.Count - 4; i++)
                {
                    b1x = b4x;
                    b1y = b4y;
                    b2x = (p[i].X + 2.0f*p[i + 1].X)/3.0f;
                    b2y = (p[i].Y + 2.0f*p[i + 1].Y)/3.0f;
                    b4x = (2.0f*p[i + 1].X + p[i + 2].X)/3.0f;
                    b4y = (2.0f*p[i + 1].Y + p[i + 2].Y)/3.0f;
                    b3x = (b2x + b4x)/2.0f;
                    b3y = (b2y + b4y)/2.0f;
                    segments.Add(new[]
                    {segments.Last().Last(), new Point(b1x, b1y), new Point(b2x, b2y), new Point(b3x, b3y)});
                }
                Point p1 = WPFUtils.ConvertPoint(p[p.Count - 4]);
                Point p2 = WPFUtils.ConvertPoint(p[p.Count - 3]);
                Point p3 = WPFUtils.ConvertPoint(p[p.Count - 2]);
                b1x = b4x;
                b1y = b4y;
                b2x = (p1.X + 2.0f*p2.X)/3.0f;
                b2y = (p1.Y + 2.0f*p2.Y)/3.0f;
                b4x = (p2.X + p3.X)/2.0f;
                b4y = (p2.Y + p3.Y)/2.0f;
                b3x = (b2x + b4x)/2.0f;
                b3y = (b2y + b4y)/2.0f;
                segments.Add(new[]
                {segments.Last().Last(), new Point(b1x, b1y), new Point(b2x, b2y), new Point(b3x, b3y)});
                p2 = p3;
                p3 = WPFUtils.ConvertPoint(p[p.Count - 1]);
                b1x = b4x;
                b1y = b4y;
                b2x = p2.X;
                b2y = p2.Y;
                b3x = p3.X;
                b3y = p3.Y;
                segments.Add(new[]
                {segments.Last().Last(), new Point(b1x, b1y), new Point(b2x, b2y), new Point(b3x, b3y)});
            }

            return segments;
        }


        public static double GetDistance(Point2D pt, List<Point2D> closedContour)
        {
            double distance = double.MaxValue;
            for (int i = 0; i < closedContour.Count - 1; i++)
            {
                Point2D p1 = closedContour[i];
                Point2D p2 = closedContour[i + 1];
                distance = Math.Min(distance, GetDistance(pt, p1, p2));
            }
            return distance;
        }

        public static Point GetAbsolutePointFromRelativePoint(Point pos, Rect bound)
        {
            double x = pos.X * bound.Width + bound.Left;
            double y = pos.Y * bound.Height + bound.Top;
            return new Point(x, y);
        }

        public static double GetDistance(Point p1, Point p2)
        {
            return Math.Sqrt((p1.X - p2.X)*(p1.X - p2.X) + (p1.Y - p2.Y)*(p1.Y - p2.Y));
        }

        public static double GetDistance(Point2D p1, Point2D p2)
        {
            return Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
        }


        /// <summary>
        /// From StreamingRoseRiver (TopicStream)
        /// Calculate the site of the topics
        /// </summary>
        public static Point GetCentroid(List<Point2D> pts)
        {
            double x = 0;
            double y = 0;
            double area;
            double denominator = GetArea(pts) * 6;

            int attribute = pts.Count - 1;
            for (int i = 0; i < attribute; i++)
            {
                area = pts[i].X * pts[i + 1].Y - pts[i + 1].X * pts[i].Y;
                x += (pts[i].X + pts[i + 1].X) * area;
                y += (pts[i].Y + pts[i + 1].Y) * area;
            }
            area = pts[attribute].X * pts[0].Y - pts[0].X * pts[attribute].Y;
            x += (pts[attribute].X + pts[0].X) * area;
            y += (pts[attribute].Y + pts[0].Y) * area;

            return new Point(Math.Abs(x / denominator), Math.Abs(y / denominator));
        }

        /// <summary>
        /// From StreamingRoseRiver (TopicStream)
        /// Calculate the site of the topics
        /// </summary>
        public static Point GetCentroid(List<Point> pts)
        {
            double x = 0;
            double y = 0;
            double area;
            double denominator = GetArea(pts) * 6;

            int attribute = pts.Count - 1;
            for (int i = 0; i < attribute; i++)
            {
                area = pts[i].X * pts[i + 1].Y - pts[i + 1].X * pts[i].Y;
                x += (pts[i].X + pts[i + 1].X) * area;
                y += (pts[i].Y + pts[i + 1].Y) * area;
            }
            area = pts[attribute].X * pts[0].Y - pts[0].X * pts[attribute].Y;
            x += (pts[attribute].X + pts[0].X) * area;
            y += (pts[attribute].Y + pts[0].Y) * area;

            return new Point(Math.Abs(x / denominator), Math.Abs(y / denominator));
        }

        public static double GetArea(List<Point2D> Vertices)
        {
            double area = 0;

            int attribute = Vertices.Count - 1;
            for (int i = 0; i < attribute; i++)
            {
                area += Vertices[i].X * Vertices[i + 1].Y - Vertices[i + 1].X * Vertices[i].Y;
            }
            area += Vertices[attribute].X * Vertices[0].Y - Vertices[0].X * Vertices[attribute].Y;
            return Math.Abs(area) * 0.5;
        }


        public static double GetArea(List<Point> Vertices)
        {
            double area = 0;

            int attribute = Vertices.Count - 1;
            for (int i = 0; i < attribute; i++)
            {
                area += Vertices[i].X * Vertices[i + 1].Y - Vertices[i + 1].X * Vertices[i].Y;
            }
            area += Vertices[attribute].X * Vertices[0].Y - Vertices[0].X * Vertices[attribute].Y;
            return Math.Abs(area) * 0.5;
        }

        public static double GetPerimeter(List<Point2D> pts)
        {
            double perimeter = 0;
            int ptCnt = pts.Count;
            for (int i = 0; i < ptCnt; i++)
            {
                perimeter += GetDistance(pts[i], pts[(i + 1) % ptCnt]);
            }
            return perimeter;
        }

        public static double GetPerimeter(List<Point> pts)
        {
            double perimeter = 0;
            int ptCnt = pts.Count;
            for (int i = 0; i < ptCnt; i++)
            {
                perimeter += GetDistance(pts[i], pts[(i + 1)%ptCnt]);
            }
            return perimeter;
        }

        public static bool IsPointInsideRectangle(Rect rect, Point2D pt)
        {
            return (rect.Left - pt.X)*(rect.Left + rect.Width - pt.X) <= 0 &&
                   (rect.Top - pt.Y)*(rect.Top + rect.Height - pt.Y) <= 0;
        }

        public static bool IsPointInsideEllipse(Rect rect, Point2D pt)
        {
            return (pt.X - rect.Left - rect.Width/2)*(pt.X - rect.Left - rect.Width/2)/rect.Width/rect.Width*4 +
                   (pt.Y - rect.Top - rect.Height/2)*(pt.Y - rect.Top - rect.Height/2)/rect.Height/rect.Height*4 <= 1;
        }

        public static bool IsRectangleInsideRectangle(Rect boundRect, Rect insideRect)
        {
            return IsPointInsideRectangle(boundRect, new Point2D(insideRect.Left, insideRect.Top)) &&
                IsPointInsideRectangle(boundRect, new Point2D(insideRect.Right, insideRect.Bottom));
        }

        //public static double GetArea(List<Point> pts)
        //{
        //    double area = 0;

        //    int attribute = pts.Count - 1;
        //    for (int i = 0; i < attribute; i++)
        //    {
        //        area += pts[i].X * pts[i + 1].Y - pts[i + 1].X * pts[i].Y;
        //    }
        //    area += pts[attribute].X * pts[0].Y - pts[0].X * pts[attribute].Y;
        //    return Math.Abs(area) * 0.5;
        //}


        public static Rect GetMergedRect(Rect rect1, Rect rect2)
        {
            var left = Math.Min(rect1.Left, rect2.Left);
            var top = Math.Min(rect1.Top, rect2.Top);
            var right = Math.Max(rect1.Right, rect2.Right);
            var bottom = Math.Max(rect1.Bottom, rect2.Bottom);
            return new Rect(left, top, right - left, bottom - top);
        }

        /// <summary>
        /// Have not tested
        /// </summary>
        public static Tuple<Point, Point> GetNormalizedPointsForGradientBrush(Point pt1, Point pt2)
        {
            double deltaX = Math.Abs(pt1.X - pt2.X);
            double deltaY = Math.Abs(pt1.Y - pt2.Y);

            double factor = deltaX > deltaY ? (1/deltaX) : (1/deltaY);
            double offsetX = 0.5 - factor*(pt1.X + pt2.X)/2;
            double offsetY = 0.5 - factor*(pt1.Y + pt2.Y)/2;

            return new Tuple<Point, Point>(new Point(factor * pt1.X + offsetX, factor * pt1.Y + offsetY), 
                new Point(factor * pt2.X + offsetX, factor * pt2.Y + offsetY));
        }

        public static bool IsRectInPolygon(Point[] poly, Rect rect)
        {
            bool isInside = true;
            foreach (var pt in GetRectEndPoints(rect))
            {
                if (!IsPointInPolygon(poly, pt))
                {
                    isInside = false;
                    break;
                }
            }
            return isInside;
        }

        public static List<Point> GetRectEndPoints(Rect rect)
        {
            return new List<Point>
            {
                new Point(rect.Left, rect.Top),
                new Point(rect.Left, rect.Bottom),
                new Point(rect.Right, rect.Top),
                new Point(rect.Right, rect.Bottom),
            };
        }

        /// <summary>
        /// Test whether a point is in the polygon
        /// </summary>
        public static bool IsPointInPolygon(Point[] poly, Point p)
        {
            Point p1, p2;


            bool inside = false;


            if (poly.Length < 3)
            {
                return inside;
            }


            var oldPoint = new Point(
                poly[poly.Length - 1].X, poly[poly.Length - 1].Y);


            for (int i = 0; i < poly.Length; i++)
            {
                var newPoint = new Point(poly[i].X, poly[i].Y);


                if (newPoint.X > oldPoint.X)
                {
                    p1 = oldPoint;

                    p2 = newPoint;
                }

                else
                {
                    p1 = newPoint;

                    p2 = oldPoint;
                }


                if ((newPoint.X < p.X) == (p.X <= oldPoint.X)
                    && (p.Y - (long)p1.Y) * (p2.X - p1.X)
                    < (p2.Y - (long)p1.Y) * (p.X - p1.X))
                {
                    inside = !inside;
                }


                oldPoint = newPoint;
            }


            return inside;
        }

        public static bool IsInCircle<T>(IEnumerable<T> nodes, Func<T, Point2D> locationFunc, Point2D center, double radius)
        {
            if (nodes == null || nodes.Count() == 0)
                return true;

            foreach (var node in nodes)
            {
                var pt = locationFunc(node);
                if(Maths.GetDistance(pt, center) > radius)
                    return false;
            }
            return true;
        }

        public static Rect GetRectBoundary(List<Point> locations)
        {
            if (locations == null || locations.Count() == 0)
                return new Rect();

            double left = double.MaxValue;
            double right = double.MinValue;
            double top = double.MaxValue;
            double bottom = double.MinValue;

            foreach (var pt in locations)
            {
                left = Math.Min(left, pt.X);
                right = Math.Max(right, pt.X);
                top = Math.Min(top, pt.Y);
                bottom = Math.Max(bottom, pt.Y);
            }

            return new Rect(left, top, (right - left), (bottom - top));
        }

        public static Rect GetRectBoundary(List<Point2D> locations)
        {
            if (locations == null || locations.Count() == 0)
                return new Rect();

            double left = double.MaxValue;
            double right = double.MinValue;
            double top = double.MaxValue;
            double bottom = double.MinValue;

            foreach (var pt in locations)
            {
                left = Math.Min(left, pt.X);
                right = Math.Max(right, pt.X);
                top = Math.Min(top, pt.Y);
                bottom = Math.Max(bottom, pt.Y);
            }

            return new Rect(left, top, (right - left), (bottom - top));
        }

        public static Rect GetRectBoundary<T>(IEnumerable<T> nodes, Func<T, Point2D> locationFunc)
        {
            if (nodes == null || nodes.Count() == 0)
                return new Rect();

            double left = double.MaxValue;
            double right = double.MinValue;
            double top = double.MaxValue;
            double bottom = double.MinValue;

            foreach (var node in nodes)
            {
                var pt = locationFunc(node);
                left = Math.Min(left, pt.X);
                right = Math.Max(right, pt.X);
                top = Math.Min(top, pt.Y);
                bottom = Math.Max(bottom, pt.Y);
            }

            return new Rect(left, top, (right - left), (bottom - top));
        }

        public static double GetYForPointOnLine(double x, double A, double B, double C)
        {
            if (B == 0)
            {
                throw new ArgumentException();
            }

            return (-C - A*x)/B;
        }

        public static double GetXForPointOnLine(double y, double A, double B, double C)
        {
            if (A == 0)
            {
                throw new ArgumentException();
            }

            return (-C - B * y) / A;
        }

        public static void GetLineParameter(Point2D p1, double theta, out double A, out double B, out double C)
        {
            GetLineParameter(p1, new Point2D(p1.X + Math.Cos(theta), p1.Y + Math.Sin(theta)), out A, out B, out C);
        }

        public static Point2D GetIntersection(double A1, double B1, double C1, double A2, double B2, double C2)
        {
            // Get delta and check if the lines are parallel
            double delta = A2 * B1 - A1 * B2;
            if (delta == 0)
                throw new System.Exception("Lines are parallel");

            // now return the Vector2 intersection point
            return new Point2D(
                (B2 * C1 - B1 * C2) / delta,
                (A1 * C2 - A2 * C1) / delta
            );
        }

        public static Point2D GetPerpendicularPointOnLine(Point2D p, double A, double B, double C)
        {
            Point2D v = new Point2D();
            double tmp = A * A + B * B;
            v.X = (B * B * p.X - A * B * p.Y - A * C) / tmp;
            v.Y = (A * A * p.Y - A * B * p.X - B * C) / tmp;

            return v;
        }

        public static void GetLineParameter(Point p1, Point p2, out double A, out double B, out double C)
        {
            A = p2.Y - p1.Y;    //y2-y1
            B = p1.X - p2.X;    //x1-x2
            C = p2.X * p1.Y - p1.X * p2.Y;  //x2*y1-x1*y2
        }

        public static void GetLineParameter(Point2D p1, Point2D p2, out double A, out double B, out double C)
        {
            A = p2.Y - p1.Y;    //y2-y1
            B = p1.X - p2.X;    //x1-x2
            C = p2.X * p1.Y - p1.X * p2.Y;  //x2*y1-x1*y2
        }

        public static void GetLineParameter(Point2D p1, Point2D p2, out double k, out double b)
        {
            k = (p2.Y - p1.Y)/(p2.X - p1.X);
            b = (p1.Y*p2.X - p2.Y*p1.X)/(p2.X - p1.X);
        }

        public static double GetDistance(Point2D p, Point2D linePt1, Point2D linePt2)
        {
            double A, B, C;
            GetLineParameter(linePt1, linePt2, out A, out B, out C);
            return GetDistance(p, A, B, C);
        }

        public static double GetDistance(Point2D p, double A, double B, double C)
        {
            return Math.Abs(A*p.X + B*p.Y + C)/Math.Sqrt(A*A + B*B);
        }

        /// <summary>
        /// Get the point location of p, where p: p1 --- (lambda) --- p --- (1-lambda) --- p2
        /// </summary>
        /// <returns></returns>
        public static Point GetIntermediatePoint(Point p1, Point p2, double lambda)
        {
            return new Point(p1.X * (1 - lambda) + p2.X * lambda, p1.Y * (1 - lambda) + p2.Y * lambda);
        }

        /// <summary>
        /// Get the point location of p, where p: p1 --- (lambda) --- p --- (1-lambda) --- p2
        /// </summary>
        /// <returns></returns>
        public static Point2D GetIntermediatePoint(Point2D p1, Point2D p2, double lambda)
        {
            return new Point2D(p1.X * (1 - lambda) + p2.X * lambda, p1.Y * (1 - lambda) + p2.Y * lambda);
        }

        public static Point GetRotatedPoint(Point p, double angle)
        {
            return new Point(p.X * Math.Cos(angle) - p.Y * Math.Sin(angle), p.X * Math.Sin(angle) + p.Y * Math.Cos(angle));
        }

        public static bool IsRectCircleAreaOverlap(Rect rect, double circleX, double circleY, double radius)
        {
            Point2D circleDistance = new Point2D();
            double rectX = rect.X + rect.Width/2;
            double rectY = rect.Y + rect.Height/2;
            circleDistance.X = Math.Abs(circleX - rectX);
            circleDistance.Y = Math.Abs(circleY - rectY);

            if (circleDistance.X > (rect.Width / 2 + radius)) { return false; }
            if (circleDistance.Y > (rect.Height / 2 + radius)) { return false; }

            if (circleDistance.X <= (rect.Width / 2)) { return true; }
            if (circleDistance.Y <= (rect.Height / 2)) { return true; }

            var cornerDistance_sq = Math.Pow((circleDistance.X - rect.Width/2), 2) +
                                    Math.Pow((circleDistance.Y - rect.Height/2), 2);

            return cornerDistance_sq <= (radius*radius);
        }

        /// <summary>
        /// Not fully tested, used in label boundary overlap detect
        /// </summary>
        public static bool IsRectCircleEdgeOverlap(Rect rect, double circleX, double circleY, double radius)
        {
            double rectX = (rect.Left + rect.Right) / 2;
            double rectY = (rect.Top + rect.Bottom) / 2;

            double theta = Math.Atan2(rectY - circleY, rectX - circleX);

            double nearestPointX = circleX + radius * Math.Cos(theta);
            double nearestPointY = circleY + radius * Math.Sin(theta);

            bool isInRect = ((nearestPointX - rect.Left) * (nearestPointX - rect.Right) < 0 && (nearestPointY - rect.Bottom) * (nearestPointY - rect.Top) < 0);

            //Trace.WriteLine(string.Format("Rect:{0}, Circle:({1},{2}),{3}, Theta:{4}, nearestPt:({5},{6}), isOverlap:{7}", rect, circleX, circleY, radius, theta, nearestPointX, nearestPointY, isInRect));// rect.Contains(new Point(nearestPointX, nearestPointY))));

            //return rect.Contains(new Point(nearestPointX, nearestPointY));
            return isInRect;
        }

        public static double GetDistance(Rect rect1, Rect rect2)
        {
            var dis1 = GetIntervalDistance(rect1.Left, rect1.Right, rect2.Left, rect2.Right);
            var dis2 = GetIntervalDistance(rect1.Top, rect1.Bottom, rect2.Top, rect2.Bottom);

            return Math.Sqrt(dis1*dis1 + dis2*dis2);
        }

        public static double GetIntervalDistance(double left1, double right1, double left2, double right2)
        {
            if (left1 > left2)
            {
                Util.Swap(ref left1, ref left2);
                Util.Swap(ref right1, ref right2);
            }
            if (right1 < left2)
                return left2 - right1;
            else return 0;
        }

        public static Point GetPointMultiply(Point pt, double factor)
        {
            return new Point(pt.X * factor, pt.Y * factor);
        }

        public static Point2D GetPoint2DMultiply(Point2D pt, double factor)
        {
            return new Point2D(pt.X * factor, pt.Y * factor);
        }

        public static Point GetPointAddition(Point pt1, Point pt2)
        {
            return new Point(pt1.X + pt2.X, pt1.Y + pt2.Y);
        }

        public static Point2D GetPointAddition(Point2D pt1, Point2D pt2)
        {
            return new Point2D(pt1.X + pt2.X, pt1.Y + pt2.Y);
        }

        /// <summary>
        /// return pt = pt1 - pt2
        /// </summary>
        public static Point2D GetPointSubtraction(Point2D pt1, Point2D pt2)
        {
            return new Point2D(pt1.X - pt2.X, pt1.Y - pt2.Y);
        }

        /// <summary>
        /// return pt = pt1 - pt2
        /// </summary>
        public static Point GetPointSubtraction(Point pt1, Point pt2)
        {
            return new Point(pt1.X - pt2.X, pt1.Y - pt2.Y);
        }

        public static Point GetNormalizedPoint(Point pt)
        {
            double norm = Math.Sqrt(pt.X * pt.X + pt.Y * pt.Y);
            return new Point(pt.X / norm, pt.Y / norm);
        }

        public static Point2D GetNormalizedPoint(Point2D pt)
        {
            double norm = Math.Sqrt(pt.X*pt.X + pt.Y*pt.Y);
            return new Point2D(pt.X/norm, pt.Y/norm);
        }
    }
}
