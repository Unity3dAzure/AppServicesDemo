using System.Collections;
using System.Collections.Generic;
using System;
using Unity3dAzure.AppServices;

[CLSCompliant(false)]
public class Highscore : DataModel
{
	//public string id { get; set; } // id property is provided when subclassing the DataModel

	public string username { get; set; }

	public int score { get; set; }
	
	public override string ToString()
	{
		return string.Format("username: {0} score: {1} system properties: {2}", username, score, base.ToString() );
	}
}