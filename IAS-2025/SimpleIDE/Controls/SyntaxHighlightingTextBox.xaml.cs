using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using SimpleIDE.Services;
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
        private CompletionWindow? _completionWindow;

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

            Loaded += async (s, e) => await LoadUserTemplates();

            Editor.TextChanged += (s, e) =>
            {
                if (!_isUpdating)
                {
                    _isUpdating = true;
                    SetValue(TextProperty, Editor.Text);
                    _isUpdating = false;
                }
            };
            ThemeService.ThemeChanged += OnThemeChanged;
        }
        private void OnThemeChanged()
        {
            // Обновляем фон редактора
            Editor.Background = (SolidColorBrush)Application.Current.Resources["BackgroundDark"];
            Editor.Foreground = (SolidColorBrush)Application.Current.Resources["TextPrimary"];

            // Пересоздаем подсветку
            var currentText = Editor.Text;
            Editor.SyntaxHighlighting = ThemeService.GetSyntaxHighlighting();
            Editor.Text = currentText;
        }
        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_completionWindow != null)
            {
                if (e.Key == Key.Tab || e.Key == Key.Enter)
                {
                    e.Handled = true;
                    var completionList = _completionWindow.CompletionList;
                    if (completionList.CompletionData.Any())
                    {
                        var selectedItem = completionList.SelectedItem ?? completionList.CompletionData[0];
                        completionList.RequestInsertion(e);
                    }
                    _completionWindow.Close();
                    return;
                }
            }

            if (e.Key == Key.Tab && _completionWindow == null)
            {
                e.Handled = true;
                Editor.TextArea.PerformTextInput("    ");
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

        // Системные шаблоны
        private void InsertMainTemplate_Click(object sender, RoutedEventArgs e) => InsertText(@"main{
    output ""Hello World!"";
}");

        private void InsertMainWithVarTemplate_Click(object sender, RoutedEventArgs e) => InsertText(@"main{
    int var a = 10;
    int var b = 20;
    int var sum = a + b;
    output ""Сумма: "";
    output sum;
}");

        private void InsertFunctionTemplate_Click(object sender, RoutedEventArgs e) => InsertText(@"int function name(int param arg1, int param arg2){
    int var result = arg1 + arg2;
    return result;
}");

        private void InsertRepeatTemplate_Click(object sender, RoutedEventArgs e) => InsertText(@"repeat(10){
    output ""Итерация"";
}");

        private void InsertIfTemplate_Click(object sender, RoutedEventArgs e) => InsertText(@"if (a > b) then{
    output ""a больше b"";
} else{
    output ""b больше a"";
}");

        private void InsertText(string text)
        {
            var caret = Editor.TextArea.Caret.Offset;
            Editor.Document.Insert(caret, text);
            Text = Editor.Text;
            SetValue(TextProperty, Editor.Text);
        }

        private void Copy_Click(object sender, RoutedEventArgs e) => Editor.Copy();
        private void Cut_Click(object sender, RoutedEventArgs e) => Editor.Cut();
        private void Paste_Click(object sender, RoutedEventArgs e) => Editor.Paste();
        private void ClearAll_Click(object sender, RoutedEventArgs e) { Editor.Clear(); Text = ""; SetValue(TextProperty, ""); }

        // Шаблоны пользователя
        private async Task LoadUserTemplates()
        {
            if (App.TemplateService == null) return;

            var templates = await App.TemplateService.GetUserTemplatesAsync();
            var userTemplates = templates.Where(t => !t.IsSystem).ToList();

            UserTemplatesMenu.Items.Clear();

            if (!userTemplates.Any())
            {
                var emptyItem = new MenuItem { Header = "📭 Нет шаблонов", IsEnabled = false };
                UserTemplatesMenu.Items.Add(emptyItem);
            }
            else
            {
                foreach (var template in userTemplates)
                {
                    var menuItem = new MenuItem { Header = $"📄 {template.Name}", ToolTip = template.Description };

                    // Подменю
                    var insertItem = new MenuItem { Header = "✏️ Вставить" };
                    insertItem.Click += (s, e) => InsertAtCursor(template.Content);
                    menuItem.Items.Add(insertItem);

                    var deleteItem = new MenuItem { Header = "❌ Удалить" };
                    deleteItem.Tag = template;
                    deleteItem.Click += async (s, e) =>
                    {
                        var item = s as MenuItem;
                        var t = item?.Tag as Models.Template;
                        if (t != null)
                        {
                            var confirm = MessageBox.Show($"Удалить шаблон '{t.Name}'?", "Подтверждение",
                                MessageBoxButton.YesNo, MessageBoxImage.Question);
                            if (confirm == MessageBoxResult.Yes)
                            {
                                await App.TemplateService.DeleteTemplateAsync(t.Id);
                                await LoadUserTemplates();
                            }
                        }
                    };
                    menuItem.Items.Add(deleteItem);

                    UserTemplatesMenu.Items.Add(menuItem);
                }
            }
        }

        private async Task DeleteTemplate_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var template = menuItem?.Tag as Models.Template;
            if (template != null)
            {
                var result = MessageBox.Show($"Удалить шаблон '{template.Name}'?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    await App.TemplateService.DeleteTemplateAsync(template.Id);
                    await LoadUserTemplates();
                }
            }
        }
        // Показать все шаблоны
        private async void ShowAllTemplates_Click(object sender, RoutedEventArgs e)
        {
            if (App.TemplateService == null) return;

            var templates = await App.TemplateService.GetUserTemplatesAsync();
            var userTemplates = templates.Where(t => !t.IsSystem).ToList();

            if (!userTemplates.Any())
            {
                MessageBox.Show("У вас нет сохраненных шаблонов.\n\nВыделите код, нажмите правой кнопкой и выберите 'Добавить выделенное как шаблон'",
                    "Мои шаблоны", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var message = "📋 ВАШИ ШАБЛОНЫ:\n\n";
            foreach (var t in userTemplates)
            {
                message += $"📄 {t.Name}\n";
                if (!string.IsNullOrEmpty(t.Description))
                    message += $"   📝 {t.Description}\n";
                message += $"   📏 Длина: {t.Content.Length} символов\n";
                message += $"   📅 Создан: {t.CreatedAt:dd.MM.yyyy HH:mm}\n\n";
            }

            MessageBox.Show(message, "Мои шаблоны", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Очистить все пользовательские шаблоны
        private async void ClearUserTemplates_Click(object sender, RoutedEventArgs e)
        {
            if (App.TemplateService == null) return;

            var result = MessageBox.Show("Удалить ВСЕ ваши пользовательские шаблоны?\nЭто действие нельзя отменить!",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                var templates = await App.TemplateService.GetUserTemplatesAsync();
                var userTemplates = templates.Where(t => !t.IsSystem).ToList();
                var count = userTemplates.Count;

                foreach (var t in userTemplates)
                {
                    await App.TemplateService.DeleteTemplateAsync(t.Id);
                }

                await LoadUserTemplates();
                MessageBox.Show($"🗑 Удалено {count} шаблонов!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void InsertAtCursor(string text)
        {
            var caret = Editor.TextArea.Caret.Offset;
            Editor.Document.Insert(caret, text);
            Text = Editor.Text;
            SetValue(TextProperty, Editor.Text);
        }

        // Добавление шаблона
        private async void AddAsTemplate_Click(object sender, RoutedEventArgs e)
        {
            var selectedText = Editor.SelectedText;

            if (string.IsNullOrWhiteSpace(selectedText))
            {
                MessageBox.Show("Выделите код, который хотите сохранить как шаблон!",
                    "Нет выделения", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new Views.InputDialog("✨ Введите название шаблона ✨", false);
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.Answer))
            {
                // Проверка на существование шаблона с таким именем
                var existingTemplates = await App.TemplateService.GetUserTemplatesAsync();
                if (existingTemplates.Any(t => t.Name == dialog.Answer))
                {
                    var overwrite = MessageBox.Show($"Шаблон '{dialog.Answer}' уже существует. Перезаписать?",
                        "Шаблон существует", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (overwrite != MessageBoxResult.Yes)
                        return;
                }

                var template = await App.TemplateService.AddTemplateAsync(dialog.Answer, selectedText);
                if (template != null)
                {
                    MessageBox.Show($"✅ Шаблон '{dialog.Answer}' сохранен!",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadUserTemplates();
                }
            }
        }

    }

    public class CompletionData : ICompletionData
    {
        private readonly string _text;
        private readonly string _description;

        public CompletionData(string text, string description)
        {
            _text = text;
            _description = description;
        }

        public string Text => _text;
        public object Content => _text;
        public object Description => _description;
        public double Priority => 0;
        public System.Windows.Media.ImageSource Image => null;

        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            textArea.Document.Replace(completionSegment, _text);
        }
    }
}