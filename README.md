# AppServicesDemo for Unity3d
Sample highscore leaderboard project demoing Azure App Services (previously Mobile Services) for Unity 5.

## Highscore demo features
* Client-directed login with Facebook
* Insert Highscore
* Update Highscore
* Read list of Highscores
* Query for scores > 999 (leaderboard)
* Query for username (user's scores)

## Developer guide
Read developer guide on [using Azure App Services to create Unity highscores leaderboard](http://www.deadlyfingers.net/azure/azure-app-services-for-unity3d/) for detailed instructions.

## Setup Azure App Services for Unity
1. Create an [Azure Mobile App](https://portal.azure.com/)
	* Create 'Highscores' table for app data
	* Modify 'Highscores' table Insert node script to save userId
	* Create a custom API called 'hello'
2. In Unity3d **open scene** `Scenes/AppServicesDemo.unity`
	* Check the script `Scripts/AppServicesDemo.cs` is attached to a game object in the Unity Hierarchy window.
3. Paste Azure Mobile Service app's connection strings into Unity Editor Inspector fields
	![alt Unity Editor Mobile Services config](https://cloud.githubusercontent.com/assets/1880480/14404802/c82512da-fe76-11e5-91ad-316fcd70fd5c.png)
	* App Service URL (NB: use https)
4. If you want to save score with userId then [create Facebook app](https://developers.facebook.com/apps/)
	* Fill in Azure Mobile Service's Identity > Facebook settings (App Id & App Secret)
	* Paste [Facebook access user token](https://developers.facebook.com/tools/accesstoken/) into Unity Editor Inspector field
	* Play in UnityEditor

### Azure App Services
#### Highscores Table **Insert** script
```node
table.insert(function (context) {
	if (context.user) {
		context.item.userId = context.user.id;
	}
	return context.execute();
});
```

## Supports
* iOS
* Android
* Windows

Questions or tweet #Azure #GameDev [@deadlyfingers](https://twitter.com/deadlyfingers)
