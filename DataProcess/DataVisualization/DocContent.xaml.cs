using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DataProcess.DataVisualization
{
    public class Doc
    {
        public int Index;
        public string Title;
        public string Content;
    }

    /// <summary>
    /// Interaction logic for DocContent.xaml
    /// Copied from RoseRiverV2
    /// </summary>
    public partial class DocContent : Window
    {
        public static string[] Keywords;
        static readonly Brush _highlightBGBrush = Brushes.Purple;

        public DocContent(Doc doc)
		{
			InitializeComponent();

            DocumentBodyTextBox.Width = this.Width - 20;
            DocumentBodyTextBox.AppendText(doc.Content);
            _border.Background = DocList.DocTitleBackground;
            DocumentBodyTextBox.BorderBrush = null;
            DocumentBodyTextBox.Background = Brushes.Transparent;
            DocumentBodyTextBox.Foreground = DocList.DocTitleForeground;

            if (Keywords != null && Keywords.Length > 0)
                HighlightKeywords();
		}


        private void HighlightKeywords()
        {
            foreach(var keyword in Keywords)
            {
                HighlightSearchWord(DocumentBodyTextBox, keyword);
            }
        }

        private int HighlightSearchWord(RichTextBox displaytextbox, string searchword)
        {
            if (searchword.Length > 0)
            {
                Brush backgroundBrush = _highlightBGBrush;
                List<TextRange> resultRanges = new List<TextRange>();
                TextRange textRange = new TextRange(displaytextbox.Document.ContentStart,
                    displaytextbox.Document.ContentStart);
                while ((textRange = FindWordFromPosition(textRange.End, searchword)) != null)
                {
                    textRange.ApplyPropertyValue(TextElement.BackgroundProperty, backgroundBrush);
                    resultRanges.Add(textRange);
                }
                return resultRanges.Count;
            }
            return 0;
        }


        public static TextRange FindWordFromPosition(TextPointer position, string word)
        {
            while (position != null)
            {
                if (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    string textRun = position.GetTextInRun(LogicalDirection.Forward);

                    // Find the starting index of any substring that matches "word".
                    int indexInRun = textRun.IndexOf(word, StringComparison.CurrentCulture);
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
    }
}
