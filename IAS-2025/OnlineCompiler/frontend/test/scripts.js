// Элементы DOM
const codeEditor = document.getElementById('codeEditor'); // contenteditable div
const hiddenCode = document.getElementById('hiddenCode'); // скрытый textarea
const output = document.getElementById('output');
const compileBtn = document.getElementById('compileBtn');
const copyAsmBtn = document.getElementById('copyAsmBtn');
const clearBtn = document.getElementById('clearBtn');
const downloadBtn = document.getElementById('downloadBtn');
const loader = document.getElementById('loader');
const statusText = document.getElementById('statusText');
const languageSelector = document.getElementById('languageSelector');
const exampleCards = document.querySelectorAll('.example-card');
const lineNumbers = document.getElementById('lineNumbers');

// Словарь ключевых слов для подсветки
const KEYWORDS = [
    'function', 'main', 'if', 'then', 'else', 'repeat', 'return',
    'int', 'bool', 'str', 'char', 'var', 'param',
    'output', 'lenght', 'copy', 'getLocalTimeAndDate', 'random',
    'asciiCode', 'powNumber', 'factorialOfNumber'
];

// URL бэкенда
const API_URL = 'http://localhost:8000/api';

// Примеры кода
const examples = {
    hello: `#Пример программы на языке IAS-2025
main{
    output "Hello world";
}`,
        
    functions: `#Пример с пользовательскими функциями
bool function IsEven(int param arg){
    bool var res;
    int var buf = arg % 2;
    if (buf & 0)
    then{
        res = true;
    }
    else{
        res = false;
    }
    return res;
}
main{
    int var test = 10;
    bool var evenFlag = IsEven(test);
    output evenFlag;
}`,
        
    control: `#Контрольный пример
int function get_sum(int param argOne, int param argTwo){
    int var res = argOne + argTwo;
    return res;
}

bool function IsStringsEquals(str param StringOne, str param StringTwo){
    bool var res;
    int var lenOne = lenght(StringOne);
    int var lenTwo = lenght(StringTwo);
    if (lenOne & lenTwo)
    then{
        res = true;
    }
    else{
        res = false;
    }
    return res;
}   
bool function IsEven(int param arg){
    bool var res;
    int var buf = arg % 2;
    if (buf & 0)
    then{
        res = true;
    }
    else{
        res = false;
    }
    return res;
}
main
{
output "n";
str var date = getLocalTimeAndDate();
output date;
int var a = b1010; #10
output "Переменная a";
output a;
int var b = h32; #50
output "Переменная b";
output b;
int var rnd = random(a, b);
output "Случайное число от a до b";
output rnd;
char var ch = 'c';
int var code = asciiCode(ch);
output "Код ch";
output code;
a = a >> 2;     
output "a после сдвига вправо на 2";
output a;
b = b << 2;
output "b после сдвига влево на 2";
output b;
int var iter = 0;
repeat(10){
    output iter;
    iter = iter + 1;
}
repeat(iter > 1){
    output "new iter";
    output iter;
    iter = iter - 1;
}

output "Выполнение функции get_sum(a, b)";
int var sum = get_sum(a, b);
output sum;

str var stringOne = "Hello, my name is Arseniy";
str var stringTwo = "Hello, my name is Alex";

output "Первых три элемента, скопированных из первой строки во вторую :";
str var ns = copy(stringOne, stringTwo, 3 );
output ns;
output stringOne;
output stringTwo;

bool var flag = IsStringsEquals(stringOne,stringTwo);
output flag;
int var test = 2;
output "test:";
output test;
test = powNumber(test, 3);
output "test после powNumber:";
output test;
test = factorialOfNumber(test);
output "Факториал переменной test:";
output test;
bool var evenFlag = IsEven(test);
output evenFlag;
int var oct = o12; #10
output oct;
}`,
        
    error: `main {
int var a = 11;
a = (8 / 2) * (a - 2) + (a % 2);
output a;
}`
};

// Функции для работы с уведомлениями
function showNotification(message, type = 'success') {
    // Создаем уведомление
    const notification = document.createElement('div');
    notification.className = `notification notification-${type}`;
    notification.innerHTML = `
        <div class="notification-content">
            <i class="fas ${type === 'success' ? 'fa-check-circle' : 'fa-info-circle'}"></i>
            <span>${message}</span>
        </div>
    `;
    
    // Добавляем стили для уведомления
    notification.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        background: ${type === 'success' ? 'linear-gradient(135deg, var(--success) 0%, var(--success-dark) 100%)' : 'linear-gradient(135deg, var(--primary) 0%, var(--primary-dark) 100%)'};
        color: white;
        padding: 12px 20px;
        border-radius: var(--border-radius);
        box-shadow: var(--box-shadow);
        z-index: 1000;
        animation: slideIn 0.3s ease-out;
        max-width: 300px;
        font-size: 0.9rem;
    `;
    
    // Стили для содержимого
    const style = document.createElement('style');
    style.textContent = `
        @keyframes slideIn {
            from {
                opacity: 0;
                transform: translateX(100%);
            }
            to {
                opacity: 1;
                transform: translateX(0);
            }
        }
        .notification-content {
            display: flex;
            align-items: center;
            gap: 10px;
        }
        .notification-content i {
            font-size: 1.2rem;
        }
    `;
    document.head.appendChild(style);
    
    document.body.appendChild(notification);
    
    // Автоматическое скрытие через 3 секунды
    setTimeout(() => {
        notification.style.animation = 'slideOut 0.3s ease-out';
        notification.style.opacity = '0';
        setTimeout(() => {
            if (notification.parentNode) {
                notification.parentNode.removeChild(notification);
            }
        }, 300);
    }, 3000);
    
    // Добавляем анимацию скрытия
    const hideStyle = document.createElement('style');
    hideStyle.textContent = `
        @keyframes slideOut {
            from {
                opacity: 1;
                transform: translateX(0);
            }
            to {
                opacity: 0;
                transform: translateX(100%);
            }
        }
    `;
    document.head.appendChild(hideStyle);
}

// Функция для обновления нумерации строк
function updateLineNumbers() {
    const text = getPlainText();
    const lines = text.split('\n');
    const totalLines = lines.length;
    
    // Создаем нумерацию строк
    let numbersHTML = '';
    for (let i = 1; i <= totalLines; i++) {
        numbersHTML += `<div class="line-number">${i}</div>`;
    }
    
    lineNumbers.innerHTML = numbersHTML;
}

// Функция для синхронизации прокрутки
function syncScroll() {
    lineNumbers.scrollTop = codeEditor.scrollTop;
}

// Функция для обновления вывода
function updateOutput(lines, type = 'normal') {
    output.innerHTML = '';
    
    lines.forEach(line => {
        const lineElement = document.createElement('div');
        lineElement.className = 'output-line';
        
        if (type === 'error') {
            lineElement.classList.add('output-error');
        } else if (type === 'success') {
            lineElement.classList.add('output-success');
        }
        
        // Подсветка ASM синтаксиса
        if (line.includes('section') || line.includes('segment')) {
            lineElement.style.color = '#8b949e'; // серый для директив
        } else if (line.includes('mov') || line.includes('add') || line.includes('sub') || 
                   line.includes('mul') || line.includes('div') || line.includes('cmp') ||
                   line.includes('jmp') || line.includes('je') || line.includes('jne') ||
                   line.includes('jg') || line.includes('jl') || line.includes('call') ||
                   line.includes('ret') || line.includes('push') || line.includes('pop')) {
            lineElement.style.color = '#569cd6'; // синий для инструкций
        } else if (line.includes('db') || line.includes('dw') || line.includes('dd') || 
                   line.includes('dq') || line.includes('resb') || line.includes('resw')) {
            lineElement.style.color = '#9cdcfe'; // голубой для директив данных
        } else if (line.includes('eax') || line.includes('ebx') || line.includes('ecx') ||
                   line.includes('edx') || line.includes('esi') || line.includes('edi') ||
                   line.includes('esp') || line.includes('ebp')) {
            lineElement.style.color = '#4ec9b0'; // бирюзовый для регистров
        } else if (line.includes(':') && !line.includes('//') && !line.includes(';')) {
            lineElement.style.color = '#c586c0'; // фиолетовый для меток
        } else if (line.trim().startsWith(';') || line.trim().startsWith('//')) {
            lineElement.style.color = '#6a9955'; // зеленый для комментариев
        } else if (line.includes('"') || line.includes("'")) {
            lineElement.style.color = '#ce9178'; // оранжевый для строк
        } else if (line.includes('[') && line.includes(']')) {
            lineElement.style.color = '#dcdcaa'; // желтый для адресации памяти
        }
        
        lineElement.textContent = line;
        output.appendChild(lineElement);
    });
    
    // Прокрутка к началу вывода
    output.scrollTop = 0;
}

// Функция для получения чистого текста (без тегов)
function getPlainText() {
    return hiddenCode.value || codeEditor.textContent || codeEditor.innerText;
}

// Функция для подсветки ключевых слов
function highlightKeywords() {
    const editor = codeEditor;
    let text = editor.textContent || editor.innerText;
    
    // Сохраняем чистый текст в скрытый textarea
    hiddenCode.value = text;
    
    // Подсвечиваем ключевые слова
    let highlightedHTML = text;
    
    KEYWORDS.forEach(keyword => {
        const regex = new RegExp(`\\b${keyword}\\b`, 'g');
        highlightedHTML = highlightedHTML.replace(regex, `<span class="keyword-highlight">${keyword}</span>`);
    });
    
    // Обновляем HTML редактора
    editor.innerHTML = highlightedHTML;
    
    // Восстанавливаем курсор в конец
    restoreCursor();
}

// Функция для восстановления позиции курсора
function restoreCursor() {
    const selection = window.getSelection();
    const range = document.createRange();
    range.selectNodeContents(codeEditor);
    range.collapse(false); // false = в конец
    selection.removeAllRanges();
    selection.addRange(range);
}

// Обработчик ввода текста
function handleInput() {
    // Сохраняем чистый текст в скрытый textarea
    hiddenCode.value = codeEditor.textContent || codeEditor.innerText;
    
    // Подсвечиваем ключевые слова с небольшой задержкой
    setTimeout(highlightKeywords, 10);
    
    updateLineNumbers();
    syncScroll();
}

// Функция для проверки доступности бэкенда
async function checkBackend() {
    try {
        const response = await fetch(`${API_URL}/health`);
        const data = await response.json();
        
        if (data.translator_available) {
            updateOutput(['[ИНФО] Бэкенд и транслятор доступны'], 'success');
            return true;
        } else {
            updateOutput([
                '[ПРЕДУПРЕЖДЕНИЕ] Бэкенд доступен, но транслятор не найден',
                'Пожалуйста, убедитесь что IAS-2025.exe находится в правильной директории',
                'Будет использован демо-режим'
            ], 'error');
            return false;
        }
    } catch (error) {
        updateOutput([
            '[ПРЕДУПРЕЖДЕНИЕ] Бэкенд недоступен',
            'Используется демо-режим',
            `Ошибка: ${error.message}`
        ], 'error');
        return false;
    }
}

// Функция транслирования кода в ASM (реальная версия)
async function compileCode() {
    // Берем чистый текст из скрытого textarea
    const code = hiddenCode.value.trim();
    
    if (!code) {
        updateOutput(["[ОШИБКА] Введите код для трансляции"], 'error');
        return;
    }
    
    // Показать загрузку
    loader.style.display = 'block';
    statusText.textContent = 'Трансляция в ASM...';
    compileBtn.disabled = true;
    copyAsmBtn.disabled = true;
    downloadBtn.disabled = true;
    
    try {
        // Проверяем доступность бэкенда
        const isBackendAvailable = await checkBackend();
        
        if (!isBackendAvailable) {
            // Используем демо-режим
            await compileDemo(code);
            
            // Показываем уведомление об успешной трансляции
            showNotification('Трансляция завершена успешно!');
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
            updateOutput(data.output.split('\n'), 'success');
            statusText.textContent = 'Трансляция завершена';
            
            // Показываем уведомление об успешной трансляции
            showNotification('Код успешно транслирован в ASM');
        } else {
            updateOutput([
                "[ОШИБКА] Ошибка трансляции",
                data.message,
                "",
                "Детали:",
                ...data.output.split('\n')
            ], 'error');
            statusText.textContent = 'Ошибка трансляции';
            // Не показываем уведомление при ошибке - информация только в выводе
        }
        
    } catch (error) {
        updateOutput([
            "[ОШИБКА] Не удалось выполнить трансляцию",
            `Ошибка сети: ${error.message}`,
            "",
            "Переключаюсь в демо-режим..."
        ], 'error');
        
        // Используем демо-режим при ошибке
        await compileDemo(code);
        
        // Показываем уведомление об успешной трансляции (демо-режим)
        showNotification('Трансляция завершена (демо-режим)');
        
    } finally {
        // Скрыть загрузку
        loader.style.display = 'none';
        compileBtn.disabled = false;
        copyAsmBtn.disabled = false;
        downloadBtn.disabled = false;
    }
}

// Демо-версия трансляции
async function compileDemo(code) {
    return new Promise((resolve) => {
        setTimeout(() => {
            const asmLines = [
                "; Демонстрация сгенерированного ASM кода",
                "; Этот код был бы сгенерирован транслятором",
                "",
                "section .data",
                "    msg_hello db 'Привет, мир!', 0",
                "    msg_done db 'Программа завершена', 0",
                "",
                "section .text",
                "    global _start",
                "",
                "_start:",
                "    ; Вывод приветствия",
                "    mov eax, 4      ; sys_write",
                "    mov ebx, 1      ; stdout",
                "    mov ecx, msg_hello",
                "    mov edx, 13     ; длина строки",
                "    int 0x80",
                "",
                "    ; Завершение программы",
                "    mov eax, 1      ; sys_exit",
                "    xor ebx, ebx    ; код возврата 0",
                "    int 0x80"
            ];
            
            const outputLines = [
                "[УСПЕХ] Трансляция завершена успешно! (ДЕМО-РЕЖИМ)",
                "=".repeat(50),
                "Сгенерированный ASM код:",
                ""
            ];
            
            outputLines.push(...asmLines);
            outputLines.push(
                "",
                "=".repeat(50),
                "Примечание:",
                "Это демонстрационная версия транслятора.",
                "Для реальной трансляции убедитесь что:",
                "1. Бэкенд-сервер запущен (http://localhost:8000)",
                "2. IAS-2025.exe находится в правильной директории"
            );
            
            updateOutput(outputLines, 'success');
            statusText.textContent = 'Трансляция завершена (демо)';
            
            resolve();
        }, 1000);
    });
}

// Функция копирования ASM-кода в буфер обмена с сохранением форматирования
async function copyAsmCode() {
    const outputLines = document.querySelectorAll('#output .output-line');
    
    if (!outputLines.length || output.textContent.includes('Результат появится здесь')) {
        updateOutput(["[ОШИБКА] Нет сгенерированного ASM кода для копирования"], 'error');
        return;
    }
    
    // Собираем ASM код, сохраняя исходные строки и их форматирование
    let asmCodeLines = [];
    let inAsmSection = false;
    let asmCodeFound = false;
    
    for (const lineElement of outputLines) {
        const lineText = lineElement.textContent;
        
        // Ищем начало ASM кода
        if (lineText.includes('Сгенерированный ASM код:') || 
            lineText.includes('section .data') || 
            lineText.includes('section .text') ||
            (lineText.includes(';') && !lineText.startsWith('['))) {
            inAsmSection = true;
        }
        
        if (inAsmSection) {
            // Проверяем, не достигли ли мы конца ASM секции
            if (lineText.includes('='.repeat(50)) && asmCodeLines.length > 0) {
                break;
            }
            
            // Пропускаем информационные строки
            if (lineText.startsWith('[') || 
                lineText.includes('Добро пожаловать') ||
                lineText.includes('Инструкция:')) {
                continue;
            }
            
            // Добавляем строки ASM кода
            if (lineText.trim().length > 0) {
                asmCodeLines.push(lineText);
                asmCodeFound = true;
            }
        }
    }
    
    // Если не нашли ASM секцию явно, ищем строки, похожие на ASM
    if (!asmCodeFound) {
        for (const lineElement of outputLines) {
            const lineText = lineElement.textContent;
            
            // Пропускаем информационные строки и пустые строки
            if (lineText.startsWith('[') || 
                lineText.includes('Добро пожаловать') ||
                lineText.includes('Инструкция:') ||
                lineText.trim().length === 0) {
                continue;
            }
            
            // Если строка похожа на ASM (содержит ASM инструкции или директивы)
            if (lineText.includes(';') || 
                lineText.includes('section') ||
                lineText.includes('mov') || 
                lineText.includes('db') ||
                lineText.includes(':') && !lineText.includes('//')) {
                asmCodeLines.push(lineText);
                asmCodeFound = true;
            }
        }
    }
    
    // Если все еще нет ASM кода, берем все строки, кроме информационных
    if (!asmCodeFound) {
        for (const lineElement of outputLines) {
            const lineText = lineElement.textContent;
            
            // Пропускаем информационные строки и пустые строки
            if (!lineText.startsWith('[') && 
                !lineText.includes('Добро пожаловать') &&
                !lineText.includes('Инструкция:') &&
                lineText.trim().length > 0) {
                asmCodeLines.push(lineText);
            }
        }
    }
    
    // Объединяем строки с сохранением переносов
    const asmCode = asmCodeLines.join('\n');
    
    if (!asmCode.trim()) {
        updateOutput(["[ОШИБКА] Не удалось извлечь ASM код из вывода"], 'error');
        return;
    }
    
    try {
        await navigator.clipboard.writeText(asmCode.trim());
        updateOutput(["[УСПЕХ] ASM код скопирован в буфер обмена!"], 'success');
        statusText.textContent = 'Код скопирован';
        
        // Показываем уведомление об успешном копировании
        showNotification('ASM код скопирован в буфер обмена');
        
        // Визуальная обратная связь на кнопке
        const originalText = copyAsmBtn.innerHTML;
        const originalClass = copyAsmBtn.className;
        copyAsmBtn.innerHTML = '<i class="fas fa-check"></i> Скопировано!';
        copyAsmBtn.className = 'btn btn-success';
        
        setTimeout(() => {
            copyAsmBtn.innerHTML = originalText;
            copyAsmBtn.className = originalClass;
        }, 2000);
        
    } catch (err) {
        console.error('Ошибка копирования:', err);
        updateOutput([
            "[ОШИБКА] Не удалось скопировать код в буфер обмена",
            "Выделите и скопируйте код вручную"
        ], 'error');
    }
}

// Функция загрузки ASM кода в файл
function downloadAsmCode() {
    const outputLines = document.querySelectorAll('#output .output-line');
    
    if (!outputLines.length || output.textContent.includes('Результат появится здесь')) {
        updateOutput(["[ОШИБКА] Нет сгенерированного кода для скачивания"], 'error');
        return;
    }
    
    // Собираем ASM код, сохраняя форматирование
    let asmCodeLines = [];
    let inAsmSection = false;
    let asmCodeFound = false;
    
    for (const lineElement of outputLines) {
        const lineText = lineElement.textContent;
        
        // Ищем начало ASM кода
        if (lineText.includes('Сгенерированный ASM код:') || 
            lineText.includes('section .data') || 
            lineText.includes('section .text')) {
            inAsmSection = true;
        }
        
        if (inAsmSection) {
            if (lineText.includes('='.repeat(50)) && asmCodeLines.length > 0) {
                break;
            }
            
            // Пропускаем информационные строки
            if (lineText.startsWith('[') || 
                lineText.includes('Добро пожаловать') ||
                lineText.includes('Инструкция:')) {
                continue;
            }
            
            if (lineText.trim().length > 0) {
                asmCodeLines.push(lineText);
                asmCodeFound = true;
            }
        }
    }
    
    // Если не нашли ASM секцию, ищем строки, похожие на ASM
    if (!asmCodeFound) {
        for (const lineElement of outputLines) {
            const lineText = lineElement.textContent;
            
            if (lineText.startsWith('[') || 
                lineText.includes('Добро пожаловать') ||
                lineText.includes('Инструкция:') ||
                lineText.trim().length === 0) {
                continue;
            }
            
            if (lineText.includes(';') || 
                lineText.includes('section') ||
                lineText.includes('mov') || 
                lineText.includes('db')) {
                asmCodeLines.push(lineText);
                asmCodeFound = true;
            }
        }
    }
    
    // Если все еще нет ASM кода, берем все неинформационные строки
    if (!asmCodeFound) {
        for (const lineElement of outputLines) {
            const lineText = lineElement.textContent;
            
            if (!lineText.startsWith('[') && 
                !lineText.includes('Добро пожаловать') &&
                !lineText.includes('Инструкция:') &&
                lineText.trim().length > 0) {
                asmCodeLines.push(lineText);
            }
        }
    }
    
    const asmCode = asmCodeLines.join('\n');
    
    if (!asmCode.trim()) {
        updateOutput(["[ОШИБКА] Не удалось извлечь ASM код для сохранения"], 'error');
        return;
    }
    
    const blob = new Blob([asmCode], { type: 'text/plain;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'program.asm';
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
    
    updateOutput(["[УСПЕХ] ASM код сохранен в файл program.asm"], 'success');
    statusText.textContent = 'Код скачан';
    
    // Показываем уведомление об успешном скачивании
    showNotification('ASM код сохранен в файл program.asm');
    
    // Визуальная обратная связь на кнопке
    const originalText = downloadBtn.innerHTML;
    downloadBtn.innerHTML = '<i class="fas fa-check"></i> Скачано!';
    
    setTimeout(() => {
        downloadBtn.innerHTML = originalText;
    }, 2000);
}

// Обработчики событий
compileBtn.addEventListener('click', compileCode);
copyAsmBtn.addEventListener('click', copyAsmCode);
downloadBtn.addEventListener('click', downloadAsmCode);

clearBtn.addEventListener('click', function() {
    codeEditor.innerHTML = '';
    hiddenCode.value = '';
    updateOutput(['// Редактор очищен. Введите новый код.'], 'normal');
    statusText.textContent = 'Готов к трансляции';
    updateLineNumbers();
});

// Загрузка примера кода
function loadExample(exampleId) {
    if (examples[exampleId]) {
        // Устанавливаем чистый текст в скрытый textarea
        hiddenCode.value = examples[exampleId];
        
        // Устанавливаем HTML с подсветкой в редактор
        let highlightedHTML = examples[exampleId];
        KEYWORDS.forEach(keyword => {
            const regex = new RegExp(`\\b${keyword}\\b`, 'g');
            highlightedHTML = highlightedHTML.replace(regex, `<span class="keyword-highlight">${keyword}</span>`);
        });
        codeEditor.innerHTML = highlightedHTML;
        
        updateOutput(['[ИНФО] Загружен пример: ' + exampleId, 'Отредактируйте код или нажмите "Транслировать в ASM"'], 'success');
        statusText.textContent = 'Пример загружен';
        updateLineNumbers();
    }
}

// Изменение языка
languageSelector.addEventListener('change', function() {
    const lang = this.value;
    
    if (lang === 'myLang') {
        updateOutput(['[ИНФО] Режим: Язык IAS-2025', 'Введите код на вашем языке для трансляции в Assembler'], 'success');
    } else if (lang === 'asm') {
        updateOutput(['[ИНФО] Режим: Assembler (только просмотр)', 'Трансляция доступна только из IAS-2025'], 'success');
    }
    
    statusText.textContent = 'Режим изменен';
});

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

// Обновление статуса при вводе
codeEditor.addEventListener('input', () => {
    handleInput();
    
    if (hiddenCode.value.trim()) {
        statusText.textContent = 'Код изменен';
    } else {
        statusText.textContent = 'Готов к трансляции';
    }
});

// Синхронизация прокрутки нумерации строк с редактором
codeEditor.addEventListener('scroll', syncScroll);

// Инициализация начального состояния
document.addEventListener('DOMContentLoaded', function() {
    // Инициализируем нумерацию строк для начального текста
    updateLineNumbers();
    
    // Инициализируем подсветку ключевых слов для начального текста
    setTimeout(highlightKeywords, 100);
    
    // Проверяем бэкенд при загрузке
    setTimeout(() => {
        checkBackend();
    }, 1000);
    
    updateOutput([
        "// Добро пожаловать в онлайн транслятор IAS-2025!",
        "// Этот инструмент трансформирует код с языка IAS-2025 в Assembler.",
        "",
        "Инструкция:",
        "1. Напишите код на языке IAS-2025 в левой панели",
        "2. Нажмите 'Транслировать в ASM' для получения ассемблерного кода",
        "3. Используйте 'Скопировать ASM-код' для копирования в буфер обмена",
        "4. Используйте 'Скачать ASM код' для сохранения в файл",
        "",
        "Для быстрого старта выберите один из примеров ниже.",
        "",
        "ПРИМЕЧАНИЕ:",
        "В данный момент работает демо-версия транслятора.",
        "Для подключения реального транслятора необходимо:",
        "1. Запустить бэкенд-сервер",
        "2. Разместить IAS-2025.exe в той же директории"
    ], 'success');
    document.querySelectorAll('[contenteditable="true"]').forEach(div => {
        div.addEventListener('keydown', e => {
            if (e.key === 'Enter') {
            e.preventDefault();
            document.execCommand('insertLineBreak');
            }
        });
    });
}); 