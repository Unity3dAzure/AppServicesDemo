﻿using System;
using Azure.AppServices;

[Serializable]
public class Highscore : DataModel 
{
	public string username;
	public uint score;
}