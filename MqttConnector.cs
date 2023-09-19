using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using ThinkIQ.DataManagement;

namespace SmipMqttConnector
{
    public class MqttConnector : IConnector
    {
        /// <summary>
        /// Set by Factory during instantiation.
        /// Values come from appsettings connector config params
        /// </summary>
        /// <see cref="MqttConnectorFactory"/>
        internal IDictionary<string, object> Parameters { get; set; }

        uint _tagCount;
        uint _dataTimeGapSeconds;
        public static int ReadCount = 0;
        private bool IsConnected = false;
        private static string dataRoot = "";
        internal IDictionary<string, ITag> _lastTagDict { get; set; }

        public static string HistRoot = "MqttHist";
        public static string TopicListFile = "MqttTopicList.json";
        public static string TopicSubscriptionFile = "CloudAcquiredTagList.txt";

        bool IDataSource.IsConnected { get => IsConnected; }

        /// <summary>
        /// Called by the South Bridge service when its time to Connect to your data source.
        /// </summary>
        /// <param name="info">
        /// Configuration info that comes from your ConnectorConfig. 
        /// Typically this is the contents of the Attributes section of the model.json file in the same folder.
        /// </param>
        /// <seealso cref="MqttConnector"/>
        /// <returns>True or False indicating a successful connection.</returns>
        public bool Connect(IConnectorInfo info)
        {
            //Perform the actual connection to your data source
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

            Log.Information("Connector adapter connected!");

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
            Log.Information("Connector adapter browsed, newTagOnly: " + newTagOnly.ToString());

            IDictionary<string, ITag> newTagDict = Browse();
            if (newTagOnly)
            {
                //return difference
                IDictionary<string, ITag> diffTagDict = new Dictionary<string, ITag>();
                foreach (string thisTag in newTagDict.Keys)
                {
                    if (!_lastTagDict.ContainsKey(thisTag))
                    {
                        diffTagDict.Add(thisTag, newTagDict[thisTag]);
                    }
                }
                _lastTagDict = newTagDict;
                Log.Information("New tags are: " + Newtonsoft.Json.JsonConvert.SerializeObject(diffTagDict));
                return (IDictionary<string, ITag>)diffTagDict;
            } else
            {
                _lastTagDict = newTagDict;
                return _lastTagDict;
            }
        }

        public static IDictionary<string, ITag> Browse()
        {
            Log.Debug("Connector adapter performing internal Browse...");
            var myTagDict = new Dictionary<string, ITag>();    //Create the Dictionary (list) of tags
            var topics = File.ReadAllLines(Path.Combine(FindDataRoot(), TopicListFile));
            foreach (var topic in topics)
            {
                var myVar = new Variable();
                myVar.Name = topic;
                myVar.TagType = TagType.String;
                myVar.Attributes = new Dictionary<string, object>();
                myVar.Attributes.Add("DataType", "String");
                /* UA Data Types are:
                /* SByte | Byte | Int16 | UInt16 | Int32 | UInt32 | Int64 | UInt64 | Float | Double | Boolean | DateTime | String */
                myTagDict.Add(myVar.Name, myVar);
            }
            Log.Debug("Browsed tag list was: " + Newtonsoft.Json.JsonConvert.SerializeObject(myTagDict));
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
            Log.Information("Connector adapter creating reader for: " + Newtonsoft.Json.JsonConvert.SerializeObject(tagDict));
            return new MqttReader(tagDict, acceptStartBoundValue);
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
            Log.Information("Connector adapter setting tags: " + Newtonsoft.Json.JsonConvert.SerializeObject(tagNameList));
        }

        /// <summary>
        /// Called by the South Bridge service when its OK to disconnect from your data source
        /// </summary>
        public void Disconnect()
        {
            Log.Information("Connector adapter asked to disconnect");
            //Perform any necessary disconnect actions for your data source
        }

        public static string FindDataRoot()
        {
            if (dataRoot == "")
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    dataRoot = @"C:\ProgramData\ThinkIQ\DataRoot";
                    Console.WriteLine("Hello Windows!\r\n" + dataRoot);
                }
                else
                {
                    dataRoot = "/opt/thinkiq/dataroot";
                    Console.WriteLine("Hello *nix!" + dataRoot);
                }
            }
            return dataRoot;
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
    }
}
