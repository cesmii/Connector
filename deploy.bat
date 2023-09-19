@echo off
echo Stopping Services...
net stop ThinkIQ.Monitor
net stop ThinkIQ.Opc.Ua.NorthService
net stop ThinkIQ.SouthBridge.Service

echo Copying files...
del "C:\Program Files\ThinkIQ\SouthBridgeService\Logs\*.txt"
copy "bin\Debug\netstandard2.0\SmipMqttConnector.*" "C:\Program Files\ThinkIQ\SouthBridgeService\" /Y

echo Starting Services...
net start ThinkIQ.Opc.Ua.NorthService
net start ThinkIQ.SouthBridge.Service
net start ThinkIQ.Monitor
