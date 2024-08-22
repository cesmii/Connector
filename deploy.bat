@echo off
echo Stopping Services...
net stop ThinkIQ.Monitor
net stop ThinkIQ.Opc.Ua.NorthService
net stop ThinkIQ.SouthBridge.Service

echo Copying files...
del "C:\Program Files\ThinkIQ\SouthBridgeService\Logs\*.txt"
copy "bin\Debug\netstandard2.0\SmipMqttConnector.*" "C:\Program Files\ThinkIQ\SouthBridgeService\" /Y
copy "bin\Debug\netstandard2.0\appsettings.mqtt.json" "C:\Program Files\ThinkIQ\SouthBridgeService\appsettings.json" /Y
copy "bin\Debug\netstandard2.0\appsettings.mqtt.json" "C:\Program Files\ThinkIQ\SouthBridgeService\appsettings.simulator.json" /Y

echo Starting Services...
net start ThinkIQ.Opc.Ua.NorthService
net start ThinkIQ.SouthBridge.Service
net start ThinkIQ.Monitor
