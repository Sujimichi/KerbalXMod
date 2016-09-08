using System;
using System.Linq;
using System.Text;

using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

using System.IO;
using System.Threading;

using SimpleJSON;
using UnityEngine;


namespace KerbalX
{
	public class KerbalX
	{
		public static List<string> log_data = new List<string>();
		public static string notification = "";
		public static bool logged_in = false;
		public static string token_path = Path.Combine (KSPUtil.ApplicationRootPath, "KerbalX.key");

		public static void log (string s){ 
			s = "[KerbalX] " + s;
			log_data.Add (s); 
			Debug.Log (s);
		}
		public static string last_log()
		{
			if(log_data.Count != 0){
				return log_data [log_data.Count - 1];
			}else{
				return "nothing logged yet";
			}
		}
		public static void show_log(){
			foreach (string l in log_data) { Debug.Log (l); }
		}
		public static void notify(string s){
			notification = s;
			log (s);
		}

		public static void load_token(){
			//attempt to read the token file from the root of KSP
			//read token file if present
			KerbalX.notify("Reading token from " + token_path);
			try{
				string token = System.IO.File.ReadAllText(token_path);
				Debug.Log (token);
				KerbalXAPI.authenticate_token (token);
			}
			catch{
				KerbalX.notify ("no token file found");
			}
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
			grid (300f, window => {
				GUILayout.Label (KerbalXAPI.token);
			});

			GUILayout.Label (KSPUtil.ApplicationRootPath);
			GUILayout.Label ("foo" + Path.DirectorySeparatorChar);

			if (GUILayout.Button ("print log to console")) {
				KerbalX.show_log ();
			}

			if (GUILayout.Button ("test fetch http")) {
				KerbalXAPI.get ("http://kerbalx-stage.herokuapp.com/katateochi.json", (resp, code) => {
					KerbalX.notify ("callback start, got data");
					Debug.Log ("code: " + code);
					var data = JSON.Parse (resp);
					KerbalX.notify (data["username"]);
				});
			}
			
			if (GUILayout.Button ("test fetch https")) {
				KerbalXAPI.get ("https://kerbalx.com/katateochi.json", (resp, code) => {
					KerbalX.notify ("callback start, got data");
					Debug.Log ("code: " + code);
					var data = JSON.Parse (resp);
					KerbalX.notify (data["username"]);
				});
			}

			if (GUILayout.Button ("test api/craft")) {
				KerbalXAPI.get ("http://localhost:3000/api/craft.json", (resp, code) => {
					if(code==200){
						KerbalX.notify (resp);
						var data = JSON.Parse (resp);
						Debug.Log (data["controller"]);
					}
					//KerbalX.notify (data["username"]);
				});
			}
			if (GUILayout.Button ("test api/login")) {
				NameValueCollection queries = new NameValueCollection ();
				queries.Add ("username", "barbar");
				queries.Add ("password", "someshit");
				KerbalXAPI.post ("http://localhost:3000/api/login", queries, (resp, code) => {
					if(code==200){
						KerbalX.notify (resp);
						var data = JSON.Parse (resp);
						Debug.Log (data["controller"]);
					}
					//KerbalX.notify (data["username"]);
				});
			}

		}
	}
		
}
