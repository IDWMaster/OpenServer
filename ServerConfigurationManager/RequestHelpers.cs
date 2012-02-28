/*CONCERT ORDER:
 * Retribution
Black Sun Rising
Secrets
Omega
How it ends (cellos need more of these)
You're my demon
Cosmic wager (whole class needs more of these)
Arise
 * */

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using HttpServer;
using IC80v3;
using System.Drawing;
using System.Threading;
namespace ServerConfigurationManager
{

    class RequestHelpers
    {

        string InvokeMethod(string name, object[] args)
        {
            MethodInfo handle = null;
            foreach (MethodInfo et in mtype.GetMethods())
            {
                if (et.Name == name)
                {
                    handle = et;
                }
            }
            if (handle != null)
            {
                
                return handle.Invoke(this, args) as string;
            }
            else
            {
                return "SERVER CONTROL NOT FOUND "+name;
            }
        }
        #region Login functions
        class SessionInformation:IDisposable
        {
			public bool isAdminAuthenticated = false;
			public string profileImgUrl = "backgroundImg.jpg";
			public void Dispose ()
			{
				GC.SuppressFinalize(this);
			}
			~SessionInformation() {
			
				
			}
			
        }
        Dictionary<string, SessionInformation> sessions = new Dictionary<string, SessionInformation>();
        public string backgroundURL(ClientWebRequest request) {
		SessionInformation session = sessions[sessionKey(request)];
			return session.profileImgUrl;
		}
		public string securePage(ClientWebRequest request) {
		SessionInformation session = sessions[sessionKey(request)];
			if(!session.isAdminAuthenticated) {
			ClientHttpResponse response = new ClientHttpResponse();
				response.Redirect("index.htm?sessionID="+sessionKey(request),false,request.stream);
				request.ContinueProcessing = false;
			}
		return "";
			
		}
		#region Download manager
		class Download {
		public bool paused = false;
			public string name;
			public long _size;
			public long _progress;
			Stream _source;
			Stream _dest;
			public void Pause() {
			pauseEvent.Reset();
				paused = true;
				statusUpdateEvent.Set();
			}
			public void Resume() {
			paused = false;
				pauseEvent.Set();
				statusUpdateEvent.Set();
			}
			internal static List<Download> downloads = new List<Download>();
			byte[] buffer = new byte[16384];
			ManualResetEvent pauseEvent = new ManualResetEvent(false);
			
			void write_block() {
				try {
				if(_size-_progress>buffer.Length) {
			int read = _source.Read(buffer,0,buffer.Length);
					_progress+=read;
					_dest.Write(buffer,0,read);
				}else {
				int read = _source.Read(buffer,0,(int)(_size-_progress));
					_progress+=read;
					_dest.Write(buffer,0,read);
				
				}
				statusUpdateEvent.Set();
				}catch(Exception er) {
				haserror = true;
					statusUpdateEvent.Set();
					throw er;
				}
			}
			public bool haserror = false;
			void dowrite(object sender) {
			while(true) {
					if(paused) {
				pauseEvent.WaitOne();
				}
					if(_size == _progress) {
				lock(downloads) {
					downloads.Remove(this);
						
					}
					_dest.Flush();
					_dest.Dispose();
					return;
					
				}
				write_block();
				}
			}
			public Download(string _name,Stream source, Stream destination, long size) {
			_source = source;
				name = _name;
				_dest = destination;
				_size = size;
				lock(downloads) {
				downloads.Add(this);
				}
			
			}
			public void Begin() {
			dowrite(null);
			}
			public void Abort() {
				paused = false;
				pauseEvent.Set();
				downloads.Remove(this);
				try {
				_source.Dispose();
				}catch(Exception) {
				}
				try {
				_dest.Dispose();
				}catch(Exception) {
				}
				statusUpdateEvent.Set();
				
			}
		}
		static ManualResetEvent statusUpdateEvent = new ManualResetEvent(false);
		public string downloadTable(ClientWebRequest request) {
			if(request.QueryString.ContainsKey("action")) {
			if(request.QueryString["action"] == "pause") {
				Download.downloads[Convert.ToInt32(request.QueryString["inst"])].Pause();
					
				}
				if(request.QueryString["action"] == "resume") {
				Download.downloads[Convert.ToInt32(request.QueryString["inst"])].Resume();
					
				}
				if(request.QueryString["action"] == "abort") {
				Download.downloads[Convert.ToInt32(request.QueryString["inst"])].Abort();
				
				}
				return "";
			}
			if(request.QueryString.ContainsKey("downloadFile")) {
				Stream fstr = File.Open(request.QueryString["downloadFile"],FileMode.Open,FileAccess.Read,FileShare.ReadWrite);
		
				ClientHttpResponse response = new ClientHttpResponse();
				response.StatusCode = "200 OK";
				response.ContentType = ConfigManager.getMimeType(request.QueryString["downloadFile"]);
				response.len = fstr.Length;
				response.AddHeader("Content-Disposition: attachment; filename=\""+Path.GetFileName(request.QueryString["downloadFile"])+"\"");
				
				response.WriteHeader(request.stream);
				request.ContinueProcessing = false;
				try {
					
				Download mload = new Download(request.QueryString["downloadFile"],fstr,request.stream,fstr.Length);
				mload.Begin();
				}catch(Exception er) {
				
				}
					
				return "";
			}
			try {
			statusUpdateEvent.WaitOne(5000);
			statusUpdateEvent.Reset();
			}catch(Exception) {
			
			}
		StringBuilder mbuilder = new StringBuilder();
			mbuilder.AppendLine("<table border=\"1\" inst_id=\"maintable\">");
			mbuilder.AppendLine("<tr><td><b><u>Connection name</u></b></td><td><b><u>Progress</u></b></td><td><b><u>Status</u></b></td></tr>");
			try {
				int i = 0;
			lock(Download.downloads) {
				
			foreach(Download et in Download.downloads) {
					string progress = "<div style=\"width:"+((int)(((float)et._progress/(float)et._size)*100f)).ToString()+"%;background-color:Green;\">PROGRESS</div>";
				string actions = "";
						if(et.paused) {
						actions = "PAUSED";
						}else {
						actions = "IN PROGRESS";
						}
						if(et.haserror) {
					actions = "ERROR";
						}
					
					mbuilder.AppendLine("<tr dlID=\""+i.ToString()+"\"><td>"+et.name+"</td><td style=\"background-color:Red;\">"+progress+"</td><td inst_id=\"status\">"+actions+"</td></tr>");	
				i++;
					}
				
				mbuilder.AppendLine("</table>");
			}
				}catch(Exception) {
				}
			return mbuilder.ToString();
		}
#endregion
		#region Application manager
		public string applicationManager(ClientWebRequest request) {
		if(request.QueryString.ContainsKey("action")) {
				if(request.QueryString["action"] == "terminate") {
			ConfigManager.TerminateApplication(request.QueryString["id"]);
				}
				if(request.QueryString["action"] == "startup") {
				ConfigManager.setStartup(request.QueryString["id"]);
				}
			}
			StringBuilder mbuilder = new StringBuilder();
		mbuilder.AppendLine("<h2>Web applications</h2><hr />");
		mbuilder.AppendLine("<table border=\"1\" inst_id=\"maintable\">");
		foreach(string et in ConfigManager.GetApplications()) {
			mbuilder.AppendLine("<tr><td inst_id=\"appname\">"+et+"</td></tr>");
			}
		mbuilder.AppendLine("</table>");
			return mbuilder.ToString();
			
		}
#endregion
		#region File browser
		public string fileList(string queryStringValue,ClientWebRequest request) {
		Dictionary<string,string> querystring = request.QueryString;
			if(!querystring.ContainsKey(queryStringValue)) {
			
				querystring.Add(queryStringValue,Path.GetPathRoot(Environment.CurrentDirectory));
			
			}
			StringBuilder retval = new StringBuilder();
			retval.Append("<tr><td><b><u>File name</b></u></td><td><b><u>Last modified</u></b></td></tr>");
			foreach(string et in Directory.GetFileSystemEntries(querystring[queryStringValue])) {
			retval.Append("<tr fullname=\""+et+"\" isdirectory=\""+Directory.Exists(et).ToString()+"\"><td>"+Path.GetFileName(et)+"</td><td>"+Directory.GetLastWriteTime(et)+"</td></tr>");
			}
			return retval.ToString();
		}
		public string CurrentDirectory(string queryStringValue, ClientWebRequest request) {
		Dictionary<string,string> querystring = request.QueryString;
			if(!querystring.ContainsKey(queryStringValue)) {
			
				querystring.Add(queryStringValue,Path.GetPathRoot(Environment.CurrentDirectory));
			
			}
			return querystring[queryStringValue];
		}
		#endregion
		public string sessionKey(ClientWebRequest request)
        {
            if (!request.QueryString.ContainsKey("sessionID"))
            {
                Guid msession = Guid.NewGuid();
                sessions.Add(msession.ToString(), new SessionInformation());
                request.QueryString.Add("sessionID",msession.ToString());
				return msession.ToString();
            }
            else
            {
				if(!sessions.ContainsKey(request.QueryString["sessionID"])) {
				sessions.Add(request.QueryString["sessionID"],new SessionInformation());
					
				}
                return request.QueryString["sessionID"];
            }
        }
        public string loginstatus(ClientWebRequest request)
        {
            if (request.QueryString.ContainsKey("sessionID"))
            {
                string sesid = request.QueryString["sessionID"];
                if (!sessions.ContainsKey(request.QueryString["sessionID"]))
                {
                    sessions.Add(sesid, new SessionInformation());
                }
                SessionInformation session = sessions[sesid];
                
				if(request.Method == "POST") {
				Dictionary<string,string> formdata = request.Form;
				try {
						using(Stream fstr = File.Open(Environment.CurrentDirectory+"\\admin",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.ReadWrite)) {
						Filesystem msys = new Filesystem(fstr,formdata["pswd"],16384,1024*1024*5);
							msys.Dispose();
						session.isAdminAuthenticated = true;
						}
						
					}catch(Exception er) {
						return "Login failure";
					}
				}
				if(session.isAdminAuthenticated) {
				ClientHttpResponse response = new ClientHttpResponse();
				response.Redirect("serverAdmin.htm?sessionID="+request.QueryString["sessionID"],false,request.stream);
				request.ContinueProcessing = false;
				}
				return "";
            }
            else
            {
                return "";
            }
        }
        #endregion
		
        Type mtype;
        static string Link(string code)
        {
            string linkedcode = code;
            string linkstr = "IDWOS-LINKER-INCLUDE:";
            for (int i = 0; i < linkedcode.Length; )
            {
                int offset = linkedcode.IndexOf("IDWOS-LINKER-INCLUDE:", i);
                if (offset < 0)
                {

                    break;
                }
                else
                {
                    int endoffset = linkedcode.IndexOf("\n", offset) - (offset + linkstr.Length);
                    string resolvepath = linkedcode.Substring(offset + linkstr.Length, endoffset);
                    StreamReader mreader = new StreamReader(resolvepath.Replace("\n", "").Replace("\r", ""));
                    linkedcode = linkedcode.Replace(linkstr + resolvepath, Link(mreader.ReadToEnd()));
                    mreader.Dispose();
                    i = endoffset - 5;
                }
            }
            return linkedcode;
        }
        public void ParseHTMLDocument(ref string HTML, ClientWebRequest request)
        {
            HTML = Link(HTML);
             mtype = GetType();
            StringBuilder mbuilder = new StringBuilder();
            int offset = 0;

            while (offset < HTML.Length)
            {
                int idex = HTML.IndexOf("{$server=", offset);
                if (idex > -1)
                {
                    string sbstr = HTML.Substring(idex,HTML.IndexOf("}",idex)-idex+1);
                    //TODO: Implement this
                    string name = sbstr.Substring(sbstr.IndexOf("=")+1,sbstr.IndexOf("(")-sbstr.IndexOf("=")-1);
                    object[] args;
                    if (sbstr.Contains("()"))
                    {
                        args = new string[0];
                    }
                    else
                    {
                        string argstr = sbstr.Substring(sbstr.IndexOf("(") + 1, sbstr.IndexOf(")") - sbstr.IndexOf("(") - 1);
                        args = argstr.Split(',');
                    }
                    object[] realargs = new object[args.Length + 1];
                    for (int i = 0; i < args.Length; i++)
                    {
                        realargs[i] = args[i];
                    }
                    realargs[realargs.Length - 1] = request;
                    HTML = HTML.Replace(sbstr, InvokeMethod(name,realargs));
                    offset = idex + 1;
                }
                else
                {
                    break;
                }
            }
        }
    }
}
