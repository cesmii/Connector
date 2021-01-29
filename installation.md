# Install on Linux

- Create a new connector in the Cloud, to generate the necessary Activation code
- Install .Net Core runtime
- Install the connector you just created, from its DEB package, supplying the Activation code when prompted
- Bits will be installed to /opt/thinkiq
- A `model.json` file will be created in /opt/thinkiq/services/SouthBridgeService
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

- Restart SouthService to read in new configuration: `sudo systemctl restart tiq-south-bridge.service` 
- Logs for troubleshooting can be found at `/opt/thinkiq/logs/south`

# Uninstall

- `sudo apt remove tiq-gateway`
- Ensure there are no remaining contents in `/opt/thinkiq/`
