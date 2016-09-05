using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KerbalX
{

	[KSPAddon(KSPAddon.Startup.MainMenu, false)]
	public class TestModPartless : MonoBehaviour
	{
		private Rect win_pos = new Rect();
		private string output_label = "some stuff";
		private string username = " ";
		private string password = " ";


		private void OnGUI()
		{
			win_pos = GUILayout.Window(10, win_pos, DrawWindow, "I´m the title :D");
		}

		//delegate string TestContent();
		//private void grid(int width, TestContent con)
		//{
		//	string msg = string.Format ("called with {0}", width);
		//	Debug.Log (msg);
		//	con("foo");
		//}


		private void DrawWindow(int windowId)
		{

			GUILayout.BeginHorizontal(GUILayout.Width(310f));
			GUILayout.Label ("username", GUILayout.Width (60f));
			username = GUILayout.TextField (username, 255, GUILayout.Width(250f));
			GUILayout.EndHorizontal ();

			GUILayout.BeginHorizontal(GUILayout.Width(310f));
			GUILayout.Label ("password", GUILayout.Width(60f));
			password = GUILayout.TextField (password, 255, GUILayout.Width(250f));
			GUILayout.EndHorizontal ();

			GUILayout.Label (output_label, GUILayout.Width(310f));

			if (GUILayout.Button ("Login")) {
				output_label = "checking authorization";
				//TestContent con = n => {
				//	Debug.Log("this is the content");
				//	Debug.Log(n);
				//};
				//grid (10, con);
			}


			GUI.DragWindow();
		}
	}

}
