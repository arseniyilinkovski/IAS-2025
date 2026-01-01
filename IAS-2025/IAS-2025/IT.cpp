#include "stdafx.h"
#include "IT.h"
#include "Error.h"

using namespace std;

namespace IT
{
	IdTable::IdTable(int size)
	{
		if (size > TI_MAXSIZE)
			throw ERROR_THROW(127);

		maxsize = TI_MAXSIZE;
		current_size = 0;
		table = new Entry * [size];
	}

	//функция переменная, параметр
	Entry::Entry(int idxfirstLE, string areaOfVisibility, const char* id, IDDATATYPE iddatatype, IDTYPE idtype)
	{
		this->idxfirstLE = idxfirstLE;
		strncpy(this->areaOfVisibility, areaOfVisibility.c_str(), ID_MAXSIZE - 1);
		strncpy(this->id, id, ID_MAXSIZE - 1);
		this->iddatatype = iddatatype;
		this->idtype = idtype;
		if (iddatatype == IT::IDDATATYPE::INT)
			this->value.vint = TI_INT_DEFAULT;
		else if (iddatatype == IT::IDDATATYPE::CHAR)
			this->value.vint = TI_INT_DEFAULT;  // можно использовать vint для хранения char
		else if (iddatatype == IT::IDDATATYPE::BOOL) {
			this->value.vbool = false;
		}
		else
		{
			this->value.vstr.len = 0;
			this->value.vstr.str[0] = TI_STR_DEFAULT;
		}

		this->params.count = 0;
		if (this->idtype == IT::IDTYPE::V || this->idtype == IT::IDTYPE::P)
			FullName = string(this->id) + '_' + string(this->areaOfVisibility);
		else
			FullName = string(this->id);
	}

	Entry::Entry(int idxfirstLE, IDDATATYPE iddatatype, IDTYPE idtype, char* value)
	{
		char str[10];
		static int count = 0;
		sprintf(str, "%d", count++);
		strcpy(this->id, "L");
		strcat(this->id, str);
		strcpy(this->areaOfVisibility, GLOBAL);
		this->idxfirstLE = idxfirstLE;
		this->iddatatype = iddatatype;
		this->idtype = idtype;

		if (iddatatype == IT::IDDATATYPE::INT) {
			errno = 0;
			char* endptr;
			
			unsigned long long temp = strtoull(value, &endptr, 10); // Используем strtoull вместо strtoll
			
			if (errno == ERANGE) {
				throw ERROR_THROW(711);
			}

			while (*endptr != '\0' && isspace((unsigned char)*endptr)) {
				endptr++;
			}

			if (*endptr != '\0') {
				throw ERROR_THROW(720);
			}

			// ПРОВЕРЯЕМ ДИАПАЗОН ОТ 0 ДО INT_MAX
			if (temp > INT_MAX) { // убрана проверка < INT_MIN
				throw ERROR_THROW(711);
			}

			this->value.vint = (int)temp;
		}
		else if (iddatatype == IT::IDDATATYPE::CHAR) {
			// Сохраняем символьный литерал
			if (strlen(value) >= 3 && value[0] == '\'' && value[2] == '\'') {
				this->value.vint = (unsigned char)value[1];
			}
			else if (strlen(value) == 1) {
				this->value.vint = (unsigned char)value[0];
			}
			else {
				this->value.vint = (value[0] != '\0') ? (unsigned char)value[0] : 0;
			}
		}
		else if (iddatatype == IT::IDDATATYPE::BOOL) {
			
			// СТРОГАЯ ПРОВЕРКА: только "true" или "false"
			if (strcmp(value, "true") == 0) {
				this->value.vbool = true;
			}
			else if (strcmp(value, "false") == 0) {
				this->value.vbool = false;
			}
			else {
				// Ошибка - неверное значение bool
				// Добавьте соответствующую ошибку, например:
				// throw ERROR_THROW(730); // Код ошибки для неверного bool значения
				throw ERROR_THROW(741); // Предположим, что 730 - код ошибки для неверного bool значения
			}
		}
		else {
			this->value.vstr.len = strlen(value);
			strncpy(this->value.vstr.str, value, TI_STR_MAXSIZE - 1);
			this->value.vstr.str[TI_STR_MAXSIZE - 1] = TI_STR_DEFAULT;
		}
		FullName = string(this->id);
	}


	void Add(IdTable& idtable, Entry* entry)
	{
		if (idtable.current_size < idtable.maxsize)
		{
			idtable.table[idtable.current_size] = entry;
			idtable.current_size++;
		}
		else
			throw ERROR_THROW(128);
	}
	int IsLiteralChar(IdTable& idtable, char* lexema)
	{
		if (strlen(lexema) != 3 || lexema[0] != '\'' || lexema[2] != '\'')
			return -1;

		char char_value = lexema[1];
		for (int i = 0; i < idtable.current_size; i++)
		{
			if (idtable.table[i]->idtype == IT::IDTYPE::L &&
				idtable.table[i]->iddatatype == IT::IDDATATYPE::CHAR)
			{
				if (idtable.table[i]->value.vint == (int)char_value)
					return i;
			}
		}
		return -1;
	}
	Entry GetEntry(IdTable& idtable, int n)
	{
		if (n > idtable.current_size)
			throw ERROR_THROW(129);

		return *idtable.table[n];
	}

	int IsId(IdTable& idtable, char id[ID_MAXSIZE], stack<string> areaOfVisibility)
	{
		int size = areaOfVisibility.size();
		for (int i = 0; i < size; i++)
		{
			for (int j = 0; j < idtable.current_size; j++)
			{
				if (!strcmp(idtable.table[j]->areaOfVisibility, areaOfVisibility.top().c_str()))
					if (!strcmp(idtable.table[j]->id, id))
						return j;
			}
			areaOfVisibility.pop();
		}
		return -1;
	}

	int IsLiteralInt(IdTable& idtable, char* lexema)
	{
		errno = 0;
		char* endptr;
		
		long long number = strtoll(lexema, &endptr, 10);
		
		// Проверяем ошибки преобразования
		if (errno == ERANGE) {
			return -1; // переполнение
		}

		// Проверяем, что вся строка преобразована
		while (*endptr != '\0' && isspace((unsigned char)*endptr)) {
			endptr++;
		}

		if (*endptr != '\0') {
			return -1; // не число
		}

		// Проверяем переполнение int
		if (number > INT_MAX || number < 0) {
			return -1; // переполнение int
		}

		int intNumber = (int)number;

		for (int i = 0; i < idtable.current_size; i++)
		{
			if (idtable.table[i]->idtype == IT::IDTYPE::L &&
				idtable.table[i]->iddatatype == IT::IDDATATYPE::INT)
			{
				if (idtable.table[i]->value.vint == intNumber)
					return i;
			}
		}
		return -1;
	}
	int IsLiteralBool(IdTable& idtable, char* lexema)
	{
		bool boolValue;

		// СТРОГАЯ ПРОВЕРКА: только "true" или "false"
		if (strcmp(lexema, "true") == 0) {
			boolValue = true;
		}
		else if (strcmp(lexema, "false") == 0) {
			boolValue = false;
		}
		else {
			return -1; // Не bool литерал
		}

		// Ищем в таблице
		for (int i = 0; i < idtable.current_size; i++)
		{
			if (idtable.table[i]->idtype == IT::IDTYPE::L &&
				idtable.table[i]->iddatatype == IT::IDDATATYPE::BOOL)
			{
				if (idtable.table[i]->value.vbool == boolValue)
					return i;
			}
		}
		return -1;
	}
	int IsLiteralString(IdTable& idtable, char* lexema)
	{
		for (int i = 0; i < idtable.current_size; i++)
		{
			if (idtable.table[i]->idtype == IT::IDTYPE::L && idtable.table[i]->iddatatype == IT::IDDATATYPE::STR)
				if (!strcmp(idtable.table[i]->value.vstr.str, lexema))
					return i;
		}
		return -1;
	}

	void Delete(IdTable& idtable)
	{
		for (int i = 7; i < idtable.current_size; i++)
		{
			delete idtable.table[i];
		}
		delete[] idtable.table;
	}

	void PrintIdTable(IdTable& idtable, const wchar_t* in)
	{
		wchar_t* id = new wchar_t[wcslen(PARM_ID_DEFAULT_LOCATION) + wcslen(in) + wcslen(PARM_ID_DEFAULT_EXT) + 1] {};
		wcscat(id, PARM_ID_DEFAULT_LOCATION);
		wcscat(id, in);
		wcscat(id, PARM_ID_DEFAULT_EXT);

		ofstream idStream(id);
		delete[] id;
		if (!idStream.is_open())
			throw ERROR_THROW(125);



		// ИСПРАВЛЕННЫЙ ЗАГОЛОВОК - убраны лишние табуляции
		idStream << "ПЕРЕМЕННЫЕ" << endl;
		idStream << "Индекс LE\tОбласть видимости\tИдентификатор\tТип Идентификатора\tТип данных\tЗначение\tДлина строки" << endl;

		for (int i = 0; i < idtable.current_size; i++)
		{
			if (idtable.table[i]->idtype == IT::IDTYPE::V)
			{
				if (idtable.table[i]->iddatatype == IT::IDDATATYPE::INT)
				{
					idStream << setw(9) << idtable.table[i]->idxfirstLE << '\t'
						<< setw(17) << idtable.table[i]->areaOfVisibility << '\t'
						<< setw(13) << idtable.table[i]->id << '\t'
						<< setw(18) << "V" << '\t'
						<< setw(10) << "INT" << '\t'
						<< setw(8) << idtable.table[i]->value.vint << '\t'
						<< setw(12) << "-" << endl;
				}
				else if (idtable.table[i]->iddatatype == IT::IDDATATYPE::CHAR)
				{
					idStream << setw(9) << idtable.table[i]->idxfirstLE << '\t'
						<< setw(17) << idtable.table[i]->areaOfVisibility << '\t'
						<< setw(13) << idtable.table[i]->id << '\t'
						<< setw(18) << "V" << '\t'
						<< setw(10) << "CHAR" << '\t'
						<< setw(8) << "'" << (char)idtable.table[i]->value.vint << "'" << '\t'
						<< setw(12) << "-" << endl;
				}
				else if (idtable.table[i]->iddatatype == IT::IDDATATYPE::BOOL)  // ДОБАВЬТЕ ЭТО
				{
					idStream << setw(9) << idtable.table[i]->idxfirstLE << '\t'
						<< setw(17) << idtable.table[i]->areaOfVisibility << '\t'
						<< setw(13) << idtable.table[i]->id << '\t'
						<< setw(18) << "V" << '\t'
						<< setw(10) << "BOOL" << '\t'
						<< setw(8) << (idtable.table[i]->value.vbool ? "true" : "false") << '\t'
						<< setw(12) << "-" << endl;
				}
				else  // STR
				{
					idStream << setw(9) << idtable.table[i]->idxfirstLE << '\t'
						<< setw(17) << idtable.table[i]->areaOfVisibility << '\t'
						<< setw(13) << idtable.table[i]->id << '\t'
						<< setw(18) << "V" << '\t'
						<< setw(10) << "STR" << '\t'
						<< setw(8) << idtable.table[i]->value.vstr.str << '\t'
						<< setw(12) << (int)idtable.table[i]->value.vstr.len << endl;
				}
			}

			if (idtable.table[i]->idtype == IT::IDTYPE::P)
			{
				// Аналогично для параметров
			}
		}

		idStream << "\nФУНКЦИИ" << endl;
		idStream << "Индекс LE\tОбласть видимости\tИдентификатор\tТип данных возврата" << endl;
		for (int i = 0; i < idtable.current_size; i++)
		{
			if (idtable.table[i]->idtype == IT::IDTYPE::F)
			{
				if (idtable.table[i]->iddatatype == IT::IDDATATYPE::INT)
				{
					idStream << setw(9) << idtable.table[i]->idxfirstLE << '\t'
						<< setw(17) << idtable.table[i]->areaOfVisibility << '\t'
						<< setw(13) << idtable.table[i]->id << '\t'
						<< setw(19) << "INT" << endl;
				}
				else if (idtable.table[i]->iddatatype == IT::IDDATATYPE::BOOL)  // ДОБАВЬТЕ ЭТО
				{
					idStream << setw(9) << idtable.table[i]->idxfirstLE << '\t'
						<< setw(17) << idtable.table[i]->areaOfVisibility << '\t'
						<< setw(13) << idtable.table[i]->id << '\t'
						<< setw(19) << "BOOL" << endl;
				}
				else if (idtable.table[i]->iddatatype == IT::IDDATATYPE::CHAR)  // ДОБАВЬТЕ ЭТО
				{
					idStream << setw(9) << idtable.table[i]->idxfirstLE << '\t'
						<< setw(17) << idtable.table[i]->areaOfVisibility << '\t'
						<< setw(13) << idtable.table[i]->id << '\t'
						<< setw(19) << "CHAR" << endl;
				}
				else
				{
					idStream << setw(9) << idtable.table[i]->idxfirstLE << '\t'
						<< setw(17) << idtable.table[i]->areaOfVisibility << '\t'
						<< setw(13) << idtable.table[i]->id << '\t'
						<< setw(19) << "STR" << endl;
				}
			}
		}

		idStream << "\nЛИТЕРАЛЫ" << endl;
		// ИСПРАВЛЕННЫЙ ЗАГОЛОВОК - убраны лишние setw
		idStream << "Индекс LE\tИдентификатор\tТип данных\tДлина строки\tЗначение" << endl;

		for (int i = 0; i < idtable.current_size; i++)
		{
			if (idtable.table[i]->idtype == IT::IDTYPE::L)
			{
				if (idtable.table[i]->iddatatype == IT::IDDATATYPE::INT)
				{
					idStream << setw(9) << idtable.table[i]->idxfirstLE << '\t'
						<< setw(12) << idtable.table[i]->id << '\t'
						<< setw(11) << "INT" << '\t'
						<< setw(13) << "-" << '\t'
						<< idtable.table[i]->value.vint << endl;
				}
				else if (idtable.table[i]->iddatatype == IT::IDDATATYPE::CHAR)
				{
					idStream << setw(9) << idtable.table[i]->idxfirstLE << '\t'
						<< setw(12) << idtable.table[i]->id << '\t'
						<< setw(11) << "CHAR" << '\t'
						<< setw(13) << "-" << '\t'
						<< "'" << (char)idtable.table[i]->value.vint << "'" << endl;
				}
				else if (idtable.table[i]->iddatatype == IT::IDDATATYPE::BOOL)  // ДОБАВЬТЕ ЭТО
				{
					idStream << setw(9) << idtable.table[i]->idxfirstLE << '\t'
						<< setw(12) << idtable.table[i]->id << '\t'
						<< setw(11) << "BOOL" << '\t'
						<< setw(13) << "-" << '\t'
						<< (idtable.table[i]->value.vbool ? "true" : "false") << endl;
				}
				else  // STR
				{
					// ВАЖНО: для строк ограничиваем вывод, чтобы не было мусора
					string strValue = idtable.table[i]->value.vstr.str;
					// Обрезаем если слишком длинная
					if (strValue.length() > 50)
					{
						strValue = strValue.substr(0, 47) + "...";
					}

					idStream << setw(9) << idtable.table[i]->idxfirstLE << '\t'
						<< setw(12) << idtable.table[i]->id << '\t'
						<< setw(11) << "STR" << '\t'
						<< setw(13) << (int)idtable.table[i]->value.vstr.len << '\t'
						<< "\"" << strValue << "\"" << endl;
				}
			}
		}

		idStream.close();
	}
}