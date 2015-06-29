using System.Collections;
using System.Collections.Generic;
using System;

public class Highscore
{
	public string id { get; set; }

	public string username { get; set; }

	public int score { get; set; }
	
	public override string ToString()
	{
		return string.Format("username: {1} score: {2} id: {0}", id, username, score );
	}
}