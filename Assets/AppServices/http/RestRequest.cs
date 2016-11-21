using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System;
using System.Net;

namespace Unity3dAzure.AppServices
{
	public abstract class RestRequest {

		public UnityWebRequest request { get; private set; }

		public RestRequest(string url, Method method) {
			request = new UnityWebRequest (url, method.ToString());
			request.downloadHandler = new DownloadHandlerBuffer();
		}

		public void AddHeader(string key, string value) {
			request.SetRequestHeader (key, value);
		}

		public void AddBody(byte[] bytes, string contentType) {
			if (request.uploadHandler != null) {
				Debug.LogWarning ("Request body can only be set once");
				return;
			}
			request.uploadHandler = new UploadHandlerRaw (bytes);
			request.uploadHandler.contentType = contentType;
		}

		public virtual void AddBody<T>(T data, string contentType="application/json; charset=utf-8")  {
			string jsonString = JsonUtility.ToJson (data);
			byte[] bytes = DataHelper.ToUTF8Bytes( jsonString );
			this.AddBody (bytes, contentType);
		}

		// COMMON TYPES OF REQUESTS

//		public IEnumerator GetText(Action<RestResponse> callback = null) {
//			yield return Request.Send();
//			//
//			int code = Convert.ToInt32 (Request.responseCode);
//			HttpStatusCode statusCode = (HttpStatusCode)Enum.Parse(typeof (HttpStatusCode), Request.responseCode.ToString());
//			Uri uri = new Uri(Request.url);
//			//
//			Debug.Log ("HttpStatusCode:" + statusCode + " code:" + code + " uri:" + uri);
//			if (statusCode == HttpStatusCode.OK) {
//				Debug.Log ("OK");
//			}
//			Debug.Log ("TODO: callback...");
//			//callback (new RestResponse ()); // Request.downloadHandler.text
//			Request.Dispose ();
//		}

		public void ParseData<T>(Action<IRestResponse<T>> callback = null)  {
			Debug.Log ("parse data response:" + request.url);
			int code = Convert.ToInt32 (request.responseCode);
			HttpStatusCode statusCode = (HttpStatusCode)Enum.Parse(typeof (HttpStatusCode), request.responseCode.ToString());
			string text = request.downloadHandler.text;
			// TODO: add error handling
			Debug.Log ("HttpStatusCode:" + statusCode + " code:" + code + " request url:" + request.url);
			if (statusCode == HttpStatusCode.OK) {
				Debug.Log ("Status OK");
			}
			T data = JsonUtility.FromJson<T>(text);
			callback( new RestResponse<T> (request.url, statusCode, text, data) );
			request.Dispose ();
		}

		public void ParseDataArray<T>(Action<IRestResponse<T[]>> callback = null) {
			Debug.Log ("parse data array response:" + request.url);
			int code = Convert.ToInt32 (request.responseCode);
			HttpStatusCode statusCode = (HttpStatusCode)Enum.Parse(typeof (HttpStatusCode), request.responseCode.ToString());
			string text = request.downloadHandler.text;
			// TODO: add error handling
			Debug.Log ("HttpStatusCode:" + statusCode + " code:" + code + " request url:" + request.url);
			if (statusCode == HttpStatusCode.OK) {
				Debug.Log ("Status OK");
			}
			T[] data = JsonHelper.GetJsonArray<T>(request.downloadHandler.text);
			callback ( new RestResponse<T[]> (request.url, statusCode, text, data) );
			request.Dispose ();
		}

		/*


		

		public IEnumerator Insert<T>(T item, Action<RestResponse<T>> callback = null) {
			Request.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
			Debug.Log ("Request:" + Request.method);
			string json = JsonHelper.ToJsonExcludingSystemProperties (item); //JsonUtility.ToJson (item);
			byte[] bytes = DataHelper.ToUTF8Bytes( json );
			Debug.Log (json + " bytes: '" + DataHelper.ToUTF8String(bytes) +"'");

			//yield break;

			Request.uploadHandler = new UploadHandlerRaw (bytes);
			Request.uploadHandler.contentType = "application/json";
			//
			yield return Request.Send();
			int statusCode = Convert.ToInt32 (Request.responseCode);
			if ( statusCode == 200 ) {
				Debug.LogWarning ("Expected 201 status, but got 200. Check you are using https:");
				yield break;
			}
			Debug.Log ("Status:" + Request.responseCode + " message:" + Request.error);
			if (!Request.isError && statusCode == 201) {
				Debug.Log ("ok text:"+ Request.downloadHandler.text);
				T data = JsonUtility.FromJson<T>(Request.downloadHandler.text);
				RestResponse<T> response = new RestResponse<T> (statusCode, Request.downloadHandler.text, data);
				callback (response);
			} else {
				string httpStatus = Enum.GetName (typeof(HttpStatusCode), statusCode);
				Debug.Log ("error:"+ Request.downloadHandler.text + "error:" + Request.error + "Status Code: "+ Request.responseCode + "Status:"+ httpStatus);
				string errorMessage = Request.error;
				if ( Request.downloadHandler.text.Contains("\"error\":") ){
					ResponseError responseError = JsonUtility.FromJson<ResponseError> (Request.downloadHandler.text);
					errorMessage = responseError.error;
					Debug.Log ("error message:" + errorMessage);
				} else {
					errorMessage = httpStatus + " " + Request.error + "\n" + Request.downloadHandler.text;
				}
				callback (new RestResponse<T> (errorMessage, statusCode, Request.downloadHandler.text) );
			}
			Request.Dispose ();
		}
//*/

	}
}
