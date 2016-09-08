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

		public static void set_token(string new_token){
			token = new_token;
			//and write token to KerablX.key in KSP root dir
		}

		public static bool authenticate_token(string new_token){
			//make request to site to authenticate token and return true or false
			KerbalX.notify("Authenticating token with KerbalX.com...");
			if(new_token == "bob's pyjamas"){
				token = new_token;
				KerbalX.logged_in = true;
				KerbalX.notify("logged in");
				return true;
			}else{				
				token = null;
				KerbalX.logged_in = false;
				KerbalX.notify("token was invalid");
				return false;
			}
		}

		public static void login(string username, string password){
			//make request to site to authenticate username and password and get token back
			KerbalX.notify("loging into KerbalX.com...");

			NameValueCollection queries = new NameValueCollection ();
			queries.Add ("username", username);
			queries.Add ("password", password);
			KerbalXAPI.post ("http://localhost:3000/api/login", queries, (resp, code) => {
				if(code==200){
					var data = JSON.Parse (resp);
					set_token(data["token"]);
					KerbalX.logged_in = true;
					KerbalX.notify("login succsessful, yay!");
				}else{
					KerbalX.logged_in = false;
					KerbalX.notify("login failed, check yo shit.");										
				}
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
		public static void post(string url, NameValueCollection query, RequestCallback callback){
			request ("POST", url, query, callback);
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

