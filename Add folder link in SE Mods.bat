@echo off

for %%a in (.) do set currentfolder=%%~na

mklink /J "%appdata%\SpaceEngineers\Mods\%currentfolder%" "../%currentfolder%"

if errorlevel 1 goto Error
echo Done!
goto End
:Error
echo An error occured creating the symlink.
:End

timeout 10