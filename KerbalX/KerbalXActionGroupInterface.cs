using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using UnityEngine;

namespace KerbalX
{

	public class KerbalXActionGroupInterface : KerbalXWindow
	{
		private List<string> keys;
//		private bool auto_close_action_group_panel = false;

		private void Start()
		{
			KerbalX.action_group_gui = this;
			window_title = "KerbalX::ActionGroups";			
			window_pos = new Rect(430,50,500,200); //set to be right next to the action group panel edge when it's open.
			prevent_editor_click_through = true;
			this.show (); //perform on_show actions when started
		}

		protected override void on_show (){
			keys = new List<string>(KerbalX.upload_gui.action_groups.Keys);	//Get the names of action groups - used in itterating over action groups
//			if(EditorLogic.fetch.actionPanelBtn.interactable){		
//				EditorLogic.fetch.actionPanelBtn.onClick.Invoke ();	//auto open the action group panel if it's not already open - TODO make this a setting.
//				auto_close_action_group_panel = true;				//set to true to close it again when closing this window.
//			}
		}

		protected override void on_hide(){
//			KerbalX.upload_gui.action_groups = action_groups;	//Put the altered action group info back onto the main upload gui
//			if(auto_close_action_group_panel){
//				EditorLogic.fetch.partPanelBtn.onClick.Invoke ();
//			}
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
				});
			});

			section (w => {
				GUILayout.FlexibleSpace ();
				if(GUILayout.Button ("OK", width (80f), height (30f))){
					this.hide ();
				}
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

