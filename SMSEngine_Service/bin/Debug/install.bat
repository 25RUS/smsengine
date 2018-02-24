set current_dir=%~dp0
sc create SMSEngine binPath= "%current_dir%SMSEngine.exe" start= "auto"
sc start SMSEngine
pause