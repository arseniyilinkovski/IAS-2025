#include "stdafx.h"
#include "MFST.h"
#include "GRB.h"
#include "Error.h"
#include <iostream>
#include <iomanip>

//###################
int FST_TRACE_n = -1;
char rbuf[205]{}, sbuf[205]{}, lbuf[1024]{};

//###################

#pragma region Constructors_MFSTState
MFST::MFSTState::MFSTState()
{
	this->posInLent = 0;
	this->nRuleChain = -1;
	this->nRule = -1;
}

MFST::MFSTState::MFSTState(short posInLent, MFSTSTSTACK a_stack, short currentChain)
{
	this->posInLent = posInLent;
	this->st = a_stack;
	this->nRuleChain = currentChain;
}

MFST::MFSTState::MFSTState(short posInLent, MFSTSTSTACK a_stack, short currentRule, short currentChain)
{
	this->posInLent = posInLent;
	this->st = a_stack;
	this->nRule = currentRule;
	this->nRuleChain = currentChain;
}
#pragma endregion

#pragma region Constructors_MFST_Diagnosis
MFST::MFST::MFST_Diagnosis::MFST_Diagnosis()
{
	this->posInLent = -1;
	this->rc_step = RC_STEP::SURPRISE;
	this->ruleNum = -1;
	this->nrule_chain = -1;
}

MFST::MFST::MFST_Diagnosis::MFST_Diagnosis(short posInLent, RC_STEP rc_step, short ruleNum, short ruleChainNum)
{
	this->posInLent = posInLent;
	this->rc_step = rc_step;
	this->ruleNum = ruleNum;
	this->nrule_chain = ruleChainNum;
}

#pragma endregion

#pragma region Constructors_MFST
MFST::MFST::MFST()
{
	this->lenta = 0;
	this->lenta_size;
	this->currentPosInLent = 0;
}

MFST::MFST::MFST(const LT::LexTable& lexTable, GRB::Greibach grebach)
{
	this->grebach = grebach;
	this->lexTable = lexTable;
	this->lenta = new short[this->lenta_size = lexTable.current_size];

	for (int i = 0; i < this->lenta_size; i++)
		this->lenta[i] = TS(lexTable.table[i]->lexema);

	this->currentPosInLent = 0;
	this->st.push(grebach.stbottomT);
	this->st.push(grebach.startN);
	this->currentRuleChain = -1;
}
#pragma endregion

MFST::MFST::RC_STEP MFST::MFST::step(std::ostream* stream)
{
	RC_STEP rc = SURPRISE;

	if (currentPosInLent < lenta_size)
	{
		if (ISNS(st.top()))
		{
			GRB::Rule rule;
			if ((currentRule = grebach.getRule(st.top(), rule)) >= 0)
			{
				GRB::Rule::Chain chain;
				if ((currentRuleChain = rule.getNextChain(lenta[currentPosInLent], chain, currentRuleChain + 1)) >= 0)
				{
					MFST_TRACE1(stream)
						savestate(stream);
					st.pop();
					push_chain(chain);
					rc = NS_OK;
					MFST_TRACE2(stream)
				}
				else
				{
					MFST_TRACE4(stream, "TNS_NS_NORULECHAIN/NS_NORULE")
						savedDiagnosis(NS_NORULECHAIN); rc = resetstate(stream) ? NS_NORULECHAIN : NS_NORULE;
				}
			}
			else
				rc = NS_ERROR;
		}
		else if (st.top() == lenta[currentPosInLent])
		{
			currentPosInLent++;
			st.pop();
			currentRuleChain = -1;
			rc = TS_OK;
			MFST_TRACE3(stream)
		}
		else
		{
			MFST_TRACE4(stream, "TNS_NS_NORULECHAIN/NS_NORULE")
				rc = resetstate(stream) ? TS_NOK : NS_NORULECHAIN;
		}
	}
	else
	{
		rc = LENTA_END;
		MFST_TRACE4(stream, "LENTA_END")
	}

	return rc;
}

bool MFST::MFST::push_chain(GRB::Rule::Chain chain)
{
	for (int i = chain.size - 1; i >= 0; i--)
		st.push(chain.nt[i]);
	return true;
}

bool MFST::MFST::savestate(std::ostream* stream)
{
	storestate.push(MFSTState(currentPosInLent, st, currentRule, currentRuleChain));
	MFST_TRACE6(stream, "SAVESTATE:", storestate.size())
		return true;
}

bool MFST::MFST::resetstate(std::ostream* stream)
{
	bool rc = false;
	MFSTState state;

	if (rc = (storestate.size() > 0))
	{
		state = storestate.top();
		currentPosInLent = state.posInLent;
		st = state.st;
		currentRule = state.nRule;
		currentRuleChain = state.nRuleChain;
		storestate.pop();

		MFST_TRACE5(stream, "RESTATE")
			MFST_TRACE2(stream)
	}

	return rc;
}

bool MFST::MFST::savedDiagnosis(RC_STEP prc_step)
{
	bool rc = false;
	short k = 0;

	while (k < MFST_DIAGN_NUMBER && currentPosInLent <= diagnosis[k].posInLent)
		k++;

	if (rc = (k < MFST_DIAGN_NUMBER))
	{
		diagnosis[k] = MFST_Diagnosis(currentPosInLent, prc_step, currentRule, currentRuleChain);

		for (short i = k + 1; i < MFST_DIAGN_NUMBER; i++)
			diagnosis[i].posInLent = -1;
	}

	return rc;
}

bool MFST::MFST::start(std::ostream* stream)
{
	bool rc = false;
	RC_STEP rc_step = SURPRISE;
	char buf[MFST_DIAGN_MAXSIZE]{};
	rc_step = step(stream);
	while (rc_step == NS_OK || rc_step == NS_NORULECHAIN || rc_step == TS_OK || rc_step == TS_NOK)
		rc_step = step(stream);

	switch (rc_step)
	{
	case LENTA_END:
	{
		MFST_TRACE4(stream, "------>LENTA_END")
			* stream << "------------------------------------------------------------------------------------------   ------" << std::endl;
		sprintf_s(buf, MFST_DIAGN_MAXSIZE, "%d: всего строк %d, синтаксический анализ выполнен без ошибок", 0, lenta_size);
		*stream << std::setw(4) << std::left << 0 << "всего строк " << lenta_size << ", синтаксический анализ выполнен без ошибок" << std::endl;
		rc = true;
		break;
	}

	case NS_NORULE:
	{
		MFST_TRACE4(stream, "------>NS_NORULE")
			* stream << "------------------------------------------------------------------------------------------   ------" << std::endl;

		// Попробуем получить первую диагностическую ошибку (0..MFST_DIAGN_NUMBER-1)
		for (short i = 0; i < MFST_DIAGN_NUMBER; ++i)
		{
			if (diagnosis[i].posInLent >= 0)
			{
				Error::ERROR err = getDiagnosisError(i);
				// Записываем в лог (поток уже передан в start)
				*stream << std::setw(4) << std::left << err.id << ": "
					<< "Строка " << err.inext.line << ", Позиция " << err.inext.col
					<< " — " << err.message;
				if (err.inext.word[0] != '\0')
					*stream << ", Лексема: " << err.inext.word;
				*stream << std::endl;

				// Бросаем ошибку, чтобы она дошла до catch в main
				throw err;
			}
		}

		// Если диагностики нет — бросаем общую ошибку
		throw Error::geterror(0);
	}


	case NS_NORULECHAIN: MFST_TRACE4(stream, "------>NS_NORULECHAIN") break;
	case NS_ERROR: MFST_TRACE4(stream, "------>NS_ERROR") break;
	case SURPRISE: MFST_TRACE4(stream, "------>NS_SURPRISE") break;
	}

	return rc;
}

char* MFST::MFST::getCSt(char* buf)
{

	MFSTSTSTACK temp = st;
	for (int i = (signed)st.size() - 1; i >= 0; --i)
	{
		short p = temp.top();
		temp.pop();
		buf[st.size() - 1 - i] = GRB::Rule::Chain::alphabet_to_char(p);
	}

	buf[st.size()] = '\0';

	return buf;
}

char* MFST::MFST::getCLenta(char* buf, short pos, short n)
{
	short i, k = (pos + n < lenta_size) ? pos + n : lenta_size;

	for (i = pos; i < k; i++)
		buf[i - pos] = GRB::Rule::Chain::alphabet_to_char(lenta[i]);

	buf[i - pos] = '\0';
	return buf;
}

char* MFST::MFST::getDiagnosis(short n, char* buf)
{
	char* rc = new char[1] {};
	int errid = 0;
	int lpos = -1;
	if (n < MFST_DIAGN_NUMBER && (lpos = diagnosis[n].posInLent) >= 0)
	{
		errid = grebach.getRule(diagnosis[n].ruleNum).iderror;
		Error::ERROR err = Error::geterror(errid);
		sprintf_s(buf, MFST_DIAGN_MAXSIZE, "%d: строка %d,	%s", err.id, lexTable.table[lpos]->sn, err.message);
		rc = buf;
	}
	return rc;
}

void MFST::MFST::printRules(std::ostream* stream)
{
	MFSTState state;
	GRB::Rule rule;
	std::stack<MFSTState> temp = storestate;
	MFSTState* arr = new MFSTState[storestate.size()];
	for (short i = 0; i < storestate.size(); i++)
	{
		arr[i] = temp.top();
		temp.pop();
	}
	for (short i = storestate.size() - 1; i >= 0; i--)
	{
		state = arr[i];
		rule = grebach.getRule(state.nRule);
		MFST_TRACE7(stream)
	}
	delete[] arr;
}

bool MFST::MFST::saveoutputTree()
{
	MFSTState state;
	GRB::Rule rule;
	deducation.nRules = new short[deducation.stepsCount = storestate.size()];
	deducation.nChainsOfRules = new short[deducation.stepsCount];

	std::stack<MFSTState> temp = storestate;
	MFSTState* arr = new MFSTState[storestate.size()];
	for (short i = 0; i < storestate.size(); i++)
	{
		arr[i] = temp.top();
		temp.pop();
	}

	for (short i = storestate.size() - 1, j = 0; i >= 0; i--, j++)
	{
		state = arr[i];
		deducation.nRules[j] = state.nRule;
		deducation.nChainsOfRules[j] = state.nRuleChain;
	}
	delete[] arr;

	return true;
}
// Добавьте новую функцию, которая возвращает Error::ERROR для n-й диагностики
Error::ERROR MFST::MFST::getDiagnosisError(short n)
{
	Error::ERROR defaultErr = Error::geterror(0);

	int lpos = -1;
	if (n < MFST_DIAGN_NUMBER && (lpos = diagnosis[n].posInLent) >= 0)
	{
		int errid = grebach.getRule(diagnosis[n].ruleNum).iderror;

		// Получаем строку/колонку/лексему из таблицы лексем
		int line = lexTable.table[lpos]->sn;
		int col = lexTable.table[lpos]->tn;

		// Попытка получить "слово" — если есть индекс в TI (idxTI), можно взять из таблицы идентификаторов/литералов.
		// Но чаще всего lexTable хранит саму лексему в виде символа или в IT. Для простоты:
		std::string word;
		int idxTI = lexTable.table[lpos]->idxTI;
		if (idxTI != LT_TI_NULLXDX) {
			// Если есть индекс в TI, можно попытаться взять значение из IT (если доступно).
			// Здесь безопасный fallback: берем single-char lexema из lexTable
			char tmp[2] = { lexTable.table[lpos]->lexema, '\0' };
			word = tmp;
		}
		else {
			char tmp[2] = { lexTable.table[lpos]->lexema, '\0' };
			word = tmp;
		}

		// Формируем и возвращаем Error::ERROR с лексемой
		return Error::geterrorin_word(errid, line, col, word.c_str());
	}

	return defaultErr;
}
