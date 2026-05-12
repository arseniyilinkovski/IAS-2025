using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;

namespace SimpleIDE.Controls
{
    public partial class SyntaxHighlightingTextBox : UserControl
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(SyntaxHighlightingTextBox),
                new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged));

        private bool _isUpdating = false;
        private CompletionWindow _completionWindow;

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
            Editor.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            Editor.Foreground = new SolidColorBrush(Color.FromRgb(212, 212, 212));

            Editor.SyntaxHighlighting = CreateHighlighting();

            Editor.TextArea.TextEntering += OnTextEntering;
            Editor.TextArea.TextEntered += OnTextEntered;
            Editor.TextArea.PreviewKeyDown += OnPreviewKeyDown;

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

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab && _completionWindow != null)
            {
                e.Handled = true;

                var completionList = _completionWindow.CompletionList;
                if (completionList.CompletionData.Any())
                {
                    // Выбираем первый элемент
                    completionList.SelectedItem = completionList.CompletionData[0];
                    // Вставляем его
                    completionList.RequestInsertion(e);
                }
                _completionWindow.Close();
            }
        }

        private void OnTextEntered(object sender, TextCompositionEventArgs e)
        {
            if (e.Text.Length > 0 && char.IsLetter(e.Text[0]))
            {
                ShowCompletion();
            }
        }

        private void OnTextEntering(object sender, TextCompositionEventArgs e)
        {
            if (e.Text.Length > 0 && !char.IsLetterOrDigit(e.Text[0]))
            {
                _completionWindow?.Close();
            }
        }

        private void ShowCompletion()
        {
            var word = GetCurrentWord();
            if (string.IsNullOrEmpty(word)) return;

            var keywords = new List<ICompletionData>
    {
        new CompletionData("main", "Главная функция"),
        new CompletionData("function", "Объявление функции"),
        new CompletionData("var", "Объявление переменной"),
        new CompletionData("return", "Возврат из функции"),
        new CompletionData("output", "Вывод в консоль"),
        new CompletionData("if", "Условный оператор"),
        new CompletionData("then", "Начало блока then"),
        new CompletionData("else", "Блок else"),
        new CompletionData("repeat", "Цикл"),
        new CompletionData("param", "Параметр функции"),
        new CompletionData("int", "Целочисленный тип"),
        new CompletionData("str", "Строковый тип"),
        new CompletionData("char", "Символьный тип"),
        new CompletionData("bool", "Логический тип"),
        new CompletionData("length", "Длина строки"),
        new CompletionData("copy", "Копирование строки"),
        new CompletionData("random", "Случайное число"),
        new CompletionData("factorialOfNumber", "Факториал"),
        new CompletionData("squareOfNumber", "Квадратный корень"),
        new CompletionData("asciiCode", "ASCII код"),
        new CompletionData("getLocalTimeAndDate", "Текущее время"),
        new CompletionData("powNumber", "Степень числа"),
        new CompletionData("true", "Истина"),
        new CompletionData("false", "Ложь"),
    };

            var filtered = keywords.Where(k => k.Text.StartsWith(word, StringComparison.OrdinalIgnoreCase)).ToList();

            if (filtered.Any())
            {
                _completionWindow?.Close();
                _completionWindow = new CompletionWindow(Editor.TextArea);

                // Находим начало текущего слова
                var caret = Editor.TextArea.Caret.Position;
                var document = Editor.Document;
                var line = document.GetLineByNumber(caret.Line);

                int start = caret.Column - 1;
                var lineText = document.GetText(line);

                while (start > 0 && (char.IsLetterOrDigit(lineText[start - 1]) || lineText[start - 1] == '_'))
                {
                    start--;
                }

                var startOffset = line.Offset + start;
                var endOffset = line.Offset + (caret.Column - 1);

                _completionWindow.StartOffset = startOffset;
                _completionWindow.EndOffset = endOffset;

                foreach (var item in filtered)
                {
                    _completionWindow.CompletionList.CompletionData.Add(item);
                }

                if (_completionWindow.CompletionList.CompletionData.Any())
                {
                    _completionWindow.CompletionList.SelectedItem = _completionWindow.CompletionList.CompletionData[0];
                }

                _completionWindow.Show();
                _completionWindow.Closed += (s, e) => _completionWindow = null;
            }
        }

        private string GetCurrentWord()
        {
            var caret = Editor.TextArea.Caret.Position;
            var line = caret.Line;
            var column = caret.Column;

            var document = Editor.Document;
            var lineText = document.GetText(document.GetLineByNumber(line));

            if (column <= 1) return "";

            int start = column - 2;
            while (start >= 0 && (char.IsLetterOrDigit(lineText[start]) || lineText[start] == '_'))
            {
                start--;
            }
            start++;

            if (start < 0 || start >= column - 1) return "";

            return lineText.Substring(start, (column - 1) - start);
        }

        private IHighlightingDefinition CreateHighlighting()
        {
            string xshd = @"<?xml version=""1.0"" encoding=""utf-8""?>
<SyntaxDefinition name=""IAS-2025"" xmlns=""http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008"">
    <Color name=""Keyword"" foreground=""#569CD6"" fontWeight=""bold"" />
    <Color name=""Type"" foreground=""#4EC9B0"" fontWeight=""bold"" />
    <Color name=""Function"" foreground=""#DCDCAA"" />
    <Color name=""String"" foreground=""#CE9178"" />
    <Color name=""Number"" foreground=""#B5CEA8"" />
    <Color name=""Operator"" foreground=""#D4D4D4"" />
    
    <RuleSet>
        <Span color=""String"">
            <Begin>&quot;</Begin>
            <End>&quot;</End>
        </Span>
        
        <Span color=""String"">
            <Begin>'</Begin>
            <End>'</End>
        </Span>
        
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
        
        <Keywords color=""Type"">
            <Word>int</Word>
            <Word>str</Word>
            <Word>char</Word>
            <Word>bool</Word>
        </Keywords>
        
        <Keywords color=""Function"">
            <Word>length</Word>
            <Word>copy</Word>
            <Word>random</Word>
            <Word>factorialOfNumber</Word>
            <Word>squareOfNumber</Word>
            <Word>asciiCode</Word>
            <Word>getLocalTimeAndDate</Word>
            <Word>powNumber</Word>
        </Keywords>
        
        <Rule color=""Number"">\b\d+\b</Rule>
        <Rule color=""Number"">\bb[01]+\b</Rule>
        <Rule color=""Number"">\bo[0-7]+\b</Rule>
        <Rule color=""Number"">\bh[0-9A-Fa-f]+\b</Rule>
        
        <Rule color=""Operator"">\+</Rule>
        <Rule color=""Operator"">\-</Rule>
        <Rule color=""Operator"">\*</Rule>
        <Rule color=""Operator"">/</Rule>
        <Rule color=""Operator"">%</Rule>
        <Rule color=""Operator"">=</Rule>
        <Rule color=""Operator"">&gt;</Rule>
        <Rule color=""Operator"">&lt;</Rule>
        <Rule color=""Operator"">&lt;&lt;</Rule>
        <Rule color=""Operator"">&gt;&gt;</Rule>
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

    public class CompletionData : ICompletionData
    {
        private readonly string _text;
        private readonly string _description;
        private readonly string _example;

        public CompletionData(string text, string description, string example = "")
        {
            _text = text;
            _description = description;
            _example = example;
        }

        public string Text => _text;
        public object Content => _text;
        public object Description => string.IsNullOrEmpty(_example) ? _description : $"{_description}\n\nПример: {_example}";
        public double Priority => 0;
        public System.Windows.Media.ImageSource Image => null;

        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            var currentText = textArea.Document.GetText(completionSegment);

            
            textArea.Document.Replace(completionSegment, _text);
        }
    }
}