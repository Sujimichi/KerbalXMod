using System;
using UnityEngine;
using KSP.UI.Screens;

namespace KerbalX
{

    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class KerbalXInitializer : MonoBehaviour
    {

        //addd listeners for when the application launcher is ready to take instructions
        private void Awake(){
            GameEvents.onGUIApplicationLauncherReady.Add(this.app_launcher_ready);
            GameEvents.onGUIApplicationLauncherDestroyed.Add(this.remove_from_toolbar);
        }

        //Trigger the creation of custom Skin (copy of default GUI.skin with various custom styles added to it)
        public void OnGUI(){
            if (KerbalXWindow.KXskin == null){
                StyleSheet.prepare();
            }
        }

        //Bind events to add buttons to the toolbar
        public void app_launcher_ready(){
            GameEvents.onGUIApplicationLauncherReady.Remove(this.app_launcher_ready); //remove the listener to prevent multiple calls to this method
            ApplicationLauncher.Instance.AddOnHideCallback(this.toolbar_on_hide);     //bind events to close guis when toolbar hides
            if (!KerbalX.upload_gui_toolbar_button){
                add_upload_gui_button_to_toolbar();
            }
            if (!KerbalX.download_gui_toolbar_button){
                add_download_gui_button_to_toolbar();
            }
        }

        public void add_upload_gui_button_to_toolbar(){
            KerbalX.log("Adding buttons to toolbar");
            KerbalX.upload_gui_toolbar_button = ApplicationLauncher.Instance.AddModApplication(
                toggle_upload_interface, toggle_upload_interface, 
                upload_btn_hover_on, upload_btn_hover_off, 
                null, null, 
                ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH, 
                StyleSheet.assets["editor_btn"]
            );
        }

        public void add_download_gui_button_to_toolbar(){
            KerbalX.download_gui_toolbar_button = ApplicationLauncher.Instance.AddModApplication(
                toggle_download_interface, toggle_download_interface, 
                upload_btn_hover_on, upload_btn_hover_off, 
                null, null, 
                ApplicationLauncher.AppScenes.SPACECENTER,
                StyleSheet.assets["editor_btn"]
            );
        }


        public void toggle_upload_interface(){
            if (KerbalX.upload_gui){
                KerbalX.upload_gui.toggle();
            } else{
                KerbalX.log("UploadInterface has not been started");
            }
        }

        public void toggle_download_interface(){
            if (KerbalX.download_gui){
                KerbalX.download_gui.toggle();
            } else{
                KerbalX.log("DownloadInterface has not been started");
            }
        }

        public void toolbar_on_hide(){
            if (KerbalX.upload_gui){
                GameObject.Destroy(KerbalX.upload_gui);
            }
        }

        public void upload_btn_hover_on(){
            KerbalX.upload_gui_toolbar_button.SetTexture(StyleSheet.assets["editor_btn_hover"]);
        }

        public void upload_btn_hover_off(){
            KerbalX.upload_gui_toolbar_button.SetTexture(StyleSheet.assets["editor_btn"]);
        }

        public void remove_from_toolbar(){
            KerbalX.log("Removing buttons from toolbar");
            ApplicationLauncher.Instance.RemoveModApplication(KerbalX.upload_gui_toolbar_button);
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

