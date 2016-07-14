using System.Windows;
using System.Windows.Media;

namespace Netling.Client
{
    public partial class ValueUserControl
    {
        public ValueUserControl()
        {
            InitializeComponent();
        }

        public string Title
        {
            get { return TitleTextBlock.Text; }
            set { TitleTextBlock.Text = value?.ToUpper(); }
        }

        public string Value
        {
            get { return ValueTextBlock.Text; }
            set { ValueTextBlock.Text = value; }
        }

        public string Unit
        {
            get { return UnitTextBlock.Text; }
            set { UnitTextBlock.Text = value?.ToLower(); }
        }

        public string BaselineValue
        {
            get { return BaselineValueTextBlock.Text; }
            set
            {
                BaselineValueTextBlock.Text = value;
                BaselineValueTextBlock.Visibility = !string.IsNullOrWhiteSpace(value) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public Brush ValueBrush
        {
            get { return ValueTextBlock.Foreground; }
            set
            {
                if (value != null)
                {
                    ValueTextBlock.Foreground = value;
                    UnitTextBlock.Foreground = value;
                    ValueBorder.BorderBrush = value;
                }
                else
                {
                    ValueTextBlock.Foreground = new SolidColorBrush(Colors.Black);
                    UnitTextBlock.Foreground = new SolidColorBrush(Colors.Black);
                    ValueBorder.BorderBrush = new SolidColorBrush(Colors.DarkGray);
                }
            }
        }
    }
}
