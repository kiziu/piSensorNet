@ECHO OFF
SETLOCAL

SET TMP_FILE="%TEMP%\_download.scp"

IF EXIST %TMP_FILE% DEL /F %TMP_FILE%

ECHO option batch abort >> %TMP_FILE%
ECHO option confirm off >> %TMP_FILE%
ECHO open sftp://pi:13Misiaczek13!@192.168.13.2/ >> %TMP_FILE%
ECHO cd /home/pi/piSensorNet >> %TMP_FILE%
ECHO option transfer binary >> %TMP_FILE%
ECHO get * E:\Documents\piSensorNet\piSensorNet\ -neweronly -filemask=*.cs;*.json;*. >> %TMP_FILE%
ECHO close >> %TMP_FILE%
ECHO exit >> %TMP_FILE%

CALL D:\Standalone\WinSCP.exe /console /script=%TMP_FILE% /log=_download.log