# Azure App Services for Unity3d
Contains a Unity 5 project featuring two demo scenes for Azure App Services (previously Mobile Services).  

1. Highscores demo scene
2. Inventory demo scene

## Highscore demo features
* Client-directed login with Facebook
* Insert Highscore
* Update Highscore
* Read list of Highscores using infinite scrolling (hall of fame)
* Query for today's top ten highscores (daily leaderboard)
* Query for username (user's scores)

## Inventory demo features
* Client-directed login with Facebook
* Load User's inventory.
* Save User's inventory. (Inserts if new or Updates existing record)

## Developer blogs
- [How to setup Azure App Services to create Unity highscores leaderboard](http://www.deadlyfingers.net/azure/azure-app-services-for-unity3d/).

## Setup Azure App Services for Unity
1. Create an [Azure Mobile App](https://portal.azure.com/)
	* Create 'Highscores' and 'Inventory' table for storing app data using **Easy Tables**.
2. In Unity open scene file(s) inside the *Scenes* folder:  
	* *HighscoresDemo.unity*
	* *InventoryDemo.unity*
3. Then select the *AppServicesController gameobject* in the Unity Hierarchy window and paste your **Azure App Service URL** into the Editor Inspector field.  
	![alt Unity Editor Mobile Services config](https://cloud.githubusercontent.com/assets/1880480/18139855/0e5fe626-6fab-11e6-8de6-484e3b909cc8.png)

## Setup Azure App Services with Authentication
This demo uses Facebook identity to save user's highscore or inventory items:

1. [Create Facebook app](https://developers.facebook.com/apps/)
2. Fill in the [Azure App Services](https://portal.azure.com/) Authentication settings with Facebook App Id & App Secret.
3. Paste [Facebook access user token](https://developers.facebook.com/tools/accesstoken/) into Unity access token field to enable Login button.
4. Modify 'Highscores' and 'Inventory' table script (using 'Insert' snippet below) to save `user.id`

#### **Easy Table Insert** script (*tables/Highscores.js*, *tables/Inventory.js*)
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

1. Create a 'hello' api (using "get" method) to say hello! (Example Easy API message script below)
2. Create a 'GenerateScores' api (using "post" method) to generate 10 random scores. (Example Easy API query script below)

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
    "post": function (req, res, next) {
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

## Credits
* Inventory uses [pixel art icons designed by Henrique Lazarini](http://7soul1.deviantart.com/art/420-Pixel-Art-Icons-for-RPG-129892453)

## Dependencies included
* [AppServices](https://github.com/Unity3dAzure/AppServices) for Unity.
* [TSTableView](https://bitbucket.org/tacticsoft/tstableview) is used to display recyclable list of results.

Questions or tweet #Azure #GameDev [@deadlyfingers](https://twitter.com/deadlyfingers)
