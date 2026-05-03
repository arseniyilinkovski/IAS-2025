import subprocess
import uuid
from pathlib import Path
from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
import logging

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

app = FastAPI(title="IAS-2025 Online Compiler API", version="1.0.0")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


class ExecuteRequest(BaseModel):
    code: str


class ExecuteResponse(BaseModel):
    success: bool
    output: str
    error: str = ""
    message: str


BASE_DIR = Path(__file__).parent
FILES_DIR = BASE_DIR / "Files"
BAT_FILE = BASE_DIR / "compile_and_run.bat"

FILES_DIR.mkdir(exist_ok=True)


@app.post("/api/execute", response_model=ExecuteResponse)
async def execute_code(request: ExecuteRequest):
    """Выполнение кода через bat-файл"""

    if not request.code.strip():
        raise HTTPException(status_code=400, detail="Код не может быть пустым")

    # Проверяем существование bat файла
    if not BAT_FILE.exists():
        return {
            "success": False,
            "output": "",
            "error": f"Bat файл не найден: {BAT_FILE}",
            "message": "Ошибка конфигурации сервера"
        }

    # Создаем уникальное имя файла
    file_id = str(uuid.uuid4())[:8]
    input_filename = f"code_{file_id}.txt"
    input_filepath = FILES_DIR / input_filename

    try:
        # Сохраняем код в файл
        with open(input_filepath, "w", encoding="cp1251") as f:
            f.write(request.code)
        logger.info(f"Сохранен код: {input_filepath}")

        # Запускаем bat файл
        logger.info(f"Запуск: {BAT_FILE} {input_filename}")

        result = subprocess.run(
            [str(BAT_FILE), input_filename],
            cwd=str(BASE_DIR),
            capture_output=True,
            text=True,
            encoding='cp866',  # Используем cp866 для русского вывода в консоли
            errors='replace',
            timeout=120
        )

        logger.info(f"Return code: {result.returncode}")

        if result.returncode == 0:
            return {
                "success": True,
                "output": result.stdout or "Программа выполнена успешно",
                "error": "",
                "message": "Выполнение завершено"
            }
        else:
            return {
                "success": False,
                "output": result.stdout or "",
                "error": result.stderr or f"Ошибка (код: {result.returncode})",
                "message": "Ошибка при выполнении программы"
            }

    except subprocess.TimeoutExpired:
        logger.error("Таймаут выполнения")
        return {
            "success": False,
            "output": "",
            "error": "Превышено время выполнения (120 секунд)",
            "message": "Таймаут"
        }
    except Exception as e:
        logger.error(f"Ошибка: {e}", exc_info=True)
        return {
            "success": False,
            "output": "",
            "error": str(e),
            "message": "Внутренняя ошибка сервера"
        }
    finally:
        # Очистка временных файлов через 5 секунд
        import threading
        import time
        def cleanup():
            time.sleep(5)
            for f in FILES_DIR.glob(f"code_{file_id}*"):
                try:
                    f.unlink()
                    logger.info(f"Удален: {f}")
                except:
                    pass

        threading.Thread(target=cleanup, daemon=True).start()


@app.get("/api/health")
async def health_check():
    """Проверка состояния сервера"""
    trans_exists = Path("D:\\BGTU\\IAS-2025\\IAS-2025\\OnlineCompiler\\IAS-2025.exe").exists()
    ml64_exists = Path(
        "C:\\Program Files\\Microsoft Visual Studio\\18\\Community\\VC\\Tools\\MSVC\\14.50.35717\\bin\\Hostx64\\x64\\ml64.exe").exists()
    link_exists = Path(
        "C:\\Program Files\\Microsoft Visual Studio\\18\\Community\\VC\\Tools\\MSVC\\14.50.35717\\bin\\Hostx64\\x64\\link.exe").exists()

    return {
        "status": "healthy",
        "translator_exists": trans_exists,
        "ml64_exists": ml64_exists,
        "link_exists": link_exists,
        "bat_file_exists": BAT_FILE.exists(),
        "files_dir": str(FILES_DIR)
    }


@app.get("/")
async def root():
    return {
        "message": "IAS-2025 Compiler API",
        "version": "1.0.0",
        "endpoints": {
            "/api/execute": "POST - выполнить код",
            "/api/health": "GET - проверить состояние"
        }
    }


if __name__ == "__main__":
    import uvicorn

    uvicorn.run(app, host="0.0.0.0", port=8000)