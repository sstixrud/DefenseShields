:: This script creates a symlink to the SpaceEngineers to account for different installation directories on different systems.

@echo off
echo ***
echo Example folder location:
echo ***
echo C:\Program Files (x86)\Steam\steamapps\common\SpaceEngineers
echo ***
set /p path="Please enter the folder location of your SpaceEngineers root folder (in common\SpaceEngineers folder): "
cd %~dp0
cd .

mklink /J ..\SpaceEngineers "%path%"

if errorlevel 1 goto Error
echo Done! You can now open the SE-RG-System solution without issue.
goto End
:Error
echo An error occured creating the symlink.
:End

timeout 10