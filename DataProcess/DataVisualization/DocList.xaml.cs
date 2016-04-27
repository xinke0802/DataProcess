using DataProcess.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
	/// <summary>
	/// Interaction logic for DocList.xaml
    /// Copied from RoseRiverV2
    /// </summary>
	public partial class DocList : Window
	{
        int _docContentWindowCnt = 0;
        public static Brush DocTitleBackground = new SolidColorBrush() { Color = new Color() { A = 200, R = 0, G = 0, B = 0 } };
        public static Brush DocTitleForeground = Brushes.White;
        DocContent _docContent = null;

		public DocList(IEnumerable<Doc> documents)
		{
			InitializeComponent();

            //bool first = true;
			foreach (var doc in documents)
			{
                //if (!first)
                //{
                //    DocTitlePanel.Children.Add(new Rectangle
                //    {
                //        Height = 1,
                //        Fill = _docTitleBackground //Brushes.LightGray
                //    });
                //}

                Label title = new Label { Content = doc.Title, Background = DocTitleBackground, Foreground = DocTitleForeground };
				title.MouseLeftButtonUp += (sender, e) =>
				{
					var docContentPopup = new DocContent(doc);
					docContentPopup.Owner = this;
                    docContentPopup.Closed += (s2, e2) =>
                        {
                            _docContentWindowCnt--;
                        };
					//Position
                    docContentPopup.WindowStartupLocation = WindowStartupLocation.Manual;
                    int mouseX, mouseY;
                    AutoControlUtils.GetMousePosition(out mouseX, out mouseY);
                    docContentPopup.Top = mouseY;
                    double docListLeft = this.Left;
                    double docListRight = this.Left + this.ActualWidth;
                    double windowLeft = this.Owner.Left;
                    double windowRight = this.Owner.Left + this.Owner.ActualWidth;
                    if (docListLeft - windowLeft >= windowRight - docListRight)
                    {
                        docContentPopup.Left = docListLeft - docContentPopup.Width;
                    }
                    else
                    {
                        docContentPopup.Left = docListRight;
                    }
                    //Show
                    docContentPopup.Show();
                    _docContentWindowCnt++;
                    _docContent = docContentPopup;
				};
                title.MouseEnter += (sender, e) =>
                    {
                        if (_docContent != null)
                        {
                            _docContent.Close();
                            _docContent = null;
                        }
                        title.Background = Brushes.Black;
                    };
                title.MouseLeave += (sender, e) =>
                    {
                        title.Background = DocTitleBackground;
                    };

				DocTitlePanel.Children.Add(title);
                //_docListScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
                //first = false;
			}

            this.MouseLeave += DocList_MouseLeave;
		}

        void DocList_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_docContentWindowCnt == 0)
            {
                this.Close();
            }
        }
	}
}

