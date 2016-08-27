using UnityEngine;
using System;
using System.Net;
using System.Collections.Generic;
using RestSharp;
using Pathfinding.Serialization.JsonFx;
using Unity3dAzure.AppServices;

[CLSCompliant(false)]
public class AppServicesDemo : MonoBehaviour
{
	/// <remarks>
	/// Enter your Azure App Service connection strings
	/// </remarks>

	/// Azure Mobile App connection strings
	[SerializeField]
	private string _appUrl = "PASTE_YOUR_APP_URL";

	/// Go to https://developers.facebook.com/tools/accesstoken/ to generate a new access "User Token"
	[SerializeField]
	private string _facebookAccessToken = "PASTE_YOUR_USER_TOKEN_HERE";

	/// App Service Rest Client
	private MobileServiceClient _client;

	/// App Service Table defined using a DataModel
	private MobileServiceTable<Highscore> _table;

	/// Item to insert
	private Highscore _score;

	/// List of highscores (leaderboard)
	List<Highscore> scores = new List<Highscore>();

	/// Use this for initialization
	void Start ()
	{
		Debug.Log ("Azure App Service");

		/// Create App Service client
		_client = new MobileServiceClient (_appUrl);

		Debug.Log(_client);

		/// Get App Service 'Highscores' table
		_table = _client.GetTable<Highscore>("Highscores");
		Debug.Log(_table);

		InitializeDemoUI();
	}

	/// Update is called once per frame
	//void Update () {}

	private void InitializeDemoUI()
	{
		_score = new Highscore()
		{
			score = 0,
			username = "Anon",
			id = ""
		};
	}

	float col1 = 300;
	float col2 = 200;
	float w = 50;
	float h = 50;

	Vector2 scrollPosition = new Vector2(0,0);

	/// Demo UI
	void OnGUI()
	{
		/// Bigger text style for mobile
		GUIStyle textFieldStyle = new GUIStyle("textfield");
		textFieldStyle.fontSize = 20;
		textFieldStyle.alignment = TextAnchor.UpperLeft;

		GUILayout.BeginHorizontal();
		/// COLUMNS START

		/// Column 1
		GUILayout.BeginVertical(GUILayout.Width(col1));
		GUILayout.Label("Username");
		_score.username = GUILayout.TextField(_score.username, textFieldStyle, GUILayout.Width(col1), GUILayout.Height(h) );
		GUILayout.Label("Score");
		_score.score = Int32.Parse( GUILayout.TextField( _score.score.ToString(), textFieldStyle, GUILayout.Width(col1), GUILayout.Height(h)) ) ; // TODO: refactor as gameobjects

		GUILayout.Label("#");
		_score.id = GUILayout.TextField(_score.id);
		if (_client.User == null)
		{
			if ( GUILayout.Button("Login", GUILayout.Width(col1), GUILayout.Height(h)) ) {
				DoLogin();
			}
		}
		else
		{
			GUILayout.Label("UserId: " + _client.User.user.userId ); // show logged in user
		}

		if ( String.IsNullOrEmpty(_score.id) )
		{
			if ( GUILayout.Button("Insert", GUILayout.Width(col1), GUILayout.Height(h)) ) {
				InsertItem();
			}
		}
		else
		{
			if ( GUILayout.Button("Update", GUILayout.Width(col1), GUILayout.Height(h)) ) {
				UpdateItem();
			}
			if ( GUILayout.Button("Delete", GUILayout.Width(col1), GUILayout.Height(h)) ) {
				DeleteItem();
			}
			/*
			if ( GUILayout.Button("Lookup", GUILayout.Width(col1), GUILayout.Height(h)) ) {
				LookupItem();
			}
			//*/
			if ( GUILayout.Button("Done", GUILayout.Width(col1), GUILayout.Height(h)) ) {
				InitializeDemoUI();
			}
		}

		/// <summary>
		/// Requires a custom API setup
		/// <summary>
		if ( GUILayout.Button("Custom API", GUILayout.Width(col1), GUILayout.Height(h)) ) {
			CustomApi();
		}

		GUILayout.EndVertical();

		/// Column 2
		GUILayout.BeginVertical();

		if ( GUILayout.Button("Read", GUILayout.Width(col2), GUILayout.Height(h)) ){
			ReadItems();
		}
		if ( GUILayout.Button("Query Name", GUILayout.Width(col2), GUILayout.Height(h)) ){
			QueryItemsByName();
		}
		if ( GUILayout.Button("Query Highscores", GUILayout.Width(col2), GUILayout.Height(h)) ){
			QueryItemsByHighscore();
		}

		scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false, GUILayout.Width(col2) );
        GUILayout.BeginVertical();
        foreach (Highscore score in scores)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(">", GUILayout.Width(w), GUILayout.Height(h)))
            {
                _score = score; // sets the selected item
            }
            GUILayout.Label(score.username);

			GUILayout.Label(score.score.ToString());
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();
        GUILayout.EndScrollView();

		GUILayout.EndVertical();

		/// COLUMNS END
		GUILayout.EndHorizontal();
	}

	private bool isValid(Highscore highscore)
	{
		if ( highscore.score > 0 )
		{
			return true;
		}
		else
		{
			Debug.Log("Score needs to be > 0");
			return false;
		}
	}

	private void DoLogin()
	{
		Debug.Log("Login..." );
		_client.Login(MobileServiceAuthenticationProvider.Facebook, _facebookAccessToken, OnLoginCompleted);
	}

	private void OnLoginCompleted(IRestResponse<MobileServiceUser> response)
	{
		Debug.Log("Status: " + response.StatusCode + " Uri:" + response.ResponseUri );
		Debug.Log("OnLoginCompleted: " + response.Content );

		if ( response.StatusCode == HttpStatusCode.OK)
		{
			MobileServiceUser mobileServiceUser = response.Data;
			_client.User = mobileServiceUser;
			Debug.Log("Authorized UserId: " + _client.User.user.userId );
		}
		else
		{
			Debug.Log("Authorization Error: " + response.StatusCode);
		}
	}

	private void InsertItem()
	{
		if ( isValid(_score) )
		{
			_table.Insert<Highscore>(_score, OnInsertItemCompleted);
		}
	}

	private void OnInsertItemCompleted(IRestResponse<Highscore> response)
	{
		if (response.StatusCode == HttpStatusCode.Created)
		{
			Debug.Log( "OnInsertItemCompleted: " + response.Data );
        	Highscore item = response.Data; // if successful the item will have an 'id' property value
			_score = item;
		}
		else
		{
			Debug.Log("Error Status:" + response.StatusCode + " Uri: "+response.ResponseUri );
		}
	}

	private void UpdateItem()
	{
		if ( isValid(_score) )
		{
			_table.Update<Highscore>(_score, OnUpdateItemCompleted);
		}
	}

	private void OnUpdateItemCompleted(IRestResponse<Highscore> response)
	{
		if (response.StatusCode == HttpStatusCode.OK)
		{
			Debug.Log("OnUpdateItemCompleted: " + response.Content );
			InitializeDemoUI();
		}
		else
		{
			Debug.Log("Error Status:" + response.StatusCode + " Uri: "+response.ResponseUri );
		}
	}

	private void DeleteItem()
	{
		_table.Delete<Highscore>(_score.id, OnDeleteItemCompleted);
	}

	private void OnDeleteItemCompleted(IRestResponse<Highscore> response)
	{
		if (response.StatusCode == HttpStatusCode.NoContent)
		{
			// TODO: handle error {"code":404,"error":"Error: An item with id '#' does not exist"}
			Debug.Log("OnDeleteItemCompleted");
			InitializeDemoUI();
		}
		else
		{
			Debug.Log("Error Status:" + response.StatusCode + " Uri: "+response.ResponseUri );
		}
	}

	private void ReadItems()
	{
		_table.Read<Highscore>(OnReadItemsCompleted);
	}

	private void OnReadItemsCompleted(IRestResponse<List<Highscore>> response)
	{
		if (response.StatusCode == HttpStatusCode.OK)
		{
			Debug.Log("OnReadItemsCompleted data: " + response.Content);
        	List<Highscore> items = response.Data;
        	Debug.Log("Read items count: " + items.Count);
			scores = items;
		}
		else
		{
			Debug.Log("Error Status:" + response.StatusCode + " Uri: "+response.ResponseUri );
		}
	}

	private void QueryItemsByName()
	{
		string filter = string.Format("startswith(username,'{0}')", _score.username);
		CustomQuery query = new CustomQuery(filter);
		QueryItems(query);
	}

	private void QueryItemsByHighscore()
	{
		string filter = string.Format("score gt {0}", 999);
		string orderBy = "score desc";
		CustomQuery query = new CustomQuery(filter,orderBy);
		QueryItems(query);
	}

	private void QueryItems(CustomQuery query)
	{
		_table.Query<Highscore>(query, OnReadItemsCompleted);
	}

	/// <summary>
	/// This demo 'hello' custom api just gets a response message '{"message":"Hello World!"}'
	/// </summary>
	private void CustomApi()
	{
		_client.InvokeApi<Message>("hello", OnCustomApiCompleted);
	}

	private void OnCustomApiCompleted(IRestResponse<Message> response)
	{
		if (response.StatusCode == HttpStatusCode.OK)
		{
			Debug.Log("OnCustomApiCompleted data: " + response.Content);
       		Message message = response.Data;
			Debug.Log( "Result: " + message);
		}
		else
		{
			Debug.Log("Error Status:" + response.StatusCode + " Uri: "+response.ResponseUri );
		}
	}

	/*
	/// <summary>
	/// This is an example showing how to get an item using it's id.
	/// </summary>
	/// <remarks>
	///
	/// </remarks>
	private void LookupItem()
	{
		_table.Lookup<Highscore>(_score.id, OnLookupItemCompleted);
	}

	private void OnLookupItemCompleted(IRestResponse<Highscore> response)
	{
		Debug.Log("OnLookupItemCompleted: " + response.Content );
		if (response.StatusCode == HttpStatusCode.OK)
		{
			Highscore item = response.Data;
			_score = item;
		}
		else
		{
			ResponseError err = JsonReader.Deserialize<ResponseError>(response.Content);
			Debug.Log("Error Status:" + response.StatusCode + " Code:" + err.code.ToString() + " " + err.error);
		}
	}
	//*/
}
