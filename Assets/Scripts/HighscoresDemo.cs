using UnityEngine;
using System;
using System.Net;
using System.Collections.Generic;
using RestSharp;
using Pathfinding.Serialization.JsonFx;
using Unity3dAzure.AppServices;
using UnityEngine.UI;
using Tacticsoft;
using Prefabs;
using UnityEngine.SceneManagement;

[CLSCompliant(false)]
public class HighscoresDemo : MonoBehaviour, ITableViewDataSource
{
	/// <remarks>
	/// Enter your Azure App Service url
	/// </remarks>

	[Header("Azure App Service")]
	// Azure Mobile App connection strings
	[SerializeField]
	private string _appUrl = "PASTE_YOUR_APP_URL";

	// Go to https://developers.facebook.com/tools/accesstoken/ to generate a new access "User Token"
	private string _facebookAccessToken;

	// App Service Rest Client
	private MobileServiceClient _client;

	// App Service Table defined using a DataModel
	private MobileServiceTable<Highscore> _table;

	// List of highscores (leaderboard)
	private List<Highscore> _scores = new List<Highscore>();

	private Highscore _score;

	[Header("UI")]
	// TSTableView for displaying list of results
	[SerializeField]
	private TableView _tableView;
	[SerializeField]
	private ScoreCell _cellPrefab;
	bool HasNewData = false; // to reload table view when data has changed

	[Space(10)]
	[SerializeField]
	private ModalAlert _modalAlert; 
	private Message _message;

	// Use this for initialization
	void Start ()
	{
		// Create App Service client (Using factory Create method to force 'https' url)
		_client = MobileServiceClient.Create(_appUrl); //new MobileServiceClient(_appUrl);

		// Get App Service 'Highscores' table
		_table = _client.GetTable<Highscore>("Highscores");

		// set TSTableView delegate
		_tableView.dataSource = this;

		UpdateUI();
	}

	// Update is called once per frame
	void Update () 
	{
		// Only update table when there is new data
		if (HasNewData) {
			Debug.Log ("Refresh Table Data");
			_tableView.ReloadData ();
			HasNewData = false;
		}
		// Display new score details 
		if (_score != null) {
			Debug.Log ("Show score");
			DisplayScore (_score);
			_score = null;
		}
		// Display modal where there is a new message
		if (_message != null) {
			Debug.Log ("Show message:" + _message.message);
			_modalAlert.Show(_message.message, _message.title);
			_message = null;
		}
	}

	public void Login()
	{
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
			_message = Message.Create ("Login failed", "Error");
		}
	}

	public void Insert()
	{
		Highscore score = GetScore ();
		if ( Validate(score) )
		{
			_table.Insert<Highscore>(score, OnInsertCompleted);
		}
	}

	private void OnInsertCompleted(IRestResponse<Highscore> response)
	{
		if (response.StatusCode == HttpStatusCode.Created)
		{
			Debug.Log( "OnInsertItemCompleted: " + response.Data );
        	Highscore item = response.Data; // if successful the item will have an 'id' property value
			_score = item;
		}
		else
		{
			Debug.Log("Insert Error Status:" + response.StatusCode + " Uri: "+response.ResponseUri );
		}
	}

	public void UpdateScore()
	{
		Highscore score = GetScore ();
		if ( Validate(score) )
		{
			_table.Update<Highscore>(score, OnUpdateScoreCompleted);
		}
	}

	private void OnUpdateScoreCompleted(IRestResponse<Highscore> response)
	{
		if (response.StatusCode == HttpStatusCode.OK)
		{
			Debug.Log("OnUpdateItemCompleted: " + response.Content );
		}
		else
		{
			Debug.Log("Update Error Status:" + response.StatusCode +" "+ response.ErrorMessage + " Uri: "+response.ResponseUri );
		}
	}

	public void Delete()
	{
		Highscore score = GetScore ();
		_table.Delete<Highscore>(score.id, OnDeleteCompleted);
	}

	private void OnDeleteCompleted(IRestResponse<Highscore> response)
	{
		if (response.StatusCode == HttpStatusCode.OK)
		{
			Debug.Log("OnDeleteItemCompleted");
		}
		else
		{
			Debug.Log("Delete Error Status:" + response.StatusCode +" "+ response.ErrorMessage + " Uri: "+response.ResponseUri );
		}
	}

	public void Read()
	{
		_table.Read<Highscore>(OnReadCompleted);
	}

	private void OnReadCompleted(IRestResponse<List<Highscore>> response)
	{
		if (response.StatusCode == HttpStatusCode.OK)
		{
			Debug.Log("OnReadCompleted data: " + response.ResponseUri +" data: "+ response.Content);
        	List<Highscore> items = response.Data;
        	Debug.Log("Read items count: " + items.Count);
			_scores = items;
			HasNewData = true;
		}
		else
		{
			Debug.Log("Read Error Status:" + response.StatusCode + " Uri: "+response.ResponseUri );
		}
	}

	private void OnReadNestedResultsCompleted(IRestResponse<NestedResults<Highscore>> response)
	{
		if (response.StatusCode == HttpStatusCode.OK)
		{
			Debug.Log("OnReadNestedResultsCompleted: " + response.ResponseUri +" data: "+ response.Content);
			List<Highscore> items = response.Data.results;
			Debug.Log("Read items count: " + items.Count + "/" + response.Data.count);
			_scores = items;
			HasNewData = true;
		}
		else
		{
			Debug.Log("Read Nested Results Error Status:" + response.StatusCode + " Uri: "+response.ResponseUri );
		}
	}

	public void GetAllHighscores()
	{
		CustomQuery query = new CustomQuery ("", "score desc", 50, 0, "id,username,score"); //CustomQuery.OrderBy ("score desc");
		_table.Query<NestedResults<Highscore>>(query, OnReadNestedResultsCompleted); //Query(query);
	}


	public void GetTopHighscores()
	{
		DateTime today = DateTime.Today;
		string day = today.ToString("s");
		string filter = string.Format("createdAt gt '{0}Z'", day); //string.Format("score gt {0}", 999);
		Debug.Log ("filter:" + filter);
		string orderBy = "score desc";
		CustomQuery query = new CustomQuery(filter,orderBy,10);
		Query(query);
	}

	public void GetUsernameHighscore()
	{
		Highscore score = GetScore ();
		string filter = string.Format("username eq '{0}'", score.username); // string.Format("startswith(username,'{0}')", score.username);
		string orderBy = "score desc";
		CustomQuery query = new CustomQuery(filter,orderBy);
		Query(query);
	}

	private void Query(CustomQuery query)
	{
		_table.Query<Highscore>(query, OnReadCompleted);
	}

	/// <summary>
	/// This is an example showing how to get an item using it's id. For example if $select columns are specified and the returned data is limited then this can be used to get all the details by using the items's id.
	/// </summary>
	public void Lookup()
	{
		Highscore score = GetScore ();
		_table.Lookup<Highscore>(score.id, OnLookupCompleted);
	}

	private void OnLookupCompleted(IRestResponse<Highscore> response)
	{
		Debug.Log("OnLookupItemCompleted: " + response.Content );
		if (response.StatusCode == HttpStatusCode.OK)
		{
			Highscore item = response.Data;
			_score = item;
			// show message with some details
			string message = string.Format ("Scored {0} points on {1}", _score.score, _score.createdAt);
			_message = Message.Create(message, _score.username);
		}
		else
		{
			ResponseError err = JsonReader.Deserialize<ResponseError>(response.Content);
			Debug.Log("Lookup Error Status:" + response.StatusCode + " Code:" + err.code.ToString() + " " + err.error);
		}
	}

	#region Easy APIs

	/// <summary>
	/// This demo 'hello' custom api just gets a response message eg. '{"message":"Hello World!"}'
	/// </summary>
	public void Hello()
	{
		_client.InvokeApi<Message>("hello", OnCustomApiCompleted);
	}

	public void GenerateScores()
	{
		_client.InvokeApi<Message>("GenerateScores", OnCustomApiCompleted);
	}

	private void OnCustomApiCompleted(IRestResponse<Message> response)
	{
		if (response.StatusCode == HttpStatusCode.OK)
		{
			Debug.Log("OnCustomApiCompleted data: " + response.Content);
       		Message message = response.Data;
			Debug.Log( "Result: " + message);
			_message = message;
		}
		else
		{
			Debug.Log("Api Error Status:" + response.StatusCode + " Uri: "+response.ResponseUri );
		}
	}

	#endregion

	#region UI

	/// <summary>
	/// Create data model from UI
	/// </summary>
	private Highscore GetScore() 
	{
		string name = GameObject.Find("InputName").GetComponent<InputField> ().text;
		string score = GameObject.Find("InputScore").GetComponent<InputField> ().text;
		string id = GameObject.Find("Id").GetComponent<Text> ().text;
		Highscore highscore = new Highscore();
		highscore.username = name;
		if (!String.IsNullOrEmpty (score)) {
			highscore.score = Convert.ToInt32 (score);
		}
		if (!String.IsNullOrEmpty (id)) {
			highscore.id = id;
			Debug.Log ("Existing Id:" + id);
		}
		return highscore;
	}

	/// <summary>
	/// Update UI with data model 
	/// </summary>
	private void DisplayScore(Highscore highscore) 
	{
		InputField name = GameObject.Find("InputName").GetComponent<InputField> ();
		InputField score = GameObject.Find("InputScore").GetComponent<InputField> ();
		Text id = GameObject.Find("Id").GetComponent<Text> ();
		name.text = highscore.username;
		score.text = highscore.score.ToString ();
		id.text = highscore.id;
		UpdateUI ();
	}

	/// <summary>
	/// Reset UI to insert new score
	/// </summary>
	public void ClearScore() {
		InputField name = GameObject.Find("InputName").GetComponent<InputField> ();
		InputField score = GameObject.Find("InputScore").GetComponent<InputField> ();
		Text id = GameObject.Find("Id").GetComponent<Text> ();
		name.text = "";
		score.text = "";
		id.text = "";
		UpdateUI ();
	}

	/// <summary>
	/// Validate data before sending
	/// </summary>
	private bool Validate(Highscore highscore)
	{
		bool isUsernameValid=true, isScoreValid=true;
		// Validate username
		if (String.IsNullOrEmpty (highscore.username)) {
			isUsernameValid = false;
			Debug.Log ("Error, player username required");
		}
		// Validate score
		if ( !(highscore.score > 0) )
		{
			isScoreValid = false;
			Debug.Log ("Error, player score should be greater than 0");
		}
		UpdateText ("Player", isUsernameValid);
		UpdateText ("Score", isScoreValid);
		return (isUsernameValid && isScoreValid);
	}

	/// <summary>
	/// Change text color to highlight errors
	/// </summary>
	private void UpdateText(string gameObjectName, bool isValid=true)
	{
		Text text = GameObject.Find(gameObjectName).GetComponent<Text> ();
		if (text) {
			text.color = isValid ? Color.white : Color.red;
		}
	}

	/// <summary>
	/// Handler for text changed event to update view state
	/// </summary>
	public void TextChanged() 
	{
		UpdateUI ();
	}

	/// <summary>
	/// Method to manage UI view state
	/// </summary>
	private void UpdateUI() 
	{
		// Activate login button if Facebook Access Token is entered
		Button login = GameObject.Find("Login").GetComponent<Button> ();
		_facebookAccessToken = GameObject.Find("FacebookAccessToken").GetComponent<InputField> ().text;
		login.interactable = String.IsNullOrEmpty (_facebookAccessToken) ? false : true ;

		// Insert or Update mode
		Text id = GameObject.Find("Id").GetComponent<Text> ();
		GameObject insert = GameObject.Find("GroupInsert");
		GameObject update = GameObject.Find("GroupUpdate");
		CanvasGroup groupInsert = insert.GetComponent<CanvasGroup> ();
		CanvasGroup groupUpdate = update.GetComponent<CanvasGroup> ();
		if ( String.IsNullOrEmpty(id.text) )
		{
			groupInsert.alpha = 1; 
			groupUpdate.alpha = 0;
			groupInsert.interactable = true;
			groupUpdate.interactable = false;
		} 
		else 
		{
			groupInsert.alpha = 0; 
			groupUpdate.alpha = 1;
			groupInsert.interactable = false;
			groupUpdate.interactable = true;
		}

		// Close dialog if no message
		if (_message == null) 
		{
			_modalAlert.Close();
		}
	}

	#endregion

	#region ITableViewDataSource

	public int GetNumberOfRowsForTableView(TableView tableView)
	{
		return _scores.Count;
	}

	public float GetHeightForRowInTableView(TableView tableView, int row)
	{
		return (_cellPrefab.transform as RectTransform).rect.height; //50.0f;
	}

	public TableViewCell GetCellForRowInTableView(TableView tableView, int row)
	{
		ScoreCell cell = tableView.GetReusableCell(_cellPrefab.reuseIdentifier) as ScoreCell;
		if (cell == null) {
			cell = (ScoreCell)GameObject.Instantiate (_cellPrefab);
		}
		Highscore data = _scores [row];
		cell.Name.text = data.username;
		cell.Score.text = data.score.ToString();
		cell.Rank.text = (row+1).ToString();
		cell.Btn.name = row.ToString(); // save index to button name
		return cell;
	}

	#endregion

	/// <summary>
	/// Handler to get selected row item
	/// </summary>
	public void OnSelectedRow(Button button) {
		int index = Convert.ToInt32 (button.name);
		//Debug.Log("Selected index:" + index);
		if (index >= _scores.Count) {
			return;
		}
		Highscore score = _scores [index];
		Debug.Log ("Selected:" + score.ToString());
		_score = score; // update editor with selected item
	}

	/// <summary>
	/// Handler to go to next scene
	/// </summary>
	public void GoNextScene() {
		SceneManager.LoadScene ("InventoryDemo");
	}
}
