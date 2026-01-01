#include <iostream>
#include <Windows.h>
#include <ctime>
#pragma warning(disable: 4996)


extern "C"
{
	int lenght(char* str)										//длина строки
	{
		return strlen(str);
	}

	int write_int(int p)											//вывод числа
	{
		std::cout << p << std::endl;
		return 0;
	}

	int write_str(char* str)										//вывод строки
	{
		setlocale(LC_ALL, "rus");
		std::cout << str << std::endl;
		return 0;
	}

	char* copy(char* str1, char* str2, int count) //копирование подстроки
	{
		int i;
		str1 = (char*)malloc(count);
		for (i = 0; i<count; i++) {
			str1[i] = str2[i];
		}
		str1[i] = '\0';
		
		return str1;
	}

	//-------------------------

	char* getLocalTimeAndDate() //время 
	{
		time_t now = time(0);
		char* dt = ctime(&now);
		return dt;
	}

	int powNumber(int num, int num2) //в степень
	{
		return pow(num, num2);
	}

	int random(int start, int end) //рандомное число
	{
		srand(time(NULL));
		return rand() % (end - start + 1) + start;
	}

	int factorialOfNumber(int num) //факториал числа
	{
		int res = 1;
		for (int i = 1; i <= num; i++) {
			res = res * i;
		}
		return res;
	}
	
	int squareOfNumber(int num) //корень
	{
		return sqrt(num);
	}
	int asciiCode(char c)//ASCII-код символа
	{
		return static_cast<unsigned char>(c);
	}

}