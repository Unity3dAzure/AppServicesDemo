using Azure.AppServices;
using RESTClient;
using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Net;
using Tacticsoft;
using Prefabs;
using UnityEngine.SceneManagement;

/// <summary>
/// Virtual inventory item used to populate table cell
/// </summary>
public class InventoryItem
{
	public string name;
	public uint amount;
}

public class InventoryDemo : MonoBehaviour, ITableViewDataSource
{

	/// <remarks>
	/// Enter your Azure App Service url
	/// </remarks>

	[Header ("Azure App Service")]
	// Azure Mobile App connection strings
	[SerializeField]
	private string _appUrl = "PASTE_YOUR_APP_URL";

	[Header ("User Authentication")]
	// Client-side login requires auth token from identity provider.
	// NB: Remember to update Azure App Service authentication provider with your app key and secret and SAVE changes!

	// Facebook - go to https://developers.facebook.com/tools/accesstoken/ to generate a "User Token".
	[Header ("Facebook")]
	[SerializeField]
	private string _facebookUserToken = "";

	// Twitter auth - go to https://apps.twitter.com/ and select "Keys and Access Tokens" to generate a access "Access Token" and "Access Token Secret".
	[Header ("Twitter")]
	[SerializeField]
	private string _twitterAccessToken = "";
	[SerializeField]
	private string _twitterTokenSecret = "";

	// App Service Rest Client
	private AppServiceClient _client;

	// App Service Table defined using a DataModel
	private AppServiceTable<Inventory> _table;

	// Data
	private Inventory _inventory;

	// List of virtual items (inventory)
	private List<InventoryItem> _items = new List<InventoryItem> ();

	[Header ("UI")]
	// TSTableView for displaying list of results
	[SerializeField]
	private TableView _tableView;
	[SerializeField]
	private InventoryCell _cellPrefab;
	bool HasNewData = false;
	// to reload table view when data has changed

	[Space (10)]
	[SerializeField]
	private ModalAlert _modalAlert;
	private Message _message;

	// to show user's inventory after successful login
	bool DidLogin = false;

	// Use this for initialization
	void Start ()
	{
		// Create App Service client
		_client = new AppServiceClient (_appUrl);

		// Get App Service 'Highscores' table
		_table = _client.GetTable<Inventory> ("Inventory");

		// set TSTableView delegate
		_tableView.dataSource = this;

		// hide controls until login
		CanvasGroup group = GameObject.Find ("UserDataGroup").GetComponent<CanvasGroup> ();
		group.alpha = 0;
		group.interactable = false;

		UpdateUI ();
	}

	// Update is called once per frame
	void Update ()
	{
		// show controls after login
		if (DidLogin) {
			CanvasGroup group = GameObject.Find ("UserDataGroup").GetComponent<CanvasGroup> ();
			group.alpha = 1;
			group.interactable = true;
			DidLogin = false;
		}
		// update editor fields and reload table data
		if (HasNewData && _inventory != null) {
			InputField strawberries = GameObject.Find ("Input1").GetComponent<InputField> ();
			InputField melons = GameObject.Find ("Input2").GetComponent<InputField> ();
			InputField lemons = GameObject.Find ("Input3").GetComponent<InputField> ();
			InputField medicine = GameObject.Find ("Input4").GetComponent<InputField> ();
			//Debug.Log ("strawberries: " + _inventory.strawberries + " melons: " + _inventory.melons + " lemons: " + _inventory.lemons + " medicine: " + _inventory.medicine);
			strawberries.text = _inventory.strawberries.ToString ();
			melons.text = _inventory.melons.ToString ();
			lemons.text = _inventory.lemons.ToString ();
			medicine.text = _inventory.medicine.ToString ();
			ReloadTableData ();
			HasNewData = false;
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
		if (!string.IsNullOrEmpty(_facebookUserToken)) 
		{
			StartCoroutine (_client.LoginWithFacebook (_facebookUserToken, OnLoginCompleted));
			return;
		}
		if (!string.IsNullOrEmpty(_twitterAccessToken) && !string.IsNullOrEmpty(_twitterTokenSecret)) 
		{
			StartCoroutine (_client.LoginWithTwitter (_twitterAccessToken, _twitterTokenSecret, OnLoginCompleted));
			return;
		}
		Debug.LogWarning("Login requires Facebook or Twitter access tokens");
	}

	private void OnLoginCompleted (IRestResponse<AuthenticatedUser> response)
	{
		if (!response.IsError) {
			Debug.Log ("OnLoginCompleted: " + response.Content + " Status: " + response.StatusCode + " Url:" + response.Url);
			Debug.Log ("Authorized UserId: " + _client.User.user.userId);
			DidLogin = true;
			Load (); // auto load user data
		} else {
			Debug.LogWarning ("Authorization Error: " + response.StatusCode);
			_message = Message.Create ("Login failed", "Error");
		}
	}

	public void Load ()
	{
		string filterPredicate = string.Format ("userId eq '{0}'", _client.User.user.userId);
		TableQuery query = new TableQuery (filterPredicate);
		Debug.Log ("Load data for UserId: " + _client.User.user.userId + " query:" + query);

		StartCoroutine (_table.Query<Inventory> (query, OnLoadCompleted));
	}

	private void OnLoadCompleted (IRestResponse<Inventory[]> response)
	{
		if (!response.IsError) {
			Debug.Log ("OnLoadItemsCompleted data: " + response.Content);
			Inventory[] results = response.Data;
			Debug.Log ("Load results count: " + results.Length);
			// no record 
			if (results.Length == 0) {
				_inventory = new Inventory ();
			}
			if (results.Length >= 1) {
				Debug.Log ("inventory result: " + results [0]);
				_inventory = results [0];
			}
			HasNewData = true;
		} else {
			Debug.LogWarning ("Read Error Status:" + response.StatusCode + " Url: " + response.Url);
		}
	}

	public void Save ()
	{
		if (_inventory == null) {
			Debug.Log ("Error, no inventory");
			return;
		}
		// If new Insert, else Update existing user inventory
		if (String.IsNullOrEmpty (_inventory.id)) {
			InsertInventory ();
		} else {
			UpdateInventory ();
		}
	}

	private void InsertInventory ()
	{
		RecalculateInventoryItems ();
		Debug.Log ("Insert:" + _inventory.ToString ());
		StartCoroutine (_table.Insert<Inventory> (_inventory, OnInsertCompleted));
	}

	private void OnInsertCompleted (IRestResponse<Inventory> response)
	{
		if (!response.IsError && response.StatusCode == HttpStatusCode.Created) {
			Debug.Log ("OnInsertItemCompleted: " + response.Data);
			Inventory item = response.Data; // if successful the item will have an 'id' property value
			_inventory = item;
			_message = Message.Create ("Inventory saved", "Inserted"); // show confirmation message
		} else {
			Debug.LogWarning ("Insert Error Status:" + response.StatusCode + " " + response.ErrorMessage + " Url: " + response.Url);
		}
	}

	private void UpdateInventory ()
	{
		RecalculateInventoryItems ();
		Debug.Log ("Update:" + _inventory.ToString ());
		StartCoroutine (_table.Update<Inventory> (_inventory, OnUpdateCompleted));
	}

	private void OnUpdateCompleted (IRestResponse<Inventory> response)
	{
		if (!response.IsError) {
			Debug.Log ("OnUpdateCompleted: " + response.Content);
			_message = Message.Create ("Inventory saved", "Updated"); // show confirmation message
		} else {
			Debug.LogWarning ("Update Error Status:" + response.StatusCode + " " + response.ErrorMessage + " Url: " + response.Url);
		}
	}

	#region UI

	/// <summary>
	/// Update inventory using input values
	/// </summary>
	private void RecalculateInventoryItems ()
	{
		if (_inventory == null) {
			Debug.Log ("Error, no inventory");
		}
		InputField strawberries = GameObject.Find ("Input1").GetComponent<InputField> ();
		InputField melons = GameObject.Find ("Input2").GetComponent<InputField> ();
		InputField lemons = GameObject.Find ("Input3").GetComponent<InputField> ();
		InputField medicine = GameObject.Find ("Input4").GetComponent<InputField> ();
		_inventory.strawberries = Convert.ToUInt32 (strawberries.text);
		_inventory.melons = Convert.ToUInt32 (melons.text);
		_inventory.lemons = Convert.ToUInt32 (lemons.text);
		_inventory.medicine = Convert.ToUInt32 (medicine.text);
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
		// Close dialog if no message
		if (_message == null) {
			_modalAlert.Close ();
		}
	}

	/// <summary>
	/// Reloads table data
	/// </summary>
	private void ReloadTableData ()
	{
		// start with new list 
		_items = new List<InventoryItem> ();

		if (_inventory == null) {
			return;
		}

		// Inventory data model properties
		string[] properties = { "strawberries", "melons", "lemons", "medicine" };
		foreach (string property in properties) {
			// Check property exists in data model then check amount value
			if (ReflectionHelper.HasField (_inventory, property)) {
				var x = ReflectionHelper.GetField (_inventory, property);
				Nullable<uint> value = x.GetValue (_inventory) as Nullable<uint>;
				uint amount = value ?? 0;
				// Only display items with 1 or more
				if (amount > 0) {
					InventoryItem item = new InventoryItem ();
					item.name = property;
					item.amount = amount;
					// Add to table view list
					_items.Add (item);
				}
			}
		}

		// reload table data
		_tableView.ReloadData ();
	}

	#endregion

	#region ITableViewDataSource

	public int GetNumberOfRowsForTableView (TableView tableView)
	{
		return _items.Count;
	}

	public float GetHeightForRowInTableView (TableView tableView, int row)
	{
		return (_cellPrefab.transform as RectTransform).rect.height; //50.0f;
	}

	public TableViewCell GetCellForRowInTableView (TableView tableView, int row)
	{
		InventoryCell cell = tableView.GetReusableCell (_cellPrefab.reuseIdentifier) as InventoryCell;
		if (cell == null) {
			cell = (InventoryCell)GameObject.Instantiate (_cellPrefab);
		}
		InventoryItem data = _items [row];
		cell.Name.text = data.name;
		cell.Amount.text = data.amount.ToString ();
		cell.Icon.sprite = LoadImage (data.name);
		cell.Btn.name = row.ToString (); // save index to button name
		return cell;
	}

	#endregion

	public Sprite LoadImage (string filename)
	{
		Texture2D texture = Resources.Load (filename) as Texture2D;
		if (texture == null) {
			return null;
		}
		Rect rect = new Rect (0, 0, texture.width, texture.height);
		Vector2 pivot = new Vector2 (0.0f, 0.0f);
		return Sprite.Create (texture, rect, pivot);
	}

	/// <summary>
	/// Handler to go to next scene
	/// </summary>
	public void GoNextScene ()
	{
		SceneManager.LoadScene ("HighscoresDemo");
	}
}