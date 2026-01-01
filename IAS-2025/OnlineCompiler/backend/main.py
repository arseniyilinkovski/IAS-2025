import os
import subprocess
import shutil
from pathlib import Path
from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse
from pydantic import BaseModel
import logging

# Настройка логирования
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Создаем экземпляр FastAPI
app = FastAPI(title="IAS-2025 Online Compiler API", version="1.0.0")

# Настройка CORS
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # В продакшене укажите конкретные домены
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


# Модели данных
class CompileRequest(BaseModel):
    code: str
    language: str = "myLang"
    

class ExecuteRequest(BaseModel):
    code: str


class CompileResponse(BaseModel):
    success: bool
    asm_code: str
    message: str
    output: str = ""


class ExecuteResponse(BaseModel):
    success: bool
    output: str
    message: str


# Конфигурация
BASE_DIR = Path(__file__).parent
TEMP_DIR = BASE_DIR / "temp"
EXE_PATH = BASE_DIR.parent / "IAS-2025.exe"  # Или укажите полный путь

# Создаем временную директорию
TEMP_DIR.mkdir(exist_ok=True)


def cleanup_temp_files():
    """Очистка временных файлов"""
    try:
        for file in TEMP_DIR.glob("*"):
            if file.is_file():
                file.unlink()
        logger.info("Временные файлы очищены")
    except Exception as e:
        logger.error(f"Ошибка при очистке временных файлов: {e}")


@app.on_event("startup")
async def startup_event():
    """Действия при запуске приложения"""
    logger.info("Запуск IAS-2025 Compiler Backend")
    logger.info(f"Путь к транслятору: {EXE_PATH}")

    # Проверяем существование транслятора
    if not EXE_PATH.exists():
        logger.warning(f"Транслятор {EXE_PATH} не найден!")

    # Очищаем временные файлы при запуске
    cleanup_temp_files()


@app.on_event("shutdown")
async def shutdown_event():
    """Действия при остановке приложения"""
    logger.info("Остановка IAS-2025 Compiler Backend")
    cleanup_temp_files()


@app.get("/")
async def root():
    """Корневой эндпоинт"""
    return {
        "message": "IAS-2025 Online Compiler API",
        "version": "1.0.0",
        "endpoints": {
            "compile": "/api/compile",
            "execute": "/api/execute",
            "health": "/api/health"
        }
    }


@app.get("/api/health")
async def health_check():
    """Проверка состояния сервера"""
    translator_exists = EXE_PATH.exists()

    # Проверяем доступность EXE файла
    exe_access = False
    if translator_exists:
        try:
            # Проверяем, можем ли прочитать файл
            with open(EXE_PATH, 'rb') as f:
                # Просто проверяем доступность файла
                pass
            exe_access = True
        except Exception as e:
            logger.error(f"Ошибка доступа к транслятору: {e}")
            exe_access = False

    return {
        "status": "healthy",
        "translator_available": translator_exists and exe_access,
        "translator_exists": translator_exists,
        "exe_accessible": exe_access,
        "temp_directory": str(TEMP_DIR),
        "translator_path": str(EXE_PATH)
    }


@app.post("/api/compile", response_model=CompileResponse)
async def compile_code(request: CompileRequest):
    """Трансляция кода из IAS-2025 в Assembler"""

    # Проверяем наличие транслятора
    if not EXE_PATH.exists():
        return JSONResponse(
            status_code=503,
            content={
                "success": False,
                "asm_code": "",
                "message": "Транслятор IAS-2025.exe не найден",
                "output": ""
            }
        )

    # Проверяем наличие кода
    if not request.code.strip():
        raise HTTPException(
            status_code=400,
            detail="Код не может быть пустым"
        )

    try:
        # Создаем уникальное имя для временных файлов
        import uuid
        file_id = str(uuid.uuid4())[:8]
        input_file = TEMP_DIR / f"code_{file_id}.txt"
        asm_file = TEMP_DIR / f"code_{file_id}.txt.asm"

        # Сохраняем код в файл (ANSI кодировка)
        # Используем cp1251 для ANSI с поддержкой кириллицы
        code_text = request.code
        try:
            with open(input_file, "w", encoding="cp1251") as f:
                f.write(code_text)
            logger.info(f"Сохранен код в файл: {input_file} (размер: {len(code_text)} символов)")
        except Exception as e:
            logger.error(f"Ошибка сохранения файла: {e}")
            # Попробуем сохранить в utf-8, если cp1251 не работает
            with open(input_file, "w", encoding="utf-8") as f:
                f.write(code_text)
            logger.info(f"Сохранен код в файл с UTF-8 кодировкой: {input_file}")

        # Проверяем существование файла
        if not input_file.exists():
            return {
                "success": False,
                "asm_code": "",
                "message": "Не удалось создать входной файл",
                "output": "Ошибка создания временного файла"
            }

        # Запускаем транслятор с параметром -in
        cmd = [str(EXE_PATH), f"-in:{input_file}"]
        logger.info(f"Запуск команды: {' '.join(cmd)}")

        # Запускаем процесс
        process = subprocess.Popen(
            cmd,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            shell=False,
            cwd=str(EXE_PATH.parent)  # Запускаем из директории с exe файлом
        )

        # Ждем завершения с таймаутом
        try:
            stdout, stderr = process.communicate(timeout=30)
            returncode = process.returncode

            # Декодируем вывод
            try:
                stdout_str = stdout.decode('cp1251', errors='replace')
                stderr_str = stderr.decode('cp1251', errors='replace')
            except:
                stdout_str = stdout.decode('utf-8', errors='replace')
                stderr_str = stderr.decode('utf-8', errors='replace')

            logger.info(f"Транслятор завершился с кодом: {returncode}")
            if stdout_str:
                logger.info(f"STDOUT (первые 500 символов): {stdout_str[:500]}...")
            if stderr_str:
                logger.warning(f"STDERR (первые 500 символов): {stderr_str[:500]}...")

        except subprocess.TimeoutExpired:
            process.kill()
            stdout, stderr = process.communicate()
            return {
                "success": False,
                "asm_code": "",
                "message": "Транслятор превысил время выполнения (30 секунд)",
                "output": "ТАЙМАУТ: Трансляция заняла слишком много времени"
            }

        # Проверяем, создался ли ASM файл
        if not asm_file.exists():
            error_msg = f"Транслятор не создал ASM файл. Код возврата: {returncode}. "
            if stderr_str:
                error_msg += f"Ошибка: {stderr_str[:500]}"
            elif stdout_str:
                error_msg += f"Вывод: {stdout_str[:500]}"
            else:
                error_msg += "Неизвестная ошибка."

            return {
                "success": False,
                "asm_code": "",
                "message": error_msg,
                "output": stderr_str if stderr_str else stdout_str
            }

        # Читаем сгенерированный ASM код
        try:
            with open(asm_file, "r", encoding="cp1251") as f:
                asm_code = f.read()
        except:
            # Пробуем другую кодировку
            with open(asm_file, "r", encoding="utf-8") as f:
                asm_code = f.read()

        # Форматируем результат для вывода
        output_lines = [
            
        ]
        output_lines.extend(asm_code.splitlines())

        return {
            "success": True,
            "asm_code": asm_code,
            "message": "Трансляция завершена успешно",
            "output": "\n".join(output_lines)
        }

    except Exception as e:
        logger.error(f"Ошибка при трансляции: {str(e)}", exc_info=True)
        return {
            "success": False,
            "asm_code": "",
            "message": f"Ошибка при выполнении трансляции: {str(e)}",
            "output": f"[ОШИБКА] {str(e)}"
        }

    finally:
        # Очищаем временные файлы
        try:
            if 'input_file' in locals() and input_file.exists():
                input_file.unlink()
            if 'asm_file' in locals() and asm_file.exists():
                asm_file.unlink()
        except Exception as e:
            logger.error(f"Ошибка при очистке файлов: {e}")


@app.post("/api/execute", response_model=ExecuteResponse)
async def execute_code(request: ExecuteRequest):
    """Выполнение кода (демо-версия или через транслятор)"""

    # В этом примере - демо-режим
    # В реальном приложении здесь будет запуск скомпилированного кода

    if not request.code.strip():
        raise HTTPException(
            status_code=400,
            detail="Код не может быть пустым"
        )

    # Демонстрационный вывод
    import random
    demo_output = f"""[УСПЕХ] Выполнение завершено успешно!
{'=' * 50}
Результат выполнения программы:

Привет, мир!
Добро пожаловать в онлайн транслятор!

Счетчик: 1
Счетчик: 2
Счетчик: 3
Счетчик: 4
Счетчик: 5

Условие выполнено!

Программа завершена с кодом возврата: {random.randint(0, 255)}

{'=' * 50}
Примечание:
Это демонстрационный результат выполнения.
Для реального выполнения требуется настройка среды выполнения ASM.
"""

    return {
        "success": True,
        "output": demo_output,
        "message": "Выполнение завершено (демо-режим)"
    }


@app.get("/api/test-translator")
async def test_translator():
    """Тестирование транслятора с простым кодом"""
    test_code = """программа Тест:
    начать
        вывод("Тест транслятора")
    конец"""

    try:
        # Создаем временный файл
        test_file = TEMP_DIR / "test_input.txt"
        with open(test_file, "w", encoding="cp1251") as f:
            f.write(test_code)

        # Запускаем транслятор
        cmd = [str(EXE_PATH), f"-in:{test_file}"]
        logger.info(f"Тестирование команды: {' '.join(cmd)}")

        process = subprocess.Popen(
            cmd,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            shell=False,
            cwd=str(EXE_PATH.parent)
        )

        stdout, stderr = process.communicate(timeout=30)

        # Проверяем результат
        asm_file = TEMP_DIR / "test_input.txt.asm"
        success = asm_file.exists()

        return {
            "success": success,
            "command": " ".join(cmd),
            "return_code": process.returncode,
            "asm_file_exists": success,
            "stdout": stdout.decode('cp1251', errors='replace')[:1000],
            "stderr": stderr.decode('cp1251', errors='replace')[:1000]
        }

    except Exception as e:
        return {
            "success": False,
            "error": str(e),
            "command": " ".join(cmd) if 'cmd' in locals() else "N/A"
        }


@app.get("/api/cleanup")
async def cleanup():
    """Очистка временных файлов (для отладки)"""
    cleanup_temp_files()
    return {"message": "Временные файлы очищены"}


if __name__ == "__main__":
    import uvicorn

    uvicorn.run(app, host="0.0.0.0", port=8000)
