@echo off
chcp 1251 >nul
setlocal enabledelayedexpansion

echo ========================================
echo IAS-2025 Compiler and Runner
echo ========================================
echo.

REM ========== ПУТИ ==========
set "TRANS_EXE=D:\BGTU\IAS-2025\IAS-2025\OnlineCompiler\IAS-2025.exe"
set "FILES_DIR=D:\BGTU\IAS-2025\IAS-2025\OnlineCompiler\backend\Files"
set "MSVC_PATH=C:\Program Files\Microsoft Visual Studio\18\Community\VC\Tools\MSVC\14.50.35717"
set "ML_EXE=%MSVC_PATH%\bin\Hostx64\x64\ml64.exe"
set "LINK_EXE=%MSVC_PATH%\bin\Hostx64\x64\link.exe"

if "%1"=="" (
    echo [ERROR] No input file
    exit /b 1
)

set "INPUT_FILE=%1"
set "INPUT_PATH=%FILES_DIR%\%INPUT_FILE%"
set "BASENAME=%~n1"

if not exist "%INPUT_PATH%" (
    echo [ERROR] Input file not found: %INPUT_PATH%
    exit /b 1
)

cd /d "%FILES_DIR%"

echo [1/5] Трансляция кода в ASM...
"%TRANS_EXE%" -in:"%INPUT_FILE%"
if errorlevel 1 (
    echo [ERROR] Translation failed
    exit /b 1
)
echo [OK] Translation complete
echo.

echo [2/5] Подготовка ASM файла...
if exist "%BASENAME%.txt.asm" (
    copy "%BASENAME%.txt.asm" "program.asm" >nul
    echo [OK] ASM file copied
) else (
    echo [ERROR] ASM file not found
    exit /b 1
)
echo.

echo [3/5] Исправление ASM файла...
REM Создаем исправленную версию ASM файла
powershell -Command @"
$content = Get-Content 'program.asm' -Raw

# Удаляем точки в начале строк
$content = $content -replace '(?m)^\.', ''

# Исправляем синтаксис
$content = $content -replace 'ExitProcess PROTO: dword', 'ExitProcess PROTO :DWORD'
$content = $content -replace 'EXTRN', 'EXTERN'
$content = $content -replace 'main PROC', 'main PROC'
$content = $content -replace 'main ENDP', 'main ENDP'
$content = $content -replace 'end main', 'END main'

# Удаляем problematic строки
$content = $content -replace '(?m)^\d+.*', ''
$content = $content -replace 'includelib.*', ''

# Сохраняем
$content | Set-Content 'program_fixed.asm' -NoNewline
"@
echo [OK] ASM file fixed
echo.

echo [4/5] Компиляция и линковка...
echo Компиляция ASM в OBJ...
"%ML_EXE%" /c /nologo /Fo"program.obj" "program_fixed.asm" 2>compile_errors.txt
if errorlevel 1 (
    echo [WARNING] Compilation with fixed file failed, trying original...
    "%ML_EXE%" /c /nologo /Fo"program.obj" "program.asm"
    if errorlevel 1 (
        echo [ERROR] Compilation failed
        type compile_errors.txt
        exit /b 1
    )
)
echo [OK] Compilation successful
echo.

echo Линковка OBJ в EXE...
"%LINK_EXE%" /nologo /SUBSYSTEM:CONSOLE /ENTRY:main /OUT:"program.exe" "program.obj"
if errorlevel 1 (
    echo [ERROR] Linking failed
    exit /b 1
)
echo [OK] Linking successful
echo.

echo [5/5] Выполнение программы...
echo.
echo ============================================
echo PROGRAM OUTPUT:
echo ============================================
program.exe
echo ============================================
echo.

echo [OK] Done!
exit /b 0