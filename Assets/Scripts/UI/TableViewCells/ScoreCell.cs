using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Tacticsoft;
using System;


namespace Prefabs 
{
	[CLSCompliant(false)]
	public class ScoreCell : TableViewCell 
	{
		public Text Rank;
		public Text Score;
		public Text Name;
		public Button Btn;
	}
}