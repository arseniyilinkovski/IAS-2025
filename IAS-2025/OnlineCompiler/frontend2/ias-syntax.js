// Режим подсветки синтаксиса для языка IAS-2025 (полная поддержка)
(function() {
    'use strict';

    // Ключевые слова IAS-2025 (из контрольного примера)
    const keywords = [
        'программа', 'function', 'main', 'output', 'input',
        'int', 'str', 'bool', 'char', 'float', 'void',
        'var', 'param', 'return', 'if', 'then', 'else',
        'repeat', 'for', 'while', 'do', 'break', 'continue',
        'true', 'false', 'null', 'and', 'or', 'not'
    ];

    // Встроенные функции (из контрольного примера)
    const builtins = [
        'getLocalTimeAndDate', 'random', 'asciiCode', 'lenght', 'copy',
        'powNumber', 'factorialOfNumber', 'sin', 'cos', 'tan', 'sqrt',
        'abs', 'round', 'floor', 'ceil', 'log', 'exp', 'min', 'max',
        'concat', 'substring', 'toUpper', 'toLower', 'trim'
    ];

    // Системные типы и константы
    const types = ['int', 'str', 'bool', 'char', 'float', 'void'];
    const constants = ['true', 'false', 'null'];

    // Операторы
    const operators = ['\\+', '-', '\\*', '/', '%', '=', '==', '!=', '<', '>', '<=', '>=', '&', '\\|', '~', '<<', '>>', '\\+\\+', '--'];

    // Определяем режим CodeMirror для IAS-2025
    CodeMirror.defineMode('ias', function(config, parserConfig) {
        const indentUnit = config.indentUnit;
        
        // Регулярные выражения для токенов
        const tokenRegexes = {
            // Ключевые слова - полное совпадение
            keyword: new RegExp(`^(?:${keywords.join('|')})\\b`),
            
            // Встроенные функции
            builtin: new RegExp(`^(?:${builtins.join('|')})\\b`),
            
            // Типы данных
            type: new RegExp(`^(?:${types.join('|')})\\b`),
            
            // Константы
            constant: new RegExp(`^(?:${constants.join('|')})\\b`),
            
            // Числовые литералы (двоичные, шестнадцатеричные, восьмеричные, десятичные)
            number: /^(?:b[01]+|h[0-9a-fA-F]+|o[0-7]+|\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)\b/,
            
            // Строки (двойные кавычки)
            string: /^"[^"\\]*(?:\\.[^"\\]*)*"/,
            
            // Символы (одинарные кавычки)
            char: /^'[^'\\]*(?:\\.[^'\\]*)*'/,
            
            // Комментарии (# и //)
            comment: /^(?:#|\/\/).*/,
            
            // Многострочные комментарии
            blockCommentStart: /^\/\*/,
            blockCommentEnd: /\*\//,
            
            // Операторы
            operator: new RegExp(`^(?:${operators.join('|')})`),
            
            // Скобки
            bracket: /^[{}()\[\]]/,
            
            // Пунктуация
            punctuation: /^[;,.:]/,
            
            // Идентификаторы
            identifier: /^[a-zA-Zа-яА-Я_][a-zA-Zа-яА-Я0-9_]*/,
            
            // Пробелы
            whitespace: /^\s+/
        };

        // Состояние для многострочных комментариев
        function inBlockComment(stream, state) {
            if (stream.match(tokenRegexes.blockCommentEnd)) {
                state.inBlockComment = false;
                return 'comment';
            }
            
            stream.skipToEnd();
            return 'comment';
        }

        function tokenBase(stream, state) {
            // Обработка многострочных комментариев
            if (state.inBlockComment) {
                return inBlockComment(stream, state);
            }

            // Пропускаем пробелы
            if (stream.eatSpace()) {
                return null;
            }

            // Многострочные комментарии
            if (stream.match(tokenRegexes.blockCommentStart)) {
                state.inBlockComment = true;
                return 'comment';
            }

            // Однострочные комментарии
            if (stream.match(tokenRegexes.comment)) {
                stream.skipToEnd();
                return 'comment';
            }

            // Строки
            if (stream.match(tokenRegexes.string)) {
                return 'string';
            }
            
            // Символы
            if (stream.match(tokenRegexes.char)) {
                return 'string-2';
            }

            // Числа
            if (stream.match(tokenRegexes.number)) {
                return 'number';
            }

            // Ключевые слова
            if (stream.match(tokenRegexes.keyword)) {
                const word = stream.current();
                if (types.includes(word)) {
                    return 'type';
                }
                if (constants.includes(word)) {
                    return 'constant';
                }
                return 'keyword';
            }

            // Встроенные функции
            if (stream.match(tokenRegexes.builtin)) {
                return 'builtin';
            }

            // Операторы
            if (stream.match(tokenRegexes.operator)) {
                return 'operator';
            }

            // Скобки
            if (stream.match(tokenRegexes.bracket)) {
                return 'bracket';
            }

            // Пунктуация
            if (stream.match(tokenRegexes.punctuation)) {
                return null;
            }

            // Идентификаторы
            if (stream.match(tokenRegexes.identifier)) {
                return 'variable';
            }

            // Любой другой символ
            stream.next();
            return null;
        }

        // Функция для расчета отступов
        function indent(state, textAfter) {
            const firstChar = textAfter && textAfter.charAt(0);
            let indent = state.indentLevel || 0;

            // Увеличиваем отступ после {
            if (state.lastToken === '{') {
                indent += indentUnit;
            }

            // Уменьшаем отступ перед }
            if (textAfter && textAfter.charAt(0) === '}') {
                indent -= indentUnit;
            }

            // Уменьшаем отступ перед else, then
            if (textAfter.match(/^\s*(?:else|then|конец_если)/)) {
                indent -= indentUnit;
            }

            return Math.max(0, indent);
        }

        return {
            startState: function() {
                return {
                    indentLevel: 0,
                    lastToken: null,
                    inBlockComment: false,
                    inString: false,
                    inChar: false
                };
            },

            token: function(stream, state) {
                const style = tokenBase(stream, state);
                
                if (stream.eol()) {
                    state.lastToken = null;
                } else if (style && style !== 'whitespace') {
                    state.lastToken = stream.current();
                }

                // Обновляем состояние для отступов
                if (style === 'bracket') {
                    const bracket = stream.current();
                    if (bracket === '{') {
                        state.indentLevel += indentUnit;
                    } else if (bracket === '}') {
                        state.indentLevel = Math.max(0, state.indentLevel - indentUnit);
                    }
                }

                return style;
            },

            indent: indent,

            electricChars: /^{}$/,
            lineComment: '#',
            blockCommentStart: '/*',
            blockCommentEnd: '*/',
            fold: 'brace'
        };
    });

    // Регистрируем режим
    CodeMirror.defineMIME('text/x-ias', 'ias');

    // Кастомная тема "Red Steel" для IAS-2025
    CodeMirror.defineTheme('red-steel', {
        '&': {
            backgroundColor: '#0f0f15',
            color: '#c0c0c0',  // Серый цвет текста
            fontFamily: "'Roboto Mono', monospace",
            fontSize: '14px'
        },
        '.cm-comment': {
            color: '#6a9955',
            fontStyle: 'italic'
        },
        '.cm-keyword': {
            color: '#ff6b6b',
            fontWeight: 'bold'
        },
        '.cm-type': {
            color: '#4ec9b0',
            fontWeight: 'bold'
        },
        '.cm-builtin': {
            color: '#dcdcaa'
        },
        '.cm-variable': {
            color: '#9cdcfe'
        },
        '.cm-variable-2': {
            color: '#4fc1ff'
        },
        '.cm-variable-3': {
            color: '#c586c0'
        },
        '.cm-string': {
            color: '#ce9178'
        },
        '.cm-string-2': {
            color: '#d7ba7d'
        },
        '.cm-number': {
            color: '#b5cea8'
        },
        '.cm-constant': {
            color: '#569cd6'
        },
        '.cm-operator': {
            color: '#d4d4d4'
        },
        '.cm-bracket': {
            color: '#808080'
        },
        '.cm-tag': {
            color: '#569cd6'
        },
        '.cm-attribute': {
            color: '#9cdcfe'
        },
        '.cm-link': {
            color: '#569cd6'
        },
        '.cm-error': {
            backgroundColor: '#ff5555',
            color: '#f8f8f2'
        },
        '.cm-searching': {
            backgroundColor: '#ffb86c80'
        },
        
        '&.CodeMirror-focused .CodeMirror-selected': {
            backgroundColor: '#3a3a3a'
        },
        '.CodeMirror-selected': {
            backgroundColor: '#2a2a2a'
        },
        
        '.CodeMirror-gutters': {
            backgroundColor: '#0a0a0f',
            borderRight: '1px solid #222230',
            color: '#555566'
        },
        '.CodeMirror-guttermarker': {
            color: '#ff5555'
        },
        '.CodeMirror-guttermarker-subtle': {
            color: '#555566'
        },
        '.CodeMirror-linenumber': {
            color: '#555566',
            minWidth: '40px'
        },
        '.CodeMirror-cursor': {
            borderLeft: '2px solid #ff5555',
            borderLeftWidth: '2px'
        },
        '.CodeMirror-activeline-background': {
            backgroundColor: '#1a1a25'
        },
        '.CodeMirror-matchingbracket': {
            backgroundColor: 'transparent',
            color: '#ff5555 !important',
            fontWeight: 'bold',
            textDecoration: 'underline'
        },
        '.CodeMirror-matchingtag': {
            backgroundColor: '#2a2a3a'
        },
        '.CodeMirror-foldgutter-open': {
            color: '#555566'
        },
        '.CodeMirror-foldgutter-folded': {
            color: '#ff5555'
        },
        '.CodeMirror-foldmarker': {
            color: '#ff5555',
            textShadow: 'none',
            fontFamily: 'monospace',
            lineHeight: '0.3',
            cursor: 'pointer'
        }
    });

    // Режим для ASM с красной темой
    CodeMirror.defineMode('asm-red', function() {
        return {
            token: function(stream) {
                if (stream.eatSpace()) return null;
                
                // Комментарии
                if (stream.match(/^;.*/)) {
                    stream.skipToEnd();
                    return 'asm-comment';
                }
                
                // Директивы
                if (stream.match(/^\.(?:data|text|bss|section)/)) return 'asm-directive';
                if (stream.match(/^\.\w+/)) return 'asm-keyword';
                
                // Метки
                if (stream.match(/^[A-Za-z_][A-Za-z0-9_]*:/)) return 'asm-label';
                
                // Инструкции
                if (stream.match(/^\b(mov|add|sub|mul|div|inc|dec|cmp|jmp|je|jne|jg|jl|call|ret|push|pop|lea|and|or|xor|not|shl|shr|test|nop|retf|iret|sti|cli|in|out)\b/i)) {
                    return 'asm-instruction';
                }
                
                // Регистры
                if (stream.match(/^\b(e?[abcd]x|e?[bs]p|e?[ds]i|e?ip|r\d{1,2}[dwb]?|al|ah|bl|bh|cl|ch|dl|dh|sil|dil|spl|bpl|xmm\d+|ymm\d+|zmm\d+)\b/i)) {
                    return 'asm-register';
                }
                
                // Типы памяти
                if (stream.match(/^\b((byte|word|dword|qword)\s+ptr)\b/i)) {
                    return 'asm-type';
                }
                
                // Числа (шестнадцатеричные, двоичные, десятичные)
                if (stream.match(/^(0x[0-9a-fA-F]+|0b[01]+|[0-9]+|[-]?[0-9]+)\b/)) return 'asm-number';
                
                // Строки
                if (stream.match(/^'[^']*'/)) return 'asm-string';
                if (stream.match(/^"[^"]*"/)) return 'asm-string';
                
                // Операторы
                if (stream.match(/^[+\-*/=<>[\]()]/)) return 'asm-operator';
                
                stream.next();
                return null;
            }
        };
    });

    CodeMirror.defineMIME('text/x-asm-red', 'asm-red');

    // Стили для ASM подсветки
    const style = document.createElement('style');
    style.textContent = `
        /* Стили для IAS-2025 редактора */
        .cm-s-red-steel {
            background: #0f0f15 !important;
        }
        
        .cm-s-red-steel .CodeMirror-line {
            color: #c0c0c0 !important;
        }
        
        /* Особые стили для конструкций IAS-2025 */
        .cm-s-red-steel .cm-function {
            color: #dcdcaa;
            font-style: italic;
        }
        
        .cm-s-red-steel .cm-param {
            color: #9cdcfe;
        }
        
        .cm-s-red-steel .cm-main {
            color: #ff6b6b;
            font-weight: bold;
        }
        
        .cm-s-red-steel .cm-output {
            color: #4ec9b0;
            font-weight: bold;
        }
        
        .cm-s-red-steel .cm-repeat {
            color: #c586c0;
            font-weight: bold;
        }
        
        .cm-s-red-steel .cm-if {
            color: #ff6b6b;
            font-weight: bold;
        }
        
        .cm-s-red-steel .cm-then {
            color: #4ec9b0;
            font-weight: bold;
        }
        
        .cm-s-red-steel .cm-else {
            color: #569cd6;
            font-weight: bold;
        }
        
        /* Стили для ASM редактора */
        .cm-s-red-steel .cm-asm-comment {
            color: #6a9955;
            font-style: italic;
        }
        
        .cm-s-red-steel .cm-asm-instruction {
            color: #ff6b6b;
            font-weight: bold;
        }
        
        .cm-s-red-steel .cm-asm-directive {
            color: #c586c0;
            font-weight: bold;
        }
        
        .cm-s-red-steel .cm-asm-keyword {
            color: #569cd6;
        }
        
        .cm-s-red-steel .cm-asm-label {
            color: #4ec9b0;
        }
        
        .cm-s-red-steel .cm-asm-register {
            color: #9cdcfe;
        }
        
        .cm-s-red-steel .cm-asm-type {
            color: #dcdcaa;
        }
        
        .cm-s-red-steel .cm-asm-number {
            color: #b5cea8;
        }
        
        .cm-s-red-steel .cm-asm-string {
            color: #ce9178;
        }
        
        .cm-s-red-steel .cm-asm-operator {
            color: #d4d4d4;
        }
        
        /* Подсветка парных скобок */
        .cm-s-red-steel .CodeMirror-matchingbracket {
            background-color: #3a3a3a !important;
            color: #ff6b6b !important;
            padding: 1px 0;
            border-radius: 2px;
        }
        
        /* Подсветка текущей строки */
        .cm-s-red-steel .CodeMirror-activeline {
            background: #1a1a25 !important;
        }
        
        /* Стили для скроллбара редактора */
        .CodeMirror-vscrollbar, .CodeMirror-hscrollbar {
            scrollbar-width: thin;
            scrollbar-color: #ff5555 #1a1a1a;
        }
        
        .CodeMirror-vscrollbar::-webkit-scrollbar,
        .CodeMirror-hscrollbar::-webkit-scrollbar {
            width: 10px;
            height: 10px;
        }
        
        .CodeMirror-vscrollbar::-webkit-scrollbar-track,
        .CodeMirror-hscrollbar::-webkit-scrollbar-track {
            background: #1a1a1a;
        }
        
        .CodeMirror-vscrollbar::-webkit-scrollbar-thumb,
        .CodeMirror-hscrollbar::-webkit-scrollbar-thumb {
            background: #ff5555;
            border-radius: 5px;
        }
        
        .CodeMirror-vscrollbar::-webkit-scrollbar-thumb:hover,
        .CodeMirror-hscrollbar::-webkit-scrollbar-thumb:hover {
            background: #ff3333;
        }
    `;
    document.head.appendChild(style);

    // Автодополнение для IAS-2025
    CodeMirror.registerHelper('hintWords', 'ias', 
        keywords.concat(builtins).concat(['main', 'output', 'input'])
    );

    console.log('IAS-2025 syntax highlighting loaded with full support');
})();

// (В конце функции, после создания стилей, добавляем принудительные стили)

// Принудительные стили для редактора
const editorStyles = document.createElement('style');
editorStyles.textContent = `
    /* ПРИНУДИТЕЛЬНЫЕ СТИЛИ ДЛЯ РЕДАКТОРА КОДА */
    .CodeMirror, .CodeMirror * {
        color: #c0c0c0 !important;
        font-family: 'Roboto Mono', monospace !important;
    }
    
    /* Основной текст редактора */
    .CodeMirror-line {
        color: #c0c0c0 !important;
    }
    
    /* Гарантируем серый цвет всего текста */
    .CodeMirror-line > span {
        color: #c0c0c0 !important;
    }
    
    /* Отдельные стили для разных типов токенов */
    .cm-s-red-steel .cm-keyword { color: #ff6b6b !important; }
    .cm-s-red-steel .cm-type { color: #4ec9b0 !important; }
    .cm-s-red-steel .cm-builtin { color: #dcdcaa !important; }
    .cm-s-red-steel .cm-string { color: #ce9178 !important; }
    .cm-s-red-steel .cm-number { color: #b5cea8 !important; }
    .cm-s-red-steel .cm-comment { color: #6a9955 !important; }
    .cm-s-red-steel .cm-variable { color: #9cdcfe !important; }
    .cm-s-red-steel .cm-operator { color: #d4d4d4 !important; }
    .cm-s-red-steel .cm-def { color: #dcdcaa !important; }
    .cm-s-red-steel .cm-property { color: #9cdcfe !important; }
    .cm-s-red-steel .cm-meta { color: #569cd6 !important; }
    .cm-s-red-steel .cm-qualifier { color: #d7ba7d !important; }
    .cm-s-red-steel .cm-atom { color: #569cd6 !important; }
    .cm-s-red-steel .cm-tag { color: #569cd6 !important; }
    .cm-s-red-steel .cm-attribute { color: #9cdcfe !important; }
    
    /* Специальные конструкции IAS-2025 */
    .cm-s-red-steel .cm-function { color: #dcdcaa !important; font-style: italic !important; }
    .cm-s-red-steel .cm-param { color: #4fc1ff !important; }
    .cm-s-red-steel .cm-main { color: #ff6b6b !important; font-weight: bold !important; }
    .cm-s-red-steel .cm-output { color: #4ec9b0 !important; }
    .cm-s-red-steel .cm-repeat { color: #c586c0 !important; }
    .cm-s-red-steel .cm-if { color: #ff6b6b !important; }
    .cm-s-red-steel .cm-then { color: #4ec9b0 !important; }
    .cm-s-red-steel .cm-else { color: #569cd6 !important; }
    
    /* Фон редактора */
    .cm-s-red-steel.CodeMirror {
        background-color: #0f0f15 !important;
        color: #c0c0c0 !important;
    }
    
    /* Номера строк */
    .cm-s-red-steel .CodeMirror-gutters {
        background-color: #0a0a0f !important;
        border-right-color: #222230 !important;
    }
    
    .cm-s-red-steel .CodeMirror-linenumber {
        color: #666677 !important;
    }
    
    /* Курсор */
    .cm-s-red-steel .CodeMirror-cursor {
        border-left-color: #ff6b6b !important;
    }
`;
document.head.appendChild(editorStyles);