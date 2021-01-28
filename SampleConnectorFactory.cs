using System.Collections.Generic;
using ThinkIQ.DataManagement;

namespace CESMII
{
    /// <summary>
    /// The Factory patterns allows the SM Edge Gateway to instantiate Connectors without knowing the details
    /// of how a given Connector needs to be constructed.
    /// </summary>
    public class SampleConnectorFactory : IConnectorFactory
    {
        private IDictionary<string, object> _parameters;

        /// <summary>
        /// The Gateway will call Initialize first, passing in any parameters that are configured in the appsettings.json.
        /// Specifically, the AppConfiguration.Connector.Params section of the appsettings.json file, found in the SouthBridgeService folder
        /// </summary>
        /// <param name="parameters">Parameters configured in appsettings.json</param>
        public void Initialize(IDictionary<string, object> parameters)
        {
            _parameters = parameters;   //remember the parameters that were passed in.
        }

        /// <summary>
        /// The Gateway will call Create next, to allow the Factory to create a new instance of your Connector.
        /// By convention, the Create function takes no arguments, so any parameters needed have to be stored when the Initialize function is called.
        /// </summary>
        /// <returns>An instance of your Connector</returns>
        public IConnector Create()
        {
            var myConnector = new SampleConnector();
            myConnector.Parameters = _parameters;   //Set the Connector Parameters property to equal the parameters value we remembered during Initialize
            return myConnector;

            /* More concise way to write the above
            return new GPIOConnector { Parameters = _parameters };
            */
        }

        /// <summary>
        /// Used to instantiate our Configuration class. See GPIOConnectorConfig.cs
        /// </summary>
        /// <seealso cref="SampleConnector"/>
        /// <returns>A new ConnectorConfig instance</returns>
        public IConnectorConfig GetConfig()
        {
            return new SampleConnectorConfig();
        }

    }
}
