

//██╗  ██╗███████╗██████╗ ██████╗  █████╗ ██╗     ██╗  ██╗    ███╗   ███╗ ██████╗ ██████╗ 
//██║ ██╔╝██╔════╝██╔══██╗██╔══██╗██╔══██╗██║     ╚██╗██╔╝    ████╗ ████║██╔═══██╗██╔══██╗
//█████╔╝ █████╗  ██████╔╝██████╔╝███████║██║      ╚███╔╝     ██╔████╔██║██║   ██║██║  ██║
//██╔═██╗ ██╔══╝  ██╔══██╗██╔══██╗██╔══██║██║      ██╔██╗     ██║╚██╔╝██║██║   ██║██║  ██║
//██║  ██╗███████╗██║  ██║██████╔╝██║  ██║███████╗██╔╝ ██╗    ██║ ╚═╝ ██║╚██████╔╝██████╔╝
//╚═╝  ╚═╝╚══════╝╚═╝  ╚═╝╚═════╝ ╚═╝  ╚═╝╚══════╝╚═╝  ╚═╝    ╚═╝     ╚═╝ ╚═════╝ ╚═════╝ 
// ____              _  __          _             _                           _       _ 
//|  _ \            | |/ /         | |           | |                         | |     (_)
//| |_) |  _   _    | ' /    __ _  | |_    __ _  | |_    ___    ___     ___  | |__    _ 
//|  _ <  | | | |   |  <    / _` | | __|  / _` | | __|  / _ \  / _ \   / __| | '_ \  | |
//| |_) | | |_| |   | . \  | (_| | | |_  | (_| | | |_  |  __/ | (_) | | (__  | | | | | |
//|____/   \__, |   |_|\_\  \__,_|  \__|  \__,_|  \__|  \___|  \___/   \___| |_| |_| |_|
//          __/ |                                                                       
//         |___/                                                                        


//Built Against KSP 1.2
//build id = 01586
//2016-10-11_12-44-44
//Branch: master


using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using KSP.UI.Screens;


namespace KerbalX
{
    public class KerbalX
    {
        public static string site_url = "http://localhost:3000";
//        public static string site_url = "http://192.168.1.2:3000";
        //public static string site_url = "http://kerbalx-stage.herokuapp.com";

        public static string token_path = Paths.joined(KSPUtil.ApplicationRootPath, "KerbalX.key");
        public static string screenshot_dir = Paths.joined(KSPUtil.ApplicationRootPath, "Screenshots");//TODO make this a setting, oh and make settings.
        public static string version = "0.0.2";

        public static bool failed_to_connect          = false;
        public static string server_error_message     = null;
        public static bool upgrade_required           = false;
        public static string upgrade_required_message = null;

        public static List<string> log_data = new List<string>();
        public static Dictionary<int, Dictionary<string, string>> existing_craft;//container for listing of user's craft already on KX and some details about them.


        //window handles (cos a window without a handle is just a pane)
        public static KerbalXWindow console                        = null;
        public static KerbalXLoginInterface login_gui               = null;
        public static KerbalXUploadInterface upload_gui             = null;
        public static KerbalXDownloadInterface download_gui         = null;
        public static KerbalXImageSelector image_selector           = null;
        public static KerbalXActionGroupInterface action_group_gui  = null;

        //Toolbar Buttons
        public static ApplicationLauncherButton upload_gui_toolbar_button   = null;
        public static ApplicationLauncherButton download_gui_toolbar_button = null;
        public static ApplicationLauncherButton console_button              = null;


        //logging, not suitable for lumberjacks
        public static void log(string s) { 
            s = "[KerbalX] " + s;
            log_data.Add(s); 
            Debug.Log(s);
        }

    }

    public delegate void DialogContent(KerbalXWindow dialog);
    public class KerbalXDialog : KerbalXWindow
    {
        public static KerbalXDialog instance = null;
        public DialogContent content;

        private void Start() {
            KerbalXDialog.instance = this;
            footer = false;
            is_dialog = true;
        }

        protected override void WindowContent(int win_id) {            
            content(this);
        }

        public static void close() {
            if (KerbalXDialog.instance) {
                GameObject.Destroy(KerbalXDialog.instance);
            }
        }
    }



}
