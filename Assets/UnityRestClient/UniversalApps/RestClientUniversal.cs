#region License
//   Copyright 2010 John Sheehan
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License. 
#endregion
#if NETFX_CORE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Windows.Web.Http;
using System.Text;
using Windows.Web.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using RestSharp.Serializers;
using RestSharp.Deserializers;

namespace RestSharp
{
    /// <summary>
    /// Client to translate RestRequests into Http requests and process response result
    /// </summary>
    public partial class RestClient : IRestClient
    {
        // silverlight friendly way to get current version
        static readonly Version version = new Version(1, 0, 0, 0);

        /// <summary>
        /// Default constructor that registers default content handlers
        /// </summary>
        public RestClient()
        {
            ContentHandlers = new Dictionary<string, IDeserializer>();
            AcceptTypes = new List<string>();
            DefaultParameters = new List<Parameter>();

            // register default handlers
            JsonDeserializer json = new JsonDeserializer();
            AddHandler("application/json", json);
            AddHandler("text/json", json);
            AddHandler("text/x-json", json);
            AddHandler("text/javascript", json);


            FollowRedirects = true;
        }

        /// <summary>
        /// Sets the BaseUrl property for requests made by this client instance
        /// </summary>
        /// <param name="baseUrl"></param>
        public RestClient(string baseUrl)
            : this()
        {
            BaseUrl = baseUrl;
        }

        private IDictionary<string, IDeserializer> ContentHandlers { get; set; }
        private IList<string> AcceptTypes { get; set; }

        /// <summary>
        /// Parameters included with every request made with this instance of RestClient
        /// If specified in both client and request, the request wins
        /// </summary>
        public IList<Parameter> DefaultParameters { get; private set; }

        /// <summary>
        /// Registers a content handler to process response content
        /// </summary>
        /// <param name="contentType">MIME content type of the response content</param>
        /// <param name="deserializer">Deserializer to use to process content</param>
        public void AddHandler(string contentType, IDeserializer deserializer)
        {
            ContentHandlers[contentType] = deserializer;
            if (contentType != "*")
            {
                AcceptTypes.Add(contentType);
                // add Accept header based on registered deserializers
                var accepts = string.Join(", ", AcceptTypes.ToArray());
                this.RemoveDefaultParameter("Accept");
                this.AddDefaultParameter("Accept", accepts, ParameterType.HttpHeader);
            }
        }

        /// <summary>
        /// Remove a content handler for the specified MIME content type
        /// </summary>
        /// <param name="contentType">MIME content type to remove</param>
        public void RemoveHandler(string contentType)
        {
            ContentHandlers.Remove(contentType);
            AcceptTypes.Remove(contentType);
            this.RemoveDefaultParameter("Accept");
        }

        /// <summary>
        /// Remove all content handlers
        /// </summary>
        public void ClearHandlers()
        {
            ContentHandlers.Clear();
            AcceptTypes.Clear();
            this.RemoveDefaultParameter("Accept");
        }

        /// <summary>
        /// Retrieve the handler for the specified MIME content type
        /// </summary>
        /// <param name="contentType">MIME content type to retrieve</param>
        /// <returns>IDeserializer instance</returns>
        IDeserializer GetHandler(string contentType)
        {
            if (string.IsNullOrEmpty(contentType) && ContentHandlers.ContainsKey("*"))
            {
                return ContentHandlers["*"];
            }

            var semicolonIndex = contentType.IndexOf(';');
            if (semicolonIndex > -1) contentType = contentType.Substring(0, semicolonIndex);
            IDeserializer handler = null;
            if (ContentHandlers.ContainsKey(contentType))
            {
                handler = ContentHandlers[contentType];
            }
            else if (ContentHandlers.ContainsKey("*"))
            {
                handler = ContentHandlers["*"];
            }

            return handler;
        }

        private HttpClient _client;   //  Underlying HttpClient used with this instance of RestClient

        private HttpClient Client
        {
            get
            {
                if(_client == null)
                {
                    _client = new HttpClient();
                }

                return _client;
            }
        }

        public void AddDefaultHeader(string name, string value)
		{
            Client.DefaultRequestHeaders.Add(name, value);
		}

		/// <summary>
		/// The CookieContainer used for requests made by this client instance
		/// </summary>
		public CookieContainer CookieContainer { get; set; }

        /// <summary>
        /// Maximum number of redirects to follow if FollowRedirects is true
        /// </summary>
        public int? MaxRedirects { get; set; }

        /// <summary>
        /// Default is true. Determine whether or not requests that result in 
        /// HTTP status codes of 3xx should follow returned redirect
        /// </summary>
        public bool FollowRedirects { get; set; }

        /// <summary>
        /// UserAgent to use for requests made by this client instance
        /// </summary>
        public string UserAgent { get; set; }

        /// <summary>
        /// Timeout in milliseconds to use for requests made by this client instance
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// The number of milliseconds before the writing or reading times out.
        /// </summary>
        public int ReadWriteTimeout { get; set; }

        /// <summary>
        /// Whether to invoke async callbacks using the SynchronizationContext.Current captured when invoked
        /// </summary>
        public bool UseSynchronizationContext { get; set; }

        private string _baseUrl;
        /// <summary>
        /// Combined with Request.Resource to construct URL for request
        /// Should include scheme and domain without trailing slash.
        /// </summary>
        /// <example>
        /// client.BaseUrl = "http://example.com";
        /// </example>
        public virtual string BaseUrl
        {
            get
            {
                return _baseUrl;
            }
            set
            {
                _baseUrl = value;
                if (_baseUrl != null && _baseUrl.EndsWith("/"))
                {
                    _baseUrl = _baseUrl.Substring(0, _baseUrl.Length - 1);
                }
            }
        }

        /// <summary>
        /// Assembles URL to call based on parameters, method and resource
        /// </summary>
        /// <param name="request">RestRequest to execute</param>
        /// <returns>Assembled System.Uri</returns>

        //public Uri BuildUri(IRestRequest request)
        //{
        //    throw new NotImplementedException("Not Used for Windows.  Please Implement if needed");
        //}

        public Uri BuildUri(IRestRequest request)
        {
            var assembled = request.Resource;
            var urlParms = request.Parameters.Where(p => p.Type == ParameterType.UrlSegment);
            foreach (var p in urlParms)
            {
                assembled = assembled.Replace("{" + p.Name + "}", WebUtility.UrlEncode(p.Value.ToString()));
            }

            if (!string.IsNullOrEmpty(assembled) && assembled.StartsWith("/"))
            {
                assembled = assembled.Substring(1);
            }

            if (!string.IsNullOrEmpty(BaseUrl))
            {
                if (string.IsNullOrEmpty(assembled))
                {
                    assembled = BaseUrl;
                }
                else
                {
                    assembled = string.Format("{0}/{1}", BaseUrl, assembled);
                }
            }

            IEnumerable<Parameter> parameters = null;

            if (request.Method != Method.POST && request.Method != Method.PUT && request.Method != Method.PATCH)
            {
                // build and attach querystring if this is a get-style request
                parameters = request.Parameters.Where(p => p.Type == ParameterType.GetOrPost || p.Type == ParameterType.QueryString);
            }
            else
            {
                parameters = request.Parameters.Where(p => p.Type == ParameterType.QueryString);
            }

            // build and attach querystring 
            if (parameters != null && parameters.Any())
            {
                var data = EncodeParameters(parameters);
                assembled = string.Format("{0}?{1}", assembled, data);
            }

            return new Uri(assembled);
        }

        private static string EncodeParameters(IEnumerable<Parameter> parameters)
        {
            var querystring = new StringBuilder();
            foreach (var p in parameters)
            {
                if (querystring.Length > 1)
                    querystring.Append("&");
                querystring.AppendFormat("{0}={1}", WebUtility.UrlEncode(p.Name), WebUtility.UrlEncode(p.Value.ToString()));
            }

            return querystring.ToString();
        }

        private HttpMethod ConvertHttpMethod(Method src)
        {
            switch(src)
            {
                case Method.DELETE:
                    return HttpMethod.Delete;
                case Method.GET:
                    return HttpMethod.Get;
                case Method.HEAD:
                    return HttpMethod.Head;
                case Method.OPTIONS:
                    return HttpMethod.Options;
                case Method.PATCH:
                    return HttpMethod.Patch;
                case Method.POST:
                    return HttpMethod.Post;
                case Method.PUT:
                    return HttpMethod.Put;
                default:
                    return HttpMethod.Get;
            }
        }

        //  This is called from Execute Async
        private HttpRequestMessage ConfigureHttp(IRestRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("IRestRequest parameter was null");
            }

            //if(string.IsNullOrEmpty(request.Resource))
            //{
            //    throw new ArgumentException("There was no URI Specified in request.Resource");
            //}

            Uri requestUri = BuildUri(request);
            //  Create HttpRequest message based on request
            HttpRequestMessage reqMessage = new HttpRequestMessage(ConvertHttpMethod(request.Method), requestUri);
            
            //   Look for request parameter type of RequestBody
            var reqBody = (from p in request.Parameters
                          where p.Type == ParameterType.RequestBody
                          select p).FirstOrDefault();

            //  Only Support JSON Right now for Windows Universal
            if(reqBody != null  && request.RequestFormat == DataFormat.Json)
            {
                object val = reqBody.Value;
                HttpStringContent content;
                if (val is byte[])
                {

                    content = new HttpStringContent(Encoding.UTF8.GetString((byte [])val, 0, ((byte [])val).Length));
                    
                }
                else
                {
                    content = new HttpStringContent(val.ToString());
                }
                
                content.Headers.ContentType = HttpMediaTypeHeaderValue.Parse("application/json");
                reqMessage.Content = content;
            }


            // Copy Headers from request.Parameters to request message
            var headers = from p in request.Parameters
                          where p.Type == ParameterType.HttpHeader
                          select new HttpHeader
                          {
                              Name = p.Name,
                              Value = p.Value.ToString()
                          };

            foreach (var header in headers)
            {
                reqMessage.Headers.Add(header.Name, header.Value);
            }

            return reqMessage;
        }

        private async Task<RestResponse> ConvertToRestResponse(IRestRequest request, HttpResponseMessage httpResponse)
        {
            
            if(httpResponse == null)
            {
                throw new ArgumentNullException("HttpResponseMessage value was Null in ConvertToRestResponse function");
            }

            var restResponse = new RestResponse();
            
            restResponse.Content = await httpResponse.Content.ReadAsStringAsync();
            restResponse.ContentLength = restResponse.Content.Length;
            
            if(!string.IsNullOrEmpty(restResponse.Content))
            { 
                restResponse.RawBytes = Encoding.UTF8.GetBytes(restResponse.Content);
            }
            
            if(httpResponse.IsSuccessStatusCode)
            {
                restResponse.ResponseStatus = ResponseStatus.Completed;   //   Always Completed if get this far.  Will set error in Try/Catch
            }
            else
            {
                restResponse.ResponseStatus = ResponseStatus.Error;  
            }

            restResponse.StatusCode = (System.Net.HttpStatusCode)httpResponse.StatusCode;
            restResponse.StatusDescription = httpResponse.ReasonPhrase;
            restResponse.Request = request;
            if(httpResponse.Content.Headers.ContentType != null)
            { 
                restResponse.ContentType = httpResponse.Content.Headers.ContentType.MediaType;
            }
            
            //  Cookies will be returned via the headers here.  
            foreach (var header in httpResponse.Headers)
            {
                switch(header.Key)
                {
                    case "Set-Cookie":  //  Could parse this out to the cookie collection but not right now.
                    default:
                        restResponse.Headers.Add(new Parameter { Name = header.Key, Value = header.Value, Type = ParameterType.HttpHeader });
                        break;
                }

            }

            return restResponse;
        }

        private async Task<RestResponse<T>> ConvertToRestResponse<T>(IRestRequest request, HttpResponseMessage httpResponse)
        {
            if (httpResponse == null)
            {
                throw new ArgumentNullException("HttpResponseMessage value was Null in ConvertToRestResponse function");
            }

            var restResponse = new RestResponse<T>();
            restResponse.Content = await httpResponse.Content.ReadAsStringAsync();
            restResponse.ContentLength = restResponse.Content.Length;
            if (!string.IsNullOrEmpty(restResponse.Content))
            {
                restResponse.RawBytes = Encoding.UTF8.GetBytes(restResponse.Content);
            }

            if (httpResponse.IsSuccessStatusCode)
            {
                restResponse.ResponseStatus = ResponseStatus.Completed;   //   Always Completed if get this far.  Will set error in Try/Catch
            }
            else
            {
                restResponse.ResponseStatus = ResponseStatus.Error;
            }




            restResponse.StatusCode = (System.Net.HttpStatusCode)httpResponse.StatusCode;
            restResponse.StatusDescription = httpResponse.ReasonPhrase;
            restResponse.Request = request;

            if (httpResponse.Content.Headers.ContentType != null)
            {
                restResponse.ContentType = httpResponse.Content.Headers.ContentType.MediaType;
            }

            //  Cookies will be returned via the headers here.  
            foreach (var header in httpResponse.Headers)
            {
                switch (header.Key)
                {
                    case "Set-Cookie":  //  Could parse this out to the cookie collection but not right now.
                    default:
                        restResponse.Headers.Add(new Parameter { Name = header.Key, Value = header.Value, Type = ParameterType.HttpHeader });
                        break;
                }
            }

            //   Try and Deserialize here
            IDeserializer deserializer = GetHandler(restResponse.ContentType);
            if(deserializer != null)
            { 
                restResponse.Data = deserializer.Deserialize<T>(restResponse);
            }



            return restResponse;
        }

        public string Serialize(object obj)
        {
            JsonSerializer serializer = new JsonSerializer();
            return serializer.Serialize(obj);
        }
        

        //   This does all of the work for the HttpRequest/response and calls the callback
        private async Task ExecuteAsync(IRestRequest request, Action<IRestResponse, RestRequestAsyncHandle> callback, RestRequestAsyncHandle handle)
        {
            RestResponse response;
            try
            {
                //  Set up a HttpRequestMessage based on the request properties
                HttpRequestMessage reqMessage = ConfigureHttp(request);
                HttpClient client = Client;

                //  Make the request and wait until response is read
                HttpResponseMessage responseMessage = await client.SendRequestAsync(reqMessage, HttpCompletionOption.ResponseContentRead);

                //  Convert response to IRestResponse
                response = await ConvertToRestResponse(request, responseMessage);
            }
            catch(Exception e)
            {
                response = new RestResponse();
                response.ResponseStatus = ResponseStatus.Error;
                response.ErrorException = e;
                response.StatusDescription = "Exception Occurred when executing the request.  Check the ErrorException for details";
            }

            callback(response, handle);
        }


        private async Task ExecuteAsync<T>(IRestRequest request, Action<IRestResponse<T>, RestRequestAsyncHandle> callback, RestRequestAsyncHandle handle)
        {
            RestResponse<T> response;
            try
            {
                //  Set up a HttpRequestMessage based on the request properties
                HttpRequestMessage reqMessage = ConfigureHttp(request);
                HttpClient client = Client;

                //  Make the request and wait until response is read
                HttpResponseMessage responseMessage = await client.SendRequestAsync(reqMessage, HttpCompletionOption.ResponseContentRead);

                //  Convert response to IRestResponse
                response = await ConvertToRestResponse<T>(request, responseMessage);
            }
            catch (Exception e)
            {
                response = new RestResponse<T>();
                response.ResponseStatus = ResponseStatus.Error;
                response.ErrorException = e;
                response.StatusDescription = "Exception Occurred when executing the request.  Check the ErrorException for details";
            }
            
            callback(response, handle);
        }

        //  This creates the RestRequestAsyncHandle, starts the Http operation on another thread, and returns the RestRequestAsyncHandle
        private RestRequestAsyncHandle ExecuteAsyncInternal(IRestRequest request, Action<IRestResponse, RestRequestAsyncHandle> callback)
        {
            var asyncHandle = new RestRequestAsyncHandle(new CancellationTokenSource());

            //  Start a task here to do the Http Request and then return asyncHandle that client can use to cancel
            Task.Factory.StartNew(() => ExecuteAsync(request, callback, asyncHandle), asyncHandle.CancelToken.Token);

            return asyncHandle;
        }


        public RestRequestAsyncHandle ExecuteAsync(IRestRequest request, Action<IRestResponse, RestRequestAsyncHandle> callback, string httpMethod)
        {
            return ExecuteAsyncInternal(request, callback);
        }



		public RestRequestAsyncHandle ExecuteAsync(IRestRequest request, Action<IRestResponse, RestRequestAsyncHandle> callback)
        {
            return ExecuteAsyncInternal(request, callback);
        }



        private RestRequestAsyncHandle ExecuteAsyncInternal<T>(IRestRequest request, Action<IRestResponse<T>, RestRequestAsyncHandle> callback)
        {
            var asyncHandle = new RestRequestAsyncHandle(new CancellationTokenSource());

            //  Start a task here to do the Http Request and then return asyncHandle that client can use to cancel
            Task.Factory.StartNew(() => ExecuteAsync<T>(request, callback, asyncHandle), asyncHandle.CancelToken.Token);

            return asyncHandle;
        }

		public RestRequestAsyncHandle ExecuteAsync<T>(IRestRequest request, Action<IRestResponse<T>, RestRequestAsyncHandle> callback)
        {
            return ExecuteAsyncInternal<T>(request, callback);
        }

        public RestRequestAsyncHandle ExecuteAsyncGet(IRestRequest request, Action<IRestResponse, RestRequestAsyncHandle> callback, string httpMethod)
		{
            request.Method = Method.GET;
            return ExecuteAsyncInternal(request, callback);       
        }
        public RestRequestAsyncHandle ExecuteAsyncPost(IRestRequest request, Action<IRestResponse, RestRequestAsyncHandle> callback, string httpMethod)
        {
            request.Method = Method.POST;
            return ExecuteAsyncInternal(request, callback);
        }
        public RestRequestAsyncHandle ExecuteAsyncGet<T>(IRestRequest request, Action<IRestResponse<T>, RestRequestAsyncHandle> callback, string httpMethod)
        {
            request.Method = Method.GET;
            return ExecuteAsyncInternal<T>(request, callback);
        }

        public RestRequestAsyncHandle ExecuteAsyncPost<T>(IRestRequest request, Action<IRestResponse<T>, RestRequestAsyncHandle> callback, string httpMethod)
        {
            request.Method = Method.POST;
            return ExecuteAsyncInternal<T>(request, callback);
        }


   		/// <summary>
		/// Executes the request and callback asynchronously, authenticating if needed
		/// </summary>
		/// <param name="client">The IRestClient this method extends</param>
		/// <param name="request">Request to be executed</param>
		/// <param name="callback">Callback function to be executed upon completion</param>
		public RestRequestAsyncHandle ExecuteAsync(IRestRequest request, Action<IRestResponse> callback)
		{
			return ExecuteAsync(request, (response, handle) => callback(response));
		}

		/// <summary>
		/// Executes the request and callback asynchronously, authenticating if needed
		/// </summary>
		/// <param name="client">The IRestClient this method extends</param>
		/// <typeparam name="T">Target deserialization type</typeparam>
		/// <param name="request">Request to be executed</param>
		/// <param name="callback">Callback function to be executed upon completion providing access to the async handle</param>
		public RestRequestAsyncHandle ExecuteAsync<T>(IRestRequest request, Action<IRestResponse<T>> callback) where T : new()
		{
			return ExecuteAsync<T>(request, (response, asyncHandle) => callback(response));
		}

		public RestRequestAsyncHandle GetAsync<T>(IRestRequest request, Action<IRestResponse<T>, RestRequestAsyncHandle> callback) where T : new()
		{
			request.Method = Method.GET;
			return ExecuteAsync<T>(request, callback);
		}

		public RestRequestAsyncHandle PostAsync<T>(IRestRequest request, Action<IRestResponse<T>, RestRequestAsyncHandle> callback) where T : new()
		{
			request.Method = Method.POST;
			return ExecuteAsync<T>(request, callback);
		}

		public RestRequestAsyncHandle PutAsync<T>(IRestRequest request, Action<IRestResponse<T>, RestRequestAsyncHandle> callback) where T : new()
		{
			request.Method = Method.PUT;
			return ExecuteAsync<T>(request, callback);
		}

		public RestRequestAsyncHandle HeadAsync<T>(IRestRequest request, Action<IRestResponse<T>, RestRequestAsyncHandle> callback) where T : new()
		{
			request.Method = Method.HEAD;
			return ExecuteAsync<T>(request, callback);
		}

		public RestRequestAsyncHandle OptionsAsync<T>(IRestRequest request, Action<IRestResponse<T>, RestRequestAsyncHandle> callback) where T : new()
		{
			request.Method = Method.OPTIONS;
			return ExecuteAsync<T>(request, callback);
		}

		public RestRequestAsyncHandle PatchAsync<T>(IRestRequest request, Action<IRestResponse<T>, RestRequestAsyncHandle> callback) where T : new()
		{
			request.Method = Method.PATCH;
			return ExecuteAsync<T>(request, callback);
		}

		public RestRequestAsyncHandle DeleteAsync<T>(IRestRequest request, Action<IRestResponse<T>, RestRequestAsyncHandle> callback) where T : new()
		{
			request.Method = Method.DELETE;
			return ExecuteAsync<T>(request, callback);
		}

		public RestRequestAsyncHandle GetAsync(IRestRequest request, Action<IRestResponse, RestRequestAsyncHandle> callback)
		{
			request.Method = Method.GET;
			return ExecuteAsync(request, callback);
		}

		public RestRequestAsyncHandle PostAsync(IRestRequest request, Action<IRestResponse, RestRequestAsyncHandle> callback)
		{
			request.Method = Method.POST;
			return ExecuteAsync(request, callback);
		}

		public RestRequestAsyncHandle PutAsync(IRestRequest request, Action<IRestResponse, RestRequestAsyncHandle> callback)
		{
			request.Method = Method.PUT;
			return ExecuteAsync(request, callback);
		}

		public RestRequestAsyncHandle HeadAsync(IRestRequest request, Action<IRestResponse, RestRequestAsyncHandle> callback)
		{
			request.Method = Method.HEAD;
			return ExecuteAsync(request, callback);
		}

		public RestRequestAsyncHandle OptionsAsync(IRestRequest request, Action<IRestResponse, RestRequestAsyncHandle> callback)
		{
			request.Method = Method.OPTIONS;
			return ExecuteAsync(request, callback);
		}

		public RestRequestAsyncHandle PatchAsync(IRestRequest request, Action<IRestResponse, RestRequestAsyncHandle> callback)
		{
			request.Method = Method.PATCH;
			return ExecuteAsync(request, callback);
		}

		public RestRequestAsyncHandle DeleteAsync(IRestRequest request, Action<IRestResponse, RestRequestAsyncHandle> callback)
		{
			request.Method = Method.DELETE;
			return ExecuteAsync(request, callback);
		}

		/// <summary>
		/// Add a parameter to use on every request made with this client instance
		/// </summary>
		/// <param name="restClient">The IRestClient instance</param>
		/// <param name="p">Parameter to add</param>
		/// <returns></returns>
		public void AddDefaultParameter(Parameter p)
		{
			if (p.Type == ParameterType.RequestBody)
			{
				throw new NotSupportedException(
					"Cannot set request body from default headers. Use Request.AddBody() instead.");
			}

			DefaultParameters.Add(p);
		}

		/// <summary>
		/// Removes a parameter from the default parameters that are used on every request made with this client instance
		/// </summary>
		/// <param name="restClient">The IRestClient instance</param>
		/// <param name="name">The name of the parameter that needs to be removed</param>
		/// <returns></returns>
		public void RemoveDefaultParameter(string name)
		{
			var parameter = DefaultParameters.SingleOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
			if (parameter != null)
			{
				DefaultParameters.Remove(parameter);
			}
		}

		/// <summary>
		/// Adds a HTTP parameter (QueryString for GET, DELETE, OPTIONS and HEAD; Encoded form for POST and PUT)
		/// Used on every request made by this client instance
		/// </summary>
		/// <param name="restClient">The IRestClient instance</param>
		/// <param name="name">Name of the parameter</param>
		/// <param name="value">Value of the parameter</param>
		/// <returns>This request</returns>
		public void AddDefaultParameter(string name, object value)
		{
			AddDefaultParameter(new Parameter { Name = name, Value = value, Type = ParameterType.GetOrPost });
		}

		/// <summary>
		/// Adds a parameter to the request. There are four types of parameters:
		///	- GetOrPost: Either a QueryString value or encoded form value based on method
		///	- HttpHeader: Adds the name/value pair to the HTTP request's Headers collection
		///	- UrlSegment: Inserted into URL if there is a matching url token e.g. {AccountId}
		///	- RequestBody: Used by AddBody() (not recommended to use directly)
		/// </summary>
		/// <param name="restClient">The IRestClient instance</param>
		/// <param name="name">Name of the parameter</param>
		/// <param name="value">Value of the parameter</param>
		/// <param name="type">The type of parameter to add</param>
		/// <returns>This request</returns>
		public void AddDefaultParameter(string name, object value, ParameterType type)
		{
			AddDefaultParameter(new Parameter { Name = name, Value = value, Type = type });
		}


		/// <summary>
		/// Shortcut to AddDefaultParameter(name, value, UrlSegment) overload
		/// </summary>
		/// <param name="restClient">The IRestClient instance</param>
		/// <param name="name">Name of the segment to add</param>
		/// <param name="value">Value of the segment to add</param>
		/// <returns></returns>
		public void AddDefaultUrlSegment(string name, string value)
		{
			AddDefaultParameter(name, value, ParameterType.UrlSegment);
		}
    }
}
#endif