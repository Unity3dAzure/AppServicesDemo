using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;

namespace Prefabs
{
	public class ModalAlert : MonoBehaviour
	{

		public Text titleBox;
		public Text messageBox;

		/// <summary>
		/// Modal alert
		/// </summary>
		public void Show (string message = "", string title = "Alert")
		{
			// set message
			titleBox.text = title;
			messageBox.text = message;
			// show alert
			ShowModalAlert ();
		}

		public void Close ()
		{
			ShowModalAlert (false);
		}

		private void ShowModalAlert (bool show = true)
		{
			CanvasGroup modal = gameObject.GetComponent<CanvasGroup> ();
			if (!show) {
				modal.alpha = 0;
				modal.interactable = false;
				modal.blocksRaycasts = false;
				return;
			}
			// show modal alert
			modal.alpha = 1;
			modal.interactable = true;
			modal.blocksRaycasts = true; // disable other buttons behind
		}
	}
}