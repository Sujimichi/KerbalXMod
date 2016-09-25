using System;
using System.Collections.Generic;
using UnityEngine;

namespace KerbalX
{
	//defines a set of GUIStyle elements for various label and button styles
	//it's like we need a sorta sheet of styles, maybe one that can cascade, a cascading style sheet if you will.
	public class StyleSheet : MonoBehaviour
	{

		public static Dictionary<string, Texture> assets = new Dictionary<string, Texture> ();

		public static void prepare(){

			if(KerbalXWindow.KXskin == null){

				//Texture2D kx_logo_small = new Texture2D(166,30, TextureFormat.ARGB32, false);
				Texture2D kx_logo_small = GameDatabase.Instance.GetTexture (Paths.joined ("KerbalX", "Assets", "KXlogo_small"), false);
				Texture2D kx_logo_large = GameDatabase.Instance.GetTexture (Paths.joined ("KerbalX", "Assets", "KXlogo"), false); //664x120
				assets.Add ("logo_small", kx_logo_small);
				assets.Add ("logo_large", kx_logo_large);



				GUIStyle h1 = new GUIStyle (GUI.skin.label);
				h1.fontStyle = FontStyle.Bold;
				h1.fontSize = 30;
				h1.name = "h1";
				
				GUIStyle h2 = new GUIStyle (h1);
				h2.fontSize = 20;
				h2.name = "h2";

				GUIStyle h3 = new GUIStyle (h1);
				h3.fontSize = 15;
				h3.name = "h3";

				GUIStyle hyperlink = new GUIStyle (GUI.skin.label);
				hyperlink.normal.textColor = new Color (0.4f,0.5f,0.9f,1); //color also known as KerbalX Blue - #6E91EB
				hyperlink.hover.textColor = Color.red; //can't seem to make this work
				hyperlink.name = "hyperlink";

				GUIStyle hyperlink_h2 = new GUIStyle (hyperlink);
				hyperlink_h2.fontSize = 20;
				hyperlink_h2.fontStyle = FontStyle.Bold;
				hyperlink_h2.alignment = TextAnchor.UpperCenter;
				hyperlink_h2.name = "hyperlink.h2";

				GUIStyle hyperlink_h3 = new GUIStyle (hyperlink);
				hyperlink_h3.fontSize = 15;
				hyperlink_h3.name = "hyperlink.h3";


				GUIStyle alert = new GUIStyle (GUI.skin.label);
				alert.normal.textColor = Color.red;
				alert.name = "alert";
				
				GUIStyle h2_alert = new GUIStyle (alert);
				h2_alert.name = "h2.alert";
				h2_alert.fontSize = 20;
				
				GUIStyle small = new GUIStyle (GUI.skin.label);
				small.name = "small";
				small.fontSize = 12;
				
				GUIStyle centered = new GUIStyle (GUI.skin.label);
				centered.name = "centered";
				centered.alignment = TextAnchor.UpperCenter;
				

				GUIStyle no_style = new GUIStyle (GUI.skin.label);
				no_style.name = "no_style";
				no_style.margin = new RectOffset (0, 0, 0, 0);
				no_style.padding = new RectOffset (0, 0, 0, 0);
				
				GUIStyle pic_link = new GUIStyle (GUI.skin.label);
				pic_link.name = "pic.link";
				pic_link.padding = new RectOffset (5, 5, 5, 5);
				pic_link.margin = new RectOffset (0, 0, 0, 0);
				
				GUIStyle pic_hover = new GUIStyle (pic_link);
				pic_hover.name = "pic.hover";
				pic_hover.normal.textColor = Color.black;

				Texture2D pic_highlight = new Texture2D(1, 1,   TextureFormat.RGBA32, false);
				pic_highlight.SetPixel(0, 0, new Color (0.4f,0.5f,0.9f,1));
				pic_highlight.Apply ();
				pic_hover.normal.background = pic_highlight;
				
				GUIStyle upload_button = new GUIStyle (GUI.skin.button);
				upload_button.name = "button.upload";
				upload_button.fontSize = 20;
				upload_button.fontStyle = FontStyle.Bold;
				upload_button.padding = new RectOffset (3, 3, 10, 10);
				upload_button.margin = new RectOffset (20, 20, 20, 5);
				
				GUIStyle screenshot_button = new GUIStyle (GUI.skin.button);
				screenshot_button.name = "button.screenshot";
				screenshot_button.fontSize = 15;
				screenshot_button.padding = new RectOffset (3, 3, 10, 10);
				
				GUIStyle wrapped_button = new GUIStyle (GUI.skin.button);
				wrapped_button.name = "button.wrapped";
				wrapped_button.wordWrap = true;
				

				Texture2D dark_background = new Texture2D(1, 1,   TextureFormat.RGBA32, false);
				dark_background.SetPixel(0, 0, new Color (0.12f,0.12f,0.12f,0.7f));
				dark_background.Apply ();
				GUIStyle dark_back = new GUIStyle ();
				dark_back.name = "background.dark";
				dark_back.normal.background = dark_background;

				GUIStyle dark_back_offset = new GUIStyle (dark_back);
				dark_back_offset.name = "background.dark.margin";
				dark_back_offset.margin = new RectOffset (0, 0, 5, 0);



				KerbalXWindow.KXskin = Instantiate (GUI.skin);
				KerbalXWindow.KXskin.customStyles = new GUIStyle[]{ 
					h1, h2, h3, hyperlink, hyperlink_h2, hyperlink_h3, alert, h2_alert, small, centered, 
					pic_link, pic_hover, dark_back, dark_back_offset, no_style,
					upload_button, screenshot_button, wrapped_button
				};
			}

		}


	}

	//	Experimental Idea - toying with idea of a cascading set of styles that can be passed onto one another.
	//	GUILayout.Label ("this label is bold and large", css.header ());
	//	GUILayout.Label ("this label is red and normal size", css.alert ());
	//	GUILayout.Label ("this label is bold, large AND red", css.header (css.alert()));
	public class CascadingStyleSheet : MonoBehaviour
	{
		public GUIStyle base_style(GUIStyle foundation, params GUIStyle[] styles){
			GUIStyle b = new GUIStyle (foundation);
			if(styles.Length != 0){
				b = styles [0];
			}
			return b;
		}

		public GUIStyle header(params GUIStyle[] styles){
			GUIStyle h = base_style(GUI.skin.label, styles);
			h.fontSize = 15;
			h.fontStyle = FontStyle.Bold;
			return h;
		}

		public GUIStyle alert(params GUIStyle[] styles){
			GUIStyle l = base_style(GUI.skin.label, styles);
			l.normal.textColor = Color.red;
			return l;
		}
		
	}
}

