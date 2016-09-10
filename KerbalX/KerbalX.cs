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
		public static string token_path = Path.Combine (KSPUtil.ApplicationRootPath, "KerbalX.key");
		public static List<string> log_data = new List<string>();
		public static string notice = "";
		public static string alert = "";
		public static bool show_login = false;
		public static string site_url = "http://localhost:3000";

		public static string url_to (string path){
			if(!path.StartsWith ("/")){ path = "/" + path;}
			return site_url + path;
		}


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
			notice = s;
			log (s);
		}

		public static void load_token(){
			KerbalX.notify("Reading token from " + token_path);
			try{
				string token = System.IO.File.ReadAllText(token_path);
				KerbalXAPI.authenticate_token (token);
			}
			catch{
				KerbalX.notify("Enter your KerbalX username and password");
				KerbalX.show_login = true;
			}
		}

		public static void save_token(string token){
			System.IO.File.WriteAllText(token_path, token);
		}
	}

	[KSPAddon(KSPAddon.Startup.MainMenu, false)]
	public class KerbalXLoginWindow : KerbalXWindow
	{
		private string username = "";
		private string password = "";
		public static bool enable_login = true;  //used to toggle enabled/disabled state on login fields and button
		GUIStyle alert_style = new GUIStyle();


		private void Start(){
			window_title = "KerbalX::Login";
			window_pos = new Rect((Screen.width/2 - 310/2),100, 310, 5);
			alert_style.normal.textColor = Color.red;

			KerbalX.show_login = false;
			if (KerbalXAPI.token == null) {
				KerbalX.load_token ();
			}
		}

		protected override void WindowContent(int win_id)
		{
			if(KerbalX.show_login == true){					
				GUI.enabled = enable_login;
				grid (310f, window => {
					GUILayout.Label ("username", GUILayout.Width (60f));
					username = GUILayout.TextField (username, 255, GUILayout.Width (250f));
				});

				grid (310f, window => {
					GUILayout.Label ("password", GUILayout.Width(60f));
					password = GUILayout.PasswordField (password, '*', 255, GUILayout.Width(250f));
				});
				GUI.enabled = true;
			}

			if (KerbalX.notice != "") {
				GUILayout.Label (KerbalX.notice, GUILayout.Width (310f));
			};

			if (KerbalX.alert != "") {	
				GUILayout.Label (KerbalX.alert, alert_style, GUILayout.Width (310f) );
			};

			GUI.enabled = enable_login;
			if (KerbalX.show_login == true) {
				if (GUILayout.Button ("Login")) {				
					KerbalX.alert = "";
					enable_login = false;
					KerbalXAPI.login (username, password);
				}
			}else{
				if (GUILayout.Button ("Log out")) {
					KerbalX.show_login = true;
					KerbalXAPI.token = null;
					KerbalX.notify ("logged out");
				}				
			}
			GUI.enabled = true;
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
			grid (300f, window => { GUILayout.Label (KerbalX.last_log ());	});
			grid (300f, window => { GUILayout.Label (KerbalXAPI.token); 	});

			if (GUILayout.Button ("print log to console")) { KerbalX.show_log (); }

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
				KerbalXAPI.get (KerbalX.url_to ("api/craft.json"), (resp, code) => {
					if(code==200){
						KerbalX.log (resp);
					}
				});
			}
		}
	}

		
	[KSPAddon(KSPAddon.Startup.MainMenu, false)]
	public class JumpStart : MonoBehaviour
	{
		public static bool autostart = true;
		public static string save_name = "default";
		public static string craft_name = "testy";

		public void Start()
		{
			if(autostart){
				HighLogic.SaveFolder = save_name;
				GamePersistence.LoadGame("persistent", HighLogic.SaveFolder, true, false);
				if(craft_name != null || craft_name != ""){
					string path = Path.Combine (KSPUtil.ApplicationRootPath, "saves/" + save_name + "/Ships/VAB/" + craft_name + ".craft");
					EditorDriver.StartAndLoadVessel (path, EditorFacility.VAB);
				}else{
					EditorDriver.StartEditor (EditorFacility.VAB);
				}

			}
		}
	}


}
