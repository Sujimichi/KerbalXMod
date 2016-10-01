using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using UnityEngine;

namespace KerbalX
{

	public class KerbalXActionGroupInterface : KerbalXWindow
	{
		private List<string> keys;

		private void Start()
		{
			KerbalX.action_group_gui = this;
			window_title = "KerbalX::ActionGroups";			
			window_pos = new Rect(430,50,500,200); //set to be right next to the action group panel edge when it's open.
			prevent_editor_click_through = true;
			keys = new List<string>(KerbalX.upload_gui.action_groups.Keys);	//Get the names of action groups - used in itterating over action groups
		}

		public static void close(){
			if(KerbalX.action_group_gui){
				GameObject.Destroy (KerbalX.action_group_gui);
			}
		}

		protected override void WindowContent(int win_id){

			GUILayout.Button ("Add descriptions of your action groups", "h2");
			section (500f, w => {
				//Left Column - Numbered Action groups 0-9, well, 1-9,0
				v_section (w*0.5f, w2 => {
					foreach (string key in keys) {
						if(Regex.IsMatch (key, @"^\d+$")){ //If the action group name is a string containing just a number then draw it in this column
							action_group_field ("Group " + key, key, w2);
						}
					}
				});
				//Right Column - Named Action groups 
				v_section (w*0.5f, w2 => {
					foreach (string key in keys) {
						if(!Regex.IsMatch (key, @"^\d+$")){ //if the action group name does not just contain a number then draw it in this column
							action_group_field (key, key, w2);
						}
					}
					GUILayout.Space (20f);
					section (w3 => {
						GUILayout.FlexibleSpace ();
						if(GUILayout.Button ("Done", width (w2*0.6f), height (40f))){
							this.hide ();
						}
						GUILayout.FlexibleSpace ();
					});
				});
			});

		}

		//adds the label and field for an action group - just keepin' it DRY up in here. 
		private void action_group_field(string name, string key, float container_width){
			section (container_width, w => {
				GUILayout.Label (name, width (w*0.2f));
				KerbalX.upload_gui.action_groups[key] = GUILayout.TextArea(KerbalX.upload_gui.action_groups[key], width (w*0.7f));
			});
		}
	}	 

}

