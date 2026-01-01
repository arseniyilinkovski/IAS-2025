.586							; система команд (процессор Pentium)
.model flat, stdcall			; модель памяти, соглашение о вызовах
includelib kernel32.lib
includelib libucrt.lib
includelib StaticLib.lib

ExitProcess PROTO: dword		; прототип функции для завершения процесса Windows

EXTRN lenght: proc
EXTRN write_int: proc
EXTRN write_str : proc
EXTRN copy: proc
EXTRN getLocalTimeAndDate: proc
EXTRN random: proc
EXTRN squareOfNumber: proc
EXTRN factorialOfNumber: proc
EXTRN powNumber: proc

EXTRN asciiCode: proc

.stack 4096

.const							; сегмент констант - литералы
nulError byte 'runtime error', 0
nul sdword 0, 0

.data							; сегмент данных - переменные и параметры
_true_str db 'true', 0
_false_str db 'false', 0

.code							; сегмент кода

;----------- MAIN ------------
main PROC

jmp goodExit
errorExit:
push offset nulError
call write_str
goodExit:
push 0
call ExitProcess
main ENDP
end main