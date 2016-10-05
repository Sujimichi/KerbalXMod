using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using UnityEngine;
using KSP;
using KSP.UI;


namespace KerbalX
{
	
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class JumpStart : MonoBehaviour
    {
        public bool autostart = true;
        public string save_name = "t1";
        public string mode = "spacecenter";
//        public string mode = "editor";
        public string craft_name = "testy";

        public void Start(){

            if(autostart){
                HighLogic.SaveFolder = save_name;
                DebugToolbar.toolbarShown = true;

                if(mode == "editor"){
                    var editor = EditorFacility.VAB;
                    GamePersistence.LoadGame("persistent", HighLogic.SaveFolder, true, false);
                    if(craft_name != null || craft_name != ""){					
                        string path = Paths.joined(KSPUtil.ApplicationRootPath, "saves", save_name, "Ships", "VAB", craft_name + ".craft");
                        EditorDriver.StartAndLoadVessel(path, editor);
                    } else{
                        EditorDriver.StartEditor(editor);
                    }
                } else if(mode == "spacecenter"){
                    HighLogic.LoadScene(GameScenes.SPACECENTER);
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
                KerbalX.console.window_pos = new Rect(Screen.width - 400, Screen.height / 2, 310, 5);
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
            enable_request_handler();
            prevent_click_through = true;
            visible = false;
        }

        private Dictionary<string, Dictionary<string, int>> part_map = new Dictionary<string, Dictionary<string, int>>();
        private Dictionary<string, AvailablePart> part_lookup = new Dictionary<string, AvailablePart>();

        private void map_parts(){
            foreach(AvailablePart part in PartLoader.LoadedPartsList){
                part_lookup.Add(part.name, part);
                Dictionary<string, int> tags = new Dictionary<string, int>();
                foreach(string tag in Regex.Split(part.title, @"/\W/")){
                    if(!string.IsNullOrEmpty(tag)){
                        tags.Add(tag.ToLower().Trim(), 12);                    
                    }
                }
                foreach(string tag in Regex.Split(part.name, @"/\W/")){
                    if(!string.IsNullOrEmpty(tag)){
                        tags.Add(tag.ToLower().Trim(), 6);                    
                    }
                }
                foreach(string tag in Regex.Split(part.description, @"/\W/")){
                    if(!string.IsNullOrEmpty(tag)){
                        tags.Add(tag.ToLower().Trim(), 2);                    
                    }
                }
                part_map.Add(part.name, tags);
            }
        }

        private List<KeyValuePair<string, int>> search(string search_str){
            search_str = search_str.ToLower().Trim();
            Dictionary<string, int> r = new Dictionary<string, int>();

            foreach(KeyValuePair<string, Dictionary<string, int>> part_tags  in part_map){
                int score = 0;
                foreach(KeyValuePair<string, int> tag in part_tags.Value){
                    if(tag.Key == search_str){ 
                        score += tag.Value;
                    }else if(tag.Key.Contains(search_str)){ 
                        score += tag.Value / 2;
                    }
                    foreach(string p in Regex.Split(search_str, @"/\W/")){
                        if(tag.Key == p){
                            score += tag.Value;
                        }else if(tag.Key.Contains(p)){
                            score += tag.Value / 2;
                        }
                    }
                }
                r.Add(part_tags.Key, score);
            }
            List<KeyValuePair<string, int>> sorted_list = r.ToList();
            sorted_list.Sort((pair1,pair2) => pair2.Value.CompareTo(pair1.Value));
            return sorted_list;
        }

        private string search_str = "";
        private List<KeyValuePair<string, int>> results = new List<KeyValuePair<string, int>>();

        protected override void WindowContent(int win_id){
            section(300f, e =>{
                GUILayout.Label(KerbalX.last_log());
            });



            search_str = GUILayout.TextField(search_str);
            if(GUILayout.Button("search")){
                results = search(search_str);
            }
            if(GUILayout.Button("make map")){
                map_parts();
            }
            foreach(KeyValuePair<string, int> r in results){
                if(r.Value > 0){
                    GUILayout.Label(part_lookup[r.Key].title + " - " + r.Value);
                }
            }


            if(GUILayout.Button("test")){
                Debug.Log(PartLoader.LoadedPartsList.Count);
                Debug.Log(PartLoader.LoadedPartsList.First().name);
                Debug.Log(PartLoader.LoadedPartsList.First().title);
                Debug.Log(PartLoader.LoadedPartsList.First().description);

                foreach(AvailablePart part in PartLoader.LoadedPartsList){
                    Debug.Log(part.name);
                    Debug.Log(part.title);
                    Debug.Log(part.description);
                }
            }



            if(GUILayout.Button("update existing craft")){
                KerbalXAPI.fetch_existing_craft(() =>{});
            }

            if(GUILayout.Button("toggle upload interface")){
                if(KerbalX.upload_gui){
                    GameObject.Destroy(KerbalX.upload_gui);
                } else{
                    gameObject.AddOrGetComponent<KerbalXUploadInterface>();
                }
            }

            if(GUILayout.Button("toggle download interface")){
                if(KerbalX.download_gui){
                    GameObject.Destroy(KerbalX.download_gui);
                } else{
                    gameObject.AddOrGetComponent<KerbalXDownloadInterface>();
                }
            }


            if(GUILayout.Button("show Login")){
                KerbalXLoginWindow login_window = gameObject.AddOrGetComponent<KerbalXLoginWindow>();
                login_window.after_login_action = () =>{
                    on_login();
                };
            }

            if(KerbalX.upload_gui != null){
                if(GUILayout.Button("show thing")){
                    KerbalX.upload_gui.show_upload_compelte_dialog("fooobar/moo");
                }
            }

            if(GUILayout.Button("print log to console")){
                KerbalX.show_log();
            }
        }

        protected override void on_login(){
            base.on_login();
        }
    }

}

