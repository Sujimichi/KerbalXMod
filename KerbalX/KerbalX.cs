using System;

//using System.Linq;
using System.Text;

//using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using KSP.UI.Screens;

//Building Against KSP 1.2
//build id = 01586
//2016-10-11_12-44-44
//Branch: master

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
        public static KerbalXConsole console                        = null;
        public static KerbalXLoginWindow login_gui                  = null;
        public static KerbalXUploadInterface upload_gui             = null;
        public static KerbalXDownloadInterface download_gui         = null;
        public static KerbalXImageSelector image_selector           = null;
        public static KerbalXActionGroupInterface action_group_gui  = null;

        //Toolbar Buttons
        public static ApplicationLauncherButton upload_gui_toolbar_button   = null;
        public static ApplicationLauncherButton download_gui_toolbar_button = null;
        public static ApplicationLauncherButton console_button              = null;


        //logging stuf, not suitable for lumberjacks
        public static void log(string s) { 
            s = "[KerbalX] " + s;
            log_data.Add(s); 
            Debug.Log(s);
        }

        public static string last_log() {
            if (log_data.Count != 0) {
                return log_data[log_data.Count - 1];
            } else {
                return "nothing logged yet";
            }
        }



        public static void show_log() {
            foreach (string l in log_data) {
                Debug.Log(l);
            }
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


    public delegate void AfterLoginAction();
    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class KerbalXLoginWindow : KerbalXWindow
    {
        private string username = "";
        private string password = "";
        public bool enable_login     = true; //used to toggle enabled/disabled state on login fields and button
        public bool login_failed     = false;//if true, displays login failed message and link to recover password on the site
        public bool login_successful = false;//if true, hides login field and shows logged in as and a logout button


        //in the the case of a login being trigger from elsewhere, enables the window which triggered it to add actions to happen after login
        public AfterLoginAction after_login_action = () => {};


        private void Start() {
            window_title = "KerbalX::Login";
            window_pos = new Rect(50, 50, 400, 5);
            KerbalX.login_gui = this;
            enable_request_handler();
            //try to load a token from file and if present authenticate it with KerbalX.  if token isn't present or token authentication fails then show login fields.
            if (KerbalXAPI.logged_out()) {
                KerbalXAPI.load_and_authenticate_token();	
            }
        }

        //Shows an upgrade available message after login if the server provides a upload available message string
        public void show_upgrade_available_message(string message) {
            if (!String.IsNullOrEmpty(message)) {
                KerbalXDialog dialog = show_dialog((d) => {
                    v_section(w => {
                        GUILayout.Label("KerbalX Update Available", "h2");
                        GUILayout.Label("A new version of the KerbalX mod is available");
                        GUILayout.Label(message);

                        section(w2 => {
                            if (GUILayout.Button("Remind me later", height(30), width(w * 0.5f))) {
                                close_dialog();
                            }
                            if (GUILayout.Button("Yeah ok, stop bugging me", height(30), width(w * 0.5f))) {
                                KerbalXAPI.dismiss_current_update_notification();
                                close_dialog();
                            }
                        });

                    });
                });
                dialog.window_title = "KerbaX - Update Available";
                dialog.window_pos = new Rect(window_pos.x + window_pos.width + 10, window_pos.y, 400f, 5);
            }
        }

        protected override void WindowContent(int win_id) {
            if (KerbalXAPI.logged_out()) {					
                GUI.enabled = enable_login;
                GUILayout.Label("Enter your KerbalX username and password");
                section(w => {
                    GUILayout.Label("username", GUILayout.Width(60f));
                    username = GUILayout.TextField(username, 255, width(w - 60f));
                });
                section(w => {
                    GUILayout.Label("password", GUILayout.Width(60f));
                    password = GUILayout.PasswordField(password, '*', 255, width(w - 60f));
                });
                Event e = Event.current;
                if (e.type == EventType.keyDown && e.keyCode == KeyCode.Return && !String.IsNullOrEmpty(username) && !String.IsNullOrEmpty(password)) {
                    KerbalXAPI.login(username, password);
                }
                GUI.enabled = true;
            }

            if (KerbalXAPI.logged_in()) {
                GUILayout.Label("You are logged in as " + KerbalXAPI.logged_in_as());
            }

            if (login_successful) {
                section(w => {
                    GUILayout.Label("KerbalX.key saved in KSP root", width(w - 20f));
                    if (GUILayout.Button("?", width(20f))) {
                        KerbalXDialog dialog = show_dialog((d) => {
                            string message = "The KerbalX.key is a token that is used to authenticate you with the site." +
                                             "\nIt will also persist your login, so next time you start KSP you won't need to login again." +
                                             "\nIf you want to login to KerbalX from multiple KSP installs, copy the KerbalX.key file into each install.";
                            GUILayout.Label(message);
                            if (GUILayout.Button("OK")) {
                                close_dialog();
                            }
                            ;
                        });
                        dialog.window_title = "KerablX Token File";
                        dialog.window_pos = new Rect(window_pos.x + window_pos.width + 10, window_pos.y, 350f, 5);
                    }
                });
            }

            if (KerbalXAPI.logged_out()) {
                GUI.enabled = enable_login;
                if (GUILayout.Button("Login", "button.login")) {				
                    KerbalXAPI.login(username, password);
                    password = "";
                }
                GUI.enabled = true;
            } else {
                if (GUILayout.Button("Log out", "button.login")) {
                    KerbalXAPI.log_out();
                    username = "";
                    password = ""; //should already be empty, but just in case
                }				
            }
            GUI.enabled = true; //just in case

            if (login_failed) {
                v_section(w => {
                    GUILayout.Label("Login failed, check your things", "alert");
                    if (GUILayout.Button("Forgot your password? Go to KerbalX to reset it.")) {
                        Application.OpenURL("https://kerbalx.com/users/password/new");
                    }
                });
            }
        }
    }

}
