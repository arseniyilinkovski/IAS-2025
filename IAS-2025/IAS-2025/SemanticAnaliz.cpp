#include "stdafx.h"
#include "SemanticAnaliz.h"
#include "Error.h"

using namespace std;

namespace SM
{

	bool checkShiftOverflow(long long value, int shift, char shiftType)
	{
		const long long INT_MAX_VALUE = 2147483647LL;
		const long long INT_MIN_VALUE = -2147483648LL;

		// Проверка некорректного сдвига (должна быть 711)
		if (shift < 0 || shift > 63)
		{
			return false;
		}

		// Проверка сдвига за пределы 32 бит (711)
		if (shift >= 32)
		{
			return false;
		}

		if (shiftType == 'b') // << (сдвиг влево)
		{
			// Проверяем, не превысит ли результат максимальное значение int
			long long result = (long long)value << shift;

			// Проверяем границы int
			if (result > INT_MAX_VALUE || result < INT_MIN_VALUE)
			{
				return false;
			}

			// Дополнительная проверка: если старшие биты будут потеряны
			if (shift > 0 && value != 0)
			{
				// Проверяем, не будут ли потеряны значащие биты
				// Для положительных чисел
				if (value > 0)
				{
					long long maxShiftable = INT_MAX_VALUE >> shift;
					if (value > maxShiftable)
					{
						return false;
					}
				}
				// Для отрицательных чисел
				else
				{
					long long minShiftable = INT_MIN_VALUE >> shift;
					if (value < minShiftable)
					{
						return false;
					}
				}
			}
		}
		// Для сдвига вправо всегда безопасно

		return true;
	}

	void semAnaliz(LT::LexTable& lextable, IT::IdTable& idtable)
	{
		for (int i = 0; i < lextable.current_size; i++)
		{
			int idxTI = lextable.table[i]->idxTI;
			std::string tmp_name;               // временная строка для fallback (без статического буфера)
			const char* name1 = nullptr;

			if (idxTI != LT_TI_NULLXDX && idxTI >= 0 && idxTI < idtable.current_size) {
				name1 = idtable.table[idxTI]->id;
			}
			else {
				// fallback: используем одиночный символ lexema как строку
				tmp_name.resize(1);
				tmp_name[0] = lextable.table[i]->lexema;
				name1 = tmp_name.c_str();
			}

			switch (lextable.table[i]->lexema)
			{
			case LEX_REPEAT: // '~' - проверка repeat
			{
				// Проверяем конструкцию repeat(expression)
				if (i + 1 < lextable.current_size &&
					lextable.table[i + 1]->lexema == LEX_LEFTHESIS)
				{
					// Ищем выражение внутри скобок
					int j = i + 2; // после '('
					bool foundExpression = false;
					bool isLiteral = false;
					bool isIdentifier = false;
					int exprIndex = -1;

					// Проходим до закрывающей скобки
					while (j < lextable.current_size &&
						lextable.table[j]->lexema != LEX_RIGHTHESIS)
					{
						if (lextable.table[j]->idxTI != LT_TI_NULLXDX)
						{
							foundExpression = true;
							exprIndex = j;

							// Проверяем тип выражения
							IT::Entry* entry = idtable.table[lextable.table[j]->idxTI];

							if (lextable.table[j]->lexema == LEX_LITERAL)
							{
								isLiteral = true;
								// Проверяем, что литерал целочисленный
								if (entry->iddatatype != IT::IDDATATYPE::INT)
								{
									throw ERROR_THROW_IN_WORD(731, lextable.table[i]->sn, lextable.table[i]->tn, name1);
								}

								// Проверяем значение литерала
								if (entry->value.vint <= 0)
								{
									throw ERROR_THROW_IN_WORD(731, lextable.table[i]->sn, lextable.table[i]->tn, name1);
								}

								// Проверяем, что значение не слишком большое
								if (entry->value.vint > 1000000) // или другое разумное ограничение
								{
									throw ERROR_THROW_IN_WORD(732, lextable.table[i]->sn, lextable.table[i]->tn, name1);
								}
							}
							else if (lextable.table[j]->lexema == LEX_ID)
							{
								isIdentifier = true;
								// Проверяем, что переменная целочисленного типа
								if (entry->iddatatype != IT::IDDATATYPE::INT)
								{
									throw ERROR_THROW_IN_WORD(730, lextable.table[i]->sn, lextable.table[i]->tn, name1);
								}

								// Не можем проверить значение статически
								// Можно добавить предупреждение
							}
						}
						j++;
					}

					// Проверяем, что нашли выражение
					if (!foundExpression)
					{
						throw ERROR_THROW_IN_WORD(733, lextable.table[i]->sn, lextable.table[i]->tn, name1);
					}

					// Проверяем, что после параметра идет тело цикла
					if (j + 1 < lextable.current_size &&
						lextable.table[j + 1]->lexema == LEX_LEFTBRACE)
					{
						// Все в порядке, проверяем содержимое тела цикла
						// (дополнительные проверки можно добавить здесь)
					}
					else
					{
						throw ERROR_THROW_IN_WORD(734, lextable.table[i]->sn, lextable.table[i]->tn, name1);
					}
				}
				// Если это не repeat(expression), а просто цикл while/for
				// (обрабатывается другими правилами)
				break;
			}
			case LEX_OPERATOR:	// äåëåíèå íà 0
			{
				if (lextable.table[i]->sign == '/')
					if (lextable.table[i + 1]->lexema == LEX_LITERAL)
					{
						if (idtable.table[lextable.table[i + 1]->idxTI]->value.vint == 0)
							throw ERROR_THROW_IN_WORD(700, lextable.table[i]->sn, lextable.table[i]->tn, name1);
					}
				break;
			}
			case 'b': // <<
			case 'n': // >>
			{
				// Проверяем сдвиг влево (<<) и вправо (>>)
				char shiftType = lextable.table[i]->lexema;

				// Находим левый операнд
				if (i > 0 && lextable.table[i - 1]->idxTI != LT_TI_NULLXDX)
				{
					IT::Entry* leftOperand = idtable.table[lextable.table[i - 1]->idxTI];

					// Проверяем только для целых чисел
					if (leftOperand->iddatatype != IT::IDDATATYPE::INT)
					{
						throw ERROR_THROW_IN_WORD(720, lextable.table[i]->sn, lextable.table[i]->tn, name1);
					}

					// Если правый операнд - литерал
					if (i + 1 < lextable.current_size &&
						lextable.table[i + 1]->lexema == LEX_LITERAL)
					{
						IT::Entry* rightOperand = idtable.table[lextable.table[i + 1]->idxTI];

						if (rightOperand->iddatatype != IT::IDDATATYPE::INT)
						{
							throw ERROR_THROW_IN_WORD(720, lextable.table[i]->sn, lextable.table[i]->tn, name1);
						}

						long long leftValue = leftOperand->value.vint;
						int shiftValue = (int)rightOperand->value.vint;

						// Проверяем значение сдвига (711 если >= 32)
						if (shiftValue < 0 || shiftValue >= 32)
						{
							throw ERROR_THROW_IN_WORD(720, lextable.table[i]->sn, lextable.table[i]->tn, name1);
						}

						// Проверяем переполнение для сдвига влево
						if (shiftType == 'b' && !checkShiftOverflow(leftValue, shiftValue, 'b'))
						{
							throw ERROR_THROW_IN_WORD(710, lextable.table[i]->sn, lextable.table[i]->tn, name1);
						}
					}
					// Если правый операнд - переменная
					else if (i + 1 < lextable.current_size &&
						lextable.table[i + 1]->lexema == LEX_ID)
					{
						IT::Entry* rightOperand = idtable.table[lextable.table[i + 1]->idxTI];

						if (rightOperand->iddatatype != IT::IDDATATYPE::INT)
						{
							throw ERROR_THROW_IN_WORD(720, lextable.table[i]->sn, lextable.table[i]->tn, name1);
						}

						// Не можем проверить статически значение переменной
						// Можно только проверить тип
					}
					// Если правый операнд не найден
					else
					{
						throw ERROR_THROW_IN_WORD(720, lextable.table[i]->sn, lextable.table[i]->tn, name1);
					}
				}
				else
				{
					throw ERROR_THROW_IN_WORD(720, lextable.table[i]->sn, lextable.table[i]->tn, name1);
				}
				break;
			}
			case LEX_EQUAL: // выражение
			{
				if (i)
				{
					IT::IDDATATYPE lefttype = idtable.table[lextable.table[i - 1]->idxTI]->iddatatype;	// левый операнд
					bool ignore = false;
					bool inFunctionCall = false;

					// ОСОБАЯ ПРОВЕРКА ДЛЯ BOOL ПЕРЕМЕННЫХ
					if (lefttype == IT::IDDATATYPE::BOOL)
					{
						bool validBoolValue = false;
						bool foundValue = false;

						for (int k = i + 1; k < lextable.current_size && lextable.table[k]->lexema != LEX_SEMICOLON; k++)
						{
							if (lextable.table[k]->idxTI != LT_TI_NULLXDX)
							{
								foundValue = true;
								IT::Entry* rightEntry = idtable.table[lextable.table[k]->idxTI];

								// Разрешаем присваивать bool:
								// 1. BOOL литералы (true/false) - 'o'
								// 2. BOOL переменные
								// 3. INT литералы 0 или 1 (автоматическое преобразование)
								// 4. INT переменные (с риском)

								if (rightEntry->iddatatype == IT::IDDATATYPE::BOOL)
								{
									validBoolValue = true;
									break;
								}
								
								else if (rightEntry->iddatatype == IT::IDDATATYPE::STR ||
									rightEntry->iddatatype == IT::IDDATATYPE::CHAR)
								{
									// STR и CHAR нельзя присваивать BOOL
									throw ERROR_THROW_IN_WORD(741, lextable.table[i]->sn, lextable.table[i]->tn, name1);
								}
							}
							// Проверка вызова функции для bool
							else if (lextable.table[k]->lexema == LEX_ID &&
								k + 1 < lextable.current_size &&
								lextable.table[k + 1]->lexema == LEX_LEFTHESIS)
							{
								// Это вызов функции, проверка будет в другом месте
								validBoolValue = true; // Предполагаем, что функция возвращает правильный тип
								break;
							}
						}

						if (foundValue && !validBoolValue)
						{
							// Если нашли какое-то значение, но оно невалидное
							throw ERROR_THROW_IN_WORD(741, lextable.table[i]->sn, lextable.table[i]->tn, name1);
						}
						else if (!foundValue)
						{
							// Просто "bool flag;" без инициализации - это нормально
							break;
						}
					}

					// СТАНДАРТНАЯ ПРОВЕРКА ДЛЯ ВСЕХ ТИПОВ
					for (int k = i + 1; lextable.table[k]->lexema != LEX_SEMICOLON; k++)
					{
						if (lextable.table[k]->idxTI != LT_TI_NULLXDX) // если ид - проверить совпадение типов
						{
							if (!ignore && !inFunctionCall)
							{
								IT::IDDATATYPE righttype = idtable.table[lextable.table[k]->idxTI]->iddatatype;

								// Для bool пропускаем проверку с int (обработано выше)
								if (lefttype == IT::IDDATATYPE::BOOL && righttype == IT::IDDATATYPE::INT)
								{
									// Уже проверили выше
									continue;
								}

								if (lefttype != righttype) // типы данных в выражении не совпадают
									throw ERROR_THROW_IN_WORD(701, lextable.table[i]->sn, lextable.table[i]->tn, name1);
							}
							// если лексема сразу после идентиф скобка - это вызов функции
							if (k + 1 < lextable.current_size && lextable.table[k + 1]->lexema == LEX_LEFTHESIS &&
								idtable.table[lextable.table[k]->idxTI]->idtype == IT::IDTYPE::F)
							{
								inFunctionCall = true;
								continue;
							}
							// закрывающая скобка после списка параметров
							if (ignore && lextable.table[k + 1]->lexema == LEX_RIGHTHESIS)
							{
								ignore = false;
								continue;
							}
						}

						// Проверки для специфических типов
						if (lefttype == IT::IDDATATYPE::STR) // справа только литерал, ид или вызов строковой ф-ции
						{
							char l = lextable.table[k]->lexema;
							if (l == LEX_OPERATOR) // выражения недопустимы
								throw ERROR_THROW_IN_WORD(702, lextable.table[i]->sn, lextable.table[i]->tn, name1);
						}

						// Проверка для CHAR типа
						if (lefttype == IT::IDDATATYPE::CHAR)
						{
							char l = lextable.table[k]->lexema;
							if (l == LEX_OPERATOR) // выражения недопустимы для char
								throw ERROR_THROW_IN_WORD(701, lextable.table[i]->sn, lextable.table[i]->tn, name1);
						}
						// --- Проверка смешивания несовместимых типов в бинарных выражениях ---
						if (lextable.table[k]->lexema == LEX_OPERATOR)
						{
							char op = lextable.table[k]->sign;

							// Проверяем только бинарные операторы
							if (op == '+' || op == '-' || op == '*' || op == '/')
							{
								// Левый и правый операнд должны быть идентификаторами или литералами
								if (k - 1 >= 0 &&
									lextable.table[k - 1]->idxTI != LT_TI_NULLXDX &&
									k + 1 < lextable.current_size &&
									lextable.table[k + 1]->idxTI != LT_TI_NULLXDX)
								{
									IT::IDDATATYPE leftType = idtable.table[lextable.table[k - 1]->idxTI]->iddatatype;
									IT::IDDATATYPE rightType = idtable.table[lextable.table[k + 1]->idxTI]->iddatatype;

									bool leftIsNum = (leftType == IT::IDDATATYPE::INT);
									bool rightIsNum = (rightType == IT::IDDATATYPE::INT);

									bool leftIsBool = (leftType == IT::IDDATATYPE::BOOL);
									bool rightIsBool = (rightType == IT::IDDATATYPE::BOOL);

									bool leftIsStr = (leftType == IT::IDDATATYPE::STR || leftType == IT::IDDATATYPE::CHAR);
									bool rightIsStr = (rightType == IT::IDDATATYPE::STR || rightType == IT::IDDATATYPE::CHAR);

									// Запрещаем:
									// INT + BOOL
									// BOOL + INT
									// STR + INT
									// INT + STR
									// STR + BOOL
									// BOOL + STR
									if ((leftIsNum && rightIsBool) ||
										(leftIsBool && rightIsNum) ||
										(leftIsStr && rightIsNum) ||
										(leftIsNum && rightIsStr) ||
										(leftIsStr && rightIsBool) ||
										(leftIsBool && rightIsStr))
									{
										throw ERROR_THROW_IN_WORD(701, lextable.table[k]->sn, lextable.table[k]->tn, name1);
									}
								}
							}
						}


						// Пропускаем операторы в выражениях
						if (lextable.table[k]->lexema == LEX_OPERATOR ||
							lextable.table[k]->lexema == 'b' ||  // <<
							lextable.table[k]->lexema == 'n')    // >>
						{
							ignore = true;
							continue;
						}

						// Пропускаем скобки в выражениях
						if (lextable.table[k]->lexema == LEX_LEFTHESIS ||
							lextable.table[k]->lexema == LEX_RIGHTHESIS)
						{
							continue;
						}

						// Если встретили запятую в списке параметров функции
						if (lextable.table[k]->lexema == LEX_COMMA && inFunctionCall)
						{
							continue;
						}
					}
				}
				break;
			}
			case LEX_ID: // ïðîâåðêà òèïà âîçâðàùàåìîãî çíà÷åíèÿ  
			{
				IT::Entry* e;
				e = idtable.table[lextable.table[i]->idxTI];

				if (i && lextable.table[i - 1]->lexema == LEX_FUNCTION)	// îáúÿâëåíèå ôóíêöèè
				{
					for (int k = i + 1; ; k++)
					{
						char l = lextable.table[k]->lexema;
						if (l == LEX_RETURN)
						{
							int next = lextable.table[k + 1]->idxTI; // ñëåä. çà return
							if (idtable.table[next]->iddatatype != e->iddatatype)
								throw ERROR_THROW_IN_WORD(703, lextable.table[i]->sn, lextable.table[i]->tn, name1);
							break;
						}
					}
				}
				if (lextable.table[i + 1]->lexema == LEX_LEFTHESIS && lextable.table[i - 1]->lexema != LEX_FUNCTION) // èìåííî âûçîâ
				{
					if (e->idtype == IT::IDTYPE::F) // òî÷íî ôóíêöèÿ
					{
						int paramscount = NULL;
						// ïðîâåðêà ïåðåäàâàåìûõ ïàðàìåòðîâ
						for (int j = i + 1; lextable.table[j]->lexema != LEX_RIGHTHESIS; j++)
						{
							// ïðîâåðêà ñîîòâåòñòâèÿ ïåðåäàâàåìûõ ïàðàìåòðîâ ïðîòîòèïàì
							if (lextable.table[j]->lexema == LEX_ID || lextable.table[j]->lexema == LEX_LITERAL || lextable.table[j]->lexema == LEX_CHAR_LITERAL)
							{
								paramscount++;
								IT::IDDATATYPE ctype = idtable.table[lextable.table[j]->idxTI]->iddatatype;
								if (paramscount > e->params.count)
									throw ERROR_THROW_IN_WORD(705, lextable.table[i]->sn, lextable.table[i]->tn, name1);
								if (ctype != e->params.types[paramscount - 1])
									throw ERROR_THROW_IN_WORD(704, lextable.table[i]->sn, lextable.table[i]->tn, name1);
							}
						}
						if (paramscount != e->params.count)
							throw ERROR_THROW_IN_WORD(705, lextable.table[i]->sn, lextable.table[i]->tn, name1);
					}
				}
				break;
			}
			case LEX_WRITE:
			{
				// output func(...) — запрещено
				if (i + 1 < lextable.current_size &&
					lextable.table[i + 1]->lexema == LEX_ID)
				{
					IT::Entry* e = idtable.table[lextable.table[i + 1]->idxTI];

					// Если это функция и дальше идёт '(' — ошибка
					if (e->idtype == IT::IDTYPE::F &&
						i + 2 < lextable.current_size &&
						lextable.table[i + 2]->lexema == LEX_LEFTHESIS)
					{
						throw ERROR_THROW_IN_WORD(
							720, // выбери свободный код ошибки
							lextable.table[i]->sn,
							lextable.table[i]->tn,
							e->id
						);
					}
				}

				break;
			}
			case LEX_MORE:	case LEX_LESS:
			{
				// ëåâûé è ïðàâûé îïåðàíä - ÷èñëîâîé òèï
				bool flag = true;
				if (lextable.table[i - 1]->idxTI != LT_TI_NULLXDX)
				{
					if (idtable.table[lextable.table[i - 1]->idxTI]->iddatatype != IT::IDDATATYPE::INT)
						flag = false;
				}
				if (lextable.table[i + 1]->idxTI != LT_TI_NULLXDX)
				{
					if (idtable.table[lextable.table[i + 1]->idxTI]->iddatatype != IT::IDDATATYPE::INT)
						flag = false;
				}
				if (!flag)
					throw ERROR_THROW_IN_WORD(706, lextable.table[i]->sn, lextable.table[i]->tn, name1);
				break;
			}
			case LEX_COMPARE:   // оператор логического равенства (==)
			{
				// Левый операнд
				if (lextable.table[i - 1]->idxTI == LT_TI_NULLXDX ||
					lextable.table[i + 1]->idxTI == LT_TI_NULLXDX)
				{
					throw ERROR_THROW_IN_WORD(701, lextable.table[i]->sn, lextable.table[i]->tn, name1);
				}

				IT::Entry* left = idtable.table[lextable.table[i - 1]->idxTI];
				IT::Entry* right = idtable.table[lextable.table[i + 1]->idxTI];

				// Типы должны совпадать
				if (left->iddatatype != right->iddatatype)
				{
					throw ERROR_THROW_IN_WORD(701, lextable.table[i]->sn, lextable.table[i]->tn, name1);
				}

				break;
			}
			}
		}
	}
}