using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DataProcess.Utils
{
    class IOUtils
    {
        public static void InjectSettings(string filename)
        {
            foreach (var line in System.IO.File.ReadAllLines(filename).Where(line => line.Trim().Length > 1))
            {
                var statement = line.Trim();
                if (statement.EndsWith(";"))
                {
                    statement = statement.TrimEnd(';');
                }
                var tokens = statement.Split('=').Select(str => str.Trim()).ToArray();
                var typeName = tokens[0].Substring(0, tokens[0].LastIndexOf('.'));
                var fieldName = tokens[0].Substring(tokens[0].LastIndexOf('.') + 1);
                var value = tokens[1];
                var type = Type.GetType(typeName);
                if (type == null)
                {
                    MessageBox.Show(statement, "Cannot inject setting");
                    continue;
                }
                var field = type.GetField(fieldName);
                if (field != null)
                {
                    field.SetValue(null, System.Convert.ChangeType(value, field.FieldType));
                    continue;
                }
                var prop = type.GetProperty(fieldName);
                if (prop != null)
                {
                    prop.SetValue(null, System.Convert.ChangeType(value, field.FieldType));
                    continue;
                }
                MessageBox.Show(statement, "Cannot inject setting");
            }
        }

    }
}
