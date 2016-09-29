using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using UnityEngine;

namespace KerbalX
{
	
	[KSPAddon(KSPAddon.Startup.MainMenu, false)]
	public class JumpStart : MonoBehaviour
	{
		public static bool autostart = true;
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


	[KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
	public class KerbalXConsole : KerbalXWindow
	{
		private void Start(){
			window_title = "KX::Konsole";
			window_pos = new Rect(0, 0, 310, 5);
			KerbalX.console = this;
			enable_request_handler ();
			//			prevent_editor_click_through = true;
		}


		protected override void WindowContent(int win_id){
			section (300f, e => { GUILayout.Label (KerbalX.last_log ());	});

	

			if (GUILayout.Button ("update existing craft")) {
				KerbalXAPI.fetch_existing_craft (() => {});
			}

			if(GUILayout.Button ("open interface")){
				gameObject.AddOrGetComponent<KerbalXUploadInterface> ();
			}

			if(GUILayout.Button ("close interface")){
				GameObject.Destroy (KerbalX.upload_gui);
			}

			if (GUILayout.Button ("show Login")) {
				KerbalXLoginWindow login_window = gameObject.AddOrGetComponent<KerbalXLoginWindow> ();
				login_window.after_login_action = () => {
					on_login ();
				};
			}

			if(KerbalX.upload_gui != null){
				if (GUILayout.Button ("show thing")) {
					KerbalX.upload_gui.show_upload_compelte_dialog ("fooobar/moo");
				}
			}

			if (GUILayout.Button ("print log to console")) { KerbalX.show_log (); }
		}

		protected override void on_login (){
			base.on_login ();
		}
	}

}

