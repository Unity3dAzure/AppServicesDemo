using System.Collections;
using System.Collections.Generic;
using System;

public class Message
{
	public string message { get; set; }
	
	public override string ToString()
	{
		return string.Format("message: {0}", message );
	}
}