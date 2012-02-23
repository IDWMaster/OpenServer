using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using HttpServer;
using IC80v3;
using System.Drawing;
namespace FreeSocial
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
			public IndexedFS fs;
			public string UserName;
			public void Dispose ()
			{
				fs.Commit();
				fs.Dispose();
			GC.SuppressFinalize(this);
			}
			~SessionInformation() {
			fs.Commit();
				fs.Dispose();
				
			}
			
        }
        Dictionary<string, SessionInformation> sessions = new Dictionary<string, SessionInformation>();
        public string sessionKey(ClientWebRequest request)
        {
            if (!request.QueryString.ContainsKey("sessionID"))
            {
                Guid msession = Guid.NewGuid();
                sessions.Add(msession.ToString(), new SessionInformation());
                return msession.ToString();
            }
            else
            {
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
				
					Stream fstr = File.Open(formdata["username"],FileMode.OpenOrCreate);
				try {
					session.fs = new IndexedFS(new Filesystem(fstr,formdata["pswd"],1024*1024,1024*1024*512));
					//return "Authorization success!";
			        session.UserName = formdata["username"];
						bool dirsetup = false;
						foreach(string et in session.fs.Directories) {
						if(et == formdata["username"]) {
							dirsetup = true;
							}
						}
						if(!dirsetup) {
						session.fs.CreateDirectory(formdata["username"]);
						}
					session.fs.Commit();
					
					}catch(Exception er) {
					fstr.Close();
						return "Invalid password";
					}
				}
				if(session.fs !=null) {
				ClientHttpResponse response = new ClientHttpResponse();
					request.ContinueProcessing = false;	
					response.Redirect("profile.html?sessionID="+request.QueryString["sessionID"],false,request.stream);
				
				}
				return sesid;
            }
            else
            {
                return "";
            }
        }
        #endregion
		#region User profile
		public string UserName(ClientWebRequest request) {
		SessionInformation session = sessions[request.QueryString["sessionID"]];
		return session.UserName;
		}
		public string ProfilePicture(ClientWebRequest request) {
			
		SessionInformation session = sessions[request.QueryString["sessionID"]];
			if(request.QueryString.ContainsKey("GetProfilePic")) {
			foreach(string et in session.fs.OpenDir(request.QueryString["GetProfilePic"]).Files) {
			if(et == "pic") {
				    Bitmap tmap = new Bitmap(32,32);
						Graphics aix = Graphics.FromImage(tmap);
						Bitmap amage = new Bitmap(session.fs.OpenDir(request.QueryString["GetProfilePic"]).OpenFile("pic"));
					aix.DrawImage(amage,new Rectangle(0,0,32,32));
						aix.Dispose();
						amage.Dispose();
						MemoryStream ystream = new MemoryStream();
						tmap.Save(ystream,System.Drawing.Imaging.ImageFormat.Jpeg);
						ystream.Position = 0;
						tmap.Dispose();
						ClientHttpResponse _response = new ClientHttpResponse();
						_response.len = ystream.Length;
						_response.ContentType = "image/jpg";
						_response.WriteHeader(request.stream);
						_response.WriteStream(ystream,request.stream,16384);
						
					}
			}
				Bitmap mmap = new Bitmap(32,32);
				Graphics mfix = Graphics.FromImage(mmap);
						mfix.DrawLine(Pens.Red,new Point(0,0),new Point(32,32));
				mfix.DrawLine(Pens.Red,new Point(0,32),new Point(32,0));
				mfix.Dispose();
				MemoryStream mstream = new MemoryStream();
				mmap.Save(mstream,System.Drawing.Imaging.ImageFormat.Jpeg);
				mmap.Dispose();
				mstream.Position = 0;
				ClientHttpResponse response = new ClientHttpResponse();
				response.len = mstream.Length;
				response.ContentType = "image/jpg";
				response.WriteHeader(request.stream);
				response.WriteStream(mstream,request.stream,16384);
				request.ContinueProcessing = false;
				return "";
			}
			return "profile.html?GetProfilePic="+session.UserName+"&sessionID="+request.QueryString["sessionID"];
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
                int idex = HTML.IndexOf("<$server=", offset);
                if (idex > -1)
                {
                    string sbstr = HTML.Substring(idex,HTML.IndexOf(">",idex)-idex+1);
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
