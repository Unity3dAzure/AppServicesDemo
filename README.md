# MobileServicesDemo and AppServicesDemo for Unity3d
Sample Unity 5 project demoing Azure Mobile Services or Azure App Service for Unity3d.

## Highscore demo features
* Client-directed login with Facebook
* Insert Highscore
* Update Highscore
* Read list of Highscores
* Query for scores > 999 (leaderboard)
* Query for username (user's scores)

## Setup Azure Mobile Services for Unity
1. Create an [Azure Mobile Service](https://manage.windowsazure.com/)
	* Create 'Highscores' table for app data
	* Modify 'Highscores' table Insert node script to save userId
	* Create a custom API called 'hello'
2. In Unity3d **open scene** `Scenes/MobileServicesDemo.unity`
	* Check the script `Scripts/MobileServicesDemo.cs` is attached to a game object in the Unity Hierarchy window.
3. Paste Azure Mobile Service app's connection strings into Unity Editor Inspector fields
	![alt Unity Editor Mobile Services config](https://cloud.githubusercontent.com/assets/1880480/14404803/c8287754-fe76-11e5-8b0c-7ba729a8c19d.png)
	* Mobile Service URL
	* Mobile Service Application Key
4. If you want to save score with userId then [create Facebook app](https://developers.facebook.com/apps/)
	* Fill in Azure Mobile Service's Identity > Facebook settings (App Id & App Secret)
	* Paste [Facebook access user token](https://developers.facebook.com/tools/accesstoken/) into Unity Editor Inspector field
	* Play in UnityEditor

### Azure Mobile Services
#### Highscores Table **Insert** script
```node
function insert(item, user, request) {
  if (user.userId) {
    item.userId = user.userId;
  }
  request.execute();
}
```

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
