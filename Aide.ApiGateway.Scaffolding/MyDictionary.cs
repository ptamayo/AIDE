using System.Collections.Generic;

namespace Aide.ApiGateway.Scaffolding
{
    internal static class MyDictionary
    {
        #region UAT environment

        internal static IDictionary<int, int> PortUat = new Dictionary<int, int>
        {
            { 5004, 4004 }, // Key: ApiAdmin Local Dev
            { 5001, 4001 }, // Key: ApiClaims Local Dev
            { 5003, 4003 }, // Key: Hub Local Dev
        };

        #endregion

        #region K8sDev environment

        internal static IDictionary<int, int> PortK8sDev = new Dictionary<int, int>
        {
            { 5004, 8080 }, // Key: ApiAdmin Local Dev
            { 5001, 8080 }, // Key: ApiClaims Local Dev
            { 5003, 8080 }, // Key: Hub Local Dev
        };

        internal static IDictionary<int, string> HostK8sDev = new Dictionary<int, string>
        {
            { 5004, "aide-api-admin-service" },
            { 5001, "aide-api-claims-service" },
            { 5003, "aide-api-notifications-service" },
        };

        #endregion

        #region GlobalConfiguration

        internal static IDictionary<TargetEnvironment, string> GlobalConfigurationBaseUrl = new Dictionary<TargetEnvironment, string>
        {
            { TargetEnvironment.Dev, "http://api.demoplk.aideguru.com" },
            { TargetEnvironment.UAT, "https://api-uat-aide.glaass.com.mx" },
            { TargetEnvironment.Prod, "https://api-aide.glaass.com.mx" },
            { TargetEnvironment.DockerDev, "http://localhost" },
            { TargetEnvironment.K8sDev, "http://localhost" }
        };

        #endregion
    }
}
