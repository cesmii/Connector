# SM Edge Gateway Connector Adapter for MQTT

This Connector Adapter works in conjunction with a MQTT service (to be posted elsewhere) to provide MQTT data to the SMIP. 

MQTT Topics are treated like Tags. Simple MQTT payloads are treated as individual data points. 
Complex MQTT payloads in JSON are treated as multiple datapoints that can be individually stored.
Since MQTT has no type-safety, all data points are created as Strings, and any payload that can't be parsed is ignored.

Visit the [parent repo to learn more about the SMIP Gateway Connector](https://github.com/cesmii/Connector)

## Known Limitations

This initial version does not support historical reads -- only the most recent MQTT message is available.