# MobileServicesDemo
Sample Unity 5 project demoing Azure [MobileServices](https://github.com/Unity3dAzure/MobileServices) for Unity3d.

## Setup
1. [Create a Mobile Service](https://manage.windowsazure.com/)
	* Create 'Highscores' table for app data
	* Modify 'Highscores' table Insert node script to save userId
	* Create a custom API called 'hello'
2. Paste Azure Mobile Service app's connection strings into `Scripts/Config.cs`
	* Mobile Service URL
	* Mobile Service Application Key
3. [Create Facebook app](https://developers.facebook.com/apps/async/create/platform-setup/dialog/)
	* Fill in Azure Mobile Service's Identity > Facebook settings (App Id & App Secret)
	* Paste [Facebook access user token](https://developers.facebook.com/tools/accesstoken/) into `Scripts/Config.cs`
4. In Unity3d open scene `Scenes/HighscoresDemo.unity`
	* Play in UnityEditor

## Azure Mobile Services 
###Highscores Table **Insert** script
```node
function insert(item, user, request) {
    if (user.userId) {
        item.userId = user.userId;
    }
    request.execute();
}
```

## Unity3d 
### Highscore Demo UI
* Client-directed login with Facebook
* Insert Highscore
* Update Highscore
* Read list of Highscores
* Query for scores > 999 (leaderboard)
* Query for username (user's scores)

Questions or tweet #Azure #GameDev [@deadlyfingers](https://twitter.com/deadlyfingers)