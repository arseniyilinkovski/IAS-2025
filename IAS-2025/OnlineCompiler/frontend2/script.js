// Конфигурация
const API_BASE_URL = 'http://localhost:8000';
const TEMPLATES = {
    hello: `main{
        output "Hello world!";
    }`,

    calc: `программа Калькулятор:
начать
    int var a = 10
    int var b = 5
    
    вывод("a = " + a)
    вывод("b = " + b)
    вывод("a + b = " + (a + b))
    вывод("a - b = " + (a - b))
    вывод("a * b = " + (a * b))
    вывод("a / b = " + (a / b))
конец`,

    loop: `программа Циклы:
начать
    вывод("Цикл for:")
    повтор (5 раз) {
        вывод("Итерация: " + (х + 1))
    }
    
    вывод("\\nЦикл while:")
    int var i = 0
    пока (i < 5) {
        вывод("i = " + i)
        i = i + 1
    }
конец`,

    func: `int function сложить(int param a, int param b)
начать
    возврат a + b
конец

bool function четное(int param число)
начать
    если (число % 2 == 0)
    тогда
        возврат истина
    иначе
        возврат ложь
    конец_если
конец

программа Функции:
начать
    int var результат = сложить(10, 20)
    вывод("10 + 20 = " + результат)
    
    если (четное(результат))
    тогда
        вывод("Результат четный")
    иначе
        вывод("Результат нечетный")
    конец_если
конец`,

    array: `программа Массивы:
начать
    int var массив[5] = {1, 2, 3, 4, 5}
    
    вывод("Элементы массива:")
    повтор (5 раз) {
        вывод("массив[" + i + "] = " + массив[i])
    }
    
    int var сумма = 0
    повтор (5 раз) {
        сумма = сумма + массив[i]
    }
    вывод("Сумма элементов: " + сумма)
конец`,

    full: `int function get_sum(int param argOne, int param argTwo){
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
}`
};

// Глобальные переменные
let codeEditor, asmEditor;
let currentFileName = 'new_program.ias';
let autoSaveInterval;
let compileStartTime;

// Инициализация
document.addEventListener('DOMContentLoaded', function() {
    // Инициализация загрузки
    simulateLoading();
    
    // Инициализация редакторов
    setTimeout(() => {
        initializeEditors();
        setupEventListeners();
        initializeTemplates();
        startAutoSave();
        updateServerStatus();
        updateTime();
        
        // Интервалы
        setInterval(updateTime, 1000);
        setInterval(updateServerStatus, 30000);
        
        // Показать приветственное уведомление
        setTimeout(() => {
            showNotification('IAS-2025 Компилятор готов к работе!', 'success');
        }, 1000);
    }, 2000);
});

function simulateLoading() {
    const loader = document.querySelector('.loader');
    const progress = document.querySelector('.loader-progress');
    const text = document.querySelector('.loader-text');
    const texts = [
        'Инициализация компилятора...',
        'Загрузка синтаксических правил...',
        'Подключение к серверу трансляции...',
        'Настройка интерфейса...',
        'Готово!'
    ];
    
    let progressValue = 0;
    let textIndex = 0;
    
    const interval = setInterval(() => {
        progressValue += Math.random() * 15 + 5;
        if (progressValue >= 100) {
            progressValue = 100;
            clearInterval(interval);
            
            // Переход к основному контенту
            setTimeout(() => {
                loader.style.opacity = '0';
                setTimeout(() => {
                    loader.style.display = 'none';
                }, 500);
            }, 500);
        }
        
        progress.style.width = `${progressValue}%`;
        
        // Обновление текста каждые 20%
        if (progressValue >= (textIndex + 1) * 20 && textIndex < texts.length - 1) {
            textIndex++;
            text.textContent = texts[textIndex];
        }
    }, 100);
}
// (В функции initializeEditors добавляем принудительную установку стилей)
function initializeEditors() {
    // Основной редактор кода
    codeEditor = CodeMirror.fromTextArea(document.getElementById('codeEditor'), {
        mode: 'text/x-ias',
        theme: 'red-steel',
        lineNumbers: true,
        lineWrapping: true,
        autoCloseBrackets: true,
        matchBrackets: true,
        indentUnit: 4,
        tabSize: 4,
        indentWithTabs: false,
        electricChars: true,
        styleActiveLine: true,
        showCursorWhenSelecting: true,
        extraKeys: {
            'Ctrl-Enter': compileCode,
            'Ctrl-S': saveCode,
            'Ctrl-N': newFile,
            'Ctrl-L': loadExample,
            'Ctrl-D': 'deleteLine',
            'Ctrl-/': 'toggleComment',
            'Shift-Tab': 'indentLess',
            'Tab': function(cm) {
                if (cm.somethingSelected()) {
                    cm.indentSelection('add');
                } else {
                    const spaces = Array(cm.getOption('indentUnit') + 1).join(' ');
                    cm.replaceSelection(spaces);
                }
            }
        },
        gutters: ['CodeMirror-linenumbers', 'CodeMirror-foldgutter', 'CodeMirror-lint-markers'],
        foldGutter: true,
        lint: true,
        viewportMargin: 10
    });
    
    // ПРИНУДИТЕЛЬНАЯ УСТАНОВКА СТИЛЕЙ
    setTimeout(() => {
        // Основной редактор
        const cmElement = codeEditor.getWrapperElement();
        cmElement.style.color = '#c0c0c0';
        cmElement.style.backgroundColor = '#0f0f15';
        cmElement.style.fontFamily = "'Roboto Mono', monospace";
        cmElement.style.fontSize = '14px';
        cmElement.style.lineHeight = '1.6';
        
        // Строки кода
        const lines = cmElement.querySelectorAll('.CodeMirror-line');
        lines.forEach(line => {
            line.style.color = '#c0c0c0';
        });
        
        // Номера строк
        const lineNumbers = cmElement.querySelectorAll('.CodeMirror-linenumber');
        lineNumbers.forEach(num => {
            num.style.color = '#666677';
        });
        
        console.log('Принудительные стили применены к основному редактору');
    }, 100);
    
    // Редактор ASM кода
    asmEditor = CodeMirror(function(node) {
        document.getElementById('asmOutput').appendChild(node);
    }, {
        value: '; ASM код появится здесь после трансляции\n',
        mode: 'text/x-asm-red',
        theme: 'red-steel',
        lineNumbers: true,
        lineWrapping: true,
        readOnly: true,
        viewportMargin: Infinity,
        styleActiveLine: true
    });
    
    // ПРИНУДИТЕЛЬНАЯ УСТАНОВКА СТИЛЕЙ ДЛЯ ASM РЕДАКТОРА
    setTimeout(() => {
        const asmElement = asmEditor.getWrapperElement();
        asmElement.style.color = '#c0c0c0';
        asmElement.style.backgroundColor = '#0f0f15';
        asmElement.style.fontFamily = "'Roboto Mono', monospace";
        asmElement.style.fontSize = '13px';
        
        console.log('Принудительные стили применены к ASM редактору');
    }, 100);
    
    // Обновление информации
    codeEditor.on('cursorActivity', updateCursorInfo);
    codeEditor.on('change', updateStats);
    
    // Загружаем полный пример
    loadFullExample();
    updateStats();
}

// (Добавим функцию для обновления стилей при изменении размера)
function refreshEditorStyles() {
    if (codeEditor) {
        const cmElement = codeEditor.getWrapperElement();
        cmElement.style.color = '#c0c0c0';
        cmElement.style.backgroundColor = '#0f0f15';
        
        // Обновляем все строки
        const lines = cmElement.querySelectorAll('.CodeMirror-line');
        lines.forEach(line => {
            line.style.color = '#c0c0c0';
        });
    }
    
    if (asmEditor) {
        const asmElement = asmEditor.getWrapperElement();
        asmElement.style.color = '#c0c0c0';
        asmElement.style.backgroundColor = '#0f0f15';
    }
}

// Вызываем обновление стилей при загрузке и ресайзе
window.addEventListener('load', refreshEditorStyles);
window.addEventListener('resize', refreshEditorStyles);

function setupEventListeners() {
    // Кнопки компиляции
    document.getElementById('compileBtn').addEventListener('click', compileCode);
    document.getElementById('quickCompile').addEventListener('click', compileCode);
    document.getElementById('clearBtn').addEventListener('click', clearEditor);
    
    // Файловые операции
    document.getElementById('newFile').addEventListener('click', newFile);
    document.getElementById('saveFile').addEventListener('click', saveCode);
    
    // Шаблоны
    document.getElementById('loadTemplate').addEventListener('click', loadSelectedTemplate);
    document.querySelectorAll('.template-card').forEach(card => {
        card.addEventListener('click', () => {
            const template = card.dataset.template;
            loadTemplate(template);
        });
    });
    
    // Результат
    document.getElementById('copyAsm').addEventListener('click', copyAsmCode);
    document.getElementById('executeAsm').addEventListener('click', executeAsmCode);
    document.getElementById('clearResult').addEventListener('click', clearResult);
    
    // Вкладки
    document.querySelectorAll('.tab-btn').forEach(tab => {
        tab.addEventListener('click', () => {
            const tabId = tab.dataset.tab;
            switchTab(tabId);
        });
    });
    
    // Примеры
    document.getElementById('quickExample').addEventListener('click', loadExample);
    
    // Настройки
    document.getElementById('quickSettings').addEventListener('click', showSettings);
    document.getElementById('quickHelp').addEventListener('click', showHelp);
    
    document.getElementById('saveSettings').addEventListener('click', saveSettings);
    document.getElementById('cancelSettings').addEventListener('click', hideSettings);
    document.querySelectorAll('.modal-close').forEach(btn => {
        btn.addEventListener('click', hideSettings);
    });
    
    // Уведомления
    document.querySelector('.notification-close').addEventListener('click', hideNotification);
    
    // Горячие клавиши
    document.addEventListener('keydown', handleHotkeys);
}

function initializeTemplates() {
    const select = document.getElementById('templateSelect');
    select.addEventListener('change', function() {
        if (this.value) {
            loadTemplate(this.value);
            this.value = '';
        }
    });
}

function startAutoSave() {
    const autoSaveCheckbox = document.getElementById('autoSave');
    if (autoSaveCheckbox && autoSaveCheckbox.checked) {
        autoSaveInterval = setInterval(() => {
            const code = codeEditor.getValue();
            if (code.trim()) {
                localStorage.setItem('ias_autosave', code);
            }
        }, 30000);
    }
    
    // Восстановление автосохранения
    const savedCode = localStorage.getItem('ias_autosave');
    if (savedCode) {
        if (confirm('Найдено автосохранение. Загрузить?')) {
            codeEditor.setValue(savedCode);
            updateStats();
        }
    }
}

// Основные функции
async function compileCode() {
    const code = codeEditor.getValue();
    
    if (!code.trim()) {
        showNotification('Введите код для трансляции', 'warning');
        return;
    }
    
    // Показать индикатор
    const compileBtn = document.getElementById('compileBtn');
    const quickBtn = document.getElementById('quickCompile');
    const originalText = compileBtn.innerHTML;
    
    compileBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Трансляция...';
    quickBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i>';
    compileBtn.disabled = quickBtn.disabled = true;
    
    compileStartTime = Date.now();
    asmEditor.setValue('; Трансляция кода...\n');
    switchTab('asm');
    
    try {
        const response = await fetch(`${API_BASE_URL}/api/compile`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                code: code,
                language: 'myLang'
            })
        });
        
        const result = await response.json();
        const compileTime = Date.now() - compileStartTime;
        
        // Обновление информации об отладке
        document.getElementById('compileTime').textContent = `${compileTime} мс`;
        document.getElementById('debugStatus').textContent = result.success ? 'Успешно' : 'Ошибка';
        document.getElementById('debugStatus').className = result.success ? 'debug-value success' : 'debug-value error';
        
        if (result.success) {
            asmEditor.setValue(result.asm_code);
            document.getElementById('asmSize').textContent = `${result.asm_code.length} байт`;
            document.getElementById('errorCount').textContent = '0';
            document.getElementById('errorCount').className = 'debug-value success';
            
            showNotification('Код успешно транслирован!', 'success');
            
            // Включаем кнопки
            document.getElementById('copyAsm').disabled = false;
            document.getElementById('executeAsm').disabled = false;
        } else {
            asmEditor.setValue(result.output || result.message);
            document.getElementById('errorCount').textContent = '1+';
            document.getElementById('errorCount').className = 'debug-value error';
            
            showNotification(result.message, 'error');
        }
        
    } catch (error) {
        console.error('Ошибка при трансляции:', error);
        asmEditor.setValue(`; Ошибка соединения с сервером\n; ${error.message}`);
        showNotification('Не удалось подключиться к серверу', 'error');
    } finally {
        // Восстановление кнопок
        compileBtn.innerHTML = originalText;
        quickBtn.innerHTML = '<i class="fas fa-bolt"></i> Транслировать';
        compileBtn.disabled = quickBtn.disabled = false;
    }
}

async function executeAsmCode() {
    const asmCode = asmEditor.getValue();
    
    if (!asmCode.trim() || asmCode.includes('ASM код появится здесь')) {
        showNotification('Сначала сгенерируйте ASM код', 'warning');
        return;
    }
    
    const executeBtn = document.getElementById('executeAsm');
    executeBtn.disabled = true;
    executeBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i>';
    
    document.getElementById('programOutput').textContent = 'Выполнение программы...';
    switchTab('output');
    
    try {
        const response = await fetch(`${API_BASE_URL}/api/execute-asm`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                asm_code: asmCode
            })
        });
        
        const result = await response.json();
        
        if (result.success) {
            document.getElementById('programOutput').textContent = result.output;
            showNotification('Программа выполнена успешно!', 'success');
        } else {
            document.getElementById('programOutput').textContent = result.output || result.message;
            showNotification(result.message, 'error');
        }
        
    } catch (error) {
        console.error('Ошибка при выполнении:', error);
        document.getElementById('programOutput').textContent = `Ошибка соединения:\n${error.message}`;
        showNotification('Ошибка выполнения программы', 'error');
    } finally {
        executeBtn.disabled = false;
        executeBtn.innerHTML = '<i class="fas fa-play"></i>';
    }
}

// Шаблоны кода
function loadTemplate(templateName) {
    if (TEMPLATES[templateName]) {
        codeEditor.setValue(TEMPLATES[templateName]);
        updateStats();
        showNotification(`Загружен шаблон: ${templateName}`, 'info');
    }
}

function loadSelectedTemplate() {
    const select = document.getElementById('templateSelect');
    if (select.value) {
        loadTemplate(select.value);
        select.value = '';
    }
}

function loadExample() {
    loadTemplate('full');
}

// Файловые операции
function newFile() {
    if (codeEditor.getValue().trim() && !confirm('Текущий код будет потерян. Продолжить?')) {
        return;
    }
    
    codeEditor.setValue('');
    currentFileName = `program_${Date.now()}.ias`;
    document.getElementById('fileName').textContent = currentFileName;
    updateStats();
    showNotification('Создан новый файл', 'info');
}

function saveCode() {
    const code = codeEditor.getValue();
    if (!code.trim()) {
        showNotification('Нет кода для сохранения', 'warning');
        return;
    }
    
    const blob = new Blob([code], { type: 'text/plain' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = currentFileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
    
    showNotification('Код сохранен в файл', 'success');
}

// Вспомогательные функции
function updateCursorInfo() {
    const cursor = codeEditor.getCursor();
    document.getElementById('cursorPos').textContent = 
        `Строка ${cursor.line + 1}, Колонка ${cursor.ch + 1}`;
}

function updateStats() {
    const code = codeEditor.getValue();
    const lines = code.split('\n').length;
    const chars = code.length;
    const words = code.split(/\s+/).filter(w => w).length;
    
    document.getElementById('lineCount').textContent = `${lines} строк`;
    document.getElementById('charCount').textContent = `${chars} символов, ${words} слов`;
}

function clearEditor() {
    if (codeEditor.getValue().trim() && confirm('Очистить редактор?')) {
        codeEditor.setValue('');
        updateStats();
        showNotification('Редактор очищен', 'info');
    }
}

function clearResult() {
    asmEditor.setValue('; ASM код появится здесь после трансляции\n');
    document.getElementById('programOutput').textContent = '// Запустите программу, чтобы увидеть вывод здесь';
    showNotification('Результат очищен', 'info');
}

async function copyAsmCode() {
    const asmCode = asmEditor.getValue();
    
    if (!asmCode.trim() || asmCode.includes('ASM код появится здесь')) {
        showNotification('Нет кода для копирования', 'warning');
        return;
    }
    
    try {
        await navigator.clipboard.writeText(asmCode);
        showNotification('ASM код скопирован', 'success');
    } catch (err) {
        const textArea = document.createElement('textarea');
        textArea.value = asmCode;
        document.body.appendChild(textArea);
        textArea.select();
        document.execCommand('copy');
        document.body.removeChild(textArea);
        showNotification('Код скопирован (резервный метод)', 'success');
    }
}

function switchTab(tabId) {
    // Скрыть все вкладки
    document.querySelectorAll('.tab-pane').forEach(pane => {
        pane.classList.remove('active');
    });
    
    // Убрать активность у всех кнопок
    document.querySelectorAll('.tab-btn').forEach(btn => {
        btn.classList.remove('active');
    });
    
    // Показать выбранную вкладку
    document.getElementById(`${tabId}-tab`).classList.add('active');
    
    // Активировать кнопку
    document.querySelector(`.tab-btn[data-tab="${tabId}"]`).classList.add('active');
}

// Статус сервера
async function updateServerStatus() {
    try {
        const response = await fetch(`${API_BASE_URL}/api/health`);
        const data = await response.json();
        
        const statusElement = document.getElementById('serverStatus');
        const translatorElement = document.getElementById('translatorStatus');
        
        if (data.status === 'healthy') {
            statusElement.innerHTML = '<i class="fas fa-server"></i> <span>Сервер активен</span>';
            statusElement.style.color = '#00ff88';
            translatorElement.textContent = data.translator_available ? 'Доступен' : 'Недоступен';
            translatorElement.style.color = data.translator_available ? '#00ff88' : '#ff0033';
        } else {
            statusElement.innerHTML = '<i class="fas fa-server"></i> <span>Сервер недоступен</span>';
            statusElement.style.color = '#ff0033';
            translatorElement.textContent = 'Неизвестно';
            translatorElement.style.color = '#ffaa00';
        }
    } catch (error) {
        const statusElement = document.getElementById('serverStatus');
        statusElement.innerHTML = '<i class="fas fa-server"></i> <span>Сервер недоступен</span>';
        statusElement.style.color = '#ff0033';
    }
}

function updateTime() {
    const now = new Date();
    const timeString = now.toLocaleTimeString('ru-RU', {
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit'
    });
    document.getElementById('currentTime').textContent = timeString;
}

// Уведомления
function showNotification(message, type = 'info') {
    const notification = document.getElementById('notification');
    const title = document.getElementById('notificationTitle');
    const text = document.getElementById('notificationText');
    const icon = notification.querySelector('.notification-icon i');
    
    // Настройка типа
    let iconClass, borderColor;
    switch(type) {
        case 'success':
            iconClass = 'fas fa-check';
            borderColor = '#00ff88';
            break;
        case 'error':
            iconClass = 'fas fa-times';
            borderColor = '#ff0033';
            break;
        case 'warning':
            iconClass = 'fas fa-exclamation-triangle';
            borderColor = '#ffaa00';
            break;
        default:
            iconClass = 'fas fa-info-circle';
            borderColor = '#3366ff';
    }
    
    // Установка значений
    icon.className = iconClass;
    notification.style.borderLeftColor = borderColor;
    notification.querySelector('.notification-icon').style.background = `${borderColor}20`;
    notification.querySelector('.notification-icon').style.color = borderColor;
    
    title.textContent = type === 'success' ? 'Успешно!' : 
                       type === 'error' ? 'Ошибка!' : 
                       type === 'warning' ? 'Внимание!' : 'Информация';
    text.textContent = message;
    
    // Показать уведомление
    notification.classList.add('show');
    
    // Автоскрытие
    setTimeout(hideNotification, 5000);
}

function hideNotification() {
    document.getElementById('notification').classList.remove('show');
}

// Настройки
function showSettings() {
    document.getElementById('settingsModal').classList.add('show');
}

function hideSettings() {
    document.getElementById('settingsModal').classList.remove('show');
}

function saveSettings() {
    const apiUrl = document.getElementById('apiUrl').value;
    const timeout = document.getElementById('timeout').value;
    const fontSize = document.getElementById('fontSize').value;
    const autoSave = document.getElementById('autoSave').checked;
    
    // Сохраняем настройки в localStorage
    localStorage.setItem('ias_settings', JSON.stringify({
        apiUrl,
        timeout,
        fontSize,
        autoSave
    }));
    
    // Применяем настройки
    codeEditor.getWrapperElement().style.fontSize = `${fontSize}px`;
    asmEditor.getWrapperElement().style.fontSize = `${fontSize}px`;
    
    // Обновляем автосохранение
    if (autoSave && !autoSaveInterval) {
        startAutoSave();
    } else if (!autoSave && autoSaveInterval) {
        clearInterval(autoSaveInterval);
        autoSaveInterval = null;
    }
    
    hideSettings();
    showNotification('Настройки сохранены', 'success');
}

function showHelp() {
    showNotification('Используйте Ctrl+Enter для быстрой трансляции. Выберите шаблон из меню.', 'info');
}

// Горячие клавиши
function handleHotkeys(e) {
    // Ctrl+Enter - быстрая трансляция
    if ((e.ctrlKey || e.metaKey) && e.key === 'Enter') {
        e.preventDefault();
        compileCode();
    }
    
    // Ctrl+S - сохранение
    if ((e.ctrlKey || e.metaKey) && e.key === 's') {
        e.preventDefault();
        saveCode();
    }
    
    // Ctrl+L - загрузка примера
    if ((e.ctrlKey || e.metaKey) && e.key === 'l') {
        e.preventDefault();
        loadExample();
    }
    
    // Ctrl+K - очистка редактора
    if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
        e.preventDefault();
        clearEditor();
    }
}