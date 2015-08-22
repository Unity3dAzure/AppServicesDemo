#if !NETFX_CORE
using System;
using System.Net;

namespace RestSharp
{
	public class RestRequestAsyncHandle
	{
        //  This object should be a HttpWebRequest
        private object webRequest;

        public HttpWebRequest WebRequest 
        {
            get 
            {
                return (HttpWebRequest)webRequest;
            }
            set
            {
                webRequest = value;
            }
            
        }

		public RestRequestAsyncHandle()
		{

        }

		public RestRequestAsyncHandle(HttpWebRequest webRequest)
		{
            WebRequest = webRequest;
		}

        public void Abort()
		{
            if (WebRequest != null)
				((HttpWebRequest)WebRequest).Abort();
		}
	}
}
#endif