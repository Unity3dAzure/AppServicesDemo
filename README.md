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
For detailed instructions read blog on how to [setup Azure App Services to create Unity highscores leaderboard](http://www.deadlyfingers.net/azure/azure-app-services-for-unity3d/).

## Setup Azure App Services for Unity
1. Create an [Azure Mobile App](https://portal.azure.com/)
	* Create 'Highscores' table for storing app data using **Easy Tables**.
2. In Unity open scene "Scenes/HighscoresDemo.unity"
3. Select the *AppServicesController gameobject* in the Unity Hierarchy window and paste your **Azure App Service URL** into the Editor Inspector field.  
	![alt Unity Editor Mobile Services config](https://cloud.githubusercontent.com/assets/1880480/18139855/0e5fe626-6fab-11e6-8de6-484e3b909cc8.png)

## Setup Azure App Services with Authentication
If you wish to save score using Facebook identity:
1. [Create Facebook app](https://developers.facebook.com/apps/)
2. Fill in the [Azure App Services](https://portal.azure.com/) Authentication settings with Facebook App Id & App Secret.
3. Paste [Facebook access user token](https://developers.facebook.com/tools/accesstoken/) into Unity access token field to enable Login button.
4. Modify 'Highscores' table 'Insert' node script (using snippet below) to save `user.id`

#### **Easy Table Insert** script (*tables/Highscores.js*)
```node
var table = module.exports = require('azure-mobile-apps').table();
table.insert(function (context) {
	if (context.user) {
		context.item.userId = context.user.id;
	}
	return context.execute();
});
```

## Setup Azure App Services custom APIs with **Easy APIs**
With [Azure App Services](https://portal.azure.com/) you can create custom APIs using **Easy APIs**.
1. Create a 'hello' api to say hello! (Example Easy API message script below)
2. Create a 'GenerateScores' api to generate 10 random scores. (Example Easy API query script below)

#### Easy API 'hello' script (*api/hello.js*)
```node
module.exports = {
    "get": function (req, res, next) {
        res.send(200, { message : "Hello Unity!" });
    }
}
```

#### Easy API 'GenerateScores' script (*api/GenerateScores.js*)
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
