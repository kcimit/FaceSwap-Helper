@echo off
call _internal\setenv.bat

"%PYTHON_EXECUTABLE%" "%OPENDEEPFACESWAP_ROOT%\main.py" util ^
    --input-dir "%WORKSPACE%\data_dst\aligned" ^
    --add-landmarks-debug-images

pause