using System;
using UnityEngine;
using KSP.UI.Screens;

namespace KerbalX
{

	[KSPAddon(KSPAddon.Startup.MainMenu, false)]
	public class KerbalXInitializer : MonoBehaviour
	{
		public static KerbalXInitializer instance = null;
		public Texture button_texture = new Texture();

		private void Awake(){
			instance = this;
			button_texture = StyleSheet.assets ["editor_btn"];
			GameEvents.onGUIApplicationLauncherReady.Add (this.app_launcher_ready);
			GameEvents.onGUIApplicationLauncherDestroyed.Add (this.remove_from_toolbar);
		}

		public void OnGUI(){
			if(KerbalXWindow.KXskin == null){
				StyleSheet.prepare ();
			}
		}


		public void app_launcher_ready(){
			GameEvents.onGUIApplicationLauncherReady.Remove (this.app_launcher_ready);
			ApplicationLauncher.Instance.AddOnHideCallback (this.toolbar_on_hide);
			if(!KerbalX.editor_toolbar_button){
				add_to_toolbar ();
			}
		}

		public void add_to_toolbar(){
			KerbalX.log ("Adding buttons to toolbar");
			KerbalX.editor_toolbar_button = ApplicationLauncher.Instance.AddModApplication (
				toggle_upload_interface, toggle_upload_interface, 
				editor_btn_hover_on, editor_btn_hover_off, 
				null, null, 
				ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH, 
				StyleSheet.assets["editor_btn"]
			);
		}

		public void toggle_upload_interface(){
			if(KerbalX.upload_gui){
				KerbalX.upload_gui.toggle ();
			}else{
				KerbalX.log ("UploadInterface has not been started");
			}
		}

		public void toolbar_on_hide(){
			if(KerbalX.upload_gui){
				GameObject.Destroy (KerbalX.upload_gui);
			}
		}
		
		public void editor_btn_hover_on(){
			KerbalX.editor_toolbar_button.SetTexture (StyleSheet.assets["editor_btn_hover"]);
		}
		public void editor_btn_hover_off(){
			KerbalX.editor_toolbar_button.SetTexture (StyleSheet.assets["editor_btn"]);
		}

		public void remove_from_toolbar(){
			KerbalX.log ("Removing buttons from toolbar");
			ApplicationLauncher.Instance.RemoveModApplication (KerbalX.editor_toolbar_button);
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

