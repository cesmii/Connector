# SM Edge Gateway Adapters
The SM Edge Gateway is a small, on-prem component of the SM Platform that facilitates secure, high-speed ingress of data into the SM Platform.

The Gateway uses an extensible approach to protocol adaptation through the use of Connectors. Connectors can be used to adapt a variety of datasources to OPC UA for tranmission into the platform.

The Sample contained here illustrates how to build your own Connector, using the cross-platform .NET Core runtime and C#. 

## Lifecycle
As you look through the code, it may be helpful to understand the basic stages of the Connector's life cycle
- Instantiation: the Gateway's South Bridge service calls your ConnectoryFactory to create an Instance of your Connector
- Connect: the South Bridge service calls your Connector's Connect method to instruct it to make a connection to your data source
- Browse: the South Bridge service calls your Connector's Browse method to get a list of tags (data points) your Connector can provide
- CreateReader: the South Bridge service creates one or more instances of your Reader to service sets of tags configured for ingress
- Read: The South Bridge services calls your Reader's Read method to gather samples for the set of tags that Reader instance was created to service
- Dispose: Your Reader instance is no longer needed and can clean-up
- Disconnect: Your Connector is no longer needed and can Disconnect from the data source

## Additional information
Review the code comments and supplementary .md files for further information on creating, installing and configuring Connectors.
