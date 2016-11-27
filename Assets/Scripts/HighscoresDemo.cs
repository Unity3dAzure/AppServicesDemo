using UnityEngine;
using System;
using System.Net;
using System.Collections.Generic;
using Unity3dAzure.AppServices;
using UnityEngine.UI;
using Tacticsoft;
using Prefabs;
using UnityEngine.SceneManagement;
using System.Linq;

public class HighscoresDemo : MonoBehaviour, ITableViewDataSource
{
	/// <remarks>
	/// Enter your Azure App Service url
	/// </remarks>

	[Header ("Azure App Service")]
	// Azure Mobile App connection strings
	[SerializeField]
	private string _appUrl = "PASTE_YOUR_APP_URL";

	// Go to https://developers.facebook.com/tools/accesstoken/ to generate a new access "User Token"
	[Header ("User Authentication")]
	[SerializeField]
	private string _facebookAccessToken = "";

	// App Service Rest Client
	private MobileServiceClient _client;

	// App Service Table defined using a DataModel
	private MobileServiceTable<Highscore> _table;

	// List of highscores (leaderboard)
	private List<Highscore> _scores = new List<Highscore> ();

	private Highscore _score;

	[Header ("UI")]
	// TSTableView for displaying list of results
	[SerializeField]
	private TableView _tableView;
	[SerializeField]
	private ScoreCell _cellPrefab;
	bool HasNewData = false;
	// to reload table view when data has changed

	// infinite scroll vars
	private bool _isPaginated = false;
	// only enable infinite scrolling for paginated results
	private const float _infiniteScrollSize = 0.2f;
	private bool _isLoadingNextPage = false;
	// load once when scroll buffer is hit
	private const uint _noPageResults = 50;
	private uint _skip = 0;
	// no of records to skip
	private uint _totalCount = 0;
	// count value should be > 0 to paginate

	[Space (10)]
	[SerializeField]
	private ModalAlert _modalAlert;
	private Message _message;

	// Use this for initialization
	void Start ()
	{
		// Create App Service client
		_client = new MobileServiceClient (_appUrl);

		// Get App Service 'Highscores' table
		_table = _client.GetTable<Highscore> ("Highscores");

		// set TSTableView delegate
		_tableView.dataSource = this;

		// setup token using Unity Inspector value
		if (!String.IsNullOrEmpty (_facebookAccessToken)) {
			InputField inputToken = GameObject.Find ("FacebookAccessToken").GetComponent<InputField> ();
			inputToken.text = _facebookAccessToken;
		}

		UpdateUI ();
	}

	// Update is called once per frame
	void Update ()
	{
		// Only update table when there is new data
		if (_tableView != null && HasNewData) {
			Debug.Log ("Refresh Table Data");
			SetInteractableScrollbars (false);
			_tableView.ReloadData ();
			SetInteractableScrollbars (true);
			HasNewData = false;
		}
		// Display new score details 
		if (_score != null) {
			Debug.Log ("Show score:" + _score.ToString ());
			DisplayScore (_score);
			_score = null;
		}
		// Display modal where there is a new message
		if (_message != null) {
			Debug.Log ("Show message:" + _message.message);
			_modalAlert.Show (_message.message, _message.title);
			_message = null;
		}
	}

	public void Login ()
	{
		StartCoroutine (_client.Login (MobileServiceAuthenticationProvider.Facebook, _facebookAccessToken, OnLoginCompleted));
	}

	private void OnLoginCompleted (IRestResponse<MobileServiceUser> response)
	{
		Debug.Log ("OnLoginCompleted: " + response.Content + " Url:" + response.Url);

		if (!response.IsError && response.StatusCode == HttpStatusCode.OK) {
			MobileServiceUser mobileServiceUser = response.Data;
			_client.User = mobileServiceUser;
			Debug.Log ("Authorized UserId: " + _client.User.user.userId);
		} else {
			Debug.LogWarning ("Authorization Error: " + response.StatusCode);
			_message = Message.Create ("Login failed", "Error");
		}
	}

	public void Insert ()
	{
		Highscore score = GetScore ();
		if (Validate (score)) {
			StartCoroutine (_table.Insert<Highscore> (score, OnInsertCompleted));
		}
	}

	private void OnInsertCompleted (IRestResponse<Highscore> response)
	{
		if (!response.IsError && response.StatusCode == HttpStatusCode.Created) {
			Debug.Log ("OnInsertItemCompleted: " + response.Content + " status code:" + response.StatusCode + " data:" + response.Data);
			Highscore item = response.Data; // if successful the item will have an 'id' property value
			_score = item;
		} else {
			Debug.LogWarning ("Insert Error Status:" + response.StatusCode + " Url: " + response.Url);
		}
	}

	public void UpdateScore ()
	{
		Highscore score = GetScore ();
		if (Validate (score)) {
			StartCoroutine (_table.Update<Highscore> (score, OnUpdateScoreCompleted));
		}
	}

	private void OnUpdateScoreCompleted (IRestResponse<Highscore> response)
	{
		if (!response.IsError) {
			Debug.Log ("OnUpdateItemCompleted: " + response.Content);
		} else {
			Debug.LogWarning ("Update Error Status:" + response.StatusCode + " " + response.ErrorMessage + " Url: " + response.Url);
		}
	}

	public void Delete ()
	{
		Highscore score = GetScore ();
		StartCoroutine (_table.Delete<Highscore> (score.id, OnDeleteCompleted));
	}

	private void OnDeleteCompleted (IRestResponse<Highscore> response)
	{
		if (!response.IsError) {
			Debug.Log ("OnDeleteItemCompleted");
		} else {
			Debug.LogWarning ("Delete Error Status:" + response.StatusCode + " " + response.ErrorMessage + " Url: " + response.Url);
		}
	}

	public void Read ()
	{
		StartCoroutine (_table.Read<Highscore> (OnReadCompleted));
	}

	private void OnReadCompleted (IRestResponse<Highscore[]> response)
	{
		if (!response.IsError) {
			Debug.Log ("OnReadCompleted: " + response.Url + " data: " + response.Content);
			Highscore[] items = response.Data;
			_isPaginated = false; // default query has max. of 50 records and is not paginated so disable infinite scroll 
			_scores = items.ToList ();
			HasNewData = true;
		} else {
			Debug.LogWarning ("Read Error Status:" + response.StatusCode + " Url: " + response.Url);
		}
	}

	private void OnReadNestedResultsCompleted (IRestResponse<NestedResults<Highscore>> response)
	{
		if (!response.IsError) {
			Debug.Log ("OnReadNestedResultsCompleted: " + response.Url + " data: " + response.Content);
			Highscore[] items = response.Data.results;
			_totalCount = response.Data.count;
			Debug.Log ("Read items count: " + items.Length + "/" + response.Data.count);
			_isPaginated = true; // nested query will support pagination
			if (_skip != 0) {
				_scores.AddRange (items.ToList ());
			} else {
				_scores = items.ToList (); // set for first page of results
			}
			HasNewData = true;
		} else {
			Debug.LogWarning ("Read Nested Results Error Status:" + response.StatusCode.ToString () + " Url: " + response.Url);
		}
		_isLoadingNextPage = false; // allows next page to be loaded
	}

	public void GetAllHighscores ()
	{
		ResetList ();
		GetPageHighscores ();
	}

	private void GetPageHighscores ()
	{
		CustomQuery query = new CustomQuery ("", "score desc", _noPageResults, _skip, "id,username,score");
		StartCoroutine (_table.Query<Highscore> (query, OnReadNestedResultsCompleted));
	}

	public void GetTopHighscores ()
	{
		ResetList ();
		DateTime today = DateTime.Today;
		string day = today.ToString ("s");
		string filter = string.Format ("createdAt gt '{0}Z'", day);
		Debug.Log ("filter:" + filter);
		string orderBy = "score desc";
		CustomQuery query = new CustomQuery (filter, orderBy, 10);
		Query (query);
	}

	public void GetUsernameHighscore ()
	{
		ResetList ();
		Highscore score = GetScore ();
		string filter = string.Format ("username eq '{0}'", score.username);
		string orderBy = "score desc";
		CustomQuery query = new CustomQuery (filter, orderBy);
		Query (query);
	}

	private void Query (CustomQuery query)
	{
		StartCoroutine (_table.Query<Highscore> (query, OnReadCompleted));
	}

	private void ResetList ()
	{
		_skip = 0;
		_scores = new List<Highscore> ();
		_tableView.ReloadData ();
	}

	/// <summary>
	/// This is an example showing how to get an item using it's id. For example if $select columns are specified and the returned data is limited then this can be used to get all the details by using the items's id.
	/// </summary>
	public void Lookup ()
	{
		Highscore score = GetScore ();
		StartCoroutine (_table.Lookup<Highscore> (score.id, OnLookupCompleted));
	}

	private void OnLookupCompleted (IRestResponse<Highscore> response)
	{
		if (!response.IsError) {
			Highscore item = response.Data;
			_score = item;
			// show message with some details
			string message = string.Format ("Scored {0} points on {1}", _score.score, _score.CreatedAt ());
			_message = Message.Create (message, _score.username);
		} else {
			// TODO: Parse response error <ResponseError>(response.Content);
			Debug.Log ("Lookup Error Status:" + response.StatusCode);
		}
	}

	#region Easy APIs

	/// <summary>
	/// This demo 'hello' custom api just gets a response message eg. '{"message":"Hello World!"}'
	/// </summary>
	public void Hello ()
	{
		StartCoroutine (_client.InvokeApi<Message> ("hello", Method.GET, OnCustomApiCompleted));
	}

	public void GenerateScores ()
	{
		StartCoroutine (_client.InvokeApi<Message> ("GenerateScores", Method.POST, OnCustomApiCompleted));
	}

	private void OnCustomApiCompleted (IRestResponse<Message> response)
	{
		if (!response.IsError) {
			Debug.Log ("OnCustomApiCompleted data: " + response.Content);
			Message message = response.Data;
			Debug.Log ("Result: " + message);
			_message = message;
		} else {
			Debug.Log ("Api Error Status:" + response.StatusCode + " Url: " + response.Url);
		}
	}

	#endregion

	#region UI

	/// <summary>
	/// Create data model from UI
	/// </summary>
	private Highscore GetScore ()
	{
		string name = GameObject.Find ("InputName").GetComponent<InputField> ().text;
		string score = GameObject.Find ("InputScore").GetComponent<InputField> ().text;
		string id = GameObject.Find ("Id").GetComponent<Text> ().text;
		Highscore highscore = new Highscore ();
		highscore.username = name;
		if (!String.IsNullOrEmpty (score)) {
			highscore.score = Convert.ToUInt32 (score);
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
	private void DisplayScore (Highscore highscore)
	{
		InputField name = GameObject.Find ("InputName").GetComponent<InputField> ();
		InputField score = GameObject.Find ("InputScore").GetComponent<InputField> ();
		Text id = GameObject.Find ("Id").GetComponent<Text> ();
		name.text = highscore.username;
		score.text = highscore.score.ToString ();
		id.text = highscore.id;
		UpdateUI ();
	}

	/// <summary>
	/// Reset UI to insert new score
	/// </summary>
	public void ClearScore ()
	{
		InputField name = GameObject.Find ("InputName").GetComponent<InputField> ();
		InputField score = GameObject.Find ("InputScore").GetComponent<InputField> ();
		Text id = GameObject.Find ("Id").GetComponent<Text> ();
		name.text = "";
		score.text = "";
		id.text = "";
		UpdateUI ();
	}

	/// <summary>
	/// Validate data before sending
	/// </summary>
	private bool Validate (Highscore highscore)
	{
		bool isUsernameValid = true, isScoreValid = true;
		// Validate username
		if (String.IsNullOrEmpty (highscore.username)) {
			isUsernameValid = false;
			Debug.LogWarning ("Error, player username required");
		}
		// Validate score
		if (!(highscore.score > 0)) {
			isScoreValid = false;
			Debug.LogWarning ("Error, player score should be greater than 0");
		}
		UpdateText ("Player", isUsernameValid);
		UpdateText ("Score", isScoreValid);
		return (isUsernameValid && isScoreValid);
	}

	/// <summary>
	/// Change text color to highlight errors
	/// </summary>
	private void UpdateText (string gameObjectName, bool isValid = true)
	{
		Text text = GameObject.Find (gameObjectName).GetComponent<Text> ();
		if (text) {
			text.color = isValid ? Color.white : Color.red;
		}
	}

	/// <summary>
	/// Handler for text changed event to update view state
	/// </summary>
	public void TextChanged ()
	{
		UpdateUI ();
	}

	/// <summary>
	/// Method to manage UI view state
	/// </summary>
	private void UpdateUI ()
	{
		// Activate login button if Facebook Access Token is entered
		Button login = GameObject.Find ("Login").GetComponent<Button> ();
		_facebookAccessToken = GameObject.Find ("FacebookAccessToken").GetComponent<InputField> ().text;
		login.interactable = String.IsNullOrEmpty (_facebookAccessToken) ? false : true;

		// Insert or Update mode
		Text id = GameObject.Find ("Id").GetComponent<Text> ();
		GameObject insert = GameObject.Find ("GroupInsert");
		GameObject update = GameObject.Find ("GroupUpdate");
		CanvasGroup groupInsert = insert.GetComponent<CanvasGroup> ();
		CanvasGroup groupUpdate = update.GetComponent<CanvasGroup> ();
		if (String.IsNullOrEmpty (id.text)) {
			groupInsert.alpha = 1; 
			groupUpdate.alpha = 0;
			groupInsert.interactable = true;
			groupUpdate.interactable = false;
		} else {
			groupInsert.alpha = 0; 
			groupUpdate.alpha = 1;
			groupInsert.interactable = false;
			groupUpdate.interactable = true;
		}

		// Close dialog if no message
		if (_message == null) {
			_modalAlert.Close ();
		}
	}

	#endregion

	#region TSTableView ITableViewDataSource

	public int GetNumberOfRowsForTableView (TableView tableView)
	{
		return _scores.Count;
	}

	public float GetHeightForRowInTableView (TableView tableView, int row)
	{
		return (_cellPrefab.transform as RectTransform).rect.height;
	}

	public TableViewCell GetCellForRowInTableView (TableView tableView, int row)
	{
		ScoreCell cell = tableView.GetReusableCell (_cellPrefab.reuseIdentifier) as ScoreCell;
		if (cell == null) {
			cell = (ScoreCell)GameObject.Instantiate (_cellPrefab);
		}
		Highscore data = _scores [row];
		cell.Name.text = data.username;
		cell.Score.text = data.score.ToString ();
		cell.Rank.text = (row + 1).ToString ();
		cell.Btn.name = row.ToString (); // save index to button name
		return cell;
	}

	#endregion

	/// <summary>
	/// Handler to get selected row item
	/// </summary>
	public void OnSelectedRow (Button button)
	{
		int index = Convert.ToInt32 (button.name);
		//Debug.Log("Selected index:" + index); //Count
		if (index >= _scores.Count) {
			return;
		}
		Highscore score = _scores [index];
		Debug.Log ("Selected:" + score.ToString ());
		_score = score; // update editor with selected item
	}

	#region Infinite Scroll private methods for Highscores

	void OnEnable ()
	{
		_tableView.GetComponent<ScrollRect> ().onValueChanged.AddListener (OnScrollValueChanged);
	}

	void OnDisable ()
	{
		if (_tableView != null) {
			_tableView.GetComponent<ScrollRect> ().onValueChanged.RemoveListener (OnScrollValueChanged);
		}
	}

	private void OnScrollValueChanged (Vector2 newScrollValue)
	{
		// skip if not paginated results, or if no items, or if busy already loading next page of results
		if (!_isPaginated || _totalCount == 0 || _isLoadingNextPage) {
			return;
		}
		//Debug.Log (string.Format("Scroll y:{0} table view scroll: {1}/{2}", newScrollValue.y, _tableView.scrollY, _tableView.scrollableHeight));
		float scrollY = _tableView.scrollableHeight - _tableView.scrollY;
		float scrollBuffer = _infiniteScrollSize * _tableView.scrollableHeight;
		// scrollY is still at 'top' and so no need to load anything at this point
		if (scrollY > scrollBuffer) {
			return;
		}
		// scrollY has reached 'bottom' minus buffer size
		// only trigger request if there are more records to load
		if (_skip < _totalCount) {
			_isLoadingNextPage = true;
			_skip += _noPageResults;
			//Debug.Log (string.Format("Load next page @{0} scroll: {1}<{2}", _skip, scrollY, scrollBuffer));
			GetPageHighscores ();
		}
	}

	// Tip: When infinite scrolling and using TSTableView's ReloadData() method I prefer to wrap "disable and enable scrollbar" calls around it to help prevent jumpy behaviour when continously dragging the scrollbar thumb.
	private void SetInteractableScrollbars (bool isInteractable)
	{
		Scrollbar[] scrollbars = _tableView.GetComponentsInChildren<Scrollbar> ();
		foreach (Scrollbar scrollbar in scrollbars) {
			//Debug.Log (string.Format("Scrollbar {0} is {1}",scrollbar.name, isInteractable));
			scrollbar.interactable = isInteractable;
		}
	}

	#endregion

	/// <summary>
	/// Handler to go to next scene
	/// </summary>
	public void GoNextScene ()
	{
		SceneManager.LoadScene ("InventoryDemo");
	}
}
