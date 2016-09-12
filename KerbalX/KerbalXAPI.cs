using System;
using System.Linq;
using System.Text;

using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

using System.Net;
//using System.Net.Security;
//using System.Security.Cryptography.X509Certificates;

using System.Threading;

using SimpleJSON;


namespace KerbalX
{
	public class KerbalXAPI
	{
		public static string token = null; 
		public static string json = "";

		//make request to site to authenticate token 
		public static void authenticate_token(string new_token){
			KerbalX.notify("Authenticating with KerbalX.com...");
			NameValueCollection data = new NameValueCollection ();
			data.Add ("token", new_token);
			KerbalXAPI.post (KerbalX.url_to ("api/authenticate"), data, (resp, code) => {
				if(code==200){
					token = new_token;
					KerbalX.show_login = false;
					KerbalX.notify("You are logged in.");
				}else{
					KerbalX.show_login = true;
					KerbalX.notify("Enter your KerbalX username and password");
				}
			});
		}

		public static void login(string username, string password){
			//make request to site to authenticate username and password and get token back
			KerbalX.notify("loging into KerbalX.com...");
			NameValueCollection data = new NameValueCollection ();
			data.Add ("username", username);
			data.Add ("password", password);
			KerbalXAPI.post (KerbalX.url_to ("api/login"), data, (resp, code) => {
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
			});
		}

		public static void fetch_existing_craft(){
			NameValueCollection data = new NameValueCollection (){{"lookup", "existing_craft"}};
			KerbalXAPI.get (KerbalX.url_to ("api/craft.json"), data, (resp, code) => {
				JSONNode craft_data = JSON.Parse (resp);
				//List<Dictionary<string, object>> craft_list = new List<Dictionary<string, object>>();

				Dictionary<int, Dictionary<string, object>> craft_list = new Dictionary<int, Dictionary<string, object>>();
				KerbalX.existing_craft_by_name.Clear ();

				for(int i=0; i<craft_data.Count; i++ ){
					var c = craft_data[i];
					Dictionary<string,object> cd = new Dictionary<string,object>(){
						{"id", c["id"]},
						{"name", c["name"]},
						{"version", c["version"]}
					};
					int id = int.Parse((string)c["id"]);
					craft_list.Add (id, cd);
					string name = (string)c["name"];
					KerbalX.existing_craft_by_name.Add (name.Trim ().ToLower (), id.ToString ());
				}

				KerbalX.existing_craft = craft_list;
			});
		}

		//define delegate to be used to pass lambda statement as a callback to get, post and request methods.
		public delegate void RequestCallback(string data, int status_code);

		//Perform simple GET request 
		// Usage:
		//	KerbalXAPI.get ("http://some_website.com/path/to/stuff", (resp, code) => {
		//		//actions to perform after request completes. code provides the status code int and resp provides the returned string
		//	});
		public static void get(string url, RequestCallback callback){
			request ("GET", url, new NameValueCollection(), callback);
		}	

		//Perform GET request with query 
		// Usage:
		// NameValueCollection query = new NameValueCollection ();
		// query.Add ("username", "foobar");
		//	KerbalXAPI.get ("http://some_website.com/path/to/stuff", query, (resp, code) => {
		//		//actions to perform after request completes. code provides the status code int and resp provides the returned string
		//	});	
		public static void get(string url, NameValueCollection query, RequestCallback callback){
			request ("GET", url, query, callback);
		}

		//Perform POST request
		// Usage:
		// NameValueCollection query = new NameValueCollection ();
		// query.Add ("username", "foobar");
		//	KerbalXAPI.post ("http://some_website.com/path/to/stuff", query, (resp, code) => {
		//		//actions to perform after request completes. code provides the status code int and resp provides the returned string
		//	});	
		public static void post(string url, NameValueCollection data, RequestCallback callback){
			request ("POST", url, data, callback);
		}

		// Performs HTTP GET and POST requests - takes a method ('GET' or 'POST'), a url, query args and a callback delegate
		// The request is performed in a thread to facilitate asynchronous handling
		// Usage:
		// NameValueCollection query = new NameValueCollection ();
		// query.Add ("username", "foobar");
		//	KerbalXAPI.request ("GET", "http://some_website.com/path/to/stuff", query, (resp, code) => {
		//		//actions to perform after request completes. code provides the status code int and resp provides the returned string
		//	});	
		// OR
		//	KerbalXAPI.request ("POST", "http://some_website.com/path/to/stuff", query, (resp, code) => {
		//		//actions to perform after request completes. code provides the status code int and resp provides the returned string
		//	});	
		public static void request(string method, string url, NameValueCollection query, RequestCallback callback)
		{
			string response_data = null;
			int status_code = 500;

			var thread = new Thread (() => {
				try{
					KerbalX.log("sending request to: " + url);
					//ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
					var client = new WebClient();
					client.Headers.Add("token", token);
					//client.Headers.Add(HttpRequestHeader.UserAgent, "User-Agent: Mozilla/5.0 (Windows NT 6.1; WOW64; rv:8.0) Gecko/20100101 Firefox/8.0");
					if(method == "GET"){
						client.QueryString = query;	
						response_data = client.DownloadString (url);
					}else if(method == "POST"){
						response_data = Encoding.Default.GetString(client.UploadValues (url, "POST", query));
					}
					status_code = 200;
				}
				catch(WebException e){
					HttpWebResponse resp = (System.Net.HttpWebResponse)e.Response;
					KerbalX.log ("request failed with " + resp.StatusCode + "-" + (int)resp.StatusCode);
					status_code = (int)resp.StatusCode;
				}
				catch (Exception e){
					KerbalX.log ("unhandled exception in request: ");			
					KerbalX.log (e.Message);
					status_code = 500;
				}

				callback(response_data, status_code); //call the callback method and pass in the response and status code.
			});
			thread.Start ();
		}
	}

}

