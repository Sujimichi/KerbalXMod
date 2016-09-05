using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KerbalX
{

	[KSPAddon(KSPAddon.Startup.MainMenu, false)]
	public class KerbalXLoginWindow : KerbalXWindow
	{
		private string output_label = "some stuff";
		private string username = "";
		private string password = "";

		private void Start(){
			window_title = "KerbalX::Login";
			window_pos = new Rect(100,100, 310, 200);
		}

		protected override void WindowContent(int win_id)
		{
			grid (310f, window => {
				GUILayout.Label ("username", GUILayout.Width (60f));
				username = GUILayout.TextField (username, 255, GUILayout.Width (250f));
			});

			grid (310f, window => {
				GUILayout.Label ("password", GUILayout.Width(60f));
				password = GUILayout.TextField (password, 255, GUILayout.Width(250f));
			});

			GUILayout.Label (output_label, GUILayout.Width(310f));

			if (GUILayout.Button ("Login")) {
				output_label = "checking authorization";
				window_pos = new Rect(400,400, 310, 200);
			}
		}
	}


	[KSPAddon(KSPAddon.Startup.MainMenu, false)]
	public class KXTest : KerbalXWindow
	{

		private void Start()
		{
			window_title = "test window";
		}

		protected override void WindowContent(int win_id)
		{
			GUILayout.Label ("some nonsense", GUILayout.Width (60f));
			grid (310f, window => {
				GUILayout.Label ("username", GUILayout.Width (60f));
				GUILayout.Label ("height " + window.height);
			});
		}

	}

	/* KerbalXWindow is a base class to be inherited by classes which draw GUI windows
	It provides common setup required to draw a GUI window, enabling DRY and minimal window classes.
	A class which inherits KerbalXWindow needs to override the WindowContent method to define the content of the window
	Basic Usage:
		[KSPAddon(KSPAddon.Startup.MainMenu, false)]
		public class SimpleWindow : KerbalXWindow
		{
			protected override void WindowContent(int win_id)
			{
				GUILayout.Label ("some nonsense", GUILayout.Width (60f));
			}
		}
	Attributes like the window title, size/pos, id are set, but can be overridden by defining a Start() method
	Configured Usage:
		[KSPAddon(KSPAddon.Startup.MainMenu, false)]
		public class SimpleWindow : KerbalXWindow
		{
			private void Start()
			{
				window_pos = new Rect(100,100,500,200); //defaults to new Rect() if not set
				window_title = "test window";			//defaults to "untitled window" if not set
				window_id = 42;							//defaults to the next ID in sequence (change last_window_id in the base class to change the sequence start point)
				footer = false							//defaults to true. if true adds a common set of GUI elements defined in DrawWindow
				draggable = false;						//defaults to true. makes the windows draggable, duh. 
			}

			protected override void WindowContent(int win_id)
			{
				GUILayout.Label ("some nonsense", GUILayout.Width (60f));
			}
		}	 
	KerbalXWindow also provides the handy-dandy fabtastic grid method. grid takes a width and a lambda (delegate) statement and wraps the actions defined in the
	lambda in calls to BeginHorizontal and EndHorizontal.  This ensures End is always called after a begin, and (I think) makes for clearer and more readable code.
 	*/
	public class KerbalXWindow : MonoBehaviour
	{
		protected Rect window_pos = new Rect();
		protected string window_title = "untitled window";
		protected int window_id = 0;
		protected bool footer = true;
		protected bool draggable = true;
		static int last_window_id = 0;

		//Definition of delegate to be passed into the grid method 
		protected delegate void Content(Rect win);

		/* Essentially wraps the function of a delegate in calls to BeginHorizontal and EndHorizontal
		Takes a width float and a delegate/statement lambda and passes the width float to the call to BeginHorizontal 
		then calls the delegate and lastly calls EndHorizontal
		Usage:
			grid (width, window => {
				// Calls to draw GUI elements inside a BeginHorizontal group ie;
				// GUILayout.Label ("some nonsense", GUILayout.Width (60f));
				//window is passed in as the Rect for the window position/size
			});	
		*/
		protected void grid(float width, Content con)
		{
			GUILayout.BeginHorizontal(GUILayout.Width(width));
			con(window_pos);
			GUILayout.EndHorizontal ();
		}

		//called on each frame, handles drawing the window and will assign the next window id if it's not set
		protected void OnGUI()
		{
			if(window_id == 0){
				window_id = last_window_id + 1;
				last_window_id = last_window_id + 1;
			}
			window_pos = GUILayout.Window (window_id, window_pos, DrawWindow, window_title);
		}

		//Callback methods which is passed to GUILayout.Window in OnGUI.  Calls WindowContent and performs common window actions
		private void DrawWindow(int window_id)
		{
			WindowContent (window_id);	//Draw the main content of the window as defined by WindowContent
			if(footer){
				GUILayout.Label ("rendered with a moose");
				GUILayout.Label ("window_id: " + window_id);				
			}
			if(draggable){
				GUI.DragWindow();
			}
		}

		//The main method which defines the content of a window.  This method is provided so as to be overridden in inherited classes
		protected virtual void WindowContent(int window_id)
		{
			
		}

		private void onDestroy(){
			print ("shit was destroyed yo"); 
		}

	}
}
