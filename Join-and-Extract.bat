@echo off
rem Joins the CityDragons-win64.tar.part* files in this folder and extracts the game into .\CityDragons
cd /d "%~dp0"
if not exist "CityDragons-win64.tar.part000" ( echo Put this file next to all CityDragons-win64.tar.part* downloads. & pause & exit /b 1 )
echo Joining 10 parts (18 GB, a few minutes)...
copy /b "CityDragons-win64.tar.part000"+"CityDragons-win64.tar.part001"+"CityDragons-win64.tar.part002"+"CityDragons-win64.tar.part003"+"CityDragons-win64.tar.part004"+"CityDragons-win64.tar.part005"+"CityDragons-win64.tar.part006"+"CityDragons-win64.tar.part007"+"CityDragons-win64.tar.part008"+"CityDragons-win64.tar.part009" "CityDragons-win64.tar" >nul || ( echo Join failed: is a part missing? & pause & exit /b 1 )
echo Extracting...
tar -xf "CityDragons-win64.tar" || ( echo Extract failed. & pause & exit /b 1 )
del "CityDragons-win64.tar"
echo Done. Open the CityDragons folder and run "Play City Dragons.bat". You can delete the .part files now.
pause
