#include "stdafx.h"
#include <vector>
#include <stack>
#include <sstream>
#include "Generation.h"

using namespace std;

namespace GN
{
    void GenerationASM(std::ostream* stream, LT::LexTable& lextable, IT::IdTable& idtable)
    {
        ostream* file = stream;
        *file << BEGIN;
        *file << EXTERN;
        *file << STACK(4096);
        GenConstAndData(idtable, file);
        *file << CODE;
        GenCode(lextable, idtable, file);
        *file << END;
    }

    void GenConstAndData(IT::IdTable& idtable, ostream* file)
    {
        vector <string> result;
        vector <string> con;  con.push_back(CONST);
        vector <string> data;  data.push_back(DATA);
        data.push_back(BOOL_LITERALS);
        

        for (int i = 0; i < idtable.current_size; i++)
        {
            string str = "\t" + idtable.table[i]->FullName;

            if (idtable.table[i]->idtype == IT::IDTYPE::L)  // литерал - в .const
            {
                switch (idtable.table[i]->iddatatype)
                {
                case IT::IDDATATYPE::INT:
                    str += " sdword " + itoS(idtable.table[i]->value.vint);
                    break;
                case IT::IDDATATYPE::CHAR:
                    // Для CHAR литералов: byte 'символ', 0
                    str += " byte '" + string(1, (char)idtable.table[i]->value.vint) + "', 0";
                    break;
                case IT::IDDATATYPE::BOOL:    // ДОБАВЛЕНО ДЛЯ BOOL
                    str += " byte " + string(idtable.table[i]->value.vbool ? "1" : "0");
                    break;
                case IT::IDDATATYPE::STR:
                    str += " byte " + string(idtable.table[i]->value.vstr.str) + ", 0";
                    break;
                }
                con.push_back(str);
            }
            else if (idtable.table[i]->idtype == IT::IDTYPE::V) // переменная - в .data
            {
                switch (idtable.table[i]->iddatatype)
                {
                case IT::IDDATATYPE::INT:
                    str += " sdword 0";  // 4 байта со знаком
                    break;
                case IT::IDDATATYPE::CHAR:
                    str += " byte 0";  // 1 байт для символа
                    break;
                case IT::IDDATATYPE::BOOL:    // ДОБАВЛЕНО ДЛЯ BOOL
                    str += " byte 0";  // 1 байт для bool (0=false, 1=true)
                    break;
                case IT::IDDATATYPE::STR:
                    str += " dword ?";  // 4 байта (указатель на строку)
                    break;
                }
                data.push_back(str);
            }
        }
        result.insert(result.end(), con.begin(), con.end());
        result.insert(result.end(), data.begin(), data.end());
        for (auto r : result)
            *file << r << endl;
    }

    void GenCode(LT::LexTable& lextable, IT::IdTable& idtable, ostream* file)
    {
        string str;
        string funcName;    // имя текущей функции
        int branchingnNum = -1, open = 0;
        stack<A> kol;
        str = "jmp skip_error_handler\n";
        str += "errorExit:\n";
        str += "push offset nulError\n";
        str += "call write_str\n";
        str += "push 1\n";
        str += "call ExitProcess\n";
        str += "skip_error_handler:\n\n";

        *file << str << endl;  // Сразу выводим обработчик ошибок
        str.clear();


        for (int i = 0; i < lextable.current_size; i++)
        {
            switch (LT_ENTRY(i)->lexema)
            {
            case LEX_MAIN:  
            {
                str = SEPSTR("MAIN") + "main PROC\n";
                break;
            }
            case LEX_FUNCTION:
            {
                funcName = IT_ENTRY(i + 1)->FullName;
                str = GenFunctionCode(lextable, idtable, i);
                break;
            }
            case LEX_RETURN:
            {
                str = GenExitCode(lextable, idtable, i, funcName);
                break;
            }
            case LEX_ID: // вызов функции
            {
                if (LT_ENTRY(i + 1)->lexema == LEX_LEFTHESIS && LT_ENTRY(i + 1)->lexema != LEX_FUNCTION) // не объявление, а вызов
                    str = GenCallFuncCode(lextable, idtable, i);
                break;
            }
            case LEX_IF: // условие
            {
                branchingnNum++;
                str = GenBranchingCode(lextable, idtable, i, branchingnNum);
                break;
            }
            case LEX_LEFTBRACE:
            {
                open++;
                break;
            }
            case LEX_RIGHTBRACE:    // переход на метку в конце кондишна
            {
                open--;
                if (LT_ENTRY(i + 1)->lexema == LEX_ELSE)
                {
                    kol.pop();
                    kol.push(A(open, branchingnNum, IfEnum::thenOrElse));
                    str += "jmp next" + itoS(kol.top().branchingnNum) + '\n';
                }
                else
                    if (!kol.empty())
                        if (kol.top().openRightbrace == open)
                        {
                            if (kol.top().ifEnum == IfEnum::repeat)
                            {
                                str += "jmp cyclenext" + itoS(kol.top().branchingnNum) + '\n';
                                str += "cycle" + itoS(kol.top().branchingnNum) + ":\n";
                                kol.pop();
                            }
                            else if (kol.top().ifEnum == IfEnum::repeatLiteral)
                            {
                                // Для repeat(N) — декрементируем EBX и прыгаем, если > 0, затем восстанавливаем EBX
                                str += "dec ebx\n";
                                str += "cmp ebx, 0\n";
                                str += "jg cyclenext" + itoS(kol.top().branchingnNum) + "\n";
                                str += "pop ebx\n";
                                str += "cycle" + itoS(kol.top().branchingnNum) + ":\n";
                                kol.pop();
                            }
                            else
                            {
                                str += "next" + itoS(kol.top().branchingnNum) + ":\n";
                                kol.pop();
                            }
                        }
                break;
            }

            case LEX_THEN: // условие верно (метка)
            {
                kol.push(A(open, branchingnNum, IfEnum::thenOrElse));
                str += "true" + itoS(branchingnNum) + ":";
                break;
            }
            case LEX_ELSE: // условие неверно(метка)
            {
                str += "false" + itoS(branchingnNum) + ":";
                break;
            }
            case LEX_REPEAT: // цикл с условием (метка)
            {
                branchingnNum++;

                // Проверяем, является ли внутри скобок одиночный литерал/идентификатор: repeat(10) или repeat(n)
                // Ожидаем структуру: LEX_REPEAT, LEX_LEFTHESIS, <operand>, LEX_RIGHTBRHESIS ...
                if (LT_ENTRY(i + 1)->lexema == LEX_LEFTHESIS &&
                    (LT_ENTRY(i + 2)->lexema == LEX_LITERAL || LT_ENTRY(i + 2)->lexema == LEX_ID) &&
                    LT_ENTRY(i + 3)->lexema == LEX_RIGHTHESIS)
                {
                    // фиксированное число повторений или переменная-количество
                    int repeatCount = 0;
                    bool isLiteral = (LT_ENTRY(i + 2)->lexema == LEX_LITERAL);
                    IT::Entry* cntEntry = IT_ENTRY(i + 2);

                    kol.push(A(open, branchingnNum, IfEnum::repeatLiteral, 0));

                    // Сохраняем EBX, загружаем счётчик в EBX (callee-saved регистр)
                    str += "push ebx\n";
                    if (isLiteral)
                    {
                        // литерал: используем непосредственное значение
                        repeatCount = cntEntry->value.vint;
                        str += "mov ebx, " + itoS(repeatCount) + "\n";
                    }
                    else
                    {
                        // идентификатор: загружаем значение переменной в EBX
                        str += "mov ebx, " + string(cntEntry->FullName) + "\n";
                    }

                    // метка начала цикла
                    str += "cyclenext" + itoS(kol.top().branchingnNum) + ":\n";

                    // Пропускаем токен с числом/идом и закрывающую скобку, чтобы парсер дальше корректно шел
                    // (не меняем i здесь — GenBranchingCode не вызываем)
                }
                else
                {
                    // Обычный repeat с условием (существующая логика)
                    kol.push(A(open, branchingnNum, IfEnum::repeat));
                    str += "cyclenext" + itoS(kol.top().branchingnNum) + ":\n";
                    str += GenBranchingCode(lextable, idtable, i, kol.top().branchingnNum);
                }

                break;
            }

            case LEX_EQUAL: // присваивание (вычисление выражений)
            {
                PN::polishNotation(i, lextable, idtable);
                str = GenEqualCode(lextable, idtable, i);
                break;
            }
            case LEX_WRITE: // вывод
            {
                IT::Entry* e = IT_ENTRY(i + 1);
                switch (e->iddatatype)
                {
                case IT::IDDATATYPE::INT:
                    str += "push " + string(e->FullName) + "\ncall write_int\n";
                    break;
                case IT::IDDATATYPE::CHAR:
                    // Для вывода символа
                    if (e->idtype == IT::IDTYPE::L) {
                        // Если это литерал, используем его адрес
                        str += "push offset " + string(e->FullName) + "\ncall write_str\n";
                    }
                    else {
                        // Если это переменная, создаем временную строку
                        str += "; Создаем временную строку для вывода символа\n";
                        str += "mov al, " + string(e->FullName) + "\n";
                        str += "mov byte ptr [ebp-4], al\n";  // сохраняем в локальную переменную
                        str += "mov byte ptr [ebp-3], 0\n";   // нуль-терминатор
                        str += "lea eax, [ebp-4]\n";          // адрес строки
                        str += "push eax\n";
                        str += "call write_str\n";
                    }
                    break;
                case IT::IDDATATYPE::BOOL:    // ДОБАВЛЕНО ДЛЯ BOOL
                    // Для bool выводим "true" или "false"
                    if (e->idtype == IT::IDTYPE::L) {
                        // Литерал - используем предопределенные строки
                        if (e->value.vbool) {
                            str += "push offset _true_str\n";
                        }
                        else {
                            str += "push offset _false_str\n";
                        }
                    }
                    else {
                        // Переменная - проверяем значение
                        str += "mov al, " + string(e->FullName) + "\n";
                        str += "cmp al, 0\n";
                        str += "je print_false_" + itoS(i) + "\n";
                        str += "push offset _true_str\n";
                        str += "jmp print_done_" + itoS(i) + "\n";
                        str += "print_false_" + itoS(i) + ":\n";
                        str += "push offset _false_str\n";
                        str += "print_done_" + itoS(i) + ":\n";
                    }
                    str += "call write_str\n";
                    break;
                case IT::IDDATATYPE::STR:
                    if (e->idtype == IT::IDTYPE::L)
                        str += "\npush offset " + string(e->FullName) + "\ncall write_str\n";
                    else
                        str += "push " + string(e->FullName) + "\ncall write_str\n";
                    break;
                }
                break;
            }

            }

            if (!str.empty())
            {
                *file << str << endl;
                str.clear();
            }
        }
    }

    string GenEqualCode(LT::LexTable& lextable, IT::IdTable& idtable, int& i)
    {
        string str;
        IT::Entry* e1 = IT_ENTRY(i - 1); // левый операнд
        i++;
        switch (e1->iddatatype)
        {
        case IT::IDDATATYPE::INT:
        {
            for (; LT_ENTRY(i)->lexema != LEX_SEMICOLON; i++)
            {
                switch (LT_ENTRY(i)->lexema)
                {
                    // В GenEqualCode для INT:
                case LEX_LITERAL:
                case LEX_ID:
                {
                    if (IT_ENTRY(i)->idtype == IT::IDTYPE::F) // если в выражении вызов функции
                    {
                        str = str + GenCallFuncCode(lextable, idtable, i);
                        str = str + "push eax\n";
                        break;
                    }
                    else
                        str = str + "push " + IT_ENTRY(i)->FullName + "\n";
                    break;
                }
                case LEX_CHAR_LITERAL:
                {
                    if (IT_ENTRY(i)->idtype == IT::IDTYPE::F) // если в выражении вызов функции
                    {
                        str = str + GenCallFuncCode(lextable, idtable, i);
                        str = str + "push eax\n";
                        break;
                    }
                    else
                        str = str + "push " + IT_ENTRY(i)->FullName + "\n";
                    break;
                }
                case LEX_OPERATOR:
                    switch (LT_ENTRY(i)->sign)
                    {
                    case '+':
                        // используем ECX как временный регистр, чтобы не портить EBX (счётчик циклов)
                        str += "pop ecx\npop eax\nadd eax, ecx\npush eax\n"; break;
                    case '-':
                        str += "pop ecx\npop eax\nsub eax, ecx\n";
                        // Проверка на отрицательный результат для типа int
                        str += "cmp eax, 0\n";
                        str += "jl errorExit\n";  // если результат отрицательный, ошибка
                        str += "push eax\n";
                        break;
                    case '*':
                        str += "pop ecx\npop eax\nimul eax, ecx\npush eax\n"; break;
                    case '/':
                        str += "pop ecx\npop eax\ncmp ecx, 0\nje errorExit \ncdq\nidiv ecx\npush eax\n"; break;
                    case '%':
                        str += "pop ecx\npop eax\ncdq\nidiv ecx\npush edx\n"; break;
                    }
                    break;
                case 'b': // <<
                {
                    // Для сдвига влево (<<)
                    str += "pop ecx\n";  // количество сдвигов (правый операнд)
                    str += "pop eax\n";  // значение для сдвига (левый операнд)

                    // Сохраняем оригинальное значение
                    str += "mov edx, eax\n";

                    // Проверка: если сдвиг == 0, пропускаем
                    str += "cmp ecx, 0\n";
                    str += "je shift_done_" + itoS(i) + "\n";

                    // Проверка границ сдвига
                    str += "cmp ecx, 0\n";
                    str += "jl errorExit\n"; // сдвиг < 0 - ошибка
                    str += "cmp ecx, 31\n";
                    str += "jg errorExit\n"; // сдвиг > 31 - ошибка

                    // Проверка: если исходное число отрицательное - ошибка
                    str += "cmp edx, 0\n";
                    str += "jl errorExit\n"; // отрицательное число нельзя сдвигать влево

                    // Выполняем сдвиг
                    str += "shl eax, cl\n";

                    // Проверка: если результат стал отрицательным - переполнение
                    str += "cmp eax, 0\n";
                    str += "jl errorExit\n";

                    // Проверка знакового бита
                    str += "test eax, 80000000h\n"; // проверка 31-го бита
                    str += "jnz errorExit\n";       // если установлен - переполнение

                    str += "shift_done_" + itoS(i) + ":\n";
                    str += "push eax\n";
                    break;
                }
                case 'n': // >>
                {
                    // Для сдвига вправо (>>) - только проверка границ
                    str += "pop ecx\n";
                    str += "pop eax\n";

                    // Проверка границ сдвига
                    str += "cmp ecx, 0\n";
                    str += "jl errorExit\n";
                    str += "cmp ecx, 31\n";
                    str += "jg errorExit\n";

                    str += "sar eax, cl\n";
                    str += "push eax\n";
                    break;
                }
                }
            }

            str += "pop " + string(e1->FullName) + '\n';
            break;
        }

        case IT::IDDATATYPE::CHAR:
        {
            // Для CHAR: простые присваивания, не поддерживаем сложные выражения
            for (; LT_ENTRY(i)->lexema != LEX_SEMICOLON; i++)
            {
                if (LT_ENTRY(i)->lexema == LEX_LITERAL ||
                    LT_ENTRY(i)->lexema == LEX_CHAR_LITERAL ||
                    LT_ENTRY(i)->lexema == LEX_ID)
                {
                    IT::Entry* e2 = IT_ENTRY(i);
                    if (e2->iddatatype == IT::IDDATATYPE::CHAR)
                    {
                        // Простое присваивание char = char
                        str += "mov al, " + string(e2->FullName) + "\n";
                        str += "mov " + string(e1->FullName) + ", al\n";
                    }
                    break; // только одно значение для CHAR
                }
            }
            break;
        }
        case IT::IDDATATYPE::BOOL:
        {
            // Для bool поддерживаем только простые присваивания и логические операции
            for (; LT_ENTRY(i)->lexema != LEX_SEMICOLON; i++)
            {
                if (LT_ENTRY(i)->lexema == LEX_BOOL_LITERAL ||
                    LT_ENTRY(i)->lexema == LEX_ID)
                {
                    IT::Entry* e2 = IT_ENTRY(i);

                    // Если правый операнд — вызов функции
                    if (e2->idtype == IT::IDTYPE::F) {
                        // Сгенерировать вызов функции (возвращаемое значение в EAX)
                        str += GenCallFuncCode(lextable, idtable, i);
                        // Сохранить младший байт результата (AL) в целевую булевую переменную
                        str += "mov " + string(e1->FullName) + ", al\n";
                        break;
                    }

                    if (e2->iddatatype == IT::IDDATATYPE::BOOL)
                    {
                        if (i + 1 < lextable.current_size &&
                            (LT_ENTRY(i + 1)->lexema == '&' ||
                                LT_ENTRY(i + 1)->lexema == '|' ||
                                LT_ENTRY(i + 1)->lexema == '^'))
                        {
                            if (e2->idtype == IT::IDTYPE::L) {
                                str += "mov al, " + string(e2->value.vbool ? "1" : "0") + "\n";
                            }
                            else {
                                str += "mov al, " + string(e2->FullName) + "\n";
                            }

                            i++;

                            if (i + 1 < lextable.current_size &&
                                (LT_ENTRY(i + 1)->lexema == LEX_BOOL_LITERAL ||
                                    LT_ENTRY(i + 1)->lexema == LEX_ID))
                            {
                                i++;
                                IT::Entry* e3 = IT_ENTRY(i);
                                if (e3->idtype == IT::IDTYPE::L) {
                                    str += "mov bl, " + string(e3->value.vbool ? "1" : "0") + "\n";
                                }
                                else {
                                    str += "mov bl, " + string(e3->FullName) + "\n";
                                }
                            }
                            str += "mov " + string(e1->FullName) + ", al\n";
                        }
                        else
                        {
                            if (e2->idtype == IT::IDTYPE::L) {
                                str += "mov al, " + string(e2->value.vbool ? "1" : "0") + "\n";
                            }
                            else {
                                str += "mov al, " + string(e2->FullName) + "\n";
                            }
                            str += "mov " + string(e1->FullName) + ", al\n";
                        }
                    }
                    break;
                }
                else if (LT_ENTRY(i)->lexema == LEX_OPERATOR)
                {
                    continue;
                }
            }
            break;
        }

        case IT::IDDATATYPE::STR:
        {
            char lex = LT_ENTRY(i)->lexema;
            IT::Entry* e2 = IT_ENTRY(i);
            if (lex == LEX_ID && (e2->idtype == IT::IDTYPE::F)) // вызов функции
            {
                str += GenCallFuncCode(lextable, idtable, i);
                str += "mov " + string(e1->FullName) + ", eax";
            }
            else if (lex == LEX_LITERAL) // литерал
            {
                str = +"mov " + string(e1->FullName) + ", offset " + string(e2->FullName);
            }
            else // ид(переменная) - через регистр
            {
                str += "mov ecx, " + string(e2->FullName) + "\nmov " + string(e1->FullName) + ", ecx";
            }
        }
        }
        return str;
    }


    string GenFunctionCode(LT::LexTable& lextable, IT::IdTable& idtable, int& i)
    {
        string str = "";

        str += SEPSTR(IT_ENTRY(i + 1)->FullName) + string(IT_ENTRY(i + 1)->FullName) + string(" PROC,\t");
        //дальше параметры
        i += 3; // начало - то что сразу после открывающей скобки

        while (LT_ENTRY(i)->lexema != LEX_RIGHTHESIS) // пока параметры не кончатся
        {
            if (LT_ENTRY(i)->lexema == LEX_ID) // параметр
                str += string(IT_ENTRY(i)->FullName) + (IT_ENTRY(i)->iddatatype == IT::IDDATATYPE::INT ? " : sdword, " : " : dword, ");
            i++;
        }
        int f = str.rfind(',');
        if (f > 0)
            str[f] = ' ';

        str += "\n; --- сохранить регистры ---\npush ebx\npush edx\n; ----------------------";

        return str;
    }

    string GenExitCode(LT::LexTable& lextable, IT::IdTable& idtable, int& i, string funcname)
    {
        string str = "; --- восстановить регистры ---\npop edx\npop ebx\n; -------------------------\n";
        if (LT_ENTRY(i + 1)->lexema != LEX_SEMICOLON)   // выход из функции (вернуть значение)
        {
            IT::Entry* retEntry = IT_ENTRY(i + 1);
            if (retEntry->iddatatype == IT::IDDATATYPE::BOOL) {
                // Для bool: загрузить байт в AL и расширить в EAX
                str += "mov al, " + string(retEntry->FullName) + "\n";
                str += "movzx eax, al\n";
            }
            else {
                // Для остальных типов (int, str и т.д.)
                str += "mov eax, " + string(retEntry->FullName) + "\n";
            }
        }
        str += "ret\n";
        str += funcname + " ENDP" + SEPSTREMP;
        return str;
    }

    string GenCallFuncCode(LT::LexTable& lextable, IT::IdTable& idtable, int& i)
    {
        string str;
        IT::Entry* e = IT_ENTRY(i); // идентификатор вызываемой функции
        vector<IT::Entry*> args;    // параметры в прямом порядке

        // Собираем параметры в прямом порядке
        for (i++; LT_ENTRY(i)->lexema != '@'; i++)
        {
            if (LT_ENTRY(i)->lexema == LEX_ID ||
                LT_ENTRY(i)->lexema == LEX_LITERAL ||
                LT_ENTRY(i)->lexema == LEX_CHAR_LITERAL ||
                LT_ENTRY(i)->lexema == LEX_BOOL_LITERAL)
                args.push_back(IT_ENTRY(i));
        }

        // Для stdcall: параметры передаются справа налево
        // Значит нужно передать в обратном порядке
        for (int j = args.size() - 1; j >= 0; j--)
        {
            IT::Entry* arg = args[j];
            if (arg->idtype == IT::IDTYPE::L && arg->iddatatype == IT::IDDATATYPE::STR)
                str += "push offset " + string(arg->FullName) + "\n";
            else if (arg->iddatatype == IT::IDDATATYPE::CHAR)
            {
                str += "movzx eax, byte ptr " + string(arg->FullName) + "\n";
                str += "push eax\n";
            }
            else
                str += "push " + string(arg->FullName) + "\n";
        }

        str += "call " + string(e->FullName) + '\n';
        i++;
        return str;
    }

    string GenBranchingCode(LT::LexTable& lextable, IT::IdTable& idtable, int& i, int branchingnNum)
    {
        string str;
        IT::Entry* lft = IT_ENTRY(i + 2); // левый операнд
        IT::Entry* rgt = IT_ENTRY(i + 4); // правый операнд
        bool f = false, t = false;
        string fstr, tstr;
        if (lft->iddatatype == IT::IDDATATYPE::BOOL)
        {
            // Для bool используем 8-битные регистры
            // Если это литерал, берем его значение
            if (lft->idtype == IT::IDTYPE::L) {
                str += "mov al, " + string(lft->value.vbool ? "1" : "0") + "\n";
            }
            else {
                str += "mov al, " + lft->FullName + "\n";
            }

            if (rgt->idtype == IT::IDTYPE::L) {
                str += "mov bl, " + string(rgt->value.vbool ? "1" : "0") + "\n";
            }
            else {
                str += "mov bl, " + rgt->FullName + "\n";
            }
            str += "cmp al, bl\n";
        }
        else
        {
            str += "mov edx, " + lft->FullName + "\ncmp edx, " + rgt->FullName + "\n";
        }

        switch (LT_ENTRY(i + 3)->lexema)
        {//метки переходов в процессе сравнения
        case LEX_MORE:  tstr = "jg";  fstr = "jl";  break; //JG - если первый операнд больше второго          jl - если первый операнд меньше второго
        case LEX_LESS:   tstr = "jl";  fstr = "jg";  break;
        case LEX_COMPARE: tstr = "jz";  fstr = "jnz";  break; //jz - переход, если равно        jnz - переход, если не равно
        case LEX_NOTEQUALS:   tstr = "jnz";  fstr = "jz";  break;
        }

        if (LT_ENTRY(i)->lexema != LEX_REPEAT)
        {
            for (int j = i + 6; LT_ENTRY(j - 2)->lexema != LEX_RIGHTBRACE; j++) // пропустили условие
            {
                if (LT_ENTRY(j)->lexema == LEX_THEN)
                    t = true;
                if (LT_ENTRY(j)->lexema == LEX_ELSE)
                    f = true;
            }
            if (t) str += "\n" + tstr + " true" + itoS(branchingnNum);
            if (f) str += "\n" + fstr + " false" + itoS(branchingnNum);
            if (!t || !f)  str = str + "\njmp next" + itoS(branchingnNum);
        }
        else
        {
            str += fstr + " cycle" + itoS(branchingnNum);
        }

        return str;
    }

    string itoS(int x) //чтобы избежать дублирования меток
    {
        stringstream r;  r << x;  return r.str();
    }
}