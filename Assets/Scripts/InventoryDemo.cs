using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;
using Unity3dAzure.AppServices;
using System.Collections.Generic;
using RestSharp;
using System.Net;
using Tacticsoft;
using Prefabs;
using UnityEngine.SceneManagement;

[CLSCompliant(false)]
public class InventoryDemo : MonoBehaviour, ITableViewDataSource {

	/// <remarks>
	/// Enter your Azure App Service url
	/// </remarks>

	[Header("Azure App Service")]
	// Azure Mobile App connection strings
	[SerializeField]
	private string _appUrl = "PASTE_YOUR_APP_URL";

	// Go to https://developers.facebook.com/tools/accesstoken/ to generate a new access "User Token"
	[Header("User Authentication")]
	[SerializeField]
	private string _facebookAccessToken = "";

	// App Service Rest Client
	private MobileServiceClient _client;

	// App Service Table defined using a DataModel
	private MobileServiceTable<Inventory> _table;

	// Data
	private Inventory _inventory;

	// List of virtual items (inventory)
	private List<InventoryItem> _items = new List<InventoryItem>();

	[Header("UI")]
	// TSTableView for displaying list of results
	[SerializeField]
	private TableView _tableView;
	[SerializeField]
	private InventoryCell _cellPrefab;
	bool HasNewData = false; // to reload table view when data has changed

	[Space(10)]
	[SerializeField]
	private ModalAlert _modalAlert; 
	private Message _message;

	// to show user's inventory after successful login
	bool DidLogin = false;

	// Use this for initialization
	void Start () {
		// Create App Service client (Using factory Create method to force 'https' url)
		_client = MobileServiceClient.Create(_appUrl); //new MobileServiceClient(_appUrl);

		// Get App Service 'Highscores' table
		_table = _client.GetTable<Inventory>("Inventory");

		// set TSTableView delegate
		_tableView.dataSource = this;

		// setup token using Unity Inspector value
		if (!String.IsNullOrEmpty(_facebookAccessToken)) {
			InputField inputToken = GameObject.Find("FacebookAccessToken").GetComponent<InputField> ();
			inputToken.text = _facebookAccessToken;
		}

		// hide controls until login
		CanvasGroup group = GameObject.Find("UserDataGroup").GetComponent<CanvasGroup> ();
		group.alpha = 0;
		group.interactable = false;

		UpdateUI ();
	}

	// Update is called once per frame
	void Update () 
	{
		// show controls after login
		if (DidLogin) {
			CanvasGroup group = GameObject.Find("UserDataGroup").GetComponent<CanvasGroup> ();
			group.alpha = 1;
			group.interactable = true;
			DidLogin = false;
		}
		// update editor fields and reload table data
		if (HasNewData && _inventory != null) {
			InputField strawberries = GameObject.Find("Input1").GetComponent<InputField> ();
			InputField melons = GameObject.Find("Input2").GetComponent<InputField> ();
			InputField lemons = GameObject.Find("Input3").GetComponent<InputField> ();
			InputField medicine = GameObject.Find("Input4").GetComponent<InputField> ();
			strawberries.text = _inventory.strawberries.ToString();
			melons.text = _inventory.melons.ToString();
			lemons.text = _inventory.lemons.ToString();
			medicine.text = _inventory.medicine.ToString();
			ReloadTableData ();
			HasNewData = false;
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
			DidLogin = true;
			Load (); // auto load user data
		}
		else
		{
			Debug.Log("Authorization Error: " + response.StatusCode);
			_message = Message.Create ("Login failed", "Error");
		}
	}

	public void Load() 
	{
		string filter = string.Format("userId eq '{0}'", _client.User.user.userId);
		Debug.Log("Load data for UserId: " + _client.User.user.userId );
		CustomQuery query = new CustomQuery(filter);
		_table.Query<Inventory>(query, OnLoadCompleted);
	}

	private void OnLoadCompleted(IRestResponse<List<Inventory>> response) 
	{
		if (response.StatusCode == HttpStatusCode.OK)
		{
			Debug.Log("OnLoadItemsCompleted data: " + response.Content);
			List<Inventory> results = response.Data;
			Debug.Log("Load results count: " + results.Count);
			// no record 
			if (results.Count == 0) {
				_inventory = new Inventory ();
			}
			if (results.Count >= 1) {
				_inventory = results [0];
			}
			HasNewData = true;
		}
		else
		{
			Debug.Log("Read Error Status:" + response.StatusCode + " Uri: "+response.ResponseUri );
		}
	}

	public void Save() 
	{
		if (_inventory == null) 
		{
			Debug.Log ("Error, no inventory");
			return;
		}
		// If new Insert, else Update existing user inventory
		if ( String.IsNullOrEmpty(_inventory.id) ) 
		{
			InsertInventory();
		} 
		else 
		{
			UpdateInventory();
		}
	}

	private void InsertInventory() 
	{
		RecalculateInventoryItems ();
		Debug.Log ("Insert:" + _inventory.ToString());
		_table.Insert<Inventory>(_inventory, OnInsertCompleted);
	}

	private void OnInsertCompleted(IRestResponse<Inventory> response) 
	{
		if (response.StatusCode == HttpStatusCode.Created)
		{
			Debug.Log( "OnInsertItemCompleted: " + response.Data );
			Inventory item = response.Data; // if successful the item will have an 'id' property value
			_inventory = item;
			_message = Message.Create ("Inventory saved", "Inserted"); // show confirmation message
		}
		else
		{
			Debug.Log("Insert Error Status:" + response.StatusCode +" "+ response.ErrorMessage + " Uri: "+response.ResponseUri );
		}
	}

	private void UpdateInventory(){
		RecalculateInventoryItems ();
		Debug.Log ("Update:" + _inventory.ToString());
		_table.Update<Inventory>(_inventory, OnUpdateCompleted);
	}

	private void OnUpdateCompleted(IRestResponse<Inventory> response)
	{
		if (response.StatusCode == HttpStatusCode.OK)
		{
			Debug.Log("OnUpdateCompleted: " + response.Content );
			_message = Message.Create ("Inventory saved", "Updated"); // show confirmation message
		}
		else
		{
			Debug.Log("Update Error Status:" + response.StatusCode +" "+ response.ErrorMessage + " Uri: "+response.ResponseUri );
		}
	}

	#region UI

	/// <summary>
	/// Update inventory using input values
	/// </summary>
	private void RecalculateInventoryItems() 
	{
		if (_inventory == null) {
			Debug.Log ("Error, no inventory");
		}
		InputField strawberries = GameObject.Find("Input1").GetComponent<InputField> ();
		InputField melons = GameObject.Find("Input2").GetComponent<InputField> ();
		InputField lemons = GameObject.Find("Input3").GetComponent<InputField> ();
		InputField medicine = GameObject.Find("Input4").GetComponent<InputField> ();
		_inventory.strawberries = Convert.ToInt32(strawberries.text);
		_inventory.melons = Convert.ToInt32(melons.text);
		_inventory.lemons = Convert.ToInt32(lemons.text);
		_inventory.medicine = Convert.ToInt32(medicine.text);
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

		// Close dialog if no message
		if (_message == null) 
		{
			_modalAlert.Close();
		}
	}

	/// <summary>
	/// Reloads table data
	/// </summary>
	private void ReloadTableData() 
	{
		// start with new list 
		_items = new List<InventoryItem>();

		if (_inventory == null) {
			return;
		}

		// Inventory data model properties
		string[] properties = { "strawberries", "melons", "lemons", "medicine" };
		foreach (string property in properties) 
		{
			// Check property exists in data model then check amount value
			if (Model.HasProperty (_inventory, property)) {
				var x = Model.GetProperty(_inventory, property);
				Nullable<int> value = x.GetValue(_inventory, null) as Nullable<int>;
				int amount = value ?? 0;
				// Only display items with 1 or more
				if (amount > 0) {
					InventoryItem item = new InventoryItem ();
					item.name = property;
					item.amount = amount;
					// Add to table view list
					_items.Add(item);
				}
			}
		}

		// reload table data
		_tableView.ReloadData();
	}

	#endregion

	#region ITableViewDataSource

	public int GetNumberOfRowsForTableView(TableView tableView)
	{
		return _items.Count;
	}

	public float GetHeightForRowInTableView(TableView tableView, int row)
	{
		return (_cellPrefab.transform as RectTransform).rect.height; //50.0f;
	}

	public TableViewCell GetCellForRowInTableView(TableView tableView, int row)
	{
		InventoryCell cell = tableView.GetReusableCell(_cellPrefab.reuseIdentifier) as InventoryCell;
		if (cell == null) {
			cell = (InventoryCell)GameObject.Instantiate (_cellPrefab);
		}
		InventoryItem data = _items [row];
		cell.Name.text = data.name;
		cell.Amount.text = data.amount.ToString();
		cell.Icon.sprite = LoadImage (data.name);
		cell.Btn.name = row.ToString(); // save index to button name
		return cell;
	}

	#endregion

	public Sprite LoadImage(string filename) {
		Texture2D texture = Resources.Load(filename) as Texture2D;
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
	public void GoNextScene() {
		SceneManager.LoadScene ("HighscoresDemo");
	}
}

/// <summary>
/// Virtual inventory item used to populate table cell
/// </summary>
public class InventoryItem {
	public string name;
	public int amount;
}

