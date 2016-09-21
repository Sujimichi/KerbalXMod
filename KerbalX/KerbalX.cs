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

		public static string site_url = "http://localhost:3000";

		public static string screenshot_dir = Paths.joined (KSPUtil.ApplicationRootPath, "Screenshots"); //TODO make this a setting, oh and make settings.

		public static Dictionary<int, Dictionary<string, string>> existing_craft; //container for listing of user's craft already on KX and some details about them.

		//window handles (cos a window without a handle is just a pane)
		public static KerbalXConsole console 				= null;
		public static KerbalXLoginWindow login_gui 			= null;
		public static KerbalXUploadInterface editor_gui 	= null;
		public static KerbalXImageSelector image_selector 	= null;


		//methodical things
		//takes partial url and returns full url to site; ie url_to("some/place") -> "http://whatever_domain_site_url_defines.com/some/place"
		public static string url_to (string path){
			if(!path.StartsWith ("/")){ path = "/" + path;}
			return site_url + path;
		}

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

	public delegate void DialogAction();
	public class KerbalXDialog : KerbalXWindow
	{
		public static KerbalXDialog instance;
		public float dialog_width = 300f;
		public string message = "";
		public DialogAction ok_action = null;
		public string ok_text = "OK";

		private void Start(){
			window_pos = new Rect((Screen.width/2 - dialog_width/2), Screen.height/4, dialog_width, 5);	
			window_title = "";
			footer = false;
			ok_action = () => {
				GameObject.Destroy (KerbalXDialog.instance);
			};
		}

		protected override void WindowContent(int win_id){
			GUILayout.Label (message);
			if(ok_action != null){
				if(GUILayout.Button (ok_text)){
					ok_action ();
				}
			}
		}
	}


	[KSPAddon(KSPAddon.Startup.MainMenu, false)]
	public class KerbalXLoginWindow : KerbalXWindow
	{
		private string username = "";
		private string password = "";
		public bool enable_login = true;  //used to toggle enabled/disabled state on login fields and button
		public bool show_login = false;
		public bool login_failed = false;
		public bool login_successful = false;


		GUIStyle alert_style = new GUIStyle();


		private void Start(){
			window_title = "KerbalX::Login";
			window_pos = new Rect((Screen.width/2 - 400/2),100, 400, 5);
			KerbalX.login_gui = this;
			alert_style.normal.textColor = Color.red;
			enable_request_handler ();

			//try to load a token from file and if present authenticate it with KerbalX.  if token isn't present or authentication fails the show login fields
			if (KerbalXAPI.token_not_loaded()) {
				KerbalXAPI.load_and_authenticate_token ();	
			}
		}

		protected override void WindowContent(int win_id)
		{
			if(show_login){					
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

			if (KerbalXAPI.token_loaded ()) {
				GUILayout.Label ("You are logged in");
			}
			if(login_successful){
				section (w => {
					GUILayout.Label ("KerbalX.key saved in KSP root", width (w-20f));
					if (GUILayout.Button ("?", width (20f))) {
						KerbalXDialog dialog = gameObject.AddOrGetComponent<KerbalXDialog> ();
						KerbalXDialog.instance = dialog;
						dialog.message = "The KerbalX.key is a token that is used to authenticate you with the site." +
							"\nIt will also persist your login, so next time you start KSP you won't need to login again." +
							"\nIf you want to login to KerbalX from multiple instances of KSP copy the KerbalX.key file into each install.";
					}
				});
			}

			GUI.enabled = enable_login;
			if (show_login) {
				if (GUILayout.Button ("Login")) {				
					KerbalX.alert = "";
					enable_login = false;
					login_failed = false;
					KerbalXAPI.login (username, password);
				}
			}else{
				if (GUILayout.Button ("Log out")) {
					show_login = true;
					KerbalXAPI.clear_token ();
					KerbalX.notify ("logged out");
				}				
			}
			GUI.enabled = true;

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





	[KSPAddon(KSPAddon.Startup.EditorAny, false)]
	public class EditorActions : MonoBehaviour
	{
		private bool set_state = true;
		public string editor = null;

		private void Update(){
			if(set_state){
				set_state = false;
				KerbalX.console.window_pos = new Rect(250, 10, 310, 5);
				KerbalX.editor_gui.current_editor = EditorLogic.fetch.ship.shipFacility.ToString ();
			}
		}
	}

	[KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
	public class KerbalXConsole : KerbalXWindow
	{
		private void Start()
		{
			window_title = "KX::Konsole";
			KerbalX.console = this;
			enable_request_handler ();
		}


		protected override void WindowContent(int win_id)
		{
			section (300f, e => { GUILayout.Label (KerbalX.last_log ());	});


			if(GUILayout.Button ("test 1")){
				HTTP http = HTTP.get ("http://localhost:3000/katateochi.json");
				http.set_header ("token", "foobar").send ((resp,code) => {
					Debug.Log (resp);
				});
			}

			if(GUILayout.Button ("ping production")){
				HTTP.get ("https://KerbalX.com/katateochi.json").send ((resp,code) => {
					Debug.Log (resp);
				});
			}


			if (GUILayout.Button ("open")) {
				//Foobar fb = gameObject.AddComponent (typeof(Foobar)) as Foobar;
				Foobar ff = gameObject.AddOrGetComponent<Foobar> ();
				Foobar.this_instance = ff;
			}
			if (GUILayout.Button ("close")) {
				Foobar ff = gameObject.AddOrGetComponent<Foobar> ();
				GameObject.Destroy (ff);
			}

			if (GUILayout.Button ("add text")) {
				Foobar ff = gameObject.AddOrGetComponent<Foobar> ();
				ff.some_text = "marmalade";
			}

			if (GUILayout.Button ("print log to console")) { KerbalX.show_log (); }
		}
	}

	//[KSPAddon(KSPAddon.Startup.MainMenu, false)]
	public class Foobar : MonoBehaviour
	{
		private int window_id = 42;
		private Rect window_pos = new Rect(200, 200, 200, 200);

		public string some_text = "";
		public static Foobar this_instance = null;

		void Start(){
				
		}

		protected void OnGUI()
		{
			window_pos = GUILayout.Window (window_id, window_pos, DrawWindow, "testy moo");
		}

		public void DrawWindow(int win_id){

			GUILayout.Label ("this is a test");
			GUILayout.Label (some_text);
			GUI.DragWindow();

		}

	}


		
	[KSPAddon(KSPAddon.Startup.MainMenu, false)]
	public class JumpStart : MonoBehaviour
	{
		public static bool autostart = false;
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
