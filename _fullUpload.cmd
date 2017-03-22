@ECHO OFF
SETLOCAL

SET TMP_FILE="%TEMP%\_fullUpload.scp"
SET BLACKLIST=.git/;bin/;obj/;.gitignore;*.user;*.sln;*.xproj;*.mwb;*.bak;sqls/;debug/;*.lock.json;.vs/;*.cmd;*.log;packages/;*.db;*.opendb;

IF EXIST %TMP_FILE% DEL /F %TMP_FILE%

ECHO option batch abort >> %TMP_FILE%
ECHO option confirm off >> %TMP_FILE%
ECHO open sftp://pi:13Misiaczek13!@192.168.13.2/ >> %TMP_FILE%
ECHO cd /home/pi >> %TMP_FILE%
ECHO rm piSensorNet >> %TMP_FILE%
ECHO option transfer binary >> %TMP_FILE%
ECHO put E:\Documents\piSensorNet\piSensorNet -permissions=0777 -filemask=*^|%BLACKLIST% >> %TMP_FILE%
ECHO close >> %TMP_FILE%
ECHO exit >> %TMP_FILE%

CALL D:\Standalone\WinSCP.exe /console /script=%TMP_FILE% /log=_fullUpload.log