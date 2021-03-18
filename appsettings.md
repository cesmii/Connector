# AppSettings.json Fields

- `Connector` - Information about how to instatiate your Connector, see example below
- `DataStorer` - Information about instantiating the built-in data handlers. Do not modify.
- `UseOnPremiseAcquiredTagList`
- `UseAcquiredTagListAsWhiteList`

Combined, these two tags allow local overrides to tag lists, per the following table:

| UseOnPremiseAcquiredTagList | UseAcquiredTagListAsWhiteList  | Result            |
| :-------------------------- | :----------------------------: | ----------------: |
| False                       | False                          | Cloud configured  |
| False                       | True                           | Cloud configured  |
| True                        | False                          | Uses OnPremiseAcquiredTagListFilePath, but show all tags in the Cloud   |
| True                        | True                           | Uses OnPremiseAcquiredTagListFilePath, hides other tags from the Cloud  |

- `OnPremiseAcquiredTagListFilePath` - Path to the text file the specifies the on premise tag list
- `AcquiredTagChunkSize` - The number of tags each Reader instance should service (before creating another Reader instance)
- `TagChangeCheckIntervalInSeconds` - The number of seconds before the Connector should check for new tags to appear in the list
- `AttributeChangeCheckIntervalInSeconds`
- `AttributeChangeCheckTrigger`
- `MinAcquisitionPeriodInSeconds`
- `MaxAcquisitionPeriodInSeconds`
- `AcquisitionDelayInSeconds`
- `HistSeizedThresholdInSeconds`

# AppSetting.json Example
```
{
  "AppConfiguration": {
    "Connector": {
      "Assembly": "CESMIIConnectorSample",
      "Class": "CESMII.SampleConnectorFactory",
      "Params": {}
    },
    "DataStorer": {
      "Assembly": "ThinkIQ.JsonDataHandler",
      "Class": "ThinkIQ.JsonDataHandler.JsonDataStorer",
      "Params": {
        "DataRootDir": "/opt/thinkiq/DataRoot",
        "MaxFileCount": 10000
      }
    },
    "UseOnPremiseAcquiredTagList": false,
    "UseAcquiredTagListAsWhiteList": false,
    "OnPremiseAcquiredTagListFilePath": "./OnPremiseAcquiredTagList.txt",
    "AcquiredTagChunkSize": 50,
    "TagChangeCheckIntervalInSeconds": 60,
    "AttributeChangeCheckIntervalInSeconds": 3600,
    "AttributeChangeCheckTrigger": 0,
    "MinAcquisitionPeriodInSeconds": "10",
    "MaxAcquisitionPeriodInSeconds": "3600",
    "AcquisitionDelayInSeconds": "30",
    "HistSeizedThresholdInSeconds": "1800"
  }
}
```
