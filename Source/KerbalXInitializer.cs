using System;
using UnityEngine;
using KSP.UI.Screens;

namespace KerbalX
{

    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class KerbalXInitializer : MonoBehaviour
    {

        //addd listeners for when the application launcher is ready to take instructions
        void Awake(){
            GameEvents.onGUIApplicationLauncherReady.Add(add_to_toolbar);
            GameEvents.onGUIApplicationLauncherDestroyed.Add(remove_from_toolbar);
            GameEvents.onGameSceneLoadRequested.Add(scene_load_request);
            KerbalXDownloadController.query_new_save = true;
        }

        //Trigger the creation of custom Skin (copy of default GUI.skin with various custom styles added to it)
        public void OnGUI(){
            if(KerbalXWindow.KXskin == null){
                StyleSheet.prepare();
            }
        }

        //Bind events to add buttons to the toolbar
        void add_to_toolbar(){
            ApplicationLauncher.Instance.AddOnHideCallback(this.toolbar_on_hide);     //bind events to close guis when toolbar hides

            KerbalX.log("Adding buttons to toolbar");

            if(!KerbalX.upload_gui_toolbar_button){
                KerbalX.upload_gui_toolbar_button = ApplicationLauncher.Instance.AddModApplication(
                    toggle_upload_interface, toggle_upload_interface, 
                    upload_btn_hover_on, upload_btn_hover_off, 
                    null, null, 
                    ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH, 
                    StyleSheet.assets["upload_toolbar_btn"]
                );
            }
            if(!KerbalX.download_gui_toolbar_button){
                KerbalX.download_gui_toolbar_button = ApplicationLauncher.Instance.AddModApplication(
                    toggle_download_interface, toggle_download_interface, 
                    download_btn_hover_on, download_btn_hover_off,
                    null, null, 
                    ApplicationLauncher.AppScenes.SPACECENTER,
                    StyleSheet.assets["dnload_toolbar_btn"]
                );
            }
            if(!KerbalX.console_button){
                KerbalX.console_button = ApplicationLauncher.Instance.AddModApplication(
                    toggle_console, toggle_console, 
                    null, null,
                    null, null, 
                    ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH, 
                    GameDatabase.Instance.GetTexture(Paths.joined("KerbalX", "Assets", "console_button"), false)
                );
            }
        }

        //remove any existing KX buttons from the toolbar
        public void remove_from_toolbar(){
            KerbalX.log("Removing buttons from toolbar");
            if(KerbalX.upload_gui_toolbar_button){
                ApplicationLauncher.Instance.RemoveModApplication(KerbalX.upload_gui_toolbar_button);
                KerbalX.upload_gui_toolbar_button = null;
            }
            if(KerbalX.download_gui_toolbar_button){
                ApplicationLauncher.Instance.RemoveModApplication(KerbalX.download_gui_toolbar_button);
                KerbalX.download_gui_toolbar_button = null;
            }
            if(KerbalX.console_button){
                ApplicationLauncher.Instance.RemoveModApplication(KerbalX.console_button);
                KerbalX.console_button = null;
            }
        }

        //triggered by scene load, calls removal of the buttons
        public void scene_load_request(GameScenes scene){
            remove_from_toolbar();
        }

        //triggered when the application launcher hides, used to teardown any open GUIs
        public void toolbar_on_hide(){
            if(KerbalX.upload_gui){
                GameObject.Destroy(KerbalX.upload_gui);
            }
            if(KerbalX.download_gui){
                GameObject.Destroy(KerbalX.download_gui);
            }
        }

        //Button Actions
        //Action for upload interface button
        public void toggle_upload_interface(){
            if(KerbalX.upload_gui){
                KerbalX.upload_gui.toggle();
            } else{
                KerbalX.log("UploadInterface has not been started");
            }
        }

        //Action for download interface button
        public void toggle_download_interface(){
            if(KerbalX.download_gui){
                if(KerbalX.download_gui.visible){
                    KerbalX.download_gui.hide();
                }else{
                    KerbalXDownloadController.instance.fetch_download_queue(true);
                }
            } else{
                KerbalX.log("DownloadInterface has not been started");
            }
        }

        //Action for console button.
        public void toggle_console(){
            if(KerbalX.console){
                KerbalX.console.toggle();
            }
        }

  
        //Button hover actions

        public void upload_btn_hover_on(){
            KerbalX.upload_gui_toolbar_button.SetTexture(StyleSheet.assets["upload_toolbar_btn_hover"]);
        }

        public void upload_btn_hover_off(){
            KerbalX.upload_gui_toolbar_button.SetTexture(StyleSheet.assets["upload_toolbar_btn"]);
        }

        public void download_btn_hover_on(){
            KerbalX.download_gui_toolbar_button.SetTexture(StyleSheet.assets["dnload_toolbar_btn_hover"]);
        }

        public void download_btn_hover_off(){
            KerbalX.download_gui_toolbar_button.SetTexture(StyleSheet.assets["dnload_toolbar_btn"]);
        }

    }
}

