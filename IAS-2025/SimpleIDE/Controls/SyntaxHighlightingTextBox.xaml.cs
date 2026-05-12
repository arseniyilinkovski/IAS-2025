using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.Xml;
using System.IO;

namespace SimpleIDE.Controls
{
    public partial class SyntaxHighlightingTextBox : UserControl
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(SyntaxHighlightingTextBox),
                new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged));

        private bool _isUpdating = false;

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (SyntaxHighlightingTextBox)d;
            if (!control._isUpdating)
            {
                control._isUpdating = true;
                control.Editor.Text = e.NewValue?.ToString() ?? "";
                control._isUpdating = false;
            }
        }

        public SyntaxHighlightingTextBox()
        {
            InitializeComponent();

            Editor.Options.EnableEmailHyperlinks = false;
            Editor.Options.EnableHyperlinks = false;
            Editor.Options.EnableTextDragDrop = false;
            Editor.ShowLineNumbers = true;
            Editor.WordWrap = true;
            Editor.FontSize = 14;
            Editor.FontFamily = new FontFamily("Consolas");

            Editor.SyntaxHighlighting = CreateHighlighting();

            Editor.TextChanged += (s, e) =>
            {
                if (!_isUpdating)
                {
                    _isUpdating = true;
                    SetValue(TextProperty, Editor.Text);
                    _isUpdating = false;
                }
            };
        }

        private IHighlightingDefinition CreateHighlighting()
        {
            string xshd = @"<?xml version=""1.0"" encoding=""utf-8""?>
<SyntaxDefinition name=""IAS-2025"" xmlns=""http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008"">
    <Color name=""Keyword"" foreground=""#FF6B6B"" fontWeight=""bold"" />
    <Color name=""Type"" foreground=""#4ECDC4"" fontWeight=""bold"" />
    <Color name=""Function"" foreground=""#FFE66D"" />
    <Color name=""String"" foreground=""#FF9F1C"" />
    <Color name=""Number"" foreground=""#BFB5FF"" />
    <Color name=""Comment"" foreground=""#7F8C8D"" />
    <Color name=""Operator"" foreground=""#D4D4D4"" />
    
    <RuleSet>
        
        
        <!-- Строки в двойных кавычках -->
        <Span color=""String"">
            <Begin>&quot;</Begin>
            <End>&quot;</End>
        </Span>
        
        <!-- Символы в одинарных кавычках -->
        <Span color=""String"">
            <Begin>'</Begin>
            <End>'</End>
        </Span>
        
        <!-- Ключевые слова -->
        <Keywords color=""Keyword"">
            <Word>main</Word>
            <Word>function</Word>
            <Word>var</Word>
            <Word>return</Word>
            <Word>output</Word>
            <Word>repeat</Word>
            <Word>if</Word>
            <Word>then</Word>
            <Word>else</Word>
            <Word>true</Word>
            <Word>false</Word>
            <Word>param</Word>
        </Keywords>
        
        <!-- Типы данных -->
        <Keywords color=""Type"">
            <Word>int</Word>
            <Word>str</Word>
            <Word>char</Word>
            <Word>bool</Word>
        </Keywords>
        
        <!-- Функции библиотеки -->
        <Keywords color=""Function"">
            <Word>lenght</Word>
            <Word>copy</Word>
            <Word>random</Word>
            <Word>factorialOfNumber</Word>
            <Word>squareOfNumber</Word>
            <Word>asciiCode</Word>
            <Word>getLocalTimeAndDate</Word>
            <Word>powNumber</Word>
        </Keywords>
        
        <!-- Числа -->
        <Rule color=""Number"">\b\d+\b</Rule>
        <Rule color=""Number"">\bb[01]+\b</Rule>
        <Rule color=""Number"">\bo[0-7]+\b</Rule>
        <Rule color=""Number"">\bh[0-9A-Fa-f]+\b</Rule>
        
        <!-- Операторы (экранируем спецсимволы) -->
        <Rule color=""Operator"">[+\-*/%=>&lt;&lt;?|]</Rule>
    </RuleSet>
</SyntaxDefinition>";

            using (var reader = new StringReader(xshd))
            using (var xmlReader = XmlReader.Create(reader))
            {
                return HighlightingLoader.Load(xmlReader, HighlightingManager.Instance);
            }
        }

        public void FocusEditor()
        {
            Editor.Focus();
        }

        public void Clear()
        {
            Editor.Clear();
        }
    }
}