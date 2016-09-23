using System;
using System.Linq;
using System.Text;

using System.Collections;
using System.Collections.Generic;
//using System.Collections.Specialized;
//using System.Globalization;

using SimpleJSON;

using UnityEngine;
//using UnityEngine.UI;
//using UnityEngine.EventSystems;
//using UnityEngine.Networking;


namespace KerbalX
{
	public class KerbalX
	{
		public static string token_path = Paths.joined (KSPUtil.ApplicationRootPath, "KerbalX.key");
		public static List<string> log_data = new List<string>();
		public static string notice = "";
		public static string alert = "";
		public static bool failed_to_connect = false;


		public static string site_url = "http://localhost:3000";
		public static string screenshot_dir = Paths.joined (KSPUtil.ApplicationRootPath, "Screenshots"); //TODO make this a setting, oh and make settings.

		public static Dictionary<int, Dictionary<string, string>> existing_craft; //container for listing of user's craft already on KX and some details about them.

		//window handles (cos a window without a handle is just a pane)
		public static KerbalXConsole console 				= null;
		public static KerbalXLoginWindow login_gui 			= null;
		public static KerbalXUploadInterface editor_gui 	= null;
		public static KerbalXImageSelector image_selector 	= null;


		//methodical things


		//logging stuf, not suitable for lumberjacks
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


	}

	public delegate void DialogContent();
	public class KerbalXDialog : KerbalXWindow
	{
		public static KerbalXDialog instance;
		public DialogContent content;

		private void Start(){
			KerbalXDialog.instance = this;
			footer = false;
		}

		protected override void WindowContent(int win_id){
			content ();
		}
	}

	public delegate void AfterLoginAction();
	[KSPAddon(KSPAddon.Startup.MainMenu, false)]
	public class KerbalXLoginWindow : KerbalXWindow
	{
		private string username = "";
		private string password = "";
		public bool enable_login = true;  //used to toggle enabled/disabled state on login fields and button
		public bool login_failed = false;
		public bool login_successful = false;
		public AfterLoginAction after_login_action = () => {};




		private void Start(){
			window_title = "KerbalX::Login";
			window_pos = new Rect((Screen.width/2 - 400/2),100, 400, 5);
			KerbalX.login_gui = this;
			enable_request_handler ();

			//try to load a token from file and if present authenticate it with KerbalX.  if token isn't present or token authentication fails then show login fields.
			if (KerbalXAPI.logged_out()) {
				KerbalXAPI.load_and_authenticate_token ();	
			}
		}

		protected override void WindowContent(int win_id)
		{
			if(KerbalXAPI.logged_out ()){					
				GUI.enabled = enable_login;
				GUILayout.Label ("Enter your KerbalX username and password");
				section (w => {
					GUILayout.Label ("username", GUILayout.Width (60f));
					username = GUILayout.TextField (username, 255, width (w-60f));
				});
				section (w => {
					GUILayout.Label ("password", GUILayout.Width(60f));
					password = GUILayout.PasswordField (password, '*', 255, width (w-60f));
				});
				GUI.enabled = true;
			}

			if (KerbalXAPI.logged_in ()) {
				GUILayout.Label ("You are logged in");
			}

			if(login_successful){
				section (w => {
					GUILayout.Label ("KerbalX.key saved in KSP root", width (w-20f));
					if (GUILayout.Button ("?", width (20f))) {

						KerbalXDialog dialog = show_dialog(() => {
							string message = "The KerbalX.key is a token that is used to authenticate you with the site." +
								"\nIt will also persist your login, so next time you start KSP you won't need to login again." +
								"\nIf you want to login to KerbalX from multiple instances of KSP copy the KerbalX.key file into each install.";
							GUILayout.Label (message);
							if(GUILayout.Button ("OK")){
								close_dialog ();
							};
						});
						dialog.window_pos = new Rect(window_pos.x + window_pos.width + 10, window_pos.y, 300f, 5);
						dialog.window_title = "KerablX Token File";
					}
				});
			}

			if (KerbalXAPI.logged_out ()) {
				GUI.enabled = enable_login;
				if (GUILayout.Button ("Login")) {				
					KerbalXAPI.login (username, password);
				}
				GUI.enabled = true;
			}else{
				if (GUILayout.Button ("Log out")) {
					KerbalXAPI.log_out ();
				}				
			}
			GUI.enabled = true; //just in case

			if(login_failed){
				v_section (w => {
					GUILayout.Label ("Login failed, check your things", alert_style);
					if (GUILayout.Button ("Forgot your password? Go to KerbalX to reset it.")) {
						Application.OpenURL ("https://kerbalx.com/users/password/new");
					}
				});
			}
		}
	}


	[KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
	public class KerbalXConsole : KerbalXWindow
	{
		private void Start()
		{
			window_title = "KX::Konsole";
			window_pos = new Rect(0, 0, 310, 5);
			KerbalX.console = this;
			enable_request_handler ();
		}

		protected override void WindowContent(int win_id)
		{
			section (300f, e => { GUILayout.Label (KerbalX.last_log ());	});

			GUILayout.Label (KerbalXAPI.temp_view_token());

			if (GUILayout.Button ("count existing craft")) {
				Debug.Log ("existing craft count: " + KerbalX.existing_craft.Keys.Count);
			}

			if(GUILayout.Button ("test 1")){
				Debug.Log (KerbalX.image_selector.window_pos.width.ToString ());
			}

			if (GUILayout.Button ("Login")) {
				KerbalXLoginWindow login_window = gameObject.AddOrGetComponent<KerbalXLoginWindow> ();
				login_window.after_login_action = () => {
					on_login ();
				};
			}

			if (GUILayout.Button ("print log to console")) { KerbalX.show_log (); }
		}

		protected override void on_login ()
		{
			base.on_login ();
			Debug.Log ("dis shit called from conosle");
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
				DebugToolbar.toolbarShown = true;
				var editor = EditorFacility.VAB;
				GamePersistence.LoadGame("persistent", HighLogic.SaveFolder, true, false);
				if(craft_name != null || craft_name != ""){					
					string path = Paths.joined (KSPUtil.ApplicationRootPath, "saves", save_name, "Ships", "VAB", craft_name + ".craft");
					EditorDriver.StartAndLoadVessel (path, editor);
				}else{
					EditorDriver.StartEditor (editor);
				}

			}
		}
	}


}
