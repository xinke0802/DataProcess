using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Converters;
using System.Windows.Threading;
using ColorMine.ColorSpaces;
using Lava.Visual;

namespace DataProcess.Utils
{
    public class InteractionHelper
    {
        public static bool IsNoModifierKeyPressed()
        {
            return (Keyboard.Modifiers & ModifierKeys.None) == ModifierKeys.None;
        }

        public static bool IsControlKeyPressed()
        {
            return (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
        }

        public static bool IsShiftKeyPressed()
        {
            return (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
        }

        public static bool IsAltKeyPressed()
        {
            return (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt;
        }

        public static bool IsWindowsKeyPressed()
        {
            return (Keyboard.Modifiers & ModifierKeys.Windows) == ModifierKeys.Windows;
        }
    }

    public class ColorUtils
    {
        public static void ConvertRgbToLab(Color color, out double L, out double A, out double B)
        {
            var myRgb = new Rgb {R = color.R, G = color.G, B = color.B};
            var myLab = myRgb.To<Lab>();
            L = myLab.L;
            A = myLab.A;
            B = myLab.B;
        }

        public static Color ConvertLabToRgb(double L,  double A, double B)
        {
            var myLab = new Lab() {A = A, B = B, L = L};
            var myRgb = myLab.To<Rgb>();
            return Color.FromRgb((byte)myRgb.R, (byte)myRgb.G, (byte)myRgb.B);
        }
    }

    public class WPFUtils
    {
        public static void HighlightSearchWordUnderline(RichTextBox displaytextbox, List<string> searchwords)
        {
            foreach (var searchword in searchwords)
            {
                TextRange textRange = new TextRange(displaytextbox.Document.ContentStart,
                    displaytextbox.Document.ContentStart);
                while ((textRange = FindWordFromPosition(textRange.End, searchword)) != null)
                {
                    //textRange.ApplyPropertyValue(TextElement.BackgroundProperty, new SolidColorBrush(Color.FromRgb(100,100,100)));
                    textRange.ApplyPropertyValue(TextBlock.TextDecorationsProperty, TextDecorations.Underline);
                }
            }
        }

        public static TextRange FindWordFromPosition(TextPointer position, string word)
        {
            while (position != null)
            {
                if (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    string textRun = position.GetTextInRun(LogicalDirection.Forward);
                    textRun = textRun.ToLower();

                    // Find the starting index of any substring that matches "word".
                    int indexInRun = textRun.IndexOf(word);
                    if (indexInRun >= 0)
                    {
                        TextPointer start = position.GetPositionAtOffset(indexInRun);
                        TextPointer end = start.GetPositionAtOffset(word.Length);
                        return new TextRange(start, end);
                    }
                }

                position = position.GetNextContextPosition(LogicalDirection.Forward);
            }

            // position will be null if "word" is not found.
            return null;
        }

        //public static long GetEpoachFromDate(DateTime date)
        //{
        //    var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        //    return Convert.ToInt64((date.ToUniversalTime() - epoch).TotalSeconds);
        //}
        //public static DateTime GetDateFromEpoach(long unixTime)
        //{
        //    var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        //    return epoch.AddSeconds(unixTime);
        //}

        //public static DateTime GetDateByNumDay(DateTime startTime, int numDay)
        //{
        //    long epoachStart = GetEpoachFromDate(startTime);
        //    long resultEpoach = epoachStart + numDay * 24 * 60 * 60;
        //    return GetDateFromEpoach(resultEpoach);
        //}

        public static LinearGradientBrush GetLinearGradientBrushHorizontal(Color startColor, Color endColor)
        {
            var linearGradientBrush = new LinearGradientBrush();
            linearGradientBrush.GradientStops.Add(new GradientStop(startColor, 0));
            linearGradientBrush.GradientStops.Add(new GradientStop(endColor, 1));
            linearGradientBrush.StartPoint = new Point(0, 0.5);
            linearGradientBrush.EndPoint = new Point(1, 0.5);
            return linearGradientBrush;
        }

        public static DateTime GetDateByNumDay(DateTime startTime, int numDay)
        {
            return startTime.AddDays(numDay);
        } 

        public static void SetSearchWordsUnderline(FormattedText formattedText, IEnumerable<string> searchWords)
        {
            if (searchWords == null)
                return;

            var text = formattedText.Text.ToLower();
            foreach (var searchWord in searchWords)
            {
                int index = 0;
                if (string.IsNullOrEmpty(searchWord))
                {
                    continue;
                }

                while ((index = text.IndexOf(searchWord, index)) != -1)
                {
                    formattedText.SetTextDecorations(TextDecorations.Underline, index, searchWord.Length);
                    index += searchWord.Length;
                }
            }
        }

        public static FormattedText GetFormattedText(string str, double fontSize, double lineHeight = -1)
        {
            if (lineHeight == -1)
            {
                lineHeight = fontSize;
            }
            if (double.IsNaN(fontSize))
            {
                throw new ArgumentException();
            }

            var formattedText = new FormattedText(str,
            new System.Globalization.CultureInfo("en-us"), FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Calibri"), FontStyles.Normal,
                FontWeights.Normal, FontStretches.Normal), fontSize, Brushes.Black)
            {
                LineHeight = lineHeight,
                TextAlignment = TextAlignment.Center,
            };

            return formattedText;
        }

        public static void SaveToPdf(UIElement ui, string name)
        {
            //MigraDoc.DocumentObjectModel.Document doc = new MigraDoc.DocumentObjectModel.Document();
            //MigraDoc.Rendering.DocumentRenderer renderer = new DocumentRenderer(doc);
            //MigraDoc.Rendering.PdfDocumentRenderer pdfRenderer = new MigraDoc.Rendering.PdfDocumentRenderer();
            //pdfRenderer.PdfDocument = pDoc;
            //pdfRenderer.DocumentRenderer = renderer;
            //using (MemoryStream ms = new MemoryStream())
            //{
            //    pdfRenderer.Save(ms, false);
            //    byte[] buffer = new byte[ms.Length];
            //    ms.Seek(0, SeekOrigin.Begin);
            //    ms.Flush();
            //    ms.Read(buffer, 0, (int)ms.Length);
            //}

            string path = string.Format(name + "_{0}.pdf", DateTime.Now.ToString("MMddyyyy_hhmmss"));
            FileOperations.EnsureFileFolderExist(path);
            MemoryStream lMemoryStream = new MemoryStream();
            Package package = Package.Open(lMemoryStream, FileMode.Create);
            System.Windows.Xps.Packaging.XpsDocument doc = new System.Windows.Xps.Packaging.XpsDocument(package);
            System.Windows.Xps.XpsDocumentWriter writer = System.Windows.Xps.Packaging.XpsDocument.CreateXpsDocumentWriter(doc);
            writer.Write(ui);
            doc.Close();
            package.Close();

            PdfSharp.Xps.XpsModel.XpsDocument pdfXpsDoc = PdfSharp.Xps.XpsModel.XpsDocument.Open(lMemoryStream);
            PdfSharp.Xps.XpsConverter.Convert(pdfXpsDoc, path, 0);
        }

        public static Point GetCenterPoint(Rect rect)
        {
            return new Point(rect.X + rect.Width/2, rect.Y + rect.Height/2);
        }

        private static LinearGradientBrush _aeroNormalButtonBackgroundBrush = null;
        public static LinearGradientBrush AeroNormalButtonBackgroundBrush 
        {
            get
            {
                if (_aeroNormalButtonBackgroundBrush == null)
                {
                    _aeroNormalButtonBackgroundBrush = new LinearGradientBrush();
                    _aeroNormalButtonBackgroundBrush.GradientStops = new GradientStopCollection(); ;
                    _aeroNormalButtonBackgroundBrush.GradientStops.Add(new GradientStop() { Color = Color.FromRgb(0xF3, 0xF3, 0xF3), Offset = 0 });
                    _aeroNormalButtonBackgroundBrush.GradientStops.Add(new GradientStop() { Color = Color.FromRgb(0xEB, 0xEB, 0xEB), Offset = 0.5 });
                    _aeroNormalButtonBackgroundBrush.GradientStops.Add(new GradientStop() { Color = Color.FromRgb(0xDD, 0xDD, 0xDD), Offset = 0.5 });
                    _aeroNormalButtonBackgroundBrush.GradientStops.Add(new GradientStop() { Color = Color.FromRgb(0xCD, 0xCD, 0xCD), Offset = 1 });
                    _aeroNormalButtonBackgroundBrush.StartPoint = new Point(0, 0);
                    _aeroNormalButtonBackgroundBrush.EndPoint = new Point(0, 1);
                }
                 return _aeroNormalButtonBackgroundBrush;
            }
        }

        private static SolidColorBrush _aeroButtonNormalBorderBrush;
        public static SolidColorBrush AeroButtonNormalBorderBrush
        {
            get
            {
                if (_aeroButtonNormalBorderBrush == null)
                {
                    _aeroButtonNormalBorderBrush = new SolidColorBrush() {Color = Color.FromRgb(0x70, 0x70, 0x70)};
                }
                return _aeroButtonNormalBorderBrush;
            }
        }

        public static PathGeometry GetTrianglePathGeometry(Point[] pts)
        {
            System.Windows.Media.LineSegment lineSeg0 = new System.Windows.Media.LineSegment(pts[1], true);
            System.Windows.Media.LineSegment lineSeg1 = new System.Windows.Media.LineSegment(pts[2], true);
            System.Windows.Media.LineSegment lineSeg2 = new System.Windows.Media.LineSegment(pts[0], true);

            PathSegmentCollection pathsegmentCollection = new PathSegmentCollection();
            pathsegmentCollection.Add(lineSeg0);
            pathsegmentCollection.Add(lineSeg1);
            pathsegmentCollection.Add(lineSeg2);

            PathFigure pathFigure = new PathFigure();
            pathFigure.StartPoint = pts[0];
            pathFigure.Segments = pathsegmentCollection;

            PathFigureCollection pathFigureCollection = new PathFigureCollection();
            pathFigureCollection.Add(pathFigure);

            PathGeometry pathGeometry = new PathGeometry();
            pathGeometry.Figures = pathFigureCollection;

            return pathGeometry;
        }

        public static PathSegment GetPathSegment(List<Point> edge)
        {
            if (edge.Count == 2)
            {
                return new System.Windows.Media.LineSegment()
                {
                    Point = edge[1]
                };
            }
            if (edge.Count == 3)
            {
                QuadraticBezierSegment bezierSegment = new QuadraticBezierSegment();
                bezierSegment.Point1 = edge[1];
                bezierSegment.Point2 = edge[2];
                return bezierSegment;
            }
            else if (edge.Count == 4)
            {
                BezierSegment bezierSegment = new BezierSegment();
                bezierSegment.Point1 = edge[1];
                bezierSegment.Point2 = edge[2];
                bezierSegment.Point3 = edge[3];
                return bezierSegment;
            }
            else
            {
                throw new ArgumentException();
            }
        }

        public static PathGeometry GetPathGeometry(List<Point2D> p)
        {
            PathFigure aPath = new PathFigure();

            if (p.Count <= 1)
                return null;

            aPath.StartPoint = new Point(p[0].X, p[0].Y);
            aPath.Segments = new PathSegmentCollection();
            for (int i = 1; i < p.Count; i++)
            {
                aPath.Segments.Add(new System.Windows.Media.LineSegment { Point = new Point(p[i].X, p[i].Y) }); 
            }
            aPath.IsClosed = true;

            PathGeometry pathGeometry = new PathGeometry();
            pathGeometry.Figures = new PathFigureCollection(new List<PathFigure>() { aPath });
            return pathGeometry;
        }


        public static PathGeometry GetCurvePathGeometry(List<Point> pts)
        {
            return GetCurvePathGeometry(pts.ConvertAll(p => new Point2D(p)));
        }

        public static PathGeometry GetPolygonGeometry(List<Point2D> p)
        {
            PathFigure aPath = new PathFigure();

            if (p.Count <= 1)
                return null;
            aPath.StartPoint = new Point(p[0].X, p[0].Y);
            aPath.Segments = new PathSegmentCollection();

            for (int i = 1; i < p.Count; i++)
            {
                var point = p[i];
                aPath.Segments.Add(new System.Windows.Media.LineSegment() { Point = new Point(point.X, point.Y) });
            }

            PathGeometry pathGeometry = new PathGeometry();
            pathGeometry.Figures = new PathFigureCollection(new List<PathFigure>() { aPath });
            return pathGeometry;
        }

        public static PathGeometry GetPolygonGeometry(List<Point> p)
        {
            PathFigure aPath = new PathFigure();

            if (p.Count <= 1)
                return null;
            aPath.StartPoint = new Point(p[0].X, p[0].Y);
            aPath.Segments = new PathSegmentCollection();

            for (int i = 1; i < p.Count; i++)
            {
                var point = p[i];
                aPath.Segments.Add(new System.Windows.Media.LineSegment() { Point = new Point(point.X, point.Y) });
            }

            PathGeometry pathGeometry = new PathGeometry();
            pathGeometry.Figures = new PathFigureCollection(new List<PathFigure>() { aPath });
            return pathGeometry;
        }

        public static PathGeometry GetCurvePathGeometry(List<Point2D> p)
        {
            PathFigure aPath = new PathFigure();

            if (p.Count <= 1)
                return null;

            aPath.StartPoint = new Point(p[0].X, p[0].Y);

            if (p.Count == 2)
                aPath.Segments = new PathSegmentCollection { new System.Windows.Media.LineSegment { Point = new Point(p[1].X, p[1].Y) } };
            else if (p.Count == 3)
            {
                // System.out.println("OK3");
                aPath.Segments = new PathSegmentCollection { new QuadraticBezierSegment { Point1 = new Point(p[1].X, p[1].Y), Point2 = new Point(p[2].X, p[2].Y) } };
            }
            else if (p.Count == 4)
            {

                // System.out.println("OK4");
                aPath.Segments = new PathSegmentCollection { new BezierSegment { Point1 = new Point(p[1].X, p[1].Y), Point2 = new Point(p[2].X, p[2].Y), Point3 = new Point(p[3].X, p[3].Y) } };
            }
            else
            {
                if (p.Count == 5)
                {
                    var newp = new Point2D((p[3].X + p[4].X) / 2, (p[3].Y + p[4].Y) / 2);
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
                aPath.StartPoint = new Point(b0x, b0y);
                aPath.Segments.Add(new BezierSegment { Point1 = new Point(b1x, b1y), Point2 = new Point(b2x, b2y), Point3 = new Point(b3x, b3y) });
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
                    aPath.Segments.Add(new BezierSegment { Point1 = new Point(b1x, b1y), Point2 = new Point(b2x, b2y), Point3 = new Point(b3x, b3y) });
                }
                Point p1 = ConvertPoint(p[p.Count - 4]);
                Point p2 = ConvertPoint(p[p.Count - 3]);
                Point p3 = ConvertPoint(p[p.Count - 2]);
                b1x = b4x;
                b1y = b4y;
                b2x = (p1.X + 2.0f * p2.X) / 3.0f;
                b2y = (p1.Y + 2.0f * p2.Y) / 3.0f;
                b4x = (p2.X + p3.X) / 2.0f;
                b4y = (p2.Y + p3.Y) / 2.0f;
                b3x = (b2x + b4x) / 2.0f;
                b3y = (b2y + b4y) / 2.0f;
                aPath.Segments.Add(new BezierSegment { Point1 = new Point(b1x, b1y), Point2 = new Point(b2x, b2y), Point3 = new Point(b3x, b3y) });
                p2 = p3;
                p3 = ConvertPoint(p[p.Count - 1]);
                b1x = b4x;
                b1y = b4y;
                b2x = p2.X;
                b2y = p2.Y;
                b3x = p3.X;
                b3y = p3.Y;
                aPath.Segments.Add(new BezierSegment { Point1 = new Point(b1x, b1y), Point2 = new Point(b2x, b2y), Point3 = new Point(b3x, b3y) });
            }

            PathGeometry pathGeometry = new PathGeometry();
            pathGeometry.Figures = new PathFigureCollection(new List<PathFigure>() { aPath });
            return pathGeometry;
        }

        public static PathGeometry GetClosedCurveGeometry(List<Point> ptList1, List<Point> ptList2)
        {
            return GetClosedCurveGeometry(ptList1.ConvertAll(p => new Point2D(p)),
                ptList2.ConvertAll(p => new Point2D(p)));
        }

        public static PathGeometry GetClosedCurveGeometry(List<Point2D> ptList1, List<Point2D> ptList2)
        {
            if (ptList1.Count <= 1 || ptList2.Count <= 1)
                return null;
            var pt1F = ptList1.First();
            var pt1L = ptList1.Last();
            var pt2F = ptList2.First();
            //if (Maths.GetDistance(pt1L, pt2F) > Maths.GetDistance(pt1F, pt2F))
            //{
            //    ptList2 = new List<Point2D>(ptList2);
            //    ptList2.Reverse();
            //}
            ptList2 = new List<Point2D>(ptList2);
            ptList2.Reverse();

            var pathGeometry1 = GetCurvePathGeometry(ptList1);
            var pathFigure1 = pathGeometry1.Figures.First();

            var pathGeometry2 = GetCurvePathGeometry(ptList2);
            var pathFigure2 = pathGeometry2.Figures.First();

            PathFigure aPath = new PathFigure();
            aPath.StartPoint = pathFigure1.StartPoint;
            foreach (var segment in pathFigure1.Segments)
            {
                aPath.Segments.Add(segment);
            }
            aPath.Segments.Add(new System.Windows.Media.LineSegment { Point = pathFigure2.StartPoint });
            foreach (var segment in pathFigure2.Segments)
            {
                aPath.Segments.Add(segment);
            }
            aPath.IsClosed = true;

            PathGeometry pathGeometry = new PathGeometry();
            pathGeometry.Figures = new PathFigureCollection(new List<PathFigure>() { aPath });
            return pathGeometry;
        }

        public static Point ConvertPoint(Point2D p)
        {
            return new Point(p.X, p.Y);
        }

        public static void SetTabFocus(TabControl tableControl, TabItem focusTabItem)
        {
            tableControl.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                (System.Threading.ThreadStart)delegate
                {
                    tableControl.SelectedItem = focusTabItem;
                    tableControl.UpdateLayout();
                    focusTabItem.Focus();
                });
        }

        public static double GetColorDistance(Color color1, Color color2)
        {
            return Math.Max(Math.Pow(color1.R - color2.R, 2), Math.Pow(color1.R - color2.R - color1.A + color2.A, 2)) +
                Math.Max(Math.Pow(color1.G - color2.G, 2), Math.Pow(color1.G - color2.G - color1.A + color2.A, 2)) +
                Math.Max(Math.Pow(color1.B - color2.B, 2), Math.Pow(color1.B - color2.B - color1.A + color2.A, 2));
        }

        public static Brush GetSemiTransparentBrush(Brush brush, double opacity)
        {
            if (brush is SolidColorBrush)
            {
                var solidColorBrush = brush as SolidColorBrush;
                return new SolidColorBrush() { Color = GetSemiTransparentColor(solidColorBrush.Color, opacity) };
            }
            else if (brush is LinearGradientBrush)
            {
                var linearGradientBrush = brush as LinearGradientBrush;

                var newBrush = new LinearGradientBrush()
                {
                    StartPoint = linearGradientBrush.StartPoint,
                    EndPoint = linearGradientBrush.EndPoint
                };
                newBrush.GradientStops = new GradientStopCollection();
                foreach (var stop in linearGradientBrush.GradientStops)
                    newBrush.GradientStops.Add(new GradientStop(GetSemiTransparentColor(stop.Color, opacity), stop.Offset));

                return newBrush;
            }
            else
                throw new NotImplementedException();
        }

        public static Color GetSemiTransparentColor(Color color, double opacity)
        {
            return new Color() { A = (Byte)(255 * opacity), R = color.R, G = color.G, B = color.B };
        }

        /// <summary>
        /// The larger the lighterFactor, the lighter the brush
        /// </summary>
        public static SolidColorBrush GetLighterBrush(SolidColorBrush solidColorBrush, double lighterFactor = 1.5)
        {
            double h, s, v;
            var orgColor = solidColorBrush.Color;
            Lava.Util.ColorLib.ToHsv(orgColor, out h, out s, out v);
            return new SolidColorBrush() { Color = Lava.Util.ColorLib.FromAhsv(orgColor.A, h, s, v * lighterFactor) };
        }

        /// <summary>
        /// The smaller the darkerFactor, the darker the brush
        /// </summary>
        public static SolidColorBrush GetDarkerBrush(SolidColorBrush solidColorBrush, double darkerFactor = 0.8)
        {
            double h, s, v;
            var orgColor = solidColorBrush.Color;
            Lava.Util.ColorLib.ToHsv(orgColor, out h, out s, out v);
            return new SolidColorBrush() { Color = Lava.Util.ColorLib.FromAhsv(orgColor.A, h, s, v * darkerFactor) };
        }

        /// <summary>
        /// The smaller the darkerFactor, the darker the color
        /// </summary>
        public static Color GetDarkerColor(Color orgColor, double darkerFactor = 0.8)
        {
            double h, s, v;
            Lava.Util.ColorLib.ToHsv(orgColor, out h, out s, out v);
            return Lava.Util.ColorLib.FromAhsv(orgColor.A, h, s, v * darkerFactor);
        }

        public static Color GetBlendedColorLab(List<Tuple<Color, double>> colors)
        {
            double lSum = 0, aSum = 0, bSum = 0, alphaSum = 0, weightSum = 0;
            foreach (var tuple in colors)
            {
                var color = tuple.Item1;
                var weight = tuple.Item2;

                double l, a, b;
                ColorUtils.ConvertRgbToLab(color, out l, out a, out b);

                lSum += l * color.A * weight;
                aSum += a * color.A * weight;
                bSum += b * color.A * weight;
                alphaSum += color.A * weight;
                weightSum += weight;
            }

            //return Lava.Util.ColorLib.FromAhsv((Byte)(alphaSum / weightSum), hSum / weightSum, sSum / weightSum, vSum / weightSum);
            var color2 = ColorUtils.ConvertLabToRgb( lSum / alphaSum, aSum / alphaSum, bSum / alphaSum);
            return GetSemiTransparentColor(color2, (alphaSum/weightSum)/255);
        }

        public static Color GetBlendedColorHSV(List<Tuple<Color, double>> colors)
        {
            double hSum = 0, sSum = 0, vSum = 0, alphaSum = 0, weightSum = 0;
            foreach (var tuple in colors)
            {
                var color = tuple.Item1;
                var weight = tuple.Item2;

                double h, s, v;
                Lava.Util.ColorLib.ToHsv(color, out h, out s, out v);

                hSum += h * color.A * weight;
                sSum += s * color.A * weight;
                vSum += v * color.A * weight;
                alphaSum += color.A * weight;
                weightSum += weight;
            }

            //return Lava.Util.ColorLib.FromAhsv((Byte)(alphaSum / weightSum), hSum / weightSum, sSum / weightSum, vSum / weightSum);
            return Lava.Util.ColorLib.FromAhsv((Byte)(alphaSum / weightSum), hSum / alphaSum, sSum / alphaSum, vSum / alphaSum);
        }

        public static void BindSliderTextBox(Slider slider, TextBox textBox, Action<double> action = null)
        {
            textBox.Text = slider.Value.ToString();

            slider.ValueChanged += (s, e) =>
            {
                textBox.Text = e.NewValue.ToString();
                if (action != null)
                    action(e.NewValue);
            };
            textBox.KeyDown += (s, e) =>
            {
                if(e.Key == System.Windows.Input.Key.Enter)
                {
                    var text = textBox.Text;
                    double newValue;
                    if (double.TryParse(text, out newValue) && slider.Minimum <= newValue && slider.Maximum >= newValue)
                    {
                        if(slider.IsSnapToTickEnabled)
                        {
                            newValue = Math.Round(newValue / slider.TickFrequency) * slider.TickFrequency;
                            textBox.Text = newValue.ToString();
                        }
                        slider.Value = newValue;
                        if (action != null)
                            action(newValue);
                    }
                    else
                        textBox.Text = slider.Value.ToString();
                }                
            };
        }

        public static void BindSliderTextBoxPercentage(Slider slider, TextBox textBox, Action<double> action = null)
        {
            slider.Minimum = 0;
            slider.Maximum = 1;
            slider.TickFrequency = 0.01;
            slider.IsSnapToTickEnabled = true;
            textBox.Text = slider.Value.ToString();

            slider.ValueChanged += (s, e) =>
            {
                textBox.Text = GetPercentageString(e.NewValue);
                if (action != null)
                    action(e.NewValue);
            };
            textBox.KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter)
                {
                    var text = textBox.Text;
                    if (text.EndsWith("%"))
                        text = text.Substring(0, text.Length - 1);
                    double newValue;
                    if (double.TryParse(text, out newValue))
                    {
                        if (textBox.Text.EndsWith("%"))
                            newValue /= 100;
                        if (slider.Minimum <= newValue && slider.Maximum >= newValue)
                        {
                            if (slider.IsSnapToTickEnabled)
                            {
                                newValue = Math.Round(newValue / slider.TickFrequency) * slider.TickFrequency;
                                textBox.Text = GetPercentageString(newValue);
                            }
                            slider.Value = newValue;
                            if (action != null)
                                action(newValue);   
                        }
                        else
                            textBox.Text = GetPercentageString(slider.Value);
                    }
                    else
                        textBox.Text = GetPercentageString(slider.Value);
                }
            };
        }

        static string GetPercentageString(double val)
        {
            return Math.Round(100*val).ToString() + "%";
        }
    }
}
