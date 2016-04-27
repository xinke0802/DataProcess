using Lava.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Lava.Visual;

namespace DataProcess.Utils
{
    public abstract class TableScheme
    {
        protected ITable _table;

        public TableScheme(ITable table, string schemeProperty)
        {
            _table = table;

            if (_table.Properties.Any(kvp => kvp.Key == schemeProperty))
                throw new Exception("Table already contains a scheme!");
            _table.PutProperty(schemeProperty, this);
        }

        protected static TableScheme Get(ITable table, string schemeProperty)
        {
            return (TableScheme)table.GetProperty(schemeProperty);
        }

        #region add columns
        protected int AddColumn<T>(string cname)
        {
            return _table.GetColumnIndex(_table.AddColumn<T>(cname));
        }

        protected int AddColumn<T>(string cname, T defaultValue)
        {
            return _table.GetColumnIndex(_table.AddColumn<T>(cname, defaultValue));
        }

        protected int AddConstantColumn<T>(string cname, T constValue)
        {
            return _table.GetColumnIndex(_table.AddConstantColumn<T>(cname, constValue));
        }

        protected int AddExpressionColumn<T>(string cname, string expr)
        {
            return _table.GetColumnIndex(_table.AddExpressionColumn<T>(cname, expr));
        }

        protected int AddFuncColumn<T>(string cname, Func<IItem, T> func)
        {
            return _table.GetColumnIndex(_table.AddFuncColumn<T>(cname, func));
        }
        #endregion
    }

    public static class LavaUtils
    {
        public static void SaveToXps(Display display, string name)
        {
            var fileName = name + DateTime.Now.ToString("hhmmss") + ".xps";
            //display.SaveXPS(fileName, true);
            display.InternalSaveXPS(fileName, true);

            Trace.WriteLine("xps saved to " + fileName);
        }

        public static void AnalyzeTable(ITable table, PrintType printType = PrintType.Console, StreamWriter sw = null)
        {
            foreach (var columnName in table.ColumnManager.ColumnNames)
            {
                var column = table.GetColumn(columnName);
                DebugUtils.PrintString(string.Format("Column: index={0}, name={1}, type={2}", table.GetColumnIndex(column), columnName, column.ColumnType),
                    printType, sw);
            }
        }

        public static IEdge GetGraphEdge(IGraph graph, INode node1, INode node2)
        {
            return node1.Edges.FirstOrDefault<IEdge>(edge =>
                edge.SourceNode == node2 || edge.TargetNode == node2);
        }

        public static Rect InterpRect(ref Rect from, double width, double height, double prog)
        {
            return new Rect
            {
                X = Lava.Util.InterpolatorLib.Interp(from.X, 0 - width / 2, prog),
                Y = Lava.Util.InterpolatorLib.Interp(from.Y, 0 - height / 2, prog),
                Width = Lava.Util.InterpolatorLib.Interp(from.Width, width, prog),
                Height = Lava.Util.InterpolatorLib.Interp(from.Height, height, prog)
            };
        }

        public static Rect InterpRect(Rect from, Rect to, double prog)
        {
            return new Rect
            {
                X = Lava.Util.InterpolatorLib.Interp(from.X, to.X, prog),
                Y = Lava.Util.InterpolatorLib.Interp(from.Y, to.Y, prog),
                Width = Lava.Util.InterpolatorLib.Interp(from.Width, to.Width, prog),
                Height = Lava.Util.InterpolatorLib.Interp(from.Height, to.Height, prog)
            };
        }

   
    }
}
