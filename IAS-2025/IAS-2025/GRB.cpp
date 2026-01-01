#include "stdafx.h"
#include "GRB.h"
#include <string.h>
#define GRB_ERROR_SERIES 600

namespace GRB
{
#define NS(n) Rule::Chain::N(n)
#define TS(n) Rule::Chain::T(n)

	Greibach greibach(NS('S'), TS('$'), 13,
		Rule(NS('S'), GRB_ERROR_SERIES + 0, // Неверная структура программы
			5,	// m{N}S | tfi(F){NrU;}S | cfi(F){NrU;}S | пустой переход
			Rule::Chain(5, TS('m'), TS('{'), NS('N'), TS('}'), NS('S')),
			Rule::Chain(13, TS('t'), TS('f'), TS('i'), TS('('), NS('F'), TS(')'), TS('{'), NS('N'), TS('r'), NS('U'), TS(';'), TS('}'), NS('S')),
			// Добавлено правило для функций типа char (c)
			Rule::Chain(13, TS('c'), TS('f'), TS('i'), TS('('), NS('F'), TS(')'), TS('{'), NS('N'), TS('r'), NS('U'), TS(';'), TS('}'), NS('S')),
			Rule::Chain(13, TS('x'), TS('f'), TS('i'), TS('('), NS('F'), TS(')'), TS('{'), NS('N'), TS('r'), NS('U'), TS(';'), TS('}'), NS('S')),
			Rule::Chain()
		),
		Rule(NS('N'), GRB_ERROR_SERIES + 1, 9,
			Rule::Chain(),  // ← ПУСТОЕ ПРАВИЛО ПЕРВЫМ!
			Rule::Chain(4, TS('t'), NS('Y'), TS(';'), NS('N')),
			Rule::Chain(4, TS('c'), NS('Y'), TS(';'), NS('N')),
			Rule::Chain(5, TS('i'), TS('='), NS('E'), TS(';'), NS('N')),
			Rule::Chain(4, TS('x'), NS('Y'), TS(';'), NS('N')),
			Rule::Chain(4, TS('w'), NS('E'), TS(';'), NS('N')),
			Rule::Chain(4, TS('~'), NS('K'), NS('Z'), NS('N')),
			Rule::Chain(6, TS('~'), TS('('), TS('l'), TS(')'), NS('Z'), NS('N')),
			Rule::Chain(4, TS('?'), NS('K'), NS('J'), NS('N')),
			Rule::Chain(4, TS('i'), NS('M'), TS(';'), NS('N'))
		),
		Rule(NS('E'), GRB_ERROR_SERIES + 2,		// Ошибка в выражении
			6, // iM | lM | kM | (E)M | i(W)M
			Rule::Chain(2, TS('i'), NS('M')),
			Rule::Chain(2, TS('l'), NS('M')),
			// Добавлено для символьного литерала (k)
			Rule::Chain(2, TS('o'), NS('M')),
			Rule::Chain(2, TS('k'), NS('M')),
			Rule::Chain(4, TS('('), NS('E'), TS(')'), NS('M')),
			Rule::Chain(5, TS('i'), TS('('), NS('W'), TS(')'), NS('M'))
		),
		Rule(NS('M'), GRB_ERROR_SERIES + 2, 4,
			Rule::Chain(2, TS('#'), NS('E')),
			Rule::Chain(2, TS('b'), NS('E')),  // <<
			Rule::Chain(2, TS('n'), NS('E')),  // >>
			Rule::Chain()
		),
		Rule(NS('F'), GRB_ERROR_SERIES + 3,		// Ошибка в параметрах при определении функции
			5, // tP | cP | tP,F | cP,F
			Rule::Chain(2, TS('t'), NS('P')),
			// Добавлено для параметров типа char
			Rule::Chain(2, TS('c'), NS('P')),
			Rule::Chain(4, TS('t'), NS('P'), TS(','), NS('F')),
			// Добавлено для параметров типа char (продолжение списка)
			Rule::Chain(4, TS('c'), NS('P'), TS(','), NS('F')),
			Rule::Chain(4, TS('x'), NS('P'), TS(','), NS('F'))
		),
		Rule(NS('P'), GRB_ERROR_SERIES + 3,		// Ошибка в параметрах при определении функции
			1, // pi
			Rule::Chain(2, TS('p'), TS('i'))
		),
		Rule(NS('W'), GRB_ERROR_SERIES + 4,		// Ошибка в параметрах при вызове функции
			9, // i | l | k | i,W | l,W | k,W
			Rule::Chain(1, TS('i')),
			Rule::Chain(1, TS('l')),
			// Добавлено для char литерала (k)
			Rule::Chain(1, TS('k')),
			Rule::Chain(1, TS('o')),
			Rule::Chain(3, TS('i'), TS(','), NS('W')),
			Rule::Chain(3, TS('l'), TS(','), NS('W')),
			// Добавлено для char литерала в списке (k)
			Rule::Chain(3, TS('k'), TS(','), NS('W')),
			Rule::Chain(3, TS('o'), TS(','), NS('W')),
			Rule::Chain()
		),
		Rule(NS('U'), GRB_ERROR_SERIES + 5, 	// Только литерал или идентификатор
			11,	// l | k | i ...
			Rule::Chain(1, TS('l')),
			// Добавлено k
			Rule::Chain(1, TS('k')),
			Rule::Chain(1, TS('i')),
			Rule::Chain(2, TS('l'), NS('U')),
			Rule::Chain(2, TS('i'), NS('U')),
			Rule::Chain(3, TS('i'), TS(','), NS('U')),
			Rule::Chain(3, TS('l'), TS(','), NS('U')),
			// Добавлено k в списки
			Rule::Chain(3, TS('k'), TS(','), NS('U')),
			Rule::Chain(2, TS('#'), TS('l')),
			// Добавлено k с оператором
			Rule::Chain(2, TS('#'), TS('k')),
			Rule::Chain(3, TS('#'), TS('l'), NS('U')),
			Rule::Chain(4, TS('#'), TS('l'), TS(','), NS('U'))
		),
		Rule(NS('J'), GRB_ERROR_SERIES + 6, 	// Ошибка при условном переходе
			2,	// :{N} | :{N}!{N}
			Rule::Chain(4, TS(':'), TS('{'), NS('N'), TS('}')),
			Rule::Chain(8, TS(':'), TS('{'), NS('N'), TS('}'), TS('!'), TS('{'), NS('N'), TS('}'))
		),
		Rule(NS('Y'), GRB_ERROR_SERIES + 7, 	// Ошибка при определении переменной
			5,	// vi || vi = L
			Rule::Chain(2, TS('v'), TS('i')),
			Rule::Chain(4, TS('v'), TS('i'), TS('='), NS('L')),
			Rule::Chain(5, TS('v'), TS('i'), TS('='), NS('E'), NS('M')), // для выражений: vi = a >> 3
			Rule::Chain(4, TS('v'), TS('i'), TS('='), TS('k')),
			Rule::Chain(4, TS('v'), TS('i'), TS('='), TS('o'))
		),

		Rule(NS('K'), GRB_ERROR_SERIES + 8, 	// Ошибка при определении условия перехода
			4,	// (U<U) | (U>U)
			Rule::Chain(5, TS('('), NS('U'), TS('<'), NS('U'), TS(')')),
			Rule::Chain(5, TS('('), NS('U'), TS('>'), NS('U'), TS(')')),
			Rule::Chain(5, TS('('), NS('U'), TS('&'), NS('U'), TS(')')),
			Rule::Chain(5, TS('('), NS('U'), TS('^'), NS('U'), TS(')'))
		),
		Rule(NS('L'), GRB_ERROR_SERIES + 9, 18, // увеличиваем с 12 до 14
			Rule::Chain(1, TS('i')),
			Rule::Chain(1, TS('l')),
			Rule::Chain(1, TS('k')),
			Rule::Chain(1, TS('o')),
			Rule::Chain(4, TS('i'), TS('('), NS('U'), TS(')')),
			Rule::Chain(3, TS('i'), TS('('), TS(')')),
			Rule::Chain(2, TS('#'), TS('i')),
			Rule::Chain(2, TS('#'), TS('l')),
			Rule::Chain(2, TS('#'), TS('k')),
			Rule::Chain(2, TS('#'), TS('o')),
			Rule::Chain(3, TS('#'), TS('i'), NS('L')),
			Rule::Chain(3, TS('#'), TS('l'), NS('L')),
			Rule::Chain(3, TS('#'), TS('k'), NS('L')),
			Rule::Chain(3, TS('#'), TS('o'), NS('L')),
			Rule::Chain(2, TS('i'), NS('L')),
			Rule::Chain(2, TS('l'), NS('L')),
			Rule::Chain(2, TS('o'), NS('L')),
			Rule::Chain(3, NS('E'), NS('M'))  // ← ДОБАВИЛИ: выражение с оператором
		),
		Rule(NS('Z'), GRB_ERROR_SERIES + 10, 	//ошибка в теле цикла
			1,
			Rule::Chain(3, TS('{'), NS('N'), TS('}'))
		)
	);


	Rule::Chain::Chain(short size, GRBALPHABET s, ...)
	{
		nt = new GRBALPHABET[this->size = size];

		int* ptr = (int*)&s;
		for (short i = 0; i < size; i++)
			nt[i] = (GRBALPHABET)ptr[i];
	}

	Rule::Rule(GRBALPHABET nn, int iderror, short size, Chain c, ...)
	{
		this->nn = nn;
		this->iderror = iderror;
		this->chains = new Chain[this->size = size];

		Chain* ptr = &c;
		for (int i = 0; i < size; i++)
			this->chains[i] = ptr[i];
	}

	Greibach::Greibach(GRBALPHABET startN, GRBALPHABET stbottomT, short size, Rule r, ...)
	{
		this->startN = startN;
		this->stbottomT = stbottomT;
		this->rules = new Rule[this->size = size];
		Rule* ptr = &r;
		for (int i = 0; i < size; i++)
			rules[i] = ptr[i];
	}

	short Greibach::getRule(GRBALPHABET nn, Rule& rule)
	{
		short rc = -1, k = 0;

		while (k < size && rules[k].nn != nn)
			k++;

		if (k < size)
			rule = rules[rc = k];

		return rc;
	}

	Rule Greibach::getRule(short n)
	{
		Rule rc;
		if (n < size)
			rc = rules[n];
		return rc;
	}

	char* Rule::getCRule(char* b, short nchain)
	{
		char buf[200];
		b[0] = Chain::alphabet_to_char(nn);
		b[1] = '-';
		b[2] = '>';
		b[3] = 0;
		chains[nchain].getCChain(buf);
		strcat_s(b, sizeof(buf) + 5, buf);

		return b;
	}

	short Rule::getNextChain(GRBALPHABET t, Rule::Chain& pchain, short j)
	{
		short rc = -1;

		while (j < size && chains[j].nt[0] != t && chains[j].nt[0] != 0)
			j++;

		rc = (j < size ? j : -1);
		if (rc >= 0)
			pchain = chains[rc];
		return rc;
	}

	char* Rule::Chain::getCChain(char* b)
	{
		for (int i = 0; i < size; i++)
			b[i] = Chain::alphabet_to_char(nt[i]);
		b[size] = 0x00;
		return b;
	}

	Greibach getGreibach() { return greibach; }
}