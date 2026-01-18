using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;

// Alias WPF types to avoid conflict with System.Drawing from Windows Forms
using WpfBrush = System.Windows.Media.Brush;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfPen = System.Windows.Media.Pen;
using WpfFontFamily = System.Windows.Media.FontFamily;
using WpfFontStyle = System.Windows.FontStyle;
using WpfSize = System.Windows.Size;
using WpfPoint = System.Windows.Point;
using WpfFlowDirection = System.Windows.FlowDirection;

namespace SteamP2PInfo.App;

/// <summary>
/// 带描边效果的文字控件，用于游戏内 Overlay 显示
/// </summary>
[ContentProperty("Text")]
public class OverlayTextBlock : FrameworkElement
{
    private FormattedText? _formattedText;
    private Geometry? _textGeometry;
    private WpfPen? _pen;

    #region Dependency Properties

    public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
        nameof(Fill), typeof(WpfBrush), typeof(OverlayTextBlock),
        new FrameworkPropertyMetadata(WpfBrushes.White, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
        nameof(Stroke), typeof(WpfBrush), typeof(OverlayTextBlock),
        new FrameworkPropertyMetadata(WpfBrushes.Black, FrameworkPropertyMetadataOptions.AffectsRender, OnStrokeChanged));

    public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
        nameof(StrokeThickness), typeof(double), typeof(OverlayTextBlock),
        new FrameworkPropertyMetadata(2.0, FrameworkPropertyMetadataOptions.AffectsRender, OnStrokeChanged));

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(OverlayTextBlock),
        new FrameworkPropertyMetadata(string.Empty, OnTextChanged));

    public static readonly DependencyProperty FontFamilyProperty = DependencyProperty.Register(
        nameof(FontFamily), typeof(WpfFontFamily), typeof(OverlayTextBlock),
        new FrameworkPropertyMetadata(new WpfFontFamily("Segoe UI"), OnTextChanged));

    public static readonly DependencyProperty FontSizeProperty = DependencyProperty.Register(
        nameof(FontSize), typeof(double), typeof(OverlayTextBlock),
        new FrameworkPropertyMetadata(16.0, OnTextChanged));

    public static readonly DependencyProperty FontWeightProperty = DependencyProperty.Register(
        nameof(FontWeight), typeof(FontWeight), typeof(OverlayTextBlock),
        new FrameworkPropertyMetadata(FontWeights.Normal, OnTextChanged));

    public static readonly DependencyProperty FontStyleProperty = DependencyProperty.Register(
        nameof(FontStyle), typeof(WpfFontStyle), typeof(OverlayTextBlock),
        new FrameworkPropertyMetadata(FontStyles.Normal, OnTextChanged));

    #endregion

    #region Properties

    public WpfBrush Fill
    {
        get => (WpfBrush)GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public WpfBrush Stroke
    {
        get => (WpfBrush)GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public double StrokeThickness
    {
        get => (double)GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public WpfFontFamily FontFamily
    {
        get => (WpfFontFamily)GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public FontWeight FontWeight
    {
        get => (FontWeight)GetValue(FontWeightProperty);
        set => SetValue(FontWeightProperty, value);
    }

    public WpfFontStyle FontStyle
    {
        get => (WpfFontStyle)GetValue(FontStyleProperty);
        set => SetValue(FontStyleProperty, value);
    }

    #endregion

    public OverlayTextBlock()
    {
        UpdatePen();
    }

    private void UpdatePen()
    {
        _pen = new WpfPen(Stroke, StrokeThickness)
        {
            DashCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round,
            StartLineCap = PenLineCap.Round
        };
        InvalidateVisual();
    }

    private static void OnStrokeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is OverlayTextBlock textBlock)
            textBlock.UpdatePen();
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is OverlayTextBlock textBlock)
        {
            textBlock._formattedText = null;
            textBlock._textGeometry = null;
            textBlock.InvalidateMeasure();
            textBlock.InvalidateVisual();
        }
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        EnsureGeometry();
        if (_textGeometry != null && _pen != null)
        {
            // 先画描边，再画填充
            drawingContext.DrawGeometry(null, _pen, _textGeometry);
            drawingContext.DrawGeometry(Fill, null, _textGeometry);
        }
    }

    protected override WpfSize MeasureOverride(WpfSize availableSize)
    {
        EnsureFormattedText();
        if (_formattedText == null) return WpfSize.Empty;

        _formattedText.MaxTextWidth = Math.Min(3579139, availableSize.Width);
        _formattedText.MaxTextHeight = Math.Max(0.0001, availableSize.Height);

        return new WpfSize(Math.Ceiling(_formattedText.Width), Math.Ceiling(_formattedText.Height));
    }

    protected override WpfSize ArrangeOverride(WpfSize finalSize)
    {
        EnsureFormattedText();
        if (_formattedText != null)
        {
            _formattedText.MaxTextWidth = finalSize.Width;
            _formattedText.MaxTextHeight = Math.Max(0.0001, finalSize.Height);
        }
        _textGeometry = null;
        return finalSize;
    }

    private void EnsureFormattedText()
    {
        if (_formattedText != null) return;

        var typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretches.Normal);

        _formattedText = new FormattedText(
            Text ?? string.Empty,
            CultureInfo.CurrentCulture,
            WpfFlowDirection.LeftToRight,
            typeface,
            FontSize,
            WpfBrushes.Black,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);
    }

    private void EnsureGeometry()
    {
        if (_textGeometry != null) return;
        EnsureFormattedText();
        if (_formattedText != null)
            _textGeometry = _formattedText.BuildGeometry(new WpfPoint(0, 0));
    }
}
