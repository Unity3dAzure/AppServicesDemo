using System;
using Unity3dAzure.AppServices;

[Serializable]
public class Highscore : DataModel 
{
	public string username;
	public uint score;
}