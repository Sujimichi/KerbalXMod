using System;
//using System.Linq;
//using System.Text;

//using System.Collections;
using System.Collections.Generic;
//using System.Collections.Specialized;
//using System.Globalization;

//using SimpleJSON;

using UnityEngine;
using UnityEngine.UI;
//using UnityEngine.EventSystems;
//using UnityEngine.Networking;


namespace KerbalX
{
	public class KerbalX
	{
		public static string token_path = Paths.joined (KSPUtil.ApplicationRootPath, "KerbalX.key");
		public static List<string> log_data = new List<string>();
		public static bool failed_to_connect = false;
		public static string server_error_message = null;


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

	}

	public delegate void DialogContent(KerbalXWindow dialog);
	public class KerbalXDialog : KerbalXWindow
	{
		public static KerbalXDialog instance;
		public DialogContent content;

		private void Start(){
			KerbalXDialog.instance = this;
			footer = false;
		}

		protected override void WindowContent(int win_id){
			content (this);
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

		protected override void WindowContent(int win_id){
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
				GUILayout.Label ("You are logged in as " + KerbalXAPI.logged_in_as ());
			}

			if(login_successful){
				section (w => {
					GUILayout.Label ("KerbalX.key saved in KSP root", width (w-20f));
					if (GUILayout.Button ("?", width (20f))) {

						KerbalXDialog dialog = show_dialog((d) => {
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
				if (GUILayout.Button ("Login", "button.login")) {				
					KerbalXAPI.login (username, password);
					password = "";
				}
				GUI.enabled = true;
			}else{
				if (GUILayout.Button ("Log out", "button.login")) {
					KerbalXAPI.log_out ();
					username = "";
					password = ""; //should already be empty, but just in case
				}				
			}
			GUI.enabled = true; //just in case

			if(login_failed){
				v_section (w => {
					GUILayout.Label ("Login failed, check your things", "alert");
					if (GUILayout.Button ("Forgot your password? Go to KerbalX to reset it.")) {
						Application.OpenURL ("https://kerbalx.com/users/password/new");
					}
				});
			}
		}
	}

	[KSPAddon(KSPAddon.Startup.EditorAny, false)]
	public class KerbalXConsoleReposition : MonoBehaviour
	{
		private bool set_state = true;

		private void Update(){
			if(set_state){
				set_state = false;
//				KerbalX.console.window_pos = new Rect(250, 10, 310, 5);
				KerbalX.console.window_pos = new Rect(Screen.width - 400, Screen.height/2, 310, 5);
			}
		}
	}

	public delegate void ComboResponse(int selected);

	public class ComboBox : KerbalXWindowExtension
	{
		public static ComboBox instance;


		public string active_anchor = null;
		public Rect anchor_rect = new Rect(0,0,100,100);
		public float list_height = 150;
		public Rect container 	= new Rect(0,0,100,100);
		public KerbalXWindow parent_window;
		public Dictionary<int, string> sel_options = new Dictionary<int, string> ();
		public ComboResponse response;
		public Vector2 scroll_pos;
		public int gui_depth = 0;
		public string select_field ="";
		public string filter_string = "";

		Texture2D tex = new Texture2D(1, 1,   TextureFormat.RGBA32, false);
		int on_hover = 0;

		void Start(){
			instance = this;
			tex.SetPixel (0,0, new Color (0.4f,0.5f,0.9f,1));
			tex.Apply ();
		}

		public void open(string combo_name, Dictionary<int, string> select_options, Rect anchor, float height, KerbalXWindow parent_win, ComboResponse selection_callback){
			if(active_anchor != combo_name){
				active_anchor = combo_name;
				sel_options = select_options;
				response = selection_callback;
				anchor_rect = anchor;
				parent_window = parent_win;
				list_height = height;
			}
		}

		public void close(){
			parent_window.unlock_ui ();
			active_anchor = null;
			GameObject.Destroy (ComboBox.instance);
		}

		void OnGUI(){
			//set the container to track it's anchors position (which is relative to the parent window), relative to the screen. 
			//ie offset from screen 0,0 of the window + the offset from the window of the anchor. (#when-comments-make-less-sense-than-code)
			container.x = anchor_rect.x + parent_window.window_pos.x;
			container.y = anchor_rect.y + parent_window.window_pos.y;
			container.width = anchor_rect.width + 26f; //the 26 accouts for the 20f wide button + 3f padding either side of it.
			container.height = list_height;
				
			GUI.skin = KerbalXWindow.KXskin;
			GUI.depth = gui_depth;

			//If the mouse is NOT over the combobox and its list AND the user clicks, the close the combobox.  Otherwise set the parent window to be locked (GUI.enabled = false)
			if (!container.Contains (Event.current.mousePosition) && Input.GetKeyDown (KeyCode.Mouse0) && Input.GetMouseButtonDown (0)){
				close ();
			}else{
				parent_window.lock_ui ();
			}

			begin_group (container, () => {
				
				style_override = GUI.skin.GetStyle ("background.dark");
				v_section (container.width, w => {
					GUIStyle text_field = new GUIStyle(GUI.skin.textField);
					text_field.margin = new RectOffset(0,0,0,0);
					GUIStyle down_button = new GUIStyle(GUI.skin.button);
					down_button.margin.top = 0;
					
					GUIStyle sel_option = new GUIStyle(GUI.skin.label);
					sel_option.margin = new RectOffset(0,0,0,0);
					sel_option.padding = new RectOffset(3,3,1,1);
					
					GUIStyle sel_option_hover = new GUIStyle(sel_option);
					sel_option_hover.normal.background = tex;
					sel_option_hover.normal.textColor = Color.black;
					
					
					section(w2 => {
						GUI.SetNextControlName ("combobox.filter_field");
						select_field = GUILayout.TextField (select_field, 255, text_field, width(w-26f));
						if (GUILayout.Button ("\\/", down_button, GUILayout.Width (20f))) {
							close ();
						}
						if(GUI.changed){
							filter_string = select_field;
						}
						GUI.FocusControl ("combobox.filter_field");
//						if(filter_string == ""){
//							TextEditor te = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
//							te.SelectAll ();
//						}


					});
					
					scroll_pos = scroll (scroll_pos, w, container.height-22f, sw => {
						foreach(KeyValuePair<int, string> option in sel_options){
							if(filter_string.Trim () == "" || option.Value.ToLower ().Contains (filter_string.Trim ().ToLower ())){
								if(GUILayout.Button (option.Value, on_hover == option.Key ? sel_option_hover : sel_option)){
									response(option.Key);
									close ();
								}
								if(GUILayoutUtility.GetLastRect ().Contains (Event.current.mousePosition)){
									on_hover = option.Key;
									if(filter_string.Trim () == ""){
										select_field = option.Value;
									}
								}
							}
						}
					});
					
				});
			});

			GUI.skin = null;
		}
	}

	[KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
	public class KerbalXConsole : KerbalXWindow
	{
		private void Start(){
			window_title = "KX::Konsole";
			window_pos = new Rect(0, 0, 310, 5);
			KerbalX.console = this;
			enable_request_handler ();
			prevent_editor_click_through = true;
		}

		private Dictionary<int, string> craft_styles = new Dictionary<int, string> (){
			{0, "Ship"}, {1, "Aircraft"}, {2, "Spaceplane"}, {3, "Lander"}, {4, "Satellite"}, {5, "Station"}, {6, "Base"}, {7, "Probe"}, {8, "Rover"}, {9, "Lifter"}
		};
		private int selected_style_id = 0;
		private int selected_style_id2 = 0;


		protected override void WindowContent(int win_id){
			section (300f, e => { GUILayout.Label (KerbalX.last_log ());	});


			if (GUILayout.Button ("show thing")) {
				KerbalX.editor_gui.show_upload_compelte_dialog ("fooobar/moo");
			}

			combobox ("craft_style_select", craft_styles, selected_style_id, 150f, 150f, this, id => {selected_style_id = id;});
			combobox ("craft_style_select2", craft_styles, selected_style_id2, 80f, 150f, this, id => {selected_style_id2 = id;});



			if (GUILayout.Button ("update existing craft")) {
				KerbalXAPI.fetch_existing_craft (() => {});
			}

			if(GUILayout.Button ("open interface")){
				gameObject.AddOrGetComponent<KerbalXUploadInterface> ();
			}

			if(GUILayout.Button ("close interface")){
				GameObject.Destroy (KerbalX.editor_gui);
			}

			if (GUILayout.Button ("show Login")) {
				KerbalXLoginWindow login_window = gameObject.AddOrGetComponent<KerbalXLoginWindow> ();
				login_window.after_login_action = () => {
					on_login ();
				};
			}

			if (GUILayout.Button ("print log to console")) { KerbalX.show_log (); }
		}

		protected override void on_login (){
			base.on_login ();
		}
	}

		
	[KSPAddon(KSPAddon.Startup.MainMenu, false)]
	public class JumpStart : MonoBehaviour
	{
		public static bool autostart = false;
		public static string save_name = "default";
		public static string craft_name = "testy";

		public void Start(){
			
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
