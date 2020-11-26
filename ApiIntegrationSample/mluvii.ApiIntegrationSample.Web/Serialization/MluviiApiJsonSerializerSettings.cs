using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace mluvii.ApiIntegrationSample.Web.Serialization
{
    public class MluviiApiJsonSerializerSettings : JsonSerializerSettings
    {
        public static readonly MluviiApiJsonSerializerSettings Instance = new MluviiApiJsonSerializerSettings();

        private MluviiApiJsonSerializerSettings()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver();
            Converters = new List<JsonConverter>()
            {
                new SafeStringEnumConverter()
            };
        }
    }
}
