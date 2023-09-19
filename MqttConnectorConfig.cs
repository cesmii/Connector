using System.IO;
using ThinkIQ.DataManagement;

namespace SmipMqttConnector
{
    public class MqttConnectorConfig : IConnectorConfig
    {
        /// <summary>
        /// Called by the South Bridge Service to get configuration info for your connector
        /// </summary>
        /// <returns>Returns connector configuration information</returns>
        /// <seealso cref="MqttConnector.Connect(IConnectorInfo)"/>
        public IConnectorInfo GetConnectorInfo()
        {
            //Typically, this will come from a model.json file created by the Gateway installer
            //  You can put run time configuration for your Connector in the Attributes section of this file, and it will get passed to you on Connect
            var model = File.ReadAllText("model.json");
            var connectorInfo = ConnectorInfo.FromJson(model);
            return connectorInfo;
        }
    }
}
