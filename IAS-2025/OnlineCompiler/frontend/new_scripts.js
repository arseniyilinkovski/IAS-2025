// Элементы DOM
const codeEditor = document.getElementById('codeEditor');
const output = document.getElementById('output');
const compileBtn = document.getElementById('compileBtn');
const copyAsmBtn = document.getElementById('copyAsmBtn');
const executeBtn = document.getElementById('executeBtn');
const clearBtn = document.getElementById('clearBtn');
const downloadBtn = document.getElementById('downloadBtn');
const shareBtn = document.getElementById('shareBtn');
const loader = document.getElementById('loader');
const statusText = document.getElementById('statusText');
const languageSelector = document.getElementById('languageSelector');
const exampleCards = document.querySelectorAll('.example-card');
const statusMessage = document.getElementById('statusMessage');

// URL бэкенда
const API_URL = 'http://localhost:8000/api';

// Переменная для хранения чистого ASM кода
let currentAsmCode = '';

// Словари для подсветки синтаксиса (IAS-2025)
const syntaxRules = {
    keywords: [
        'программа', 'начать', 'конец', 'если', 'иначе', 'конец_если',
        'пока', 'конец_пока', 'для', 'от', 'до', 'шаг', 'конец_для',
        'функция', 'вернуть', 'целое', 'дробное', 'строка', 'логическое',
        'символ', 'массив', 'структура', 'истина', 'ложь', 'вывод',
        'ввод', 'возврат', 'константа', 'переменная', 'тип', 'запись'
    ],
    
    types: [
        'целое', 'дробное', 'строка', 'логическое', 'символ',
        'массив', 'структура'
    ],
    
    builtins: [
        'длина', 'случайное', 'кодASCII', 'датаВремя', 'копировать',
        'степень', 'факториал', 'строка', 'число', 'округлить',
        'макс', 'мин', 'сумма', 'среднее'
    ],
    
    operators: [
        '+', '-', '*', '/', '%', '=', '==', '!=', '>', '<', '>=', '<=',
        '&&', '||', '!', '+=', '-=', '*=', '/='
    ]
};

// Примеры кода
const examples = {
    hello: `// Простейшая программа
программа ПриветМир:
    начать
        вывод("Привет, мир!")
    конец`,
        
    calc: `// Простые арифметические операции
программа Калькулятор:
    начать
        целое a = 15
        целое b = 7
        
        вывод("a = " + строка(a))
        вывод("b = " + строка(b))
        вывод("Сумма: " + строка(a + b))
        вывод("Разность: " + строка(a - b))
        вывод("Произведение: " + строка(a * b))
        вывод("Частное: " + строка(a / b))
    конец`,
        
    loop: `// Пример использования цикла
программа Циклы:
    начать
        вывод("Числа от 1 до 10:")
        
        целое i = 1
        пока i <= 10:
            вывод("  " + строка(i))
            i = i + 1
        конец_пока
        
        вывод("Готово!")
    конец`,
        
    condition: `// Пример использования условий
программа Условия:
    начать
        целое число = 42
        
        вывод("Проверка числа: " + строка(число))
        
        если число > 50:
            вывод("Число больше 50")
        иначе если число > 30:
            вывод("Число больше 30, но не больше 50")
        иначе:
            вывод("Число 30 или меньше")
        конец_если
    конец`
};

// Функция для отображения сообщения о статусе
function showStatusMessage(message, type = 'success') {
    statusMessage.textContent = message;
    statusMessage.className = `status-message ${type}`;
    statusMessage.style.display = 'block';
    
    // Автоматически скрываем через 5 секунд
    setTimeout(() => {
        statusMessage.style.display = 'none';
    }, 5000);
}

// Функция для получения чистого текста из contenteditable
function getPlainText() {
    let text = '';
    
    // Собираем текст из всех узлов в редакторе
    function extractText(node) {
        if (node.nodeType === Node.TEXT_NODE) {
            text += node.textContent;
        } else if (node.nodeType === Node.ELEMENT_NODE) {
            // Если это span с классом, добавляем его текст
            if (node.classList && (node.classList.contains('keyword') || 
                node.classList.contains('type') || 
                node.classList.contains('builtin') || 
                node.classList.contains('string') || 
                node.classList.contains('comment') || 
                node.classList.contains('operator') || 
                node.classList.contains('number'))) {
                text += node.textContent;
            } else {
                // Рекурсивно обходим дочерние элементы
                for (let child of node.childNodes) {
                    extractText(child);
                }
            }
        }
    }
    
    for (let child of codeEditor.childNodes) {
        extractText(child);
    }
    
    // Добавляем переносы строк
    text = text.replace(/<br>/g, '\n');
    
    return text;
}

// Функция для установки текста в contenteditable
function setPlainText(text) {
    codeEditor.textContent = text;
    applySyntaxHighlighting();
}

// Основная функция подсветки синтаксиса
function applySyntaxHighlighting() {
    // Сохраняем текущее положение курсора
    const selection = window.getSelection();
    let range = null;
    if (selection.rangeCount > 0) {
        range = selection.getRangeAt(0);
    }
    
    const text = codeEditor.textContent;
    if (!text) {
        codeEditor.innerHTML = '';
        return;
    }
    
    // Очищаем содержимое
    codeEditor.innerHTML = '';
    
    // Разбиваем текст на строки
    const lines = text.split('\n');
    
    lines.forEach((line, lineIndex) => {
        if (line.trim() === '' && lineIndex === lines.length - 1) {
            // Пустая строка в конце
            return;
        }
        
        const lineDiv = document.createElement('div');
        lineDiv.style.minHeight = '1.5em';
        
        if (line.trim() === '') {
            // Пустая строка
            lineDiv.innerHTML = '&nbsp;';
        } else {
            highlightLine(line, lineDiv);
        }
        
        codeEditor.appendChild(lineDiv);
    });
    
    // Восстанавливаем курсор
    if (range) {
        selection.removeAllRanges();
        selection.addRange(range);
    }
}

// Подсветка отдельной строки
function highlightLine(line, container) {
    let processed = line;
    
    // Обрабатываем комментарии
    if (processed.includes('//')) {
        const commentIndex = processed.indexOf('//');
        const codePart = processed.substring(0, commentIndex);
        const commentPart = processed.substring(commentIndex);
        
        // Подсвечиваем код
        highlightCodePart(codePart, container);
        
        // Добавляем комментарий
        const commentSpan = document.createElement('span');
        commentSpan.className = 'comment';
        commentSpan.textContent = commentPart;
        container.appendChild(commentSpan);
    } else {
        // Если нет комментариев, подсвечиваем всю строку
        highlightCodePart(processed, container);
    }
}

// Подсветка части кода (без комментариев)
function highlightCodePart(text, container) {
    // Обрабатываем строковые литералы
    const stringRegex = /"([^"\\]|\\.)*"/g;
    let lastIndex = 0;
    let match;
    
    while ((match = stringRegex.exec(text)) !== null) {
        // Текст до строки
        if (match.index > lastIndex) {
            highlightNonString(text.substring(lastIndex, match.index), container);
        }
        
        // Строковый литерал
        const stringSpan = document.createElement('span');
        stringSpan.className = 'string';
        stringSpan.textContent = match[0];
        container.appendChild(stringSpan);
        
        lastIndex = stringRegex.lastIndex;
    }
    
    // Оставшийся текст
    if (lastIndex < text.length) {
        highlightNonString(text.substring(lastIndex), container);
    }
}

// Подсветка нестроковых частей кода
function highlightNonString(text, container) {
    if (!text) return;
    
    // Разбиваем на слова и разделители
    const tokenRegex = /(\b\w+\b|[\(\)\[\]\{\}:;,\.]|\s+|[+\-*/%=><!&|]+)/g;
    let tokens = [];
    let token;
    
    while ((token = tokenRegex.exec(text)) !== null) {
        tokens.push(token[0]);
    }
    
    tokens.forEach(token => {
        if (!token.trim()) {
            // Пробелы добавляем как есть
            container.appendChild(document.createTextNode(token));
            return;
        }
        
        // Проверяем тип токена
        const lowerToken = token.toLowerCase();
        
        if (syntaxRules.keywords.some(kw => kw.toLowerCase() === lowerToken)) {
            const span = document.createElement('span');
            span.className = 'keyword';
            span.textContent = token;
            container.appendChild(span);
        } else if (syntaxRules.types.some(type => type.toLowerCase() === lowerToken)) {
            const span = document.createElement('span');
            span.className = 'type';
            span.textContent = token;
            container.appendChild(span);
        } else if (syntaxRules.builtins.some(builtin => builtin.toLowerCase() === lowerToken)) {
            const span = document.createElement('span');
            span.className = 'builtin';
            span.textContent = token;
            container.appendChild(span);
        } else if (syntaxRules.operators.some(op => token === op)) {
            const span = document.createElement('span');
            span.className = 'operator';
            span.textContent = token;
            container.appendChild(span);
        } else if (/^\d+(\.\d+)?$/.test(token)) {
            const span = document.createElement('span');
            span.className = 'number';
            span.textContent = token;
            container.appendChild(span);
        } else if (/^[a-zA-Z_]\w*$/.test(token)) {
            // Идентификаторы
            const span = document.createElement('span');
            span.className = 'variable';
            span.textContent = token;
            container.appendChild(span);
        } else {
            // Разделители и прочее
            container.appendChild(document.createTextNode(token));
        }
    });
}

// Функция для обновления вывода (только чистый ASM код)
function updateOutput(asmCode) {
    output.innerHTML = '';
    
    // Разбиваем код на строки
    const lines = asmCode.split('\n');
    
    lines.forEach(line => {
        const lineElement = document.createElement('div');
        lineElement.className = 'output-line';
        
        // Подсветка ASM синтаксиса (красная тема)
        if (line.includes('section') || line.includes('segment')) {
            lineElement.style.color = '#ff9e6d'; // оранжевый для директив
        } else if (line.includes('mov') || line.includes('add') || line.includes('sub') || 
                   line.includes('mul') || line.includes('div') || line.includes('cmp') ||
                   line.includes('jmp') || line.includes('je') || line.includes('jne') ||
                   line.includes('jg') || line.includes('jl') || line.includes('call') ||
                   line.includes('ret') || line.includes('push') || line.includes('pop')) {
            lineElement.style.color = '#ff6b6b'; // красный для инструкций
        } else if (line.includes('db') || line.includes('dw') || line.includes('dd') || 
                   line.includes('dq') || line.includes('resb') || line.includes('resw')) {
            lineElement.style.color = '#ffd166'; // желтый для директив данных
        } else if (line.includes('eax') || line.includes('ebx') || line.includes('ecx') ||
                   line.includes('edx') || line.includes('esi') || line.includes('edi') ||
                   line.includes('esp') || line.includes('ebp')) {
            lineElement.style.color = '#a8e6cf'; // зеленый для регистров
        } else if (line.includes(':') && !line.includes('//') && !line.includes(';')) {
            lineElement.style.color = '#ff8a8a'; // светло-красный для меток
        } else if (line.trim().startsWith(';') || line.trim().startsWith('//')) {
            lineElement.style.color = '#6a9955'; // зеленый для комментариев
        } else if (line.includes('"') || line.includes("'")) {
            lineElement.style.color = '#a8e6cf'; // светло-зеленый для строк
        } else if (line.includes('[') && line.includes(']')) {
            lineElement.style.color = '#ffd166'; // желтый для адресации памяти
        } else {
            lineElement.style.color = '#e0e0e0'; // стандартный цвет
        }
        
        lineElement.textContent = line;
        output.appendChild(lineElement);
    });
    
    // Сохраняем чистый ASM код
    currentAsmCode = asmCode;
    
    // Прокрутка к началу вывода
    output.scrollTop = 0;
}

// ... остальные функции остаются без изменений ...
// (compileCode, compileDemo, copyAsmCode, и т.д. из вашего оригинального скрипта)

// Обработчики событий
compileBtn.addEventListener('click', compileCode);
copyAsmBtn.addEventListener('click', copyAsmCode);

executeBtn.addEventListener('click', function() {
    showStatusMessage('Функция выполнения программы пока недоступна', 'info');
});

clearBtn.addEventListener('click', function() {
    codeEditor.textContent = '';
    currentAsmCode = '';
    updateOutput('; Введите код на языке IAS-2025 и нажмите "Транслировать в ASM"\n; для получения ассемблерного кода.');
    statusText.textContent = 'Готов к трансляции';
    showStatusMessage('Редактор очищен', 'info');
});

downloadBtn.addEventListener('click', function() {
    if (!currentAsmCode) {
        showStatusMessage('Нет ASM кода для скачивания. Сначала выполните трансляцию.', 'error');
        return;
    }
    
    const blob = new Blob([currentAsmCode], { type: 'text/plain;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'program.asm';
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
    
    showStatusMessage('ASM код сохранен в файл program.asm', 'success');
    statusText.textContent = 'Код скачан';
});

shareBtn.addEventListener('click', function() {
    const code = getPlainText().trim();
    
    if (!code) {
        showStatusMessage('Нет кода для совместного использования', 'error');
        return;
    }
    
    navigator.clipboard.writeText(code).then(() => {
        showStatusMessage('Исходный код скопирован в буфер обмена!', 'success');
        statusText.textContent = 'Код скопирован';
    }).catch(err => {
        showStatusMessage('Выделите и скопируйте код вручную', 'info');
    });
});

// Загрузка примера кода
function loadExample(exampleId) {
    if (examples[exampleId]) {
        setPlainText(examples[exampleId]);
        updateOutput('; Загружен пример кода. Нажмите "Транслировать в ASM" для преобразования.');
        showStatusMessage(`Загружен пример: ${exampleId}`, 'success');
        statusText.textContent = 'Пример загружен';
    }
}

// Добавление обработчиков для примеров кода
exampleCards.forEach(card => {
    card.addEventListener('click', () => {
        const exampleId = card.getAttribute('data-example');
        loadExample(exampleId);
        
        // Подсветка выбранного примера
        exampleCards.forEach(c => c.style.borderColor = 'transparent');
        card.style.borderColor = 'var(--primary)';
    });
});

// Обработка ввода в редакторе
// Обновление статуса при вводе
codeEditor.addEventListener('input', () => {
    // Просто обновляем нумерацию строк и статус
    if (codeEditor.value.trim()) {
        statusText.textContent = 'Код изменен';
    } else {
        statusText.textContent = 'Готов к трансляции';
    }
    updateLineNumbers();
});
// Простая подсветка при фокусе
codeEditor.addEventListener('focus', function() {
    this.style.backgroundColor = '#0a0e14';
    this.style.color = '#c9d1d9';
});

codeEditor.addEventListener('blur', function() {
    this.style.backgroundColor = '#0d1117';
    this.style.color = '#c9d1d9';
});
// Удалите ВСЕ функции, связанные с подсветкой:
// - highlightKeywords()
// - restoreCursor() 
// - handleInput()
// - getPlainText()

// Обработка специальных клавиш
codeEditor.addEventListener('keydown', function(e) {
    // Tab для отступа
    if (e.key === 'Tab') {
        e.preventDefault();
        document.execCommand('insertText', false, '    ');
        applySyntaxHighlighting();
    }
    
    // Enter для сохранения подсветки
    if (e.key === 'Enter') {
        setTimeout(() => {
            applySyntaxHighlighting();
        }, 10);
    }
});

// Инициализация
document.addEventListener('DOMContentLoaded', function() {
    // Применяем подсветку к начальному коду
    applySyntaxHighlighting();
    
    // Устанавливаем начальный текст в output
    updateOutput([
        "// Добро пожаловать в онлайн транслятор IAS-2025!",
        "// Этот инструмент трансформирует код с языка IAS-2025 в Assembler.",
        "",
        "Инструкция:",
        "1. Напишите код на языке IAS-2025 в левой панели",
        "2. Нажмите 'Транслировать в ASM' для получения ассемблерного кода",
        "3. Используйте 'Копировать ASM-код' для копирования результата",
        "4. Используйте 'Скачать ASM код' для сохранения в файл",
        "",
        "Для быстрого старта выберите один из примеров ниже."
    ].join('\n'));
    
    // Проверяем бэкенд при загрузке
    setTimeout(() => {
        checkBackend();
    }, 1000);
});
// Функция для проверки доступности бэкенда
async function checkBackend() {
    try {
        const response = await fetch(`${API_URL}/health`);
        const data = await response.json();
        
        if (data.translator_available) {
            showStatusMessage('Бэкенд и транслятор доступны', 'success');
            return true;
        } else {
            showStatusMessage('Бэкенд доступен, но транслятор не найден. Используется демо-режим.', 'warning');
            return false;
        }
    } catch (error) {
        showStatusMessage('Бэкенд недоступен. Используется демо-режим.', 'error');
        return false;
    }
}
// Функция транслирования кода в ASM
async function compileCode() {
    const code = getPlainText().trim();
    
    if (!code) {
        showStatusMessage('Введите код для трансляции', 'error');
        return;
    }
    
    // Показать загрузку
    loader.style.display = 'block';
    statusText.textContent = 'Трансляция в ASM...';
    compileBtn.disabled = true;
    
    try {
        // Проверяем доступность бэкенда
        const isBackendAvailable = await checkBackend();
        
        if (!isBackendAvailable) {
            // Используем демо-режим
            await compileDemo(code);
            return;
        }
        
        // Отправляем запрос на реальную трансляцию
        const response = await fetch(`${API_URL}/compile`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                code: code,
                language: languageSelector.value
            })
        });
        
        const data = await response.json();
        
        if (data.success) {
            // Отображаем только чистый ASM код
            updateOutput(data.asm_code || data.output);
            showStatusMessage('Трансляция завершена успешно!', 'success');
            statusText.textContent = 'Трансляция завершена';
        } else {
            // В случае ошибки показываем сообщение об ошибке
            const errorLines = [
                "; ОШИБКА ТРАНСЛЯЦИИ",
                "; =================",
                `; ${data.message || "Неизвестная ошибка"}`,
                ";",
                ...data.output.split('\n')
            ];
            updateOutput(errorLines.join('\n'));
            showStatusMessage(`Ошибка трансляции: ${data.message}`, 'error');
            statusText.textContent = 'Ошибка трансляции';
        }
        
    } catch (error) {
        // В случае сетевой ошибки используем демо-режим
        const errorLines = [
            "; ПРЕДУПРЕЖДЕНИЕ: ДЕМО-РЕЖИМ",
            "; ===========================",
            "; Бэкенд недоступен. Показан демонстрационный ASM код.",
            "; Для реальной трансляции убедитесь что:",
            "; 1. Бэкенд-сервер запущен (http://localhost:8000)",
            "; 2. IAS-2025.exe находится в правильной директории",
            ";",
            "; Исходный код:",
            `; ${code.substring(0, 100)}${code.length > 100 ? '...' : ''}`,
            ";"
        ];
        
        await compileDemo(code);
        showStatusMessage(`Ошибка сети: ${error.message}. Используется демо-режим.`, 'error');
        
    } finally {
        // Скрыть загрузку
        loader.style.display = 'none';
        compileBtn.disabled = false;
    }
}

// Демо-версия трансляции (возвращает только чистый ASM код)
async function compileDemo(code) {
    return new Promise(resolve => {
        setTimeout(() => {
            // Генерируем демо ASM код
            const asmLines = [
                "; Демонстрация сгенерированного ASM кода",
                "; ======================================",
                "; Этот код был бы сгенерирован транслятором из языка IAS-2025",
                ";",
                "section .data",
                "    msg_hello db 'Привет, мир!', 0",
                "    msg_welcome db 'Добро пожаловать в онлайн транслятор!', 0",
                "    msg_counter db 'Счетчик: ', 0",
                "    msg_condition db 'Условие выполнено!', 0",
                "    buffer db 20 dup(0) ; Буфер для преобразования чисел",
                "",
                "section .text",
                "    global _start",
                "",
                "_start:",
                "    ; Вывод приветствия",
                "    mov eax, 4",
                "    mov ebx, 1",
                "    mov ecx, msg_hello",
                "    mov edx, 13",
                "    int 0x80",
                "",
                "    ; Вывод второго сообщения",
                "    mov eax, 4",
                "    mov ebx, 1",
                "    mov ecx, msg_welcome",
                "    mov edx, 43",
                "    int 0x80",
                "",
                "    ; Инициализация счетчика (i = 1)",
                "    mov ecx, 1",
                "",
                "_loop_start:",
                "    ; Проверка условия (i <= 5)",
                "    cmp ecx, 5",
                "    jg _loop_end",
                "",
                "    ; Вывод 'Счетчик: '",
                "    push ecx",
                "    mov eax, 4",
                "    mov ebx, 1",
                "    mov ecx, msg_counter",
                "    mov edx, 9",
                "    int 0x80",
                "    pop ecx",
                "",
                "    ; Здесь должен быть код преобразования числа в строку",
                "    ; и вывод значения счетчика",
                "    ; (опущено для упрощения демо)",
                "",
                "    ; Увеличение счетчика (i = i + 1)",
                "    inc ecx",
                "    jmp _loop_start",
                "",
                "_loop_end:",
                "    ; Вывод сообщения об условии",
                "    mov eax, 4",
                "    mov ebx, 1",
                "    mov ecx, msg_condition",
                "    mov edx, 18",
                "    int 0x80",
                "",
                "    ; Завершение программы",
                "    mov eax, 1",
                "    xor ebx, ebx",
                "    int 0x80"
            ];
            
            // Отображаем только ASM код
            updateOutput(asmLines.join('\n'));
            showStatusMessage('Трансляция завершена (демо-режим)', 'info');
            statusText.textContent = 'Трансляция завершена (демо)';
            
            resolve();
        }, 1000);
    });
}
