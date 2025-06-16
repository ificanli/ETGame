@echo off
setlocal

set "currentDir=%~dp0"

if exist "%currentDir%cn.etetet.yiuilubangen" (
    rmdir /s /q "%currentDir%cn.etetet.yiuilubangen"
)

set "targetDir=%currentDir%..\..\cn.etetet.yiuilubangen"

if exist "%targetDir%" (
    xcopy "%targetDir%" "%currentDir%cn.etetet.yiuilubangen" /e /i /y
)

if exist "%currentDir%cn.etetet.yiuilubangen\*.meta" (
    del /s /q "%currentDir%cn.etetet.yiuilubangen\*.meta"
)

echo 操作完成
pause