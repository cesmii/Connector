using Serilog;
using Serilog.Formatting.Json;
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using ThinkIQ.DataManagement;
using Newtonsoft.Json.Linq;

namespace SmipMqttConnector
{
    public class MqttReader : IHistoryReader
    {
        /// <summary>
        /// Stores the list of tags this Reader will service
        /// </summary>
        internal IDictionary<string, ITag> _tagDict { get; set; }

        /// <summary>
        /// Constructor, creates a new Reader to service a specific list of tags
        /// </summary>
        /// <param name="tagDict">The list of tags this Reader instance will service</param>
        /// <param name="acceptStartBoundValue"></param>
        public MqttReader(IDictionary<string, ITag> tagDict, bool acceptStartBoundValue)
        {
            //remember the tagDict requested in the constructor
            _tagDict = tagDict;
            Log.Information("Connector adapter reader constructed for: " + Newtonsoft.Json.JsonConvert.SerializeObject(tagDict));
        }

        /// <summary>
        /// Called by the South Bridge service when it needs this reader to service its tag list
        /// when sample values.
        /// </summary>
        /// <param name="startTime">The start of the time range for which to send sample values</param>
        /// <param name="endTime">The end of the time range for which to send sample values</param>
        /// <returns>A list of ItemData values to be historized by the platform</returns>
        public IList<ItemData> ReadRaw(DateTime startTime, DateTime endTime)
        {
            Log.Debug("Live read requested for " + startTime.ToShortTimeString() + " through " + endTime.ToShortTimeString());
            var newData = new List<ItemData>();
            if (MqttConnector.ReadCount < 2)
            {
                Log.Information("Connector adapter IList reading raw for: " + Newtonsoft.Json.JsonConvert.SerializeObject(_tagDict));
            }
            else
            {
                Log.Information("Connector adapter IList reading raw.");
            }
            
            foreach (var tag in _tagDict.Keys)  //for each of the tags this Reader was created to service
            {
                ItemData myItemData = new ItemData { VSTs = new List<VST>(), Item = tag };  //Create a new ItemData

                Log.Debug("loading cached payload for tag: " + tag);
                if (tag.Contains("/:/"))
                {
                    Log.Debug("This topic contains parseable data points in its payload, which are treated as tags.");
                    var tagParts = tag.Split(new[] { "/:/" }, StringSplitOptions.None);
                    var useTag = tagParts[0];
                    var usePayload = tagParts[1];
                    var usePath = Path.Combine(MqttConnector.FindDataRoot(), MqttConnector.HistRoot, (MqttConnector.Base64Encode(useTag) + ".txt"));

                    Log.Debug("Cached payload loading from: " + usePath);
                    Log.Debug("Payload data member should be: " + usePayload);
                    var useValue = parseJsonPayloadForKey(usePayload, usePath);
                    Log.Debug("Parsed data member value is: " + useValue);
                    
                    //Prep data for SMIP
                    myItemData.VSTs.Add(new VST(useValue, 192, endTime));
                    newData.Add(myItemData);
                } else
                {
                    Log.Debug("This topic contains a single datapoint");

                    var usePath = Path.Combine(MqttConnector.FindDataRoot(), MqttConnector.HistRoot, (MqttConnector.Base64Encode(tag) + ".json"));
                    Log.Debug("Cached payload loading from: " + usePath);
                    var useValue = File.ReadAllText(usePath);
                    Log.Debug("Single datapoint value is: " + useValue);
                    
                    //Prep data for SMIP
                    myItemData.VSTs.Add(new VST(useValue, 192, startTime));
                    newData.Add(myItemData);
                }
            }
            //return the list of new ItemData points
            return newData;
        }

        //TODO: The Mqtt Service only preserves the last payload right now, so historical reads and live data reads are the same
        IList<ItemData> IHistoryReader.ReadRaw(DateTime startTime, DateTime endTime)
        {
            Log.Debug("Historical read requested for " + startTime.ToShortTimeString() + " through " + endTime.ToShortTimeString() + " but not implemented, returning live data instead");
            if (MqttConnector.ReadCount < 1)
            {
                Log.Information("Connector adapter IHistoryReader reading raw for: " + Newtonsoft.Json.JsonConvert.SerializeObject(_tagDict));
            } else
            {
                Log.Information("Connector adapter IHistoryReader reading raw.");
            }
            MqttConnector.ReadCount = MqttConnector.ReadCount + 1;
            return ReadRaw(startTime, endTime);
        }

        private string parseJsonPayloadForKey(string compoundKey, string payloadPath)
        {
            compoundKey = compoundKey.Replace("/", ".");
            using (StreamReader file = File.OpenText(payloadPath))
            using (JsonTextReader reader = new JsonTextReader(file))
            {
                JObject payloadObj = (JObject)JToken.ReadFrom(reader);
                Log.Debug("Parsed stored payload: " + Newtonsoft.Json.JsonConvert.SerializeObject(payloadObj));
                var value = (string)payloadObj.SelectToken(compoundKey);
                return value;
            }
        }

        bool IHistoryReader.ContainsTag(string tagName)
        {
            Log.Information("Connector adapter asked if it contains tag: " + tagName);
            var topics = File.ReadAllLines(Path.Combine(MqttConnector.FindDataRoot(), MqttConnector.TopicListFile));
            return Array.IndexOf(topics, tagName) != -1;
        }

        IDictionary<string, ITag> IHistoryReader.GetCurrentTags()
        {
            Log.Information("Connector adapter asked for current tags, returning: " + Newtonsoft.Json.JsonConvert.SerializeObject(_tagDict));
            return MqttConnector.Browse();
        }

        void IDisposable.Dispose()
        {
            Log.Information("Connector adapter told to IDisposable Dispose!");
            MqttConnector.ReadCount = 0;
        }
    }
}
