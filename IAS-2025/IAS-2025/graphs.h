#pragma once

#define FST_FUNCTION(string) FST::FST(string, LEX_FUNCTION, 9, \
FST::NODE(1, FST::RELATION('f', 1)), \
FST::NODE(1, FST::RELATION('u', 2)), \
FST::NODE(1, FST::RELATION('n', 3)), \
FST::NODE(1, FST::RELATION('c', 4)), \
FST::NODE(1, FST::RELATION('t', 5)), \
FST::NODE(1, FST::RELATION('i', 6)), \
FST::NODE(1, FST::RELATION('o', 7)), \
FST::NODE(1, FST::RELATION('n', 8)), \
FST::NODE())

#define FST_INT(string) FST::FST(string, LEX_INT, 4, \
FST::NODE(1, FST::RELATION('i', 1)), \
FST::NODE(1, FST::RELATION('n', 2)), \
FST::NODE(1, FST::RELATION('t', 3)), \
FST::NODE())

#define FST_IF(string) FST::FST(string, LEX_IF, 3, \
FST::NODE(1, FST::RELATION('i', 1)), \
FST::NODE(1, FST::RELATION('f', 2)), \
FST::NODE())

#define FST_MAIN(string) FST::FST(string, LEX_MAIN, 5, \
FST::NODE(1, FST::RELATION('m', 1)), \
FST::NODE(1, FST::RELATION('a', 2)), \
FST::NODE(1, FST::RELATION('i', 3)), \
FST::NODE(1, FST::RELATION('n', 4)), \
FST::NODE())

#define FST_WRITE(string) FST::FST(string, LEX_WRITE, 7, \
FST::NODE(1, FST::RELATION('o', 1)), \
FST::NODE(1, FST::RELATION('u', 2)), \
FST::NODE(1, FST::RELATION('t', 3)), \
FST::NODE(1, FST::RELATION('p', 4)), \
FST::NODE(1, FST::RELATION('u', 5)), \
FST::NODE(1, FST::RELATION('t', 6)), \
FST::NODE())

#define FST_RETURN(string) FST::FST(string, LEX_RETURN, 7, \
FST::NODE(1, FST::RELATION('r', 1)), \
FST::NODE(1, FST::RELATION('e', 2)), \
FST::NODE(1, FST::RELATION('t', 3)), \
FST::NODE(1, FST::RELATION('u', 4)), \
FST::NODE(1, FST::RELATION('r', 5)), \
FST::NODE(1, FST::RELATION('n', 6)), \
FST::NODE())

#define FST_STR(string) FST::FST(string, LEX_STR, 4, \
FST::NODE(1, FST::RELATION('s', 1)), \
FST::NODE(1, FST::RELATION('t', 2)), \
FST::NODE(1, FST::RELATION('r', 3)), \
FST::NODE())

#define FST_COMMENT(string) FST::FST(string, LEX_COMMENT,2, \
FST::NODE(1, FST::RELATION('#', 1)), \
FST::NODE())

#define FST_OPERATOR(string) FST::FST(string, LEX_OPERATOR, 1, \
FST::NODE(5, \
FST::RELATION('+', 0), \
FST::RELATION('-', 0), \
FST::RELATION('*', 0), \
FST::RELATION('/', 0), \
FST::RELATION('%', 0)), \
FST::NODE())

#define FST_LITERAL_S(string) FST::FST(string, LEX_LITERAL, 3, \
FST::NODE(1, FST::RELATION('\"', 1)), \
FST::NODE(145, \
FST::RELATION(' ', 1), \
FST::RELATION('a', 1), \
FST::RELATION('b', 1), \
FST::RELATION('c', 1), \
FST::RELATION('d', 1), \
FST::RELATION('e', 1), \
FST::RELATION('f', 1), \
FST::RELATION('g', 1), \
FST::RELATION('h', 1), \
FST::RELATION('i', 1), \
FST::RELATION('j', 1), \
FST::RELATION('k', 1), \
FST::RELATION('l', 1), \
FST::RELATION('m', 1), \
FST::RELATION('n', 1), \
FST::RELATION('o', 1), \
FST::RELATION('p', 1), \
FST::RELATION('q', 1), \
FST::RELATION('r', 1), \
FST::RELATION('s', 1), \
FST::RELATION('t', 1), \
FST::RELATION('u', 1), \
FST::RELATION('v', 1), \
FST::RELATION('w', 1), \
FST::RELATION('x', 1), \
FST::RELATION('y', 1), \
FST::RELATION('z', 1), \
FST::RELATION('A', 1), \
FST::RELATION('B', 1), \
FST::RELATION('C', 1), \
FST::RELATION('D', 1), \
FST::RELATION('E', 1), \
FST::RELATION('F', 1), \
FST::RELATION('G', 1), \
FST::RELATION('H', 1), \
FST::RELATION('I', 1), \
FST::RELATION('J', 1), \
FST::RELATION('K', 1), \
FST::RELATION('L', 1), \
FST::RELATION('M', 1), \
FST::RELATION('N', 1), \
FST::RELATION('O', 1), \
FST::RELATION('P', 1), \
FST::RELATION('Q', 1), \
FST::RELATION('R', 1), \
FST::RELATION('S', 1), \
FST::RELATION('T', 1), \
FST::RELATION('U', 1), \
FST::RELATION('V', 1), \
FST::RELATION('W', 1), \
FST::RELATION('X', 1), \
FST::RELATION('Y', 1), \
FST::RELATION('Z', 1), \
FST::RELATION('0', 1), \
FST::RELATION('1', 1), \
FST::RELATION('2', 1), \
FST::RELATION('3', 1), \
FST::RELATION('4', 1), \
FST::RELATION('5', 1), \
FST::RELATION('6', 1), \
FST::RELATION('7', 1), \
FST::RELATION('8', 1), \
FST::RELATION('9', 1), \
FST::RELATION('а', 1), \
FST::RELATION('б', 1), \
FST::RELATION('в', 1), \
FST::RELATION('г', 1), \
FST::RELATION('д', 1), \
FST::RELATION('е', 1), \
FST::RELATION('ё', 1), \
FST::RELATION('ж', 1), \
FST::RELATION('з', 1), \
FST::RELATION('и', 1), \
FST::RELATION('й', 1), \
FST::RELATION('к', 1), \
FST::RELATION('л', 1), \
FST::RELATION('м', 1), \
FST::RELATION('н', 1), \
FST::RELATION('о', 1), \
FST::RELATION('п', 1), \
FST::RELATION('р', 1), \
FST::RELATION('с', 1), \
FST::RELATION('т', 1), \
FST::RELATION('у', 1), \
FST::RELATION('ф', 1), \
FST::RELATION('х', 1), \
FST::RELATION('ц', 1), \
FST::RELATION('ч', 1), \
FST::RELATION('ш', 1), \
FST::RELATION('щ', 1), \
FST::RELATION('ь', 1), \
FST::RELATION('ъ', 1), \
FST::RELATION('ы', 1), \
FST::RELATION('э', 1), \
FST::RELATION('ю', 1), \
FST::RELATION('я', 1), \
FST::RELATION('А', 1), \
FST::RELATION('Б', 1), \
FST::RELATION('В', 1), \
FST::RELATION('Г', 1), \
FST::RELATION('Д', 1), \
FST::RELATION('Е', 1), \
FST::RELATION('Ё', 1), \
FST::RELATION('Ж', 1), \
FST::RELATION('З', 1), \
FST::RELATION('И', 1), \
FST::RELATION('Й', 1), \
FST::RELATION('К', 1), \
FST::RELATION('Л', 1), \
FST::RELATION('М', 1), \
FST::RELATION('Н', 1), \
FST::RELATION('О', 1), \
FST::RELATION('П', 1), \
FST::RELATION('Р', 1), \
FST::RELATION('С', 1), \
FST::RELATION('Т', 1), \
FST::RELATION('У', 1), \
FST::RELATION('Ф', 1), \
FST::RELATION('Х', 1), \
FST::RELATION('Ц', 1), \
FST::RELATION('Ч', 1), \
FST::RELATION('Ш', 1), \
FST::RELATION('Щ', 1), \
FST::RELATION('Ь', 1), \
FST::RELATION('Ъ', 1), \
FST::RELATION('Ы', 1), \
FST::RELATION('Э', 1), \
FST::RELATION('Ю', 1), \
FST::RELATION('Я', 1), \
FST::RELATION('=', 1), \
FST::RELATION('+', 1), \
FST::RELATION('-', 1), \
FST::RELATION('*', 1), \
FST::RELATION('/', 1), \
FST::RELATION('(', 1), \
FST::RELATION(')', 1), \
FST::RELATION('{', 1), \
FST::RELATION('}', 1), \
FST::RELATION(';', 1), \
FST::RELATION(':', 1), \
FST::RELATION(',', 1), \
FST::RELATION('?', 1), \
FST::RELATION('!', 1), \
FST::RELATION('_', 1), \
FST::RELATION('\"', 2)), \
FST::NODE())

#define FST_LITERAL_I(string) FST::FST(string, LEX_LITERAL, 1, \
FST::NODE(10, \
FST::RELATION('0', 0), \
FST::RELATION('1', 0), \
FST::RELATION('2', 0), \
FST::RELATION('3', 0), \
FST::RELATION('4', 0), \
FST::RELATION('5', 0), \
FST::RELATION('6', 0), \
FST::RELATION('7', 0), \
FST::RELATION('8', 0), \
FST::RELATION('9', 0)), \
FST::NODE())

#define FST_LITERAL_MINUS_I(string) FST::FST(string, LEX_LITERAL, 3, \
FST::NODE(1, FST::RELATION('-', 1)), \
FST::NODE(10, \
FST::RELATION('0', 2), \
FST::RELATION('1', 2), \
FST::RELATION('2', 2), \
FST::RELATION('3', 2), \
FST::RELATION('4', 2), \
FST::RELATION('5', 2), \
FST::RELATION('6', 2), \
FST::RELATION('7', 2), \
FST::RELATION('8', 2), \
FST::RELATION('9', 2)), \
FST::NODE(10, \
FST::RELATION('0', 2), \
FST::RELATION('1', 2), \
FST::RELATION('2', 2), \
FST::RELATION('3', 2), \
FST::RELATION('4', 2), \
FST::RELATION('5', 2), \
FST::RELATION('6', 2), \
FST::RELATION('7', 2), \
FST::RELATION('8', 2), \
FST::RELATION('9', 2)), \
FST::NODE())

#define FST_ID(string) FST::FST(string, LEX_ID, 1, \
FST::NODE(53, \
FST::RELATION('A', 0), \
FST::RELATION('B', 0), \
FST::RELATION('C', 0), \
FST::RELATION('D', 0), \
FST::RELATION('E', 0), \
FST::RELATION('F', 0), \
FST::RELATION('G', 0), \
FST::RELATION('H', 0), \
FST::RELATION('I', 0), \
FST::RELATION('J', 0), \
FST::RELATION('K', 0), \
FST::RELATION('L', 0), \
FST::RELATION('M', 0), \
FST::RELATION('N', 0), \
FST::RELATION('O', 0), \
FST::RELATION('P', 0), \
FST::RELATION('Q', 0), \
FST::RELATION('R', 0), \
FST::RELATION('S', 0), \
FST::RELATION('T', 0), \
FST::RELATION('U', 0), \
FST::RELATION('V', 0), \
FST::RELATION('W', 0), \
FST::RELATION('X', 0), \
FST::RELATION('Y', 0), \
FST::RELATION('Z', 0), \
FST::RELATION('a', 0), \
FST::RELATION('b', 0), \
FST::RELATION('c', 0), \
FST::RELATION('d', 0), \
FST::RELATION('e', 0), \
FST::RELATION('f', 0), \
FST::RELATION('g', 0), \
FST::RELATION('h', 0), \
FST::RELATION('i', 0), \
FST::RELATION('j', 0), \
FST::RELATION('k', 0), \
FST::RELATION('l', 0), \
FST::RELATION('m', 0), \
FST::RELATION('n', 0), \
FST::RELATION('o', 0), \
FST::RELATION('p', 0), \
FST::RELATION('q', 0), \
FST::RELATION('r', 0), \
FST::RELATION('s', 0), \
FST::RELATION('t', 0), \
FST::RELATION('u', 0), \
FST::RELATION('v', 0), \
FST::RELATION('w', 0), \
FST::RELATION('x', 0), \
FST::RELATION('y', 0), \
FST::RELATION('z', 0), \
FST::RELATION('_', 0)), \
FST::NODE())

#define FST_PARAM(string) FST::FST(string, LEX_PARAM, 6, \
FST::NODE(1, FST::RELATION('p', 1)), \
FST::NODE(1, FST::RELATION('a', 2)), \
FST::NODE(1, FST::RELATION('r', 3)), \
FST::NODE(1, FST::RELATION('a', 4)), \
FST::NODE(1, FST::RELATION('m', 5)), \
FST::NODE())

#define FST_VAR(string) FST::FST(string, LEX_VAR, 4, \
FST::NODE(1, FST::RELATION('v', 1)), \
FST::NODE(1, FST::RELATION('a', 2)), \
FST::NODE(1, FST::RELATION('r', 3)), \
FST::NODE())

#define FST_REPEAT(string) FST::FST(string, LEX_REPEAT, 7, \
FST::NODE(1, FST::RELATION('r', 1)), \
FST::NODE(1, FST::RELATION('e', 2)), \
FST::NODE(1, FST::RELATION('p', 3)), \
FST::NODE(1, FST::RELATION('e', 4)), \
FST::NODE(1, FST::RELATION('a', 5)), \
FST::NODE(1, FST::RELATION('t', 6)), \
FST::NODE())

#define FST_LITERAL_I16(string) FST::FST(string, LEX_LITERAL, 3, \
FST::NODE(1, FST::RELATION('h', 1)), \
FST::NODE(22, \
FST::RELATION('0', 2), \
FST::RELATION('1', 2), \
FST::RELATION('2', 2), \
FST::RELATION('3', 2), \
FST::RELATION('4', 2), \
FST::RELATION('5', 2), \
FST::RELATION('6', 2), \
FST::RELATION('7', 2), \
FST::RELATION('8', 2), \
FST::RELATION('9', 2), \
FST::RELATION('A', 2), \
FST::RELATION('B', 2), \
FST::RELATION('C', 2), \
FST::RELATION('D', 2), \
FST::RELATION('E', 2), \
FST::RELATION('F', 2), \
FST::RELATION('a', 2), \
FST::RELATION('b', 2), \
FST::RELATION('c', 2), \
FST::RELATION('d', 2), \
FST::RELATION('e', 2), \
FST::RELATION('f', 2)), \
FST::NODE(22, \
FST::RELATION('0', 2), \
FST::RELATION('1', 2), \
FST::RELATION('2', 2), \
FST::RELATION('3', 2), \
FST::RELATION('4', 2), \
FST::RELATION('5', 2), \
FST::RELATION('6', 2), \
FST::RELATION('7', 2), \
FST::RELATION('8', 2), \
FST::RELATION('9', 2), \
FST::RELATION('A', 2), \
FST::RELATION('B', 2), \
FST::RELATION('C', 2), \
FST::RELATION('D', 2), \
FST::RELATION('E', 2), \
FST::RELATION('F', 2), \
FST::RELATION('a', 2), \
FST::RELATION('b', 2), \
FST::RELATION('c', 2), \
FST::RELATION('d', 2), \
FST::RELATION('e', 2), \
FST::RELATION('f', 2)), \
FST::NODE())

#define FST_LITERAL_I8(string) FST::FST(string, LEX_LITERAL, 3, \
FST::NODE(1, FST::RELATION('o', 1)), \
FST::NODE(8, \
FST::RELATION('0', 2), \
FST::RELATION('1', 2), \
FST::RELATION('2', 2), \
FST::RELATION('3', 2), \
FST::RELATION('4', 2), \
FST::RELATION('5', 2), \
FST::RELATION('6', 2), \
FST::RELATION('7', 2)), \
FST::NODE(8, \
FST::RELATION('0', 2), \
FST::RELATION('1', 2), \
FST::RELATION('2', 2), \
FST::RELATION('3', 2), \
FST::RELATION('4', 2), \
FST::RELATION('5', 2), \
FST::RELATION('6', 2), \
FST::RELATION('7', 2)), \
FST::NODE())

#define FST_LITERAL_I2(string) FST::FST(string, LEX_LITERAL, 3, \
FST::NODE(1, FST::RELATION('b', 1)), \
FST::NODE(2, \
FST::RELATION('0', 2), \
FST::RELATION('1', 2)), \
FST::NODE(2, \
FST::RELATION('0', 2), \
FST::RELATION('1', 2)), \
FST::NODE())

#define FST_THEN(string) FST::FST(string, LEX_THEN, 5, \
FST::NODE(1, FST::RELATION('t', 1)), \
FST::NODE(1, FST::RELATION('h', 2)), \
FST::NODE(1, FST::RELATION('e', 3)), \
FST::NODE(1, FST::RELATION('n', 4)), \
FST::NODE())

#define FST_ELSE(string) FST::FST(string, LEX_ELSE, 5, \
FST::NODE(1, FST::RELATION('e', 1)), \
FST::NODE(1, FST::RELATION('l', 2)), \
FST::NODE(1, FST::RELATION('s', 3)), \
FST::NODE(1, FST::RELATION('e', 4)), \
FST::NODE())

#define FST_CHAR(string) FST::FST(string, LEX_CHAR, 5, \
FST::NODE(1, FST::RELATION('c', 1)), \
FST::NODE(1, FST::RELATION('h', 2)), \
FST::NODE(1, FST::RELATION('a', 3)), \
FST::NODE(1, FST::RELATION('r', 4)), \
FST::NODE())


#define FST_LITERAL_C(string) FST::FST(string, LEX_CHAR_LITERAL, 4, \
FST::NODE(1, FST::RELATION('\'', 1)), \
FST::NODE(128, \
    FST::RELATION('a', 2), FST::RELATION('b', 2), FST::RELATION('c', 2), \
    FST::RELATION('d', 2), FST::RELATION('e', 2), FST::RELATION('f', 2), \
    FST::RELATION('g', 2), FST::RELATION('h', 2), FST::RELATION('i', 2), \
    FST::RELATION('j', 2), FST::RELATION('k', 2), FST::RELATION('l', 2), \
    FST::RELATION('m', 2), FST::RELATION('n', 2), FST::RELATION('o', 2), \
    FST::RELATION('p', 2), FST::RELATION('q', 2), FST::RELATION('r', 2), \
    FST::RELATION('s', 2), FST::RELATION('t', 2), FST::RELATION('u', 2), \
    FST::RELATION('v', 2), FST::RELATION('w', 2), FST::RELATION('x', 2), \
    FST::RELATION('y', 2), FST::RELATION('z', 2), \
    FST::RELATION('A', 2), FST::RELATION('B', 2), FST::RELATION('C', 2), \
    FST::RELATION('D', 2), FST::RELATION('E', 2), FST::RELATION('F', 2), \
    FST::RELATION('G', 2), FST::RELATION('H', 2), FST::RELATION('I', 2), \
    FST::RELATION('J', 2), FST::RELATION('K', 2), FST::RELATION('L', 2), \
    FST::RELATION('M', 2), FST::RELATION('N', 2), FST::RELATION('O', 2), \
    FST::RELATION('P', 2), FST::RELATION('Q', 2), FST::RELATION('R', 2), \
    FST::RELATION('S', 2), FST::RELATION('T', 2), FST::RELATION('U', 2), \
    FST::RELATION('V', 2), FST::RELATION('W', 2), FST::RELATION('X', 2), \
    FST::RELATION('Y', 2), FST::RELATION('Z', 2), \
    FST::RELATION('0', 2), FST::RELATION('1', 2), FST::RELATION('2', 2), \
    FST::RELATION('3', 2), FST::RELATION('4', 2), FST::RELATION('5', 2), \
    FST::RELATION('6', 2), FST::RELATION('7', 2), FST::RELATION('8', 2), \
    FST::RELATION('9', 2), \
    FST::RELATION('!', 2), FST::RELATION('@', 2), FST::RELATION('#', 2), \
    FST::RELATION('$', 2), FST::RELATION('%', 2), FST::RELATION('^', 2), \
    FST::RELATION('&', 2), FST::RELATION('*', 2), FST::RELATION('(', 2), \
    FST::RELATION(')', 2), FST::RELATION('-', 2), FST::RELATION('_', 2), \
    FST::RELATION('=', 2), FST::RELATION('+', 2), FST::RELATION('[', 2), \
    FST::RELATION(']', 2), FST::RELATION('{', 2), FST::RELATION('}', 2), \
    FST::RELATION('|', 2), FST::RELATION('\\', 2), FST::RELATION(';', 2), \
    FST::RELATION(':', 2), FST::RELATION(',', 2), FST::RELATION('.', 2), \
    FST::RELATION('<', 2), FST::RELATION('>', 2), FST::RELATION('/', 2), \
    FST::RELATION('?', 2), FST::RELATION(' ', 2)), \
FST::NODE(1, FST::RELATION('\'', 3)), \
FST::NODE())

#define FST_SHIFT_LEFT(string) FST::FST(string, LEX_SHIFT_LEFT, 2, \
FST::NODE(1, FST::RELATION('<', 1)), \
FST::NODE(1, FST::RELATION('<', 0)), \
FST::NODE())

#define FST_SHIFT_RIGHT(string) FST::FST(string, LEX_SHIFT_RIGHT, 2, \
FST::NODE(1, FST::RELATION('>', 1)), \
FST::NODE(1, FST::RELATION('>', 0)), \
FST::NODE())

#define FST_BOOL(string) FST::FST(string, LEX_BOOL, 5, \
FST::NODE(1, FST::RELATION('b', 1)), \
FST::NODE(1, FST::RELATION('o', 2)), \
FST::NODE(1, FST::RELATION('o', 3)), \
FST::NODE(1, FST::RELATION('l', 4)), \
FST::NODE())

// FST для литерала true
#define FST_TRUE(string) FST::FST(string, LEX_BOOL_LITERAL, 5, \
FST::NODE(1, FST::RELATION('t', 1)), \
FST::NODE(1, FST::RELATION('r', 2)), \
FST::NODE(1, FST::RELATION('u', 3)), \
FST::NODE(1, FST::RELATION('e', 4)), \
FST::NODE())

// FST для литерала false
#define FST_FALSE(string) FST::FST(string, LEX_BOOL_LITERAL, 6, \
FST::NODE(1, FST::RELATION('f', 1)), \
FST::NODE(1, FST::RELATION('a', 2)), \
FST::NODE(1, FST::RELATION('l', 3)), \
FST::NODE(1, FST::RELATION('s', 4)), \
FST::NODE(1, FST::RELATION('e', 5)), \
FST::NODE())