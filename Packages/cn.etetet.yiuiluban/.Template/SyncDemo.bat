@echo off
setlocal

set "currentDir=%~dp0"

if exist "%currentDir%cn.etetet.yiuilubandemo" (
    rmdir /s /q "%currentDir%cn.etetet.yiuilubandemo"
)

set "targetDir=%currentDir%..\..\cn.etetet.yiuilubandemo"

if exist "%targetDir%" (
    xcopy "%targetDir%" "%currentDir%cn.etetet.yiuilubandemo" /e /i /y
)

if exist "%currentDir%cn.etetet.yiuilubandemo\*.meta" (
    del /s /q "%currentDir%cn.etetet.yiuilubandemo\*.meta"
)

echo 操作完成
pause