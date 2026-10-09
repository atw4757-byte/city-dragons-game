@echo off
rem City Dragons launcher: the city map with the dragon game mode, DLSS Quality on NVIDIA RTX (ignored elsewhere).
cd /d "%~dp0"
start "" "CitySample.exe" /Game/Map/Small_City_LVL?game=/Game/ATL/BP_DragonGM.BP_DragonGM_C -ini:Engine:[ConsoleVariables]:FastGeo.Enable=0,[ConsoleVariables]:FastGeo.EnableTransformer=0 -ExecCmds="r.NGX.Enable 1,r.NGX.DLSS.Enable 1,r.NGX.DLSS.Quality 1,r.NGX.DLSS.AutoQualitySetting 0"
