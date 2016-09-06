using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KerablXWindow;
//using UnityEngine.UI;
//using UnityEngine.EventSystems;


namespace KerbalX
{
	public class KerbalX
	{
		public static List<string> log_data = new List<string>();
		public static string notification = "";
		public static bool logged_in = false;

		public static void log (string e){ log_data.Add (e); }
		public static string last_log()
		{
			if(log_data.Count != 0){
				return log_data [log_data.Count - 1];
			}else{
				return "nothing logged yet";
			}
		}
		public static void show_log(){
			foreach (string l in log_data) { Debug.Log ("[KerbalX] " + l); }
		}
		public static void notify(string s){
			notification = s;
			log (s);
		}
		public static void load_token(){
			//attempt to read the token file from the root of KSP
			//read token file if present
			KerbalX.notify("Reading token from file");
			KerbalXAPI.authenticate_token ("bob's pyjamas");
		}

	}

	public class KerbalXAPI
	{
		public static string token = null; 

		public static void set_token(string new_token){
			token = new_token;
			//and write token to KerablX.key in KSP root dir
		}

		public static bool authenticate_token(string new_token){
			//make request to site to authenticate token and return true or false
			KerbalX.notify("Authenticating token with KerbalX.com...");
			if(new_token == "bob's pyjamas"){
				token = new_token;
				KerbalX.logged_in = true;
				KerbalX.notify("logged in");
				return true;
			}else{				
				token = null;
				KerbalX.logged_in = false;
				KerbalX.notify("token was invalid");
				return false;
			}
		}

		public static void login(string username, string password){
			//make request to site to authenticate username and password and get token back
			KerbalX.notify("loging into KerbalX.com...");

			//succeed
			set_token("lsjdlkfjslkdjflskjdflksjdfl"); //will be token returned from site
			KerbalX.logged_in = true;
			KerbalX.notify("login succsessful, yay!");
		}

	}

	[KSPAddon(KSPAddon.Startup.MainMenu, false)]
	public class KerbalXLoginWindow : KerbalXWindow
	{

		private string username = "";
		private string password = "";


		private void Start(){
			window_title = "KerbalX::Login";
			window_pos = new Rect((Screen.width/2 - 310/2),100, 310, 5);
			KerbalX.log ("starting");
			KerbalX.load_token ();
		}

		protected override void WindowContent(int win_id)
		{
			if(KerbalX.logged_in == false){					
				grid (310f, window => {
					GUILayout.Label ("username", GUILayout.Width (60f));
					username = GUILayout.TextField (username, 255, GUILayout.Width (250f));
				});

				grid (310f, window => {
					GUILayout.Label ("password", GUILayout.Width(60f));
					password = GUILayout.TextField (password, 255, GUILayout.Width(250f));
				});
			}

			GUILayout.Label (KerbalX.notification, GUILayout.Width(310f));

			if (KerbalX.logged_in == false) {
				if (GUILayout.Button ("Login")) {
					
					KerbalXAPI.login (username, password);
				}
			}else{
				if (GUILayout.Button ("Log out")) {
					KerbalX.logged_in = false;
					KerbalX.notify ("logged out");
				}				
			}
		}
	}


	[KSPAddon(KSPAddon.Startup.EveryScene, false)]
	public class KXTest : KerbalXWindow
	{
		private void Start()
		{
			window_title = "test window";
		}

		protected override void WindowContent(int win_id)
		{
			grid (300f, window => {
				GUILayout.Label (KerbalX.last_log ());
			});

			if (GUILayout.Button ("print log to console")) {
				KerbalX.show_log ();
			}
		}
	}
		
}
