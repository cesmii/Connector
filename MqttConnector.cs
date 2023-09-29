using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        internal static IDictionary<string, ITag> _lastTagDict = new Dictionary<string, ITag>();

        public static string HistRoot = "MqttHist";
        public static string TopicListFile = "MqttTopicList.txt";
        public static string TopicSubscriptionFile = "CloudAcquiredTagList.txt";
        private static bool SouthBridgeCylcer = false;
        private static int SouthBridgeCycleTime = 2500;
        public static bool SouthBridgeReaper = false;
        public static int SouthBridgeMaxLife = 0;

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

            //Configure SouthBridge Cycler
            if (Parameters.ContainsKey("SBCycleOnNewTag") && Parameters.ContainsKey("SBCycleTime"))
            {
                try
                {
                    if (Boolean.TryParse((string)Parameters["SBCycleOnNewTag"], out SouthBridgeCylcer))
                    {
                        int.TryParse((string)Parameters["SBCycleTime"], out SouthBridgeCycleTime);
                    }
                    if (SouthBridgeCylcer == true && SouthBridgeCycleTime > 0)
                    {
                        Log.Information("South Bridge Cycler configured with Cycle Time of " + SouthBridgeCycleTime);
                    }
                    else
                    {
                        Log.Debug("South Bridge Reaper disabled");
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("Could not parse configuration for SBCylcer");
                    Log.Debug(ex.Message);
                }
            }

            //Configure SouthBridge Reaper
            if (Parameters.ContainsKey("SBReaper") && Parameters.ContainsKey("MaxLife"))
            {
                try
                {
                    if (Boolean.TryParse((string)Parameters["SBReaper"], out SouthBridgeReaper))
                    {
                        int.TryParse((string)Parameters["MaxLife"], out SouthBridgeMaxLife);
                    }
                    if (SouthBridgeReaper == true && SouthBridgeMaxLife > 0)
                    {
                        Log.Information("South Bridge Reaper configured with a Max Life of " + SouthBridgeMaxLife);
                    }
                    else
                    {
                        Log.Debug("South Bridge Reaper disabled");
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("Could not parse configuration for SBReaper");
                    Log.Debug(ex.Message);
                }
            }

            Log.Information("MQTT Adapter: Connected!"); //TODO: Determine if the helper service is running, and show a more accurate message
            IsConnected = true;
            return true;
        }

        /// <summary>
        /// Called when the South Bridge service wants a list of the tags (data points) that your Connector can service
        /// </summary>
        /// <param name="newTagOnly">If true, return only newly discovered tags</param>
        /// <returns>A list of tag names that this Connector can service</returns>
        public IDictionary<string, ITag> Browse(bool newTagOnly)
        {
            Log.Information("MQTT Adapter: Browsed, newTagOnly: " + newTagOnly.ToString());
            IDictionary<string, ITag> newTagDict = Browse();
            Log.Debug("MQTT Adapter: Back in outer browse with newTagDict count: " + newTagDict.Count);
            if (newTagOnly && newTagDict.Count > 0 && _lastTagDict.Count > 0)
            {
                Log.Debug("MQTT Adapter: Doing newTagOnly logic");
                //determine difference between old tag list and new
                IDictionary<string, ITag> diffTagDict = new Dictionary<string, ITag>();
                foreach (string thisTag in newTagDict.Keys)
                {
                    if (!_lastTagDict.ContainsKey(thisTag))
                    {
                        diffTagDict.Add(thisTag, newTagDict[thisTag]);
                    }
                }
                _lastTagDict = newTagDict;
                Log.Information("MQTT Adapter: Returning only new tag list, " + diffTagDict.Count);
                Newtonsoft.Json.JsonConvert.SerializeObject(_lastTagDict);
                if (diffTagDict.Count > 0)
                {
                    Task.Run(() => CycleSouthBridgeService());
                }
                return diffTagDict;
            } else
            {
                _lastTagDict = newTagDict;
                Log.Information("MQTT Adapter: Returning full known tag list: " + _lastTagDict.Count);
                return _lastTagDict;
            }
        }

        public static IDictionary<string, ITag> Browse()
        {
            Log.Information("MQTT Adapter: Doing internal Browse...");
            var myTagDict = new Dictionary<string, ITag>();
            try {
                List<string> topics = new List<string>();
                using (var fs = new FileStream(Path.Combine(MqttConnector.FindDataRoot(), MqttConnector.TopicListFile), FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs, Encoding.Default))
                {
                    while (!sr.EndOfStream)
                    {
                        topics.Add(sr.ReadLine());
                    }
                }
                Log.Debug("MQTT Adapter: Discovered topic list: " + Newtonsoft.Json.JsonConvert.SerializeObject(topics));
                foreach (var topic in topics)
                {
                    var myVar = new Variable();
                    myVar.Name = topic;
                    myVar.TagType = TagType.String;
                    myVar.Attributes = new Dictionary<string, object>
                    {
                        { "DataType", "String" }
                    };
                    /* UA Data Types are:
                    /* SByte | Byte | Int16 | UInt16 | Int32 | UInt32 | Int64 | UInt64 | Float | Double | Boolean | DateTime | String */
                    myTagDict.Add(myVar.Name, myVar);
                }
                Log.Debug("MQTT Adapter: Browsed tag list: " + myTagDict.Count);
            }
            catch (Exception ex) {
                Log.Error("MQTT Adapter: An error occurred reading the topic list file: " + Path.Combine(FindDataRoot(), TopicListFile));
                Log.Error(ex.Message);
            }
            Log.Debug("MQTT Adapter: Inner Browse complete");
            return myTagDict;
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
            Log.Information("MQTT Adapter: Creating reader for: " + Newtonsoft.Json.JsonConvert.SerializeObject(tagDict));
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
            Log.Information("MQTT Adapter: Setting acquired tags: " + Newtonsoft.Json.JsonConvert.SerializeObject(tagNameList));
        }

        /// <summary>
        /// Called by the South Bridge service when its OK to disconnect from your data source
        /// </summary>
        public void Disconnect()
        {
            Log.Information("MQTT Adapter: Asked to disconnect");
            //Perform any necessary disconnect actions for your data source
        }

        [Obsolete("This method should not be necessary, but the Cloud doesn't update otherwise. Need to use until fixed.")]
        public static async void CycleSouthBridgeService()
        {
            Log.Warning("MQTT Adapter: SouthBridge Service will be cycled to force tag reload");
            Thread.Sleep(SouthBridgeCycleTime * 2);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                System.Diagnostics.Process p = new System.Diagnostics.Process();
                Log.Warning("MQTT Adapter: SouthBridge Service stopping.");
                p.StartInfo.FileName = "net stop ThinkIQ.SouthBridge.Service";
                p.StartInfo.UseShellExecute = true;
                p.Start();
                Thread.Sleep(SouthBridgeCycleTime);
                Log.Warning("MQTT Adapter: SouthBridge Service starting.");
                p.StartInfo.FileName = "net start ThinkIQ.SouthBridge.Service";
                p.StartInfo.UseShellExecute = true;
                p.Start();
            }
            else
            {
                System.Diagnostics.Process p = new System.Diagnostics.Process();
                Log.Warning("MQTT Adapter: SouthBridge Service cycling now.");
                p.StartInfo.FileName = Path.Combine(MqttConnector.FindDataRoot(), "southbridge-cycle.sh");
                p.StartInfo.UseShellExecute = true;
                p.Start();
            }
        }

        public static string FindDataRoot()
        {
            if (dataRoot == "")
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    dataRoot = @"C:\ProgramData\ThinkIQ\DataRoot";
                    Log.Information("MQTT Adapter: Starting on Windows with data root: " + dataRoot);
                }
                else
                {
                    dataRoot = "/opt/thinkiq/DataRoot";
                    Log.Information("MQTT Adapter: starting on *nix with data root: " + dataRoot);
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
