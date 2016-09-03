using System.Collections;
using System.Collections.Generic;
using System;

public class Message
{
	public string message { get; set; }
	public string title { get; set; }
	
	public override string ToString()
	{
		return string.Format("message: {0}", message );
	}

	// Factory method to create a new message
	public static Message Create(string message, string title="") 
	{
		Message m = new Message ();
		m.message = message;
		m.title = title;
		return m;
	}
}