﻿# Install on Linux

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
