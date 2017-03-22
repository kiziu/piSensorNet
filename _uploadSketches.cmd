@ECHO OFF
SETLOCAL

SET TMP_FILE="%TEMP%\_uploadSketches.scp"

IF EXIST %TMP_FILE% DEL /F %TMP_FILE%

ECHO # Automatically abort script on errors >> %TMP_FILE%
ECHO option batch abort >> %TMP_FILE%
ECHO # Disable overwrite confirmations that conflict with the previous >> %TMP_FILE%
ECHO option confirm off >> %TMP_FILE%
ECHO # Connect using a password >> %TMP_FILE%
ECHO open sftp://pi:13Misiaczek13!@192.168.13.2/ >> %TMP_FILE%
ECHO # Change remote directory >> %TMP_FILE%
ECHO cd /home/pi/arduino >> %TMP_FILE%
ECHO # Force binary mode transfer >> %TMP_FILE%
ECHO option transfer binary >> %TMP_FILE%
ECHO # Download file to the local directory d:\ >> %TMP_FILE%
ECHO put R:\Arduino\build_folder\*.hex -permissions=0777 >> %TMP_FILE%
ECHO # Disconnect >> %TMP_FILE%
ECHO close >> %TMP_FILE%
ECHO # Exit WinSCP >> %TMP_FILE%
ECHO exit >> %TMP_FILE%

CALL D:\Standalone\WinSCP.exe /console /script=%TMP_FILE% /log=_uploadSketches.log