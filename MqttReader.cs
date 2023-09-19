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
            /*using (var sw = new StreamWriter(Path.Combine(MqttConnector.FindDataRoot(), MqttConnector.TopicSubscriptionFile)))
            {
                foreach (var tag in tagDict)
                {
                    sw.WriteLine(tag);
                }
            }*/
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

                //Log.Information("loading cache for tag: " + tag);
                if (tag.Contains("/:/"))
                {
                    //Log.Information("This is a payload member of a topic!");
                    var tagParts = tag.Split(new[] { "/:/" }, StringSplitOptions.None);
                    var useTag = tagParts[0];
                    var usePayload = tagParts[1];
                    var usePath = Path.Combine(MqttConnector.FindDataRoot(), MqttConnector.HistRoot, (MqttConnector.Base64Encode(useTag) + ".txt"));

                    //Log.Information("Path should be: " + usePath);
                    //Log.Information("Member should be: " + usePayload);
                    var useValue = parseJsonPayloadForKey(usePayload, usePath);
                    //Log.Information("value will be: " + useValue);

                    myItemData.VSTs.Add(new VST(useValue, 192, endTime));
                    newData.Add(myItemData);

                } else
                {
                    //Log.Information("This is a simple topic!");
                    var usePath = Path.Combine(MqttConnector.FindDataRoot(), MqttConnector.HistRoot, (MqttConnector.Base64Encode(tag) + ".json"));
                    //Log.Information("Path should be: " + usePath);
                    var useValue = File.ReadAllText(usePath);
                    //Log.Information("value will be: " + useValue);
                    //Prep data for SMIP
                    myItemData.VSTs.Add(new VST(useValue, 192, startTime));
                    newData.Add(myItemData);
                }
            }

            //return the list of new ItemData points
            return newData;
        }

        /// <summary>
        /// Clean up the Reader when no longer needed
        /// </summary>
        public void Dispose()
        {
            Log.Information("Connector adapter told to Dispose!");
            MqttConnector.ReadCount = 0;
        }

        private string parseJsonPayloadForKey(string compoundKey, string payloadPath)
        {
            compoundKey = compoundKey.Replace("/", ".");
            using (StreamReader file = File.OpenText(payloadPath))
            using (JsonTextReader reader = new JsonTextReader(file))
            {
                JObject payloadObj = (JObject)JToken.ReadFrom(reader);
                //Log.Information("Parsed: " + Newtonsoft.Json.JsonConvert.SerializeObject(payloadObj));
                var value = (string)payloadObj.SelectToken(compoundKey);
                return value;
                /*if (payloadObj.TryGetValue(compoundKey, out var val))
                {
                    return val.ToString();
                }*/

            }
            return "Nope";
        }

        IList<ItemData> IHistoryReader.ReadRaw(DateTime startTime, DateTime endTime)
        {
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

        bool IHistoryReader.ContainsTag(string tagName)
        {
            Log.Information("Connector adapter asked if it contains tag: " + tagName);
            var topics = File.ReadAllLines(Path.Combine(MqttConnector.FindDataRoot(), MqttConnector.TopicListFile));
            return Array.IndexOf(topics, tagName) != -1;
        }

        IDictionary<string, ITag> IHistoryReader.GetCurrentTags()
        {
            Log.Information("Connector adapter asked for current tags: " + Newtonsoft.Json.JsonConvert.SerializeObject(_tagDict));
            return MqttConnector.Browse();
        }

        void IDisposable.Dispose()
        {
            Log.Information("Connector adapter told to IDisposable dispose!");
            Dispose();
        }
    }
}
