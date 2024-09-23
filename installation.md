﻿# Install on Windows

- Create a new "Custom Connector" on the SMIP.
- Install .NET Core Runtime 3.1 on the PC where the connector will be installed.
- Download and install custom connecter on PC using the Activation Code. Take note of install locations.
- Clone this repository
- Open the project (VS2019 and VS2022 were tested)
- Add custom connector code to connect to data source 
- Build DLL
- From `services.msc` Stop the three (3) ThinkIQ services
- Copy your DLL (and any dependencies) to C:\Program Files\ThinkIQ\SouthBridgeService install folder (default location)
- Add reference to your DLL in C:\Program Files\ThinkIQ\SouthBridgeService\appsettings.json (More information [here](appsettings.md))
```
    "Connector": {
      "Assembly": "YourAssembly",
      "Class": "YourNamespace.YourConnectorFactory",
      "Params": {
        //Any parameters you need set before your Connector is constructed
      }
    }
```
- Modify `model.json` to add any Attributes your Adapters needs at run time:
```
{
  "Parent": [
    "platform-instance-name-set-by-cloud""
    "connector-name-set-in-cloud"
  ],
  "Name": "connector-name-set-in-cloud",
  "Attributes": {
    //Any run time parameters your Connector needs
  }
}
```
- From `services.msc` Start the three (3) ThinkIQ services
- Troubleshoot by looking at C:\Program Files\ThinkIQ\SouthBridgeService\Logs.
- If NETStandard.Library 2.0.0 is not installed, you will encounter runtime errors. 
    + Check requirements: https://docs.microsoft.com/en-us/dotnet/standard/net-standard
    + Resolve by running this in Package Manager Console (https://www.nuget.org/packages/NETStandard.Library/)
    `Install-Package NETStandard.Library -Version 2.0.0`

# Install on Linux

- Create a new connector in the Cloud, to generate the necessary Activation code
- Install .Net Core runtime
- Install the connector you just created, from its DEB package, supplying the Activation code when prompted:
    + `sudo apt install ./tiq-gateway.deb`
- Bits will be installed to /opt/thinkiq
- A `model.json` file will be created in /opt/thinkiq/services/SouthBridgeService
- Stop the services:
    + `sudo systemctl stop tiq-south-bridge.service`
    + `sudo systemctl stop tiq-opcua-north.service`
- Add your .DLL (and any dependencies) to /opt/thinkiq/services/SouthrBridgeService
- Add reference to your DLL in `appsettings.json`:

```
    "Connector": {
      "Assembly": "YourAssembly",
      "Class": "YourAssembly.YourConnectorFactory",
      "Params": {
        //Any parameters you need set before your Connector is constructed
      }
    }
```

- Modify `model.json` to add any Attributes your Adapters needs at run time:

```
{
  "Parent": [
    "platform-instance-name-set-by-cloud""
    "connector-name-set-in-cloud"
  ],
  "Name": "connector-name-set-in-cloud",
  "Attributes": {
    //Any run time parameters your Connector needs
  }
}
```
- Delete the DataRoot folder and all contents:
    + `sudo rm -rf /opt/thinkiq/DataRoot/`
- Restart the services to read in new configuration: 
    + `sudo systemctl start tiq-opcua-north.service`
    + `sudo systemctl start tiq-south-bridge.service` 
- Logs for troubleshooting can be found at `/opt/thinkiq/logs/south`

# Uninstall

- `sudo apt remove tiq-gateway`
- Ensure there are no remaining contents in `/opt/thinkiq/`
