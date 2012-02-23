using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using HttpServer;
using IC80v3;
using System.Drawing;
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
		public string fileList(string queryStringValue,ClientWebRequest request) {
		Dictionary<string,string> querystring = request.QueryString;
			if(!querystring.ContainsKey(queryStringValue)) {
			
				querystring.Add(queryStringValue,Path.GetPathRoot(Environment.CurrentDirectory));
			
			}
			StringBuilder retval = new StringBuilder();
			retval.Append("<tr><td><b><u>File name</b></u></td><td><b><u>Last modified</u></b></td></tr>");
			foreach(string et in Directory.GetFileSystemEntries(querystring[queryStringValue])) {
			retval.Append("<tr><td>"+Path.GetFileName(et)+"</td><td>"+Directory.GetLastWriteTime(et)+"</td></tr>");
			}
			return retval.ToString();
		}
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
