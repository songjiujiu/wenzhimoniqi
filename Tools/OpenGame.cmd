@echo off
chcp 65001 >nul
set "UNITY_EDITOR=D:\untyle\6000.3.23f1\Editor\Unity.exe"
if not exist "%UNITY_EDITOR%" (
  echo 请在 Unity Hub 中添加本脚本上一级目录作为工程。
  pause
  exit /b 1
)
start "" "%UNITY_EDITOR%" -projectPath "%~dp0.." -executeMethod Mosquito.Editor.ReferenceSceneSetup.OpenGame
