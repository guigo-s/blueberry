@echo off
:: Blueberry - Iniciar em segundo plano
:: O app roda invisível na bandeja do sistema.
:: Dê duplo clique no ícone da bandeja para abrir o painel.

cd /d "%~dp0publish"

:: Mata instância anterior se existir
taskkill /f /im Blueberry.exe >nul 2>&1
timeout /t 1 /nobreak >nul

:: Inicia em segundo plano
start "" "Blueberry.exe"

echo Blueberry iniciado! Veja o icone perto do relogio do Windows.
timeout /t 2 /nobreak >nul
