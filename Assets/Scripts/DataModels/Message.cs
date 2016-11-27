using System.Collections;
using System.Collections.Generic;
using System;

[Serializable]
public class Message 
{
	public string message;
	public string title;

	// Factory method to create a new message
	public static Message Create(string message, string title="") {
		Message m = new Message ();
		m.message = message;
		m.title = title;
		return m;
	}
}