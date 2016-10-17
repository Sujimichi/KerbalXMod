using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;

namespace KerbalX
{

    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class KerbalXDownloadController : KerbalXWindowExtension
    {
        public static KerbalXDownloadController instance = null;
        public static bool query_new_save = true;

        internal bool deferred_downloads_enabled = false;
        private  Dictionary<int, Dictionary<string, string>> download_queue = new Dictionary<int, Dictionary<string, string>>();
        private Rect container = new Rect(Screen.width/2 - 500/2, -90, 500, 80);
        private int disable_link = 0;
        private long start_time = 0;
        private int startup_delay = 1500;


        private void Start(){
            enable_request_handler();
            download_gui();
            start_time = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            instance = this;
            check_deferred_downloads_setting();
            fetch_download_queue();
            GameEvents.OnAppFocus.Add(app_focus);
        }

        //returns current KerbalXDownloadInterface instance or creates and returns one if none exists.
        private KerbalXDownloadInterface download_gui(){
            if(!KerbalX.download_gui){
                return gameObject.AddOrGetComponent<KerbalXDownloadInterface>();
            }
            return KerbalX.download_gui;
        }

        private void OnDestroy(){
            GameEvents.OnAppFocus.Remove(app_focus);
        }

        //triggered when the game gets or loses focus. When it gets focus this fetches an update of the download queue.
        //ie if user tabs out of game, downloads a craft on the site and tabs back, this will trigger the download notification to appear right away.
        public void app_focus(bool focus_on){
            if(focus_on){
                fetch_download_queue((KerbalX.download_gui && KerbalX.download_gui.visible && KerbalX.download_gui.mode == "Download Queue"));
            }
        }

        internal void check_deferred_downloads_setting(){
            KerbalXAPI.deferred_downloads_enabled((resp, code) =>{
                if(code == 200 && resp == "enabled"){
                    deferred_downloads_enabled = true;
                }
            });
        }

        internal void enable_deferred_downloads(){
            KerbalXAPI.enable_deferred_downloads((resp, code) =>{
                if(code == 200){
                    deferred_downloads_enabled = true;
                }
            });
        }

        //fetch list of craft tagged to download. if optional second argument is given as true, show_download_list will be called once list is fetched
        public void fetch_download_queue(){
            fetch_download_queue(false);
        }

        public void fetch_download_queue(bool show_list){
            if(KerbalXAPI.logged_in()){
                
                KerbalXAPI.fetch_download_queue((craft_data) =>{
                    download_queue.Clear();
                    download_queue = craft_data;
                    if(show_list){
                        show_download_list();
                    }
                });
                
            }else{
                show_download_list();
            }
        }

        //push the download queue list to the download interface and ensure it's visible
        public void show_download_list(){
            download_gui().set_craft_list(update_craft_data(download_queue), "Download Queue");
        }

        //Fetch list of craft the user has downloaded in the past, add in path info and push it to the download interface
        public void fetch_past_downloads(){
            KerbalXAPI.fetch_past_downloads((craft_data) =>{
                download_gui().set_craft_list(update_craft_data(craft_data), "Past Downloads");
            });
        }

        //Fetch list of craft uploaded by the user, add in path info and push it to the download interface
        public void fetch_users_craft(){
            KerbalXAPI.fetch_users_craft((craft_data) =>{
                download_gui().set_craft_list(update_craft_data(craft_data), "Your Craft");
            });
        }


        //takes craft data hot off the site and adds in path information based on the craft's type and name
        private Dictionary<int, Dictionary<string, string>> update_craft_data(Dictionary<int, Dictionary<string, string>> craft_data){
            int[] craft_ids = craft_data.Keys.ToArray();
            foreach(int id in craft_ids){
                try{
                    string type_dir = craft_data[id]["type"] == "Subassembly" ? "Subassemblies" : Paths.joined("Ships", craft_data[id]["type"]);
                    craft_data[id].Add("dir",       Paths.joined(KSPUtil.ApplicationRootPath, "saves", HighLogic.SaveFolder, type_dir));
                    craft_data[id].Add("path",      Paths.joined(KSPUtil.ApplicationRootPath, "saves", HighLogic.SaveFolder, type_dir, craft_data[id]["name"] + ".craft"));
                    craft_data[id].Add("short_path",Paths.joined(HighLogic.SaveFolder, type_dir, craft_data[id]["name"] + ".craft"));
                    craft_data[id].Add("status", "");
                }
                catch{
                    //no need to handle anything, catch gets triggered if dir, path etc data has already been added to craft_data so we can just skip if it's already there.    
                }
                if(File.Exists(craft_data[id]["path"])){
                    craft_data[id]["status"] = "In folder";
                }
            }
            return craft_data;
        }


        //fetch craft of given ID from KerbalX and write it to file.  no confirmation for overwrite is given by this method.
        //this will be called from the download interface once overwrite confirmation has been given.
        public void download_craft(int id, Dictionary<string, string> craft_info){
            KerbalXAPI.download_craft(id, (craft_file_string, code) =>{
                if(code == 200){
                    write_file(id, craft_info, craft_file_string);
                    if(download_gui().mode == "Download Queue"){
                        fetch_download_queue();
                    }
                }
            });
        }

        private void write_file(int craft_id, Dictionary<string, string> craft_info, string craft_file){
            Directory.CreateDirectory(craft_info["dir"]); //ensure directorys exist (usually just subassembly folder which is missing). 
            File.WriteAllText(craft_info["path"], craft_file);
            craft_info["status"] = "Downloaded";
            if(download_gui().craft_list.ContainsKey(craft_id)){
                download_gui().craft_list[craft_id]["status"] = "Downloaded";
            }
        }


        //checks to see if there are any craft in the current save, returns true if no craft are present.
        public bool save_is_empty(){
            int count = 0;
            string path;
            string[] sub_folders = new string[] { Paths.joined("Ships", "VAB"), Paths.joined("Ships", "SPH"), "Subassembly" };
            DirectoryInfo dir;
            foreach(string sub_folder in sub_folders){
                path = Paths.joined(KSPUtil.ApplicationRootPath, "saves", HighLogic.SaveFolder, sub_folder);
                if(Directory.Exists(path)){
                    dir = new DirectoryInfo(path);
                    count += dir.GetFiles("*.craft").Count();
                }
            }
            return count == 0;
        }

        //Fetches users craft list and then downloads all their craft which are from the same KSP version as the current game.
        //Warning - no overwrite confirmation on this, as this method is only be used when populating a new save.
        public void auto_load_users_craft(){
            string ksp_version = Versioning.GetVersionString();
            List<int> loaded_craft_ids = new List<int>();
            KerbalXAPI.fetch_users_craft((craft_data) =>{
                var user_craft_data = update_craft_data(craft_data);
                int[] craft_ids = user_craft_data.Keys.ToArray();
                foreach(int id in craft_ids){
                    if(user_craft_data[id]["version"] == ksp_version){
                        download_craft(id, craft_data[id]);
                        loaded_craft_ids.Add(id);
                    }
                }
                download_gui().show_bulk_download_complete_dialog(loaded_craft_ids, user_craft_data);
            });
        }



        private long millisecs(){
            return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }

        private void OnGUI(){
            if(KerbalXAPI.logged_in()){
                GUI.skin = KerbalXWindow.KXskin;
                if(query_new_save && save_is_empty()){
                    query_new_save = false;
                    KerbalX.download_gui.show_new_save_dialog();
                }
                if(millisecs() - start_time > startup_delay){

                    if(download_queue.Count > 0){               //show/hide the notification bar, gracefully, based on No. craft avail to download
                        if(container.y < -10){container.y+=5;}
                    } else{
                        if(container.y > -90){container.y-=3;}
                    }

                    style_override = new GUIStyle(GUI.skin.window);
                    begin_group(container, () =>{
                        v_section(500, w =>{
                            GUILayout.Space(20);
                            GUILayout.Label("You have " + download_queue.Count + " craft to download", "h2.centered");
                            GUILayout.Label("(click to view)", "centered");
                        });
                    });
                    if(disable_link <= 0 && container.Contains(Event.current.mousePosition) && Input.GetKeyDown(KeyCode.Mouse0) && Input.GetMouseButtonDown(0)){
                        show_download_list();
                        disable_link = 5;   //disable link prevents another click being registered right away. without it a single click was registering twice.
                    }
                    if(disable_link > 0){
                        disable_link--;
                    }
                }
                GUI.skin = null;
            }
        }

    }


    public class KerbalXDownloadInterface : KerbalXWindow
    {

        public Dictionary<int, Dictionary<string, string>> craft_list = new Dictionary<int, Dictionary<string, string>>();

        public string mode = "";
        private int[] craft_ids;
        private Vector2 scroll_pos;
        private float scroll_height;
        private float win_top = 200f;

        private bool only_version_compatible = true;
        private string ksp_ver;
       
        private long timer = 0;
        private bool readjust_height = false;

        private void Start(){
            KerbalX.download_gui = this;
            window_title = "KerbalX::Downloader";
            window_pos = new Rect(Screen.width / 2 - 500 / 2, win_top, 500, 5);
            require_login = true;
            visible = false;
            ksp_ver = Versioning.GetVersionString();
        }

        //Called after a succsessful login, if the login dialog was opened from this window.
        protected override void on_login(){
            base.on_login();        //inherits call to hide login window
            KerbalXDownloadController.instance.check_deferred_downloads_setting();
            KerbalXDownloadController.instance.fetch_download_queue(true);
        }

        //Takes craft data from the DownloadController and a mode name to display. Ensures the gui is visible and adjusts height of window and scroller according to craft count
        public void set_craft_list(Dictionary<int, Dictionary<string, string>> craft_data, string mode_name){
            this.show();
            mode = mode_name;
            craft_list = new Dictionary<int, Dictionary<string, string>>();
            foreach(KeyValuePair<int, Dictionary<string, string>> data in craft_data){
                craft_list.Add(data.Key, new Dictionary<string, string>(data.Value));
            }
            craft_ids = craft_list.Keys.ToArray();
            if(mode == "Download Queue"){
                bool all_current = true;
                foreach(int id in craft_ids){
                    if(craft_list[id]["version"] != ksp_ver){
                        all_current = false;
                    }
                }
                only_version_compatible = all_current;
            }else{
                only_version_compatible = true;
            }
            adjust_scroll_height();
            scroll_pos.y = 0;
        }


        //set scroll height according to number of craft and window position relative to bottom of the screen.
        private void adjust_scroll_height(){
            scroll_height = display_count() * 67;
            float max_height = Screen.height - (window_pos.y + 200);
            if(scroll_height > max_height){
                scroll_height = max_height;
            }
            if(scroll_height < 67){
                scroll_height = 67f;
            }
            autoheight();
        }

        //returns count of craft taking version filter into account
        private int display_count(){
            int count = 0;
            foreach(int id in craft_ids){
                if(ksp_ver == craft_list[id]["version"] || !only_version_compatible){
                    count++;
                }
            }
            return count;
        }


        protected override void WindowContent(int win_id){
            if(win_top != window_pos.y){
                timer = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                win_top = window_pos.y;
                readjust_height = true;
            }
            if(readjust_height && (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - timer > 500){
                readjust_height = false;
                adjust_scroll_height();
            }

            section(w =>{
                if(GUILayout.Button("Download Queue", "button.bold", width(w * 0.33f))){
                    KerbalXDownloadController.instance.fetch_download_queue(true);
                }
                if(GUILayout.Button("Past Downloads", "button.bold", width(w * 0.33f))){
                    KerbalXDownloadController.instance.fetch_past_downloads();
                }
                if(GUILayout.Button("Your Craft", "button.bold", width(w * 0.33f))){
                    KerbalXDownloadController.instance.fetch_users_craft();
                }
            });

            GUILayout.Label(mode, "h2");

            only_version_compatible = GUILayout.Toggle(only_version_compatible, " Only show KSP " + ksp_ver + " craft");
            if(GUI.changed){
                adjust_scroll_height(); //adjust the scroll height when the above toggle is changed.
            }

            scroll_pos = scroll(scroll_pos, 500f, scroll_height, sw =>{
                if(craft_list.Count > 0){
                    foreach(int id in craft_ids){
                        
                        GUI.enabled = !(mode == "Download Queue" && (craft_list[id]["status"] == "removed" || craft_list[id]["status"] == "Downloaded"));
                        if(ksp_ver == craft_list[id]["version"] || !only_version_compatible){
                            style_override = GUI.skin.GetStyle("background.dark.margin");
                            section(w =>{
                                v_section(w * 0.7f, w2 =>{
                                    GUILayout.Label(craft_list[id]["name"], "h3");
                                    GUILayout.Label(craft_list[id]["type"] + " | made in KSP:" + craft_list[id]["version"]);
                                });
                                v_section(w * 0.3f, w2 =>{
                                    section(w2, w3 => {
                                        if(GUILayout.Button("download", "button.bold")){
                                            download_craft(id);
                                        }
                                        if(mode == "Download Queue"){
                                            if(GUILayout.Button("X", "remove_link", width(10f), height(25f))){
                                                KerbalXAPI.remove_from_queue(id);
                                                KerbalXDownloadController.instance.fetch_download_queue();
                                                craft_list[id]["status"] = "removed";
                                            }
                                        }
                                    });
                                    GUILayout.Label(craft_list[id]["status"], "align.right");
                                });
                            });
                        }
                        GUI.enabled = true;
                    }
                } else{
                    string msg = "There aren't any craft in your " + mode;
                    if(mode == "Your Craft"){
                        msg = "You have not uploaded any craft yet";
                    }
                    GUILayout.Label(msg, "h3");

                    if(mode == "Your Craft"){
                        GUILayout.Label("Craft which you've uploaded to KerbalX will appear here so you can download them.");
                    }else if(mode == "Past Downloads"){
                        GUILayout.Label("Craft which you've previously downloaded will be listed here so you can re-download them");
                        scroll_height = 80;
                    }else if(mode == "Download Queue" && !KerbalXDownloadController.instance.deferred_downloads_enabled){
                        scroll_height = 120;
                        style_override = GUI.skin.GetStyle("background.dark.margin");
                        v_section(ow => {
                            GUILayout.Label("To use the download queue you need to enable \"Deferred Downloads\" in your settings on KerbalX");
                            section(w => {
                                if(GUILayout.Button("view your settings on KerbalX.com", "hyperlink")){
                                    Application.OpenURL(KerbalXAPI.url_to("/settings?tab=kx_mod"));
                                }
                                if(GUILayout.Button("Or Click to enable it")){
                                    KerbalXDownloadController.instance.enable_deferred_downloads();
                                }
                            });
                        });
                    }else if(mode == "Download Queue"){
                        scroll_height = 80;
                        section(w => {
                            GUILayout.Label("To get craft to appear here, browse KerbalX.com on any device and click download on craft you want and they will appear here.", width(w - 80f));
                            if(GUILayout.Button("refresh list", width(80f))){
                                KerbalXDownloadController.instance.fetch_download_queue(true);
                            }
                        });
                    }
                }
            });
            section(w =>{
                GUILayout.FlexibleSpace();
                if(GUILayout.Button("Close", "button.bold", width(50f), height(30f))){
                    hide();
                }
            
            });

        }


        private void download_craft(int id){
            download_craft(id, false);
        }
        private void download_craft(int id, bool force_download){

            if(force_download){
                KerbalXDownloadController.instance.download_craft(id, craft_list[id]);
            }else{
                if(File.Exists(craft_list[id]["path"])){
                    KerbalXDialog dialog = show_dialog(d =>{
                        style_override = GUI.skin.GetStyle("background.dark");
                        v_section(w =>{
                            GUILayout.Label("A craft with this name already exists", "h2");
                            GUILayout.Label("You have a craft at: " + craft_list[id]["short_path"], "small");
                        });
                        section(w =>{
                            GUILayout.FlexibleSpace();
                            if(GUILayout.Button("Cancel", "button.bold", width(w * 0.2f))){
                                close_dialog();
                            }
                            if(GUILayout.Button("Replace", "button.bold", width(w * 0.2f))){
                                KerbalXDownloadController.instance.download_craft(id, craft_list[id]);
                                close_dialog();
                            }
                        });
                    }); 
                    dialog.window_pos.width = 400f;
                    dialog.window_pos.y = Screen.height - Input.mousePosition.y - 42; //42 just seemed like the answer, for some reason.
                    dialog.window_pos.x = (window_pos.x + window_pos.width / 2) - dialog.window_pos.width / 2;
                    dialog.window_title = "Replace Existing?";
                    
                } else{
                    KerbalXDownloadController.instance.download_craft(id, craft_list[id]);
                }
            }
        }

        public void show_new_save_dialog(){
            KerbalXDialog dialog = show_dialog(d => {
                GUILayout.Label("This looks like a new save", "h2");
                GUILayout.Label("Would you like to fetch your craft from KerbalX for version " + ksp_ver, "h3");
                section(w => {
                    if(GUILayout.Button("No", "button.bold")){
                        close_dialog();
                    }
                    if(GUILayout.Button("Let me pick", "button.bold")){
                        close_dialog();
                        KerbalXDownloadController.instance.fetch_users_craft();
                    }
                    if(GUILayout.Button("Yep, fetch all my " + ksp_ver + " craft", "button.bold")){
                        close_dialog();
                        KerbalXDownloadController.instance.auto_load_users_craft();
                    }
                });
            });
            dialog.window_title = "Populate new save?";
        }

        private Vector2 dl_scroll_pos;
        private float dl_scroll_height = 372f;
        public void show_bulk_download_complete_dialog(List<int> loaded_ids, Dictionary<int, Dictionary<string, string>> craft_data){
            KerbalXDialog dialog = show_dialog(d => {
                d.footer = true;
                GUILayout.Label("You craft have been downloaded", "h2");
                GUILayout.Label("these ones to be precise:", "small");
                dl_scroll_height = 62 * loaded_ids.Count;
                if(dl_scroll_height > 372f){
                    dl_scroll_height = 372f;
                }
                dl_scroll_pos = scroll(dl_scroll_pos, 500f, dl_scroll_height, sw => {
                    foreach(int id in loaded_ids){
                        style_override = GUI.skin.GetStyle("background.dark.margin");
                        v_section(w => {
                            GUILayout.Label(craft_data[id]["name"], "h3");
                            GUILayout.Label("saved to: " + craft_data[id]["short_path"]);
                        });
                    }
                });
                section(w => {
                    GUILayout.FlexibleSpace();
                    if(GUILayout.Button("Done", "button.bold", width((150f)))){
                        close_dialog();
                    }
                });
            });
            dialog.window_title = "Download Summary";
        }

    }
}

