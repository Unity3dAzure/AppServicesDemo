using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Tacticsoft;
using System;

namespace Prefabs 
{
	[CLSCompliant(false)]
	public class InventoryCell : TableViewCell 
	{
		public Image Icon;
		public Text Name;
		public Text Amount;
		public Button Btn;
	}
}