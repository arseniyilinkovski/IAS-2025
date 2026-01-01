/* app.js
   Простой фронтенд-редактор с подсветкой синтаксиса, специальной для языка из контрольного примера.
   Поддерживает: выделение ключевых слов, типов, встроенных функций, чисел (b.., h.., o..), строк, символов и комментариев.
   Интеграция: POST /api/compile и POST /api/execute (в вашем бекенде).
*/

(function () {
  // DOM
  const editor = document.getElementById('editor');
  const highlightCode = document.getElementById('highlight-code');
  const highlightPre = document.getElementById('highlight');
  const output = document.getElementById('output');
  const asmOutput = document.getElementById('asmOutput');
  const asmPanel = document.getElementById('asmPanel');
  const btnCompile = document.getElementById('btn-compile');
  const btnExecute = document.getElementById('btn-execute');
  const btnClear = document.getElementById('btn-clear');

  // --- Синтаксис / правила подсветки ---
  // Правила сделаны по образцу "Контрольный пример" и учитывают такие конструкции как:
  // int, bool, str, char, function, main, var, param, return, if, then, else, repeat, output, true/false
  // а также числовые литералы в формате: b1010 (binary), h32 (hex), o12 (octal), обычные цифры
  // встроенные функции/имена: get_sum, IsStringsEquals, IsEven, random, asciiCode, copy, powNumber, factorialOfNumber, getLocalTimeAndDate, lenght
  // комментарий начинается с # (до конца строки)
  // этот набор правил базируется на контрольном примере. :contentReference[oaicite:1]{index=1}

  const keywords = /\b(?:int|bool|str|char|function|main|return|if|then|else|repeat|output|var|param|true|false|main)\b/;
  const types = /\b(?:int|bool|str|char)\b/;
  const builtins = /\b(?:get_sum|IsStringsEquals|IsEven|getLocalTimeAndDate|random|asciiCode|copy|powNumber|factorialOfNumber|lenght)\b/;
  const commentRE = /#.*$/m;
  const stringRE = /"(?:[^"\\]|\\.)*"|'(?:[^'\\]|\\.)*'/;
  const numberRE = /\b(?:b[01]+|h[0-9A-Fa-f]+|o[0-7]+|\d+)\b/;
  const opRE = /^(>>|<<|==|!=|<=|>=|=>|->|[%+\-*/%=<>])/;
  const punctRE = /^[{}()\[\];,]/;

  // master tokenizer: matches quotes, comments, identifiers/numbers/operators/punctuation/whitespace/any
  const masterRE = new RegExp(
    [
      // string (double or single)
      /"(?:[^"\\]|\\.)*"|'(?:[^'\\]|\\.)*'/.source,
      // comment
      /#.*(?:\n|$)/.source,
      // binary/hex/oct/decimal numbers and words (identifiers)
      /\b(?:b[01]+|h[0-9A-Fa-f]+|o[0-7]+|\d+)\b/.source,
      // words (identifiers or keywords)
      /\b[A-Za-z_][A-Za-z0-9_]*\b/.source,
      // operators >>, << etc
      />>|<<?|==|!=|<=|>=|=>|->|[%+\-*/%=<>]/.source,
      // punctuation
      /[{}()\[\];,]/.source,
      // whitespace
      /\s+/.source,
      // any single char (fallback)
      /./.source
    ].join('|'),
    'g'
  );

  // escape HTML
  function escHtml(s) {
    return s.replace(/[&<>]/g, ch => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;' }[ch]));
  }

  // highlight one input string -> html with span.token.CLASS wrappers
  function highlight(code) {
    const parts = [];
    let m;
    masterRE.lastIndex = 0;
    while ((m = masterRE.exec(code)) !== null) {
      const tok = m[0];
      // order of tests matters (comments & strings should go first)
      if (tok.startsWith('#')) {
        parts.push(`<span class="token comment">${escHtml(tok)}</span>`);
        continue;
      }
      if (tok[0] === '"' || tok[0] === "'") {
        const cls = tok.length === 3 && tok[0] === "'" ? 'char' : 'string';
        parts.push(`<span class="token ${cls}">${escHtml(tok)}</span>`);
        continue;
      }
      if (numberRE.test(tok)) {
        // differentiate b/h/o prefixes
        if (/^b[01]+$/i.test(tok) || /^h[0-9A-Fa-f]+$/i.test(tok) || /^o[0-7]+$/i.test(tok)) {
          parts.push(`<span class="token number hex">${escHtml(tok)}</span>`);
        } else {
          parts.push(`<span class="token number">${escHtml(tok)}</span>`);
        }
        continue;
      }
      if (keywords.test(tok)) {
        parts.push(`<span class="token kw">${escHtml(tok)}</span>`);
        continue;
      }
      if (builtins.test(tok)) {
        parts.push(`<span class="token builtin">${escHtml(tok)}</span>`);
        continue;
      }
      if (punctRE.test(tok)) {
        parts.push(`<span class="token punct">${escHtml(tok)}</span>`);
        continue;
      }
      if (opRE.test(tok)) {
        parts.push(`<span class="token op">${escHtml(tok)}</span>`);
        continue;
      }
      // fallback: identifier / whitespace / other
      parts.push(escHtml(tok));
    }
    return parts.join('');
  }

  // sync scrolling between textarea and pre
  function syncScroll() {
    highlightPre.scrollTop = editor.scrollTop;
    highlightPre.scrollLeft = editor.scrollLeft;
  }

  // update highlighted view
  function updateHighlight() {
    const code = editor.value;
    const html = highlight(code);
    // Keep trailing newline visible by adding a zero-width space at the end if needed
    highlightCode.innerHTML = html + (code.endsWith('\n') ? '\n' : '');
  }

  // init
  editor.addEventListener('input', () => {
    updateHighlight();
  });
  editor.addEventListener('scroll', syncScroll);

  // keyboard shortcuts
  editor.addEventListener('keydown', (e) => {
    // Ctrl+Enter = execute
    if ((e.ctrlKey || e.metaKey) && e.key === 'Enter') {
      e.preventDefault();
      doExecute();
      return;
    }
    // F9 = compile
    if (e.key === 'F9') {
      e.preventDefault();
      doCompile();
      return;
    }
    // Tab -> insert 2 spaces
    if (e.key === 'Tab') {
      e.preventDefault();
      const start = editor.selectionStart;
      const end = editor.selectionEnd;
      const val = editor.value;
      editor.value = val.slice(0, start) + '  ' + val.slice(end);
      editor.selectionStart = editor.selectionEnd = start + 2;
      updateHighlight();
    }
  });

  // UI buttons
  btnCompile.addEventListener('click', doCompile);
  btnExecute.addEventListener('click', doExecute);
  btnClear.addEventListener('click', () => {
    editor.value = '';
    updateHighlight();
    asmOutput.textContent = '';
    asmPanel.open = false;
    output.innerHTML = '<div class="console-empty">Нажмите <strong>Compile</strong> или <strong>Execute</strong>, чтобы увидеть результат.</div>';
  });

  // show status in console
  function showConsoleText(text, isError = false) {
    const node = document.createElement('pre');
    node.style.whiteSpace = 'pre-wrap';
    node.style.margin = '0';
    node.textContent = text;
    if (isError) node.style.color = '#ffd2d2';
    output.innerHTML = '';
    output.appendChild(node);
    output.scrollTop = output.scrollHeight;
  }

  // POST /api/compile
  async function doCompile() {
    const code = editor.value || '';
    if (!code.trim()) {
      showConsoleText('Код пуст. Вставьте код и попробуйте снова.', true);
      return;
    }
    showConsoleText('Отправка на /api/compile... Пожалуйста, подождите.');
    try {
      const resp = await fetch('/api/compile', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ code, language: 'myLang' })
      });

      const json = await resp.json();
      if (!resp.ok) {
        const message = (json && (json.detail || json.message)) || `HTTP ${resp.status}`;
        showConsoleText(`[ОШИБОЧНЫЙ ОТВЕТ] ${message}`, true);
        return;
      }

      if (json.success) {
        // выводим полный output либо asm
        showConsoleText(json.output || 'Трансляция завершена. Нет вывода.');
        asmOutput.textContent = json.asm_code || '';
        if (json.asm_code) asmPanel.open = true;
      } else {
        showConsoleText(`[Ошибка трансляции] ${json.message || 'Неизвестная ошибка'}`, true);
        asmOutput.textContent = json.asm_code || '';
        asmPanel.open = !!json.asm_code;
      }
    } catch (err) {
      showConsoleText(`[Сетевая ошибка] ${err.message}`, true);
    }
  }

  // POST /api/execute
  async function doExecute() {
    const code = editor.value || '';
    if (!code.trim()) {
      showConsoleText('Код пуст. Вставьте код и попробуйте снова.', true);
      return;
    }
    showConsoleText('Отправка на /api/execute... Пожалуйста, подождите.');
    try {
      const resp = await fetch('/api/execute', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ code })
      });

      const json = await resp.json();
      if (!resp.ok) {
        const message = (json && (json.detail || json.message)) || `HTTP ${resp.status}`;
        showConsoleText(`[ОШИБОЧНЫЙ ОТВЕТ] ${message}`, true);
        return;
      }

      if (json.success) {
        showConsoleText(json.output || 'Выполнение завершено. Нет вывода.');
      } else {
        showConsoleText(`[Ошибка выполнения] ${json.message || 'Неизвестная ошибка'}`, true);
      }
    } catch (err) {
      showConsoleText(`[Сетевая ошибка] ${err.message}`, true);
    }
  }

  // initial render
  updateHighlight();

  // expose for debugging (optional)
  window._iasFrontend = {
    updateHighlight, highlight, editor
  };
})();
