#include "stdafx.h"
#include <stack>

#include "LexAnalysis.h"
#include "Error.h"
#include "FST.h"
#include "graphs.h"

using namespace std;

void LexAnalysis(In::IN& in, LT::LexTable& lex, IT::IdTable& id)
{
	IT::IDDATATYPE	iddatatype = IT::IDDATATYPE::DEF;			// тип данных
	IT::IDTYPE	idtype = IT::IDTYPE::D;							// тип идентикатора
	IT::Entry* IT_ENTRY = NULL;

	IT::len.params = IT::Entry::Param{ 1, vector<IT::IDDATATYPE>{ IT::IDDATATYPE::STR} };
	IT::Add(id, &IT::len);

	IT::copy.params = IT::Entry::Param{ 3, vector<IT::IDDATATYPE>{IT::IDDATATYPE::STR, IT::IDDATATYPE::STR, IT::IDDATATYPE::INT} };
	IT::Add(id, &IT::copy);

	IT::time.params = IT::Entry::Param{ 0, vector<IT::IDDATATYPE>{} };
	IT::Add(id, &IT::time);

	IT::pow.params = IT::Entry::Param{ 2, vector<IT::IDDATATYPE>{IT::IDDATATYPE::INT, IT::IDDATATYPE::INT} };
	IT::Add(id, &IT::pow);

	IT::random.params = IT::Entry::Param{ 2, vector<IT::IDDATATYPE>{IT::IDDATATYPE::INT, IT::IDDATATYPE::INT} };
	IT::Add(id, &IT::random);

	IT::square.params = IT::Entry::Param{ 1, vector<IT::IDDATATYPE>{IT::IDDATATYPE::INT} };
	IT::Add(id, &IT::square);

	IT::factorial.params = IT::Entry::Param{ 1, vector<IT::IDDATATYPE>{IT::IDDATATYPE::INT} };
	IT::Add(id, &IT::factorial);

	IT::Entry* asciiCodeEntry = new IT::Entry(0, string(GLOBAL), FUNC_ASCIICODE, IT::IDDATATYPE::INT, IT::IDTYPE::F);
	asciiCodeEntry->params = IT::Entry::Param{ 1, vector<IT::IDDATATYPE>{IT::IDDATATYPE::CHAR} };
	IT::Add(id, asciiCodeEntry);
	

	char lexema[TI_STR_MAXSIZE];
	int currentRow = 1, currentLex = 0, indexIdTable = 8;
	bool lexID = true, lexInt = false, lexComment = false; int main = 0;
	stack<string> areaOfVisibility;
	areaOfVisibility.push(GLOBAL);

	while (FindLexema(in, lexema))
	{
		currentLex++;
		lexID = true; lexInt = false;
		if (isdigit((unsigned char)lexema[0])) {
			bool hasAlpha = false;
			for (int p = 1; lexema[p] != '\0'; ++p) {
				if (isalpha((unsigned char)lexema[p])) { hasAlpha = true; break; }
			}
			if (hasAlpha) {
				std::cerr << "Throwing error 720 at row=" << currentRow << " col=" << currentLex << " lexema=" << lexema << std::endl;
				// бросаем ошибку: не число (код 720), передаём лексему
				throw ERROR_THROW_IN_WORD(720, currentRow, currentLex, lexema);
			}
		}

		switch (*lexema)
		{
		case LEX_NOTEQUALS:
		case LEX_COMPARE:
		case LEX_RIGHTBRACE:
		case LEX_SEMICOLON:
		case LEX_COMMA:
		case LEX_LEFTBRACE:
		case LEX_LEFTHESIS:
		case LEX_RIGHTHESIS:
		case LEX_EQUAL:
		case LEX_MORE:
		case LEX_LESS:
		{
			if (strcmp(lexema, "<<") == 0) {
				// Заменяем лексему на LEX_SHIFT_LEFT ('b')
				LT::Add(lex, new LT::Entry('b', currentRow, currentLex));
			}
			else if (strcmp(lexema, ">>") == 0) {
				// Заменяем лексему на LEX_SHIFT_RIGHT ('n')
				LT::Add(lex, new LT::Entry('n', currentRow, currentLex));
			}
			else {
				// Все остальное как было
				LT::Add(lex, new LT::Entry(*lexema, currentRow, currentLex));
			}

			lexID = false;
			if (*lexema == LEX_RIGHTBRACE)
				areaOfVisibility.pop();
			break;
		}
		case IN_CODE_VERTICAL_LINE:
			currentRow++;
			currentLex = 0;
			lexID = false;
			if (lexComment)
			{
				lexComment = false;
			}
			break;

		case '#':
		{
			FST::FST fst_comment = FST_COMMENT(lexema);

			if (FST::execute(fst_comment))
			{
				lexComment = true;
			}
			break;
		}

		case 'f':
		{
			FST::FST fst_function = FST_FUNCTION(lexema);
			if (FST::execute(fst_function) && !lexComment)
			{
				LT::Add(lex, new LT::Entry(LEX_FUNCTION, currentRow, currentLex));
				idtype = IT::IDTYPE::F;
				lexID = false;
			}
			if (lexID) {
				FST::FST fst_false = FST_FALSE(lexema);
				if (FST::execute(fst_false) && !lexComment)
				{
					// Это логический литерал false
					int IsLiteralBool = IT::IsLiteralBool(id, lexema);
					if (IsLiteralBool + 1)
						LT::Add(lex, new LT::Entry(LEX_BOOL_LITERAL, currentRow, currentLex, IsLiteralBool));
					else
					{
						LT::Add(lex, new LT::Entry(LEX_BOOL_LITERAL, currentRow, currentLex, indexIdTable++));
						LITERAL_BOOL
							IT::Add(id, new IT::Entry(lex.current_size - 1,
								IT::IDDATATYPE::BOOL, IT::IDTYPE::L, lexema));
						ID_RESET
					}
					lexID = false;
				}
			}
			break;
		}
		case 'i':
		{
			FST::FST fst_int = FST_INT(lexema);
			if (FST::execute(fst_int) && !lexComment)
			{
				LT::Add(lex, new LT::Entry(LEX_INT, currentRow, currentLex));
				iddatatype = IT::IDDATATYPE::INT;
				lexID = false;
			}

			if (lexID)
			{
				FST::FST fst_if = FST_IF(lexema);
				if (FST::execute(fst_if) && !lexComment)
				{
					LT::Add(lex, new LT::Entry(LEX_IF, currentRow, currentLex));
					lexID = false;
				}
			}
			break;
		}
		case 'm':
		{
			FST::FST fst_main = FST_MAIN(lexema);
			if (FST::execute(fst_main) && !lexComment)
			{
				main++;
				LT::Add(lex, new LT::Entry(LEX_MAIN, currentRow, currentLex));
				areaOfVisibility.push(lexema);
				lexID = false;
			}
			break;
		}
		case 'p':
		{
			FST::FST fst_param = FST_PARAM(lexema);
			if (FST::execute(fst_param) && !lexComment)
			{
				LT::Add(lex, new LT::Entry(LEX_PARAM, currentRow, currentLex));
				idtype = IT::IDTYPE::P;

				if (IT_ENTRY)
				{
					IT_ENTRY->params.count++;
					IT_ENTRY->params.types.push_back(iddatatype);
				}

				lexID = false;
			}
			break;
		}

		case 'r':
		{
			FST::FST fst_return = FST_RETURN(lexema);
			if (FST::execute(fst_return) && !lexComment)
			{
				LT::Add(lex, new LT::Entry(LEX_RETURN, currentRow, currentLex));
				lexID = false;
			}
			if (lexID)
			{
				FST::FST fst_var = FST_REPEAT(lexema);
				if (FST::execute(fst_var) && !lexComment)
				{
					char str[10];
					static int count = 0;
					sprintf(str, "%d", count++);
					LT::Add(lex, new LT::Entry(LEX_REPEAT, currentRow, currentLex));
					areaOfVisibility.push(strcat(lexema, str));
					lexID = false;
				}
			}
			break;
		}
		case 's':
		{
			FST::FST fst_string = FST_STR(lexema);
			if (FST::execute(fst_string) && !lexComment)
			{
				LT::Add(lex, new LT::Entry(LEX_STR, currentRow, currentLex));
				iddatatype = IT::IDDATATYPE::STR;
				lexID = false;
			}
			break;
		}
		case 'c':
		{
			FST::FST fst_char = FST_CHAR(lexema);
			if (FST::execute(fst_char) && !lexComment) {
				LT::Add(lex, new LT::Entry(LEX_CHAR, currentRow, currentLex));
				iddatatype = IT::IDDATATYPE::CHAR;
				
				lexID = false;
			}
			break;
		}
		case 'v':
		{
			FST::FST fst_var = FST_VAR(lexema);
			if (FST::execute(fst_var) && !lexComment)
			{
				LT::Add(lex, new LT::Entry(LEX_VAR, currentRow, currentLex));
				idtype = IT::IDTYPE::V;
				lexID = false;
			}
			break;
		}
		case 'h':
		{
			FST::FST fst_literal_i16 = FST_LITERAL_I16(lexema);		// целочисленный литерал в 16-система счисления
			if (FST::execute(fst_literal_i16) && !lexComment)
			{
				lexID = false; lexInt = true;
				BaseToDecimal(lexema, 16);
			}
			break;
		}
		case 'o':
		{
			FST::FST fst_write = FST_WRITE(lexema);
			if (FST::execute(fst_write) && !lexComment)
			{
				LT::Add(lex, new LT::Entry(LEX_WRITE, currentRow, currentLex));
				lexID = false;
			}
			if (lexID) {
				FST::FST fst_literal_i8 = FST_LITERAL_I8(lexema);		// целочисленный литерал в 8-система счисления
				if (FST::execute(fst_literal_i8) && !lexComment)
				{
					lexID = false; lexInt = true;
					BaseToDecimal(lexema, 8);
				}
			}
			break;
		}
		case 'b':
		{
			FST::FST fst_literal_i2 = FST_LITERAL_I2(lexema);		// целочисленный литерал в 2-система счисления
			if (FST::execute(fst_literal_i2) && !lexComment)
			{
				lexID = false; lexInt = true;
				BaseToDecimal(lexema, 2);
			}
			if (lexID) {
				FST::FST fst_bool = FST_BOOL(lexema);
				if (FST::execute(fst_bool) && !lexComment)
				{
					LT::Add(lex, new LT::Entry(LEX_BOOL, currentRow, currentLex));
					iddatatype = IT::IDDATATYPE::BOOL;
					
					lexID = false;
				}
			}
			break;
		}
		
		case 't':
		{
			FST::FST fst_write = FST_THEN(lexema);
			if (FST::execute(fst_write) && !lexComment)
			{
				char str[10];
				static int count = 0;
				sprintf(str, "%d", count++);
				LT::Add(lex, new LT::Entry(LEX_THEN, currentRow, currentLex));
				areaOfVisibility.push(strcat(lexema, str));
				lexID = false;
			}
			if (lexID) {
				
				FST::FST fst_true = FST_TRUE(lexema);
				if (FST::execute(fst_true) && !lexComment)
				{
					// Это логический литерал true
					int IsLiteralBool = IT::IsLiteralBool(id, lexema); // Нужно реализовать
					if (IsLiteralBool + 1)
						LT::Add(lex, new LT::Entry(LEX_BOOL_LITERAL, currentRow, currentLex, IsLiteralBool));
					else
					{
						LT::Add(lex, new LT::Entry(LEX_BOOL_LITERAL, currentRow, currentLex, indexIdTable++));
						LITERAL_BOOL
							IT::Add(id, new IT::Entry(lex.current_size - 1,
								IT::IDDATATYPE::BOOL, IT::IDTYPE::L, lexema));
						ID_RESET
					}
					lexID = false;
				}
			}
			break;
		}
		case 'e':
		{
			FST::FST fst_write = FST_ELSE(lexema);
			if (FST::execute(fst_write) && !lexComment)
			{
				char str[10];
				static int count = 0;
				sprintf(str, "%d", count++);
				LT::Add(lex, new LT::Entry(LEX_ELSE, currentRow, currentLex));
				areaOfVisibility.push(strcat(lexema, str));
				lexID = false;
			}
			break;
		}
		case '+':
		case '-':
		case '*':
		case '/':
		case '%':
		{
			FST::FST fst_literal_minus_i = FST_LITERAL_MINUS_I(lexema);	
			
			if (FST::execute(fst_literal_minus_i) && !lexComment)
			{
				
				throw ERROR_THROW_IN_WORD(135, currentRow, currentLex, lexema);
			}
			if (lexID)
			{
				FST::FST fst_operator = FST_OPERATOR(lexema);				// операторы
				if (FST::execute(fst_operator) && !lexComment)
				{
					LT::Add(lex, new LT::Entry(LEX_OPERATOR, *lexema, currentRow, currentLex));
					lexID = false;
				}
			}
			break;
		}
		case IN_CODE_QUOTES: // строковые литералы
		{
			
			FST::FST fst_literal_s = FST_LITERAL_S(lexema);
			if (FST::execute(fst_literal_s) && !lexComment)
			{
				lexID = false;
				int IsLiteralString = IT::IsLiteralString(id, lexema);	// возвращает -1 если нет такого литерала, иначе указываем не него
				if (IsLiteralString + 1)
					LT::Add(lex, new LT::Entry(LEX_LITERAL, currentRow, currentLex, IsLiteralString));
				else
				{
					LT::Add(lex, new LT::Entry(LEX_LITERAL, currentRow, currentLex, indexIdTable++));

					LITERAL_STR
						IT::Add(id, new IT::Entry(lex.current_size - 1, iddatatype, idtype, lexema));
					ID_RESET
				}
			}
			break;
		}
		case IN_CODE_QUOTES_CHAR:
		{
			
			FST::FST fst_literal_char = FST_LITERAL_C(lexema);
			if (FST::execute(fst_literal_char) && !lexComment)
			{
				lexID = false;
				iddatatype = IT::IDDATATYPE::CHAR;
				int IsLiteralChar = IT::IsLiteralChar(id, lexema);
				if (IsLiteralChar + 1) {
					// ИСПОЛЬЗУЕМ LEX_CHAR_LITERAL ('k')
					LT::Add(lex, new LT::Entry(LEX_CHAR_LITERAL, currentRow, currentLex, IsLiteralChar));
				}
				else {
					// ИСПОЛЬЗУЕМ LEX_CHAR_LITERAL ('k')
					LT::Add(lex, new LT::Entry(LEX_CHAR_LITERAL, currentRow, currentLex, indexIdTable++));

					LITERAL_CHAR
						IT::Add(id, new IT::Entry(lex.current_size - 1, IT::IDDATATYPE::CHAR, idtype, lexema));
					ID_RESET
				}
			}
			break;
		}
		default:
		{
			FST::FST fst_literal_i = FST_LITERAL_I(lexema);		// целочисленные литералы
			if (FST::execute(fst_literal_i))
			{
				lexID = false; lexInt = true;
			}
			break;
		}
		}
		if (lexInt && !lexComment)
		{
			// Сначала проверим, что это действительно целое число
			errno = 0;
			char* endptr;
			long long temp = strtoll(lexema, &endptr, 10);

			// Проверяем ошибки преобразования
			if (errno == ERANGE) {
				throw ERROR_THROW_IN_WORD(711, currentRow, currentLex, lexema);
			}

			// Проверяем, что вся строка преобразована
			while (*endptr != '\0' && isspace((unsigned char)*endptr)) {
				endptr++;
			}

			

			// Теперь проверяем, есть ли такой литерал
			int IsLiteralInt = IT::IsLiteralInt(id, lexema);
			if (IsLiteralInt + 1)
				LT::Add(lex, new LT::Entry(LEX_LITERAL, currentRow, currentLex, IsLiteralInt));
			else
			{
				// ПРОВЕРЯЕМ ПЕРЕПОЛНЕНИЕ INT!
				if (temp > INT_MAX || temp < INT_MIN) {
					throw ERROR_THROW_IN_WORD(711, currentRow, currentLex, lexema);
				}

				LT::Add(lex, new LT::Entry(LEX_LITERAL, currentRow, currentLex, indexIdTable++));

				// Убеждаемся, что тип правильный
				iddatatype = IT::IDDATATYPE::INT;
				idtype = IT::IDTYPE::L;

				IT::Add(id, new IT::Entry(lex.current_size - 1, iddatatype, idtype, lexema));
				ID_RESET
			}
			if (*endptr != '\0') {
				throw ERROR_THROW_IN_WORD(720, currentRow, currentLex, lexema); // не число
			}
		}

		if (lexID && !lexComment) 	// идентификатор
		{

			FST::FST fst_id = FST_ID(lexema);
			if (FST::execute(fst_id))
			{
				int isId = IT::IsId(id, lexema, areaOfVisibility); // возвращает -1 если нет в таблице идентификаторов
				if (isId + 1)
				{
					if (idtype != IT::IDTYPE::D)
					{
						if (!strcmp(id.table[isId]->areaOfVisibility, areaOfVisibility.top().c_str()))
							throw ERROR_THROW_IN_WORD(131, currentRow, currentLex, lexema);
						if (iddatatype == IT::IDDATATYPE::DEF)
							throw ERROR_THROW_IN_WORD(121, currentRow, currentLex, lexema);

						LT::Add(lex, new LT::Entry(LEX_ID, currentRow, currentLex, indexIdTable++));
						IT::Add(id, new IT::Entry(lex.current_size - 1, areaOfVisibility.top(), lexema, iddatatype, idtype));

						ID_RESET
					}
					else
						LT::Add(lex, new LT::Entry(LEX_ID, currentRow, currentLex, isId));
				}
				else
				{
					if (iddatatype == IT::IDDATATYPE::DEF)
						throw ERROR_THROW_IN_WORD(121, currentRow, currentLex, lexema);
					if (idtype == IT::IDTYPE::D)
						throw ERROR_THROW_IN_WORD(132, currentRow, currentLex, lexema);

					LT::Add(lex, new LT::Entry(LEX_ID, currentRow, currentLex, indexIdTable++));
					IT::Add(id, new IT::Entry(lex.current_size - 1, areaOfVisibility.top(), lexema, iddatatype, idtype));

					if (idtype == IT::IDTYPE::F)
					{
						areaOfVisibility.push(lexema);
						IT_ENTRY = id.table[id.current_size - 1];
					}

					ID_RESET
				}
			}
			else
				throw ERROR_THROW_IN_WORD(120, currentRow, currentLex, lexema);
		}
	}

	if (main == 0)
		throw ERROR_THROW(133);
	if (main > 1)
		throw ERROR_THROW(134);
	LT::Add(lex, new LT::Entry('$', currentRow, currentLex));
}

bool FindLexema(In::IN& in, char* lexema)
{
	static int i = 0;
	int indexLexema = 0;
	bool isLiteral = false; // Флаг, находимся ли мы внутри кавычек

	// 1. Пропускаем любые пробельные символы перед началом лексемы
	// (включая пробел, табуляцию, переводы строк)
	while (i < in.size && (in.text[i] == IN_CODE_SPACE || in.text[i] == ' ' || in.text[i] == '\t' || in.text[i] == '\n' || in.text[i] == '\r'))
	{
		i++;
	}

	// Если дошли до конца файла
	if (i >= in.size) return false;

	if (in.text[i] == '-' && i + 1 < in.size && isdigit(in.text[i + 1]))
	{
		// Собираем полное отрицательное число
		int start = i;
		lexema[indexLexema++] = '-';
		i++;

		// Собираем все цифры
		while (i < in.size && isdigit(in.text[i]) && indexLexema < TI_STR_MAXSIZE - 1)
		{
			lexema[indexLexema++] = in.text[i++];
		}
		lexema[indexLexema] = '\0';

		

		// Проверяем, что это действительно число (а не часть идентификатора)
		char* endptr;
		strtoll(lexema, &endptr, 10);
		if (*endptr == '\0')
		{
			// Это валидное отрицательное число - возвращаем его как лексему
			return true;
		}
		else
		{
			// Не число - откатываемся
			i = start;
			indexLexema = 0;
		}
	}
	// 2. Читаем лексему
	while (i < in.size)
	{
		char c = in.text[i];
		// Обработка строковых литералов (внутри кавычек разделители игнорируются)
		if (c == IN_CODE_QUOTES) {
			isLiteral = !isLiteral;
		}
		else if (c == IN_CODE_QUOTES_CHAR) {
			if (indexLexema == 0) {
				// СОХРАНЯЕМ открывающую кавычку
				lexema[indexLexema++] = c;  // <-- ВОТ ЭТО ДОБАВЬТЕ!
				i++;

				if (i < in.size) {
					lexema[indexLexema++] = in.text[i++]; // сам символ
				}

				if (i < in.size && in.text[i] == IN_CODE_QUOTES_CHAR) {
					lexema[indexLexema++] = in.text[i++]; // закрывающая '
				}

				lexema[indexLexema] = TI_STR_DEFAULT;
				return true;
			}
		}

		if (!isLiteral)
		{
			// Если встретили пробел — лексема закончилась
			if (c == IN_CODE_SPACE || c == ' ' || c == '\t' || c == '\n' || c == '\r')
				break;

			// Если встретили односимвольный разделитель
			if (c == '{' || c == '}' || c == '(' || c == ')' || c == ';' || c == ',' || c == IN_CODE_VERTICAL_LINE)
			{
				if (indexLexema > 0)
					break;
				lexema[indexLexema++] = c;
				i++;
				break;
			}

			if (c == '=' || c == '+' || c == '-' || c == '*' ||
				c == '/' || c == '%' || c == '<' || c == '>')
			{
				
				if (indexLexema > 0) break;

				// ДОБАВЛЕНО: проверяем оператор сдвига
				// Если текущий символ < или >, и следующий такой же
				if ((c == '<' || c == '>') && i + 1 < in.size && in.text[i + 1] == c)
				{
					
					// Это оператор сдвига (<< или >>)
					lexema[indexLexema++] = c;
					lexema[indexLexema++] = c;
					i += 2;
				}
				else
				{
					// Одиночный оператор
					lexema[indexLexema++] = c;
					i++;
				}
				break;
			}
		}

		if (indexLexema >= TI_STR_MAXSIZE - 1)
			throw ERROR_THROW(125);

		lexema[indexLexema++] = in.text[i];
		i++;
	}


	lexema[indexLexema] = TI_STR_DEFAULT;
	
	return indexLexema > 0;
}

void BaseToDecimal(char* lexema, int base)
{
	int number = 0;
	int k;
	for (int i = 1; lexema[i] != '\0'; i++)
	{
		if (lexema[i] <= '9' && lexema[i] >= '0') k = lexema[i] - '0';
		else if (lexema[i] >= 'A' && lexema[i] <= 'F') k = lexema[i] - 'A' + 10;
		else if (lexema[i] >= 'a' && lexema[i] <= 'f') k = lexema[i] - 'a' + 10;
		else continue;
		number = base * number + k;
	}
	sprintf(lexema, "%d", number);
}