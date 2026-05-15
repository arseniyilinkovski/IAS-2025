using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.Windows;

namespace SimpleIDE.Services
{
    public enum AppTheme
    {
        Dark,
        PickMe,
        Dracula,
        Nord
    }

    public static class ThemeService
    {
        public static AppTheme CurrentTheme { get; private set; } = AppTheme.Dark;
        public static event Action? ThemeChanged;

        public static void ApplyTheme(AppTheme theme)
        {
            CurrentTheme = theme;

            var resources = Application.Current.Resources;

            // Удаляем старые цвета и тексты
            var toRemove = resources.MergedDictionaries
                .Where(d => d.Source?.OriginalString.Contains("Colors") == true ||
                           d.Source?.OriginalString.Contains("Texts") == true)
                .ToList();

            foreach (var dict in toRemove)
            {
                resources.MergedDictionaries.Remove(dict);
            }

            // Загружаем цвета
            var colorDict = new ResourceDictionary();
            string themePath;

            switch (theme)
            {
                case AppTheme.Dark:
                    themePath = "/Themes/DarkColors.xaml";
                    break;
                case AppTheme.PickMe:
                    themePath = "/Themes/PickMeColors.xaml";
                    break;
                case AppTheme.Dracula:
                    themePath = "/Themes/DraculaColors.xaml";
                    break;
                case AppTheme.Nord:
                    themePath = "/Themes/NordColors.xaml";
                    break;
                default:
                    themePath = "/Themes/DarkColors.xaml";
                    break;
            }

            colorDict.Source = new Uri(themePath, UriKind.RelativeOrAbsolute);
            resources.MergedDictionaries.Add(colorDict);

            // Для PickMe темы загружаем забавные тексты
            if (theme == AppTheme.PickMe)
            {
                var textDict = new ResourceDictionary();
                textDict.Source = new Uri("/Themes/PickMeTexts.xaml", UriKind.RelativeOrAbsolute);
                resources.MergedDictionaries.Add(textDict);
            }

            ThemeChanged?.Invoke();
        }

        public static string GetLocalizedString(string key, string defaultValue)
        {
            if (CurrentTheme == AppTheme.PickMe)
            {
                var resources = Application.Current.Resources;
                if (resources.Contains(key))
                {
                    return resources[key]?.ToString() ?? defaultValue;
                }
            }
            return defaultValue;
        }

        public static IHighlightingDefinition GetSyntaxHighlighting()
        {
            string colors = GetSyntaxColors();

            string xshd = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<SyntaxDefinition name=""IAS-2025"" xmlns=""http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008"">
    {colors}
    
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

            using (var reader = new System.IO.StringReader(xshd))
            using (var xmlReader = System.Xml.XmlReader.Create(reader))
            {
                return HighlightingLoader.Load(xmlReader, HighlightingManager.Instance);
            }
        }

        private static string GetSyntaxColors()
        {
            switch (CurrentTheme)
            {
                case AppTheme.Dark:
                    return @"
                        <Color name=""Keyword"" foreground=""#569CD6"" fontWeight=""bold"" />
                        <Color name=""Type"" foreground=""#4EC9B0"" fontWeight=""bold"" />
                        <Color name=""Function"" foreground=""#DCDCAA"" />
                        <Color name=""String"" foreground=""#CE9178"" />
                        <Color name=""Number"" foreground=""#B5CEA8"" />
                        <Color name=""Operator"" foreground=""#D4D4D4"" />";

                case AppTheme.PickMe:
                    return @"
                        <Color name=""Keyword"" foreground=""#FF1493"" fontWeight=""bold"" />
                        <Color name=""Type"" foreground=""#FF69B4"" fontWeight=""bold"" />
                        <Color name=""Function"" foreground=""#DA70D6"" />
                        <Color name=""String"" foreground=""#FF7F50"" />
                        <Color name=""Number"" foreground=""#FFA07A"" />
                        <Color name=""Operator"" foreground=""#C71585"" />";

                case AppTheme.Dracula:
                    return @"
                        <Color name=""Keyword"" foreground=""#FF79C6"" fontWeight=""bold"" />
                        <Color name=""Type"" foreground=""#8BE9FD"" fontWeight=""bold"" />
                        <Color name=""Function"" foreground=""#50FA7B"" />
                        <Color name=""String"" foreground=""#F1FA8C"" />
                        <Color name=""Number"" foreground=""#BD93F9"" />
                        <Color name=""Operator"" foreground=""#FFB86C"" />";

                case AppTheme.Nord:
                    return @"
                        <Color name=""Keyword"" foreground=""#81A1C1"" fontWeight=""bold"" />
                        <Color name=""Type"" foreground=""#88C0D0"" fontWeight=""bold"" />
                        <Color name=""Function"" foreground=""#8FBCBB"" />
                        <Color name=""String"" foreground=""#A3BE8C"" />
                        <Color name=""Number"" foreground=""#B48EAD"" />
                        <Color name=""Operator"" foreground=""#E5E9F0"" />";

                default:
                    return "";
            }
        }
    }
}