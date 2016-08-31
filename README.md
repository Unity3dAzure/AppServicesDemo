# AppServicesDemo for Unity3d
Sample highscore leaderboard project demoing Azure App Services (previously Mobile Services) for Unity 5.

## Highscore demo features
* Client-directed login with Facebook
* Insert Highscore
* Update Highscore
* Read list of Highscores (hall of fame)
* Query for today's top ten highscores (daily leaderboard)
* Query for username (user's scores)

## Developer guide
Read developer guide on [using Azure App Services to create Unity highscores leaderboard](http://www.deadlyfingers.net/azure/azure-app-services-for-unity3d/) for detailed instructions.

## Setup Azure App Services for Unity
1. Create an [Azure Mobile App](https://portal.azure.com/)
	* Create 'Highscores' table for storing app data using **Easy Tables**.
2. In Unity open scene "Scenes/HighscoresDemo.unity"
3. Select the *AppServicesController gameobject* in the Unity Hierarchy window and paste your **Azure App Service URL** into the Editor Inspector field.
	![alt Unity Editor Mobile Services config](https://cloud.githubusercontent.com/assets/1880480/18139855/0e5fe626-6fab-11e6-8de6-484e3b909cc8.png)
4. If you wish to save score using Facebook identity:
	* [Create Facebook app](https://developers.facebook.com/apps/)
	* Fill in the Azure App Services Authentication settings for Facebook. (Facebook App Id & App Secret required)
	* Paste [Facebook access user token](https://developers.facebook.com/tools/accesstoken/) into Unity Editor Inspector field
	* Modify 'Highscores' table 'Insert' node script (using snippet below) to save `userId`
5. Create optional custom APIs using **Easy APIs**. (Example snippets below)
	* Create a 'hello' api to say hello!
	* Create a 'GenerateScores' api to generate 10 random scores.

### Easy Table scripts
#### 'tables/Highscores.js' **Insert** script
```node
var table = module.exports = require('azure-mobile-apps').table();
table.insert(function (context) {
	if (context.user) {
		context.item.userId = context.user.id;
	}
	return context.execute();
});
```

### Easy API scripts
#### 'api/hello.js' script
```node
module.exports = {
    "get": function (req, res, next) {
        res.send(200, { message : "Hello Unity!" });
    }
}
```

#### 'api/GenerateScores.js' script
```node
var util = require('util');
module.exports = {
    "get": function (req, res, next) {
        var insert = "INSERT INTO Highscores (username,score) VALUES ";
        var i = 10;
        while (i--) {
            var min = 1;
            var max = 1000;
            var rand = Math.floor(Math.random() * (max - min)) + min;
            var values = util.format("('%s',%d),", 'Zumo', rand);
            insert = insert + values;
        }
        insert = insert.slice(0, -1); // remove last ','
        var query = {
            sql: insert
        };
        req.azureMobile.data.execute(query).then(function(results){
            res.send(200, { message : "Zumo set some highscores!" });
        });
    }
}

```

## Dependencies included
* [AppService](https://github.com/Unity3dAzure/AppServices) for Unity implements [UnityRestClient](https://github.com/ProjectStratus/UnityRestClient) which uses [JsonFx](https://bitbucket.org/TowerOfBricks/jsonfx-for-unity3d-git/) to parse JSON data.
* [TSTableView](https://bitbucket.org/tacticsoft/tstableview) is used to display recyclable list of results.

## Supports
* iOS
* Android
* Windows

Questions or tweet #Azure #GameDev [@deadlyfingers](https://twitter.com/deadlyfingers)
