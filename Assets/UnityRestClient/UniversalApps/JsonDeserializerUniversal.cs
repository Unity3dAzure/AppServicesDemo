#if NETFX_CORE
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace RestSharp.Deserializers
{
    public class JsonDeserializer : IDeserializer
    {
        public JsonDeserializer()
        {
            //  This is the workaround I have found for Winodws Store 8.1 apps.
#if UNITY_METRO_8_1
            DateFormat = "yyyy-MM-ddTHH:mm:ss.FFFFFFFZ";
#else
            DateFormat = RestSharp.DateFormat.Iso8601;
#endif
        }

        public T Deserialize<T>(IRestResponse response)
        {
            DataContractJsonSerializerSettings settings = new DataContractJsonSerializerSettings();
            if(DateFormat != null)
            {
                settings.DateTimeFormat = new System.Runtime.Serialization.DateTimeFormat(DateFormat);

            }

            DataContractJsonSerializer jsonSer = new DataContractJsonSerializer(typeof(T), settings);
            MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(response.Content));
            T target = (T)jsonSer.ReadObject(stream);
  
            return target;
        }

        public string RootElement {get; set;}

        public string Namespace {get; set;}

        public string DateFormat {get; set;}
    }
}
#endif