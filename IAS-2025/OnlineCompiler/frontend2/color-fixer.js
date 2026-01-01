// color-fixer.js - Гарантирует работу подсветки с серым текстом
(function() {
    'use strict';
    
    const COLOR_MAP = {
        // Основной текст
        'default': '#c0c0c0',
        
        // Подсветка (яркие цвета для контраста)
        'keyword': '#ff8a8a',
        'type': '#6bffd8',
        'builtin': '#ffffaa',
        'string': '#ffb380',
        'number': '#b5ff9d',
        'comment': '#8cd37a',
        'variable': '#a3d7ff',
        'operator': '#e6e6e6',
        'constant': '#8ab4f8',
        
        // Специальные классы IAS
        'function': '#ffffaa',
        'param': '#80ccff',
        'main': '#ff8a8a',
        'output': '#6bffd8',
        'repeat': '#ffa3ff',
        'if': '#ff8a8a',
        'then': '#6bffd8',
        'else': '#8ab4f8'
    };
    
    function applyColorFix() {
        // 1. Основной редактор
        const mainEditor = document.querySelector('#codeEditor + .CodeMirror');
        if (mainEditor) {
            fixEditorColors(mainEditor);
        }
        
        // 2. ASM редактор
        const asmEditor = document.querySelector('#asmOutput .CodeMirror');
        if (asmEditor) {
            fixEditorColors(asmEditor);
        }
    }
    
    function fixEditorColors(editorElement) {
        // Устанавливаем основной цвет
        editorElement.style.color = COLOR_MAP.default;
        editorElement.style.backgroundColor = '#0f0f15';
        
        // Обрабатываем все элементы с классами подсветки
        const elements = editorElement.querySelectorAll('[class*="cm-"]');
        elements.forEach(el => {
            const classes = el.className.split(' ');
            
            // Ищем класс подсветки
            let highlightClass = null;
            for (const cls of classes) {
                if (cls.startsWith('cm-')) {
                    // Извлекаем тип подсветки
                    const type = cls.replace('cm-', '');
                    if (COLOR_MAP[type]) {
                        highlightClass = type;
                        break;
                    }
                    
                    // Проверяем специальные случаи
                    if (type.includes('keyword')) highlightClass = 'keyword';
                    else if (type.includes('comment')) highlightClass = 'comment';
                    else if (type.includes('string')) highlightClass = 'string';
                    else if (type.includes('number')) highlightClass = 'number';
                    else if (type.includes('variable')) highlightClass = 'variable';
                    else if (type.includes('builtin')) highlightClass = 'builtin';
                    else if (type.includes('type')) highlightClass = 'type';
                }
            }
            
            // Применяем цвет подсветки
            if (highlightClass && COLOR_MAP[highlightClass]) {
                el.style.color = COLOR_MAP[highlightClass];
                
                // Добавляем эффекты для лучшей видимости
                if (highlightClass === 'keyword' || highlightClass === 'type') {
                    el.style.fontWeight = 'bold';
                }
                if (highlightClass === 'builtin' || highlightClass === 'function') {
                    el.style.fontStyle = 'italic';
                }
                if (highlightClass === 'comment') {
                    el.style.fontStyle = 'italic';
                    el.style.opacity = '0.9';
                }
            } else {
                // Для остального текста - серый
                el.style.color = COLOR_MAP.default;
            }
        });
        
        // Обрабатываем обычный текст (без классов)
        const textNodes = getTextNodes(editorElement);
        textNodes.forEach(node => {
            if (node.parentNode && !node.parentNode.className.includes('cm-')) {
                node.parentNode.style.color = COLOR_MAP.default;
            }
        });
    }
    
    function getTextNodes(element) {
        const walker = document.createTreeWalker(
            element,
            NodeFilter.SHOW_TEXT,
            null,
            false
        );
        
        const textNodes = [];
        let node;
        while (node = walker.nextNode()) {
            if (node.textContent.trim()) {
                textNodes.push(node);
            }
        }
        
        return textNodes;
    }
    
    // Применяем фикс сразу
    setTimeout(applyColorFix, 100);
    
    // Применяем при загрузке
    window.addEventListener('load', applyColorFix);
    
    // Применяем при изменении контента
    const observer = new MutationObserver(function(mutations) {
        mutations.forEach(function(mutation) {
            if (mutation.addedNodes.length || mutation.type === 'characterData') {
                setTimeout(applyColorFix, 50);
            }
        });
    });
    
    observer.observe(document.body, {
        childList: true,
        subtree: true,
        characterData: true
    });
    
    // Периодическое обновление
    setInterval(applyColorFix, 1000);
    
    // Экспортируем функцию
    window.fixEditorColors = applyColorFix;
    
    console.log('Color fixer loaded - подсветка синтаксиса с серым текстом активна');
})();