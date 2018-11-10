using System;
using System.IO;
using System.Collections.Generic;

namespace KerbalX
{
    internal class KerbalXSettings
    {
        protected string plugin_data_dir = Paths.joined(KSPUtil.ApplicationRootPath, "GameData", "KerbalX", "PluginData");
        protected string settings_path = Paths.joined(KSPUtil.ApplicationRootPath, "GameData", "KerbalX", "PluginData", "settings.cfg");
        protected Dictionary<string, string> settings = new Dictionary<string, string>();

        internal KerbalXSettings(){
            ensure_plugin_data_dir_exists();

            settings.Add("screenshot_dir", "<ksp_install>/Screenshots");

            if(File.Exists(settings_path)){
                ConfigNode settings_raw = ConfigNode.Load(settings_path);
                ConfigNode settings_data = settings_raw.GetNode("SETTINGS");
                List<string> keys = new List<string>(settings.Keys);
                foreach(string key in keys){
                    if(!String.IsNullOrEmpty(settings_data.GetValue(key))){
                        settings[key] = settings_data.GetValue(key);
                    }
                }
                save();
            } else{
                save();
            }

            KerbalX.screenshot_dir = get("screenshot_dir");
            KerbalX.screenshot_dir = KerbalX.screenshot_dir.Replace("<ksp_install>", KSPUtil.ApplicationRootPath);
        }

        public void ensure_plugin_data_dir_exists(){
            if(!Directory.Exists(plugin_data_dir)){
                Directory.CreateDirectory(plugin_data_dir);
            }
        }

        public string get(string key){
            if(settings.ContainsKey(key)){
                return settings[key];
            } else{
                return "";
            }
        }

        protected void save(){
            ConfigNode settings_data = new ConfigNode();
            ConfigNode settings_node = new ConfigNode();
            settings_data.AddNode("SETTINGS", settings_node);

            List<string> keys = new List<string>(settings.Keys);
            foreach(string key in keys){
                settings_node.AddValue(key, settings[key]);
            }
            ensure_plugin_data_dir_exists();
            settings_data.Save(settings_path);
            File.AppendAllText(settings_path, 
                "//pic_dir specifies the folder that the KerbalX mod will load and save pictures to.\n" +
                "//Saving pictures only applies when using the mod interface to grab a picture of a craft in the editors,\n" +
                "//it DOES NOT effect where KSP will save pictures when pressing F1.\n" +
                "//By default this is set to the Screenshots folder inside your KSP install, but you can change this location to where ever you like.\n" +
                "//To set the path to somewhere inside your KSP install use <ksp_install> as the root of the path.\n" +
                "//To set the path to a location outside your KSP install use the full path. You can use either windows (\\) or linux (/) file separators (linux style is preferable)."
            );
        }
    }
}

