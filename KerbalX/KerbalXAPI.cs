using System;
using System.Linq;
using System.Text;

using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

using UnityEngine;
using UnityEngine.Networking;

using SimpleJSON;


namespace KerbalX
{
	//define delegates to be used to pass lambda statement as callbacks in request methods.
	public delegate void RequestCallback(string data, int status_code);
	public delegate void ActionCallback();

	public class KerbalXAPI
	{
		public static string token = null; 

		//make request to site to authenticate token 
		public static void authenticate_token(string new_token){
			KerbalX.notify("Authenticating with KerbalX.com...");
			NameValueCollection data = new NameValueCollection ();
			data.Add ("token", new_token);
			HTTP.post (KerbalX.url_to ("api/authenticate"), data).send ((resp, code) => {
				if(code==200){
					token = new_token;
					KerbalX.show_login = false;
					KerbalX.notify("You are logged in.");
				}else{
					KerbalX.show_login = true;
					KerbalX.notify("Enter your KerbalX username and password");
				}
				KerbalX.login_gui.autoheight ();
			});
		}

		public static void login(string username, string password){
			//make request to site to authenticate username and password and get token back
			KerbalX.notify("loging into KerbalX.com...");
			NameValueCollection data = new NameValueCollection ();
			data.Add ("username", username);
			data.Add ("password", password);
			HTTP.post (KerbalX.url_to ("api/login"), data).send ((resp, code) => {
				if(code==200){
					var resp_data = JSON.Parse (resp);
					token = resp_data["token"];
					KerbalX.save_token (resp_data["token"]);
					KerbalX.show_login = false;
					KerbalX.notify("login succsessful! KerbalX.key saved in KSP root");
				}else{
					KerbalX.show_login = true;
					KerbalX.alert = "login failed, check yo shit.";
					KerbalX.notice = "";
				}
				KerbalXLoginWindow.enable_login = true;
				KerbalX.login_gui.autoheight ();
			});
		}

		public static void fetch_existing_craft(ActionCallback callback){
			//NameValueCollection data = new NameValueCollection (){{"lookup", "existing_craft"}};
			HTTP.get (KerbalX.url_to ("api/existing_craft.json")).set_header ("token", KerbalXAPI.token).send ((resp, code) => {
				if(code==200){
					JSONNode craft_data = JSON.Parse (resp);
					Dictionary<int, Dictionary<string, string>> craft_list = new Dictionary<int, Dictionary<string, string>>();
					for(int i=0; i<craft_data.Count; i++ ){
						var c = craft_data[i];
						int id = int.Parse((string)c["id"]);
						Dictionary<string,string> cd = new Dictionary<string,string>(){
							{"id", c["id"]},
							{"name", c["name"]},
							{"version", c["version"]},
							{"url", c["url"]}
						};
						craft_list.Add (id, cd);
					}
					KerbalX.existing_craft = craft_list;
					callback();
				}else{
					KerbalX.alert = "An error occurred while contacting KerbalX, try again later";
				}
			});
		}
	}


	public class HTTP
	{
		private UnityWebRequest request; 

		public static HTTP get(string url){
			HTTP http = new HTTP ();
			http.request = UnityWebRequest.Get (url);
			return http;
		}

		public static HTTP post(string url, NameValueCollection data){
			WWWForm form_data = new WWWForm();
			foreach(string key in data){
				form_data.AddField (key, data[key]);
			}
			HTTP http = new HTTP ();
			http.request = UnityWebRequest.Post (url, form_data);
			return http;
		}

		public HTTP set_header(string key, string value){
			request.SetRequestHeader (key, value);
			return this;
		}

		public void send(RequestCallback callback){
			RequestHandler.instance.send_request (request, callback);
		}
	}


	//[KSPAddon(KSPAddon.Startup.EveryScene, false)]
	public class RequestHandler : MonoBehaviour
	{
		public static RequestHandler instance = null;

		public void send_request(UnityWebRequest request, RequestCallback callback){
			StartCoroutine (transmit (request, callback));
		}

		public IEnumerator transmit(UnityWebRequest request, RequestCallback callback){
			KerbalX.log("sending request to: " + request.url);

			yield return request.Send ();

			if (request.isError){
				Debug.Log ("shit went wrong!!");
				Dictionary<string, string> t = request.GetResponseHeaders ();
				foreach(KeyValuePair<string, string> r in t){
					Debug.Log ("header: " + r.Key);
				}
			}else{
				Debug.Log ("request successfull"); 
				callback (request.downloadHandler.text, (int)request.responseCode);
			}
		}
	}

}

