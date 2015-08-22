# MobileServicesDemo
Sample Unity 5 project demoing Azure [MobileServices](https://github.com/Unity3dAzure/MobileServices) for Unity3d.

## Setup
1. [Create a Mobile Service](https://manage.windowsazure.com/)
	* Create 'Highscores' table for app data
	* Modify 'Highscores' table Insert node script to save userId
	* Create a custom API called 'hello'
2. In Unity3d **open scene** `Scenes/HighscoresDemo.unity`
	* Check the Demo UI script is attached to the Camera. (The script can be attached by dragging & dropping the `Scripts/HighscoresDemoUI.cs` script unto the Scene's 'Main Camera' in the Hierarchy panel.)
3. Paste Azure Mobile Service app's connection strings into Unity Editor Inspector fields (or else directly into script `Scripts/HighscoresDemoUI.cs`)
	* Mobile Service URL
	* Mobile Service Application Key
![alt Unity Editor Mobile Services config](https://cloud.githubusercontent.com/assets/1880480/9424523/2a74edb6-48e7-11e5-9e0e-81e5c1acbb53.png)
4. If you want to save score with userId then [create Facebook app](https://developers.facebook.com/apps/async/create/platform-setup/dialog/)
	* Fill in Azure Mobile Service's Identity > Facebook settings (App Id & App Secret)
	* Paste [Facebook access user token](https://developers.facebook.com/tools/accesstoken/) into Unity Editor Inspector field (or else directly into `Scripts/HighscoresDemoUI.cs`)
	* Play in UnityEditor

## Supports
* iOS
* Android
* Windows

## Azure Mobile Services
### Highscores Table **Insert** script
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
