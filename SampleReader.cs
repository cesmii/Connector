using System;
using System.Collections.Generic;
using ThinkIQ.DataManagement;

namespace CESMII
{
    public class SampleReader : IHistoryReader
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
        public SampleReader(IDictionary<string, ITag> tagDict, bool acceptStartBoundValue)
        {
            //remember the tagDict requested in the constructor
            _tagDict = tagDict;
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

            foreach (var tag in _tagDict.Keys)  //for each of the tags this Reader was created to service
            {
                ItemData myItemData = new ItemData { VSTs = new List<VST>(), Item = tag };  //Create a new ItemData

                //Get (or in this example, make) some data point samples for this tag within the requested time range
                myItemData.VSTs.Add(new VST(startTime.Ticks, 192, startTime));
                var midTime = startTime + ((endTime - startTime) / 2);
                myItemData.VSTs.Add(new VST(midTime.Ticks, 192, midTime));
                myItemData.VSTs.Add(new VST(endTime.Ticks, 192, endTime));
                newData.Add(myItemData);
            }

            //return the list of new ItemData points
            return newData;
        }

        /// <summary>
        /// Clean up the Reader when no longer needed
        /// </summary>
        public void Dispose()
        {

        }
    }
}
