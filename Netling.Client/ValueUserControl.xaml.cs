using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Netling.Client;

public partial class ValueUserControl : UserControl
{
    public ValueUserControl()
    {
        InitializeComponent(true);
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
            BaselineValueTextBlock.IsVisible = !string.IsNullOrWhiteSpace(value) ? true : false;
        }
    }

    private BaseLine _baseLine;

    public BaseLine BaseLine
    {
        get { return _baseLine; }
        set
        {
            _baseLine = value;

            switch (value)
            {
                case BaseLine.Equal:
                    ValueTextBlock.Foreground = new SolidColorBrush(Colors.Black);
                    UnitTextBlock.Foreground = new SolidColorBrush(Colors.Black);
                    ValueBorder.BorderBrush = new SolidColorBrush(Colors.DarkGray);
                    break;
                case BaseLine.Worse:
                    ValueTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                    UnitTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                    ValueBorder.BorderBrush = new SolidColorBrush(Colors.Red);
                    break;
                case BaseLine.Better:
                    ValueTextBlock.Foreground = new SolidColorBrush(Colors.Green);
                    UnitTextBlock.Foreground = new SolidColorBrush(Colors.Green);
                    ValueBorder.BorderBrush = new SolidColorBrush(Colors.Green);
                    break;
            }
        }
    }
}

public enum BaseLine
{
    Equal,
    Worse,
    Better
}