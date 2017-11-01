using System;
using Azure.AppServices;

[Serializable]
public class Inventory : DataModel 
{
	public uint strawberries;
	public uint melons;
	public uint lemons;
	public uint medicine;

	public Inventory() {
		this.strawberries = 0;
		this.melons = 0;
		this.lemons = 0;
		this.medicine = 0;
	}
}