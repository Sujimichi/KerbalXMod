using System;
using UnityEngine;
using KSP.UI.Screens;

namespace KerbalX
{

	[KSPAddon(KSPAddon.Startup.MainMenu, false)]
	public class KerbalXToolBarSetter : MonoBehaviour
	{
		public Texture button_texture = new Texture();

		private void Awake(){
//			StyleSheet.load_assets ();
			button_texture = GameDatabase.Instance.GetTexture (Paths.joined ("KerbalX", "Assets", "button"), false); 
			GameEvents.onGUIApplicationLauncherReady.Add (this.app_launcher_ready);
			GameEvents.onGUIApplicationLauncherDestroyed.Add (this.remove_from_toolbar);
		}

		public void app_launcher_ready(){
			GameEvents.onGUIApplicationLauncherReady.Remove (this.app_launcher_ready);
			//ApplicationLauncher.Instance.AddOnShowCallback ();
			if(!KerbalX.button){
				add_to_toolbar ();
			}
		}

		public void add_to_toolbar(){
			KerbalX.log ("Adding buttons to toolbar");
			KerbalX.button = ApplicationLauncher.Instance.AddModApplication (
				KerbalX.toggle_upload_interface, KerbalX.toggle_upload_interface, 
				editor_btn_hover_on, editor_btn_hover_off, 
				null, null, 
				ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH, 
				StyleSheet.assets["editor_btn"]
			);
		}

		public void editor_btn_hover_on(){
			KerbalX.button.SetTexture (StyleSheet.assets["editor_btn_hover"]);
		}
		public void editor_btn_hover_off(){
			KerbalX.button.SetTexture (StyleSheet.assets["editor_btn"]);
		}

		public void remove_from_toolbar(){
			KerbalX.log ("Removing buttons from toolbar");
			ApplicationLauncher.Instance.RemoveModApplication (KerbalX.button);
		}
	}

	//	[KSPAddon(KSPAddon.Startup.MainMenu, false)]
	//	public class ToolBarTest : MonoBehaviour
	//	{
	//		public Texture button_texture = new Texture();
	//		public static ApplicationLauncherButton button;
	//
	//		private void Awake(){
	//			button_texture = GameDatabase.Instance.GetTexture ("KerbalX/Assets/button", false); 
	//			Debug.Log ("Adding App launcher event callbacks");
	//			GameEvents.onGUIApplicationLauncherReady.Add (this.app_launcher_ready);
	//			GameEvents.onGUIApplicationLauncherDestroyed.Add (this.app_launcher_destroyed);
	//		}
	//
	//		public void app_launcher_ready(){
	//			GameEvents.onGUIApplicationLauncherReady.Remove (this.app_launcher_ready);
	//			Debug.Log ("app launcher is ready");
	//			add_to_toolbar ();
	//		}
	//
	//		public void app_launcher_destroyed(){
	//			Debug.Log ("app launcher destroyed");
	//		}
	//
	//		public void add_to_toolbar(){
	//			Debug.Log ("Adding button to toolbar");
	//			ToolBarTest.button = ApplicationLauncher.Instance.AddModApplication (
	//				button_action, button_action,
	//				null, null, 
	//				null, null, 
	//				ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH, 
	//				button_texture
	//			);
	//		}
	//		public void remove_from_toolbar(){
	//			Debug.Log ("removing button from toolbar");
	//			ApplicationLauncher.Instance.RemoveModApplication (button);
	//		}
	//
	//		public void button_action(){
	//			Debug.Log ("Oi! someone clicked me.");
	//		}
	//	}




}

