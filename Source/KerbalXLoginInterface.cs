using System;
using UnityEngine;

namespace KerbalX
{
    public delegate void AfterLoginAction();

    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class KerbalXLoginInterface : KerbalXWindow
    {
        private string username = "";
        private string password = "";
        public bool enable_login     = true; //used to toggle enabled/disabled state on login fields and button
        public bool login_failed     = false;//if true, displays login failed message and link to recover password on the site
        public bool login_successful = false;//if true, hides login field and shows logged in as and a logout button


        //in the the case of a login being trigger from a window (rather than from the main menu), 
        //this enables the window which triggered it to add actions to happen after login
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

