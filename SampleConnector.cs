using System.Collections.Generic;
using ThinkIQ.DataManagement;

namespace CESMII
{
    public class SampleConnector : IConnector
    {
        /// <summary>
        /// Set by Factory during instantiation.
        /// Values come from appsettings connector config params
        /// </summary>
        /// <see cref="SampleConnectorFactory"/>
        internal IDictionary<string, object> Parameters { get; set; }
        uint _tagCount;
        uint _dataTimeGapSeconds;
        private bool IsConnected = false;
        bool IDataSource.IsConnected { get => IsConnected; }

        /// <summary>
        /// Called by the South Bridge service when its time to Connect to your data source.
        /// </summary>
        /// <param name="info">
        /// Configuration info that comes from your ConnectorConfig. 
        /// Typically this is the contents of the Attributes section of the model.json file in the same folder.
        /// </param>
        /// <seealso cref="SampleConnector"/>
        /// <returns>True or False indicating a successful connection.</returns>
        public bool Connect(IConnectorInfo info)
        {
            //Perform the actual connection to your data source
            //return true;
            IsConnected = false;

            if (info.Attributes == null)
                return false;

            if (info.Attributes.TryGetValue("TagCount", out var configTagCount))
            {
                if (uint.TryParse(configTagCount.ToString(), out var tagCount))
                {
                    _tagCount = tagCount;
                }
            }

            if (info.Attributes.TryGetValue("DataTimeGapSeconds", out var configDataTimeGapSeconds))
            {
                if (uint.TryParse(configDataTimeGapSeconds.ToString(), out var dataTimeGapSeconds))
                {
                    _dataTimeGapSeconds = dataTimeGapSeconds;
                }
            }

            IsConnected = true;
            return IsConnected;

        }

        /// <summary>
        /// Called when the South Bridge service wants a list of the tags (data points) that your Connector can service
        /// </summary>
        /// <param name="newTagOnly">If true, return only newly discovered tags</param>
        /// <returns>A list of tag names that this Connector can service</returns>
        public IDictionary<string, ITag> Browse(bool newTagOnly)
        {
            //Typically, you would use some enumeration function in the data source you're adapting to to get this list.
            //  In this example we'll create a single tag, just to demonstrate how to construct the tag,
            //  but normally you would do this in a loop for all the tags your connector can service

            var myTagDict = new Dictionary<string, ITag>();    //Create the Dictionary (list) of tags

            var myVar = new Variable(); //Create the new Tag variable
            myVar.Name = "TemperatureSensor";   //Give it a name
            myVar.TagType = TagType.Double;     //Define the ThinkIQ tag type
            myVar.Attributes = new Dictionary<string, object>();    //Add Attributes to the Tag
            myVar.Attributes.Add("DataType", "Double"); //Specify the OPC UA DataType for the Data Source
            /* UA Data Types are:
            /* SByte | Byte | Int16 | UInt16 | Int32 | UInt32 | Int64 | UInt64 | Float | Double | Boolean | DateTime | String */

            myTagDict.Add(myVar.Name, myVar);  //Add the new Tag variable to the Tag list Dictionary
            return myTagDict;  //Return the Tag list Dictionary
        }

        /// <summary>
        /// The South Bridge service may create multiple instances of your Reader class to do the actual tag reading.
        /// This is an automatic optimization the South Bridge does to "chunk" large numbers of tags into small groups.
        /// </summary>
        /// <param name="tagDict">The list of Tags the new Reader instance will service</param>
        /// <param name="acceptStartBoundValue"></param>
        /// <returns>A new instance of your Connector's data Reader class</returns>
        public IHistoryReader CreateReader(IDictionary<string, ITag> tagDict, bool acceptStartBoundValue)
        {
            return new SampleReader(tagDict, acceptStartBoundValue);
        }

        /// <summary>
        /// This is called by the Gateway after receiving configuration information from the Cloud
        /// It specifies the tag list that Cloud user wants to historize that must be serviced
        /// by this Connector.
        /// This behavior can be overridden and customized using the appsettings.json file in the
        /// deployment directory.
        /// </summary>
        /// <param name="tagNameList">List of tags Configured for historization</param>
        /// <param name="useAcquiredTagListAsWhiteList">
        /// Incoming value is specified in appsettings.json.
        /// If set to true, a local tag list will be used, instead of the Cloud-specified tag list
        /// </param>
        public void SetAcquiredTags(IList<string> tagNameList, bool useAcquiredTagListAsWhiteList)
        {
            //Unless you need customization beyond what is specified in the appsettings, you can leave this implementation empty
        }

        /// <summary>
        /// Called by the South Bridge service when its OK to disconnect from your data source
        /// </summary>
        public void Disconnect()
        {
            //Perform any necessary disconnect actions for your data source
        }
    }
}
