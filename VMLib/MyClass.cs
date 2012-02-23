using System;
using System.Collections.Generic;
using System.Reflection;
using HttpServer;
using System.IO;
using System.Runtime.Remoting;
namespace VMLib
{
	public class VMInstance:MarshalByRefObject {
		public VMInstance() {
		
		}
		object VM;
		public void StartApplication(byte[] asm, VMExecutionEngine engine) {
		Assembly mbly = Assembly.Load(asm);
			VM = mbly.GetExportedTypes()[0].GetConstructor(new Type[] {typeof(VMExecutionEngine)}).Invoke(new object[] {engine});
		}
		public void ntfyRequest(ClientWebRequest request) {
		VM.GetType().GetMethod("onRequest").Invoke(VM,new object[] {request});
			
		}
	}
	public class VMExecutionEngine:MarshalByRefObject
	{
		
		public Dictionary<string,string> mimetypes = new Dictionary<string, string>();
		public VMExecutionEngine ()
		{
			mimetypes.Add(".htm", "text/html");
            mimetypes.Add(".jpg", "image/jpeg");
            mimetypes.Add(".html", "text/html");
            mimetypes.Add(".png", "image/png");
            mimetypes.Add(".js", "text/javascript");
            mimetypes.Add(".mp4", "video/mp4");
            mimetypes.Add(".ogg", "audio/ogg");
            mimetypes.Add(".ogv", "video/ogg");
			System.Threading.Thread mtthread = new System.Threading.Thread(inputtar);
			mtthread.Start();
		}
		void inputtar() {
		while(true) {
			string txt = Console.ReadLine();
				if(txt == "abortAll") {
				Console.WriteLine("Aborting all half-open connections....");
					HttpServer.HttpListener.resetConnections = true;
					System.Threading.Thread.Sleep(10000);
					HttpServer.HttpListener.resetConnections = false;
					Console.WriteLine("Operation complete.");
				}
			}
		}
		Dictionary<string,AppDomain> domains = new Dictionary<string, AppDomain>();
		Dictionary<string,VMInstance> instances = new Dictionary<string, VMInstance>();
		public void NtfyConnection(ClientWebRequest request) {
		string appname;
				if(request.UnsanitizedRelativeURI.Length>5) {
				int el;
				if(request.UnsanitizedRelativeURI.IndexOf("/",request.UnsanitizedRelativeURI.IndexOf("/")+1) <0) {
				el = request.UnsanitizedRelativeURI.Length-(request.UnsanitizedRelativeURI.IndexOf("/")+1);
				}else {
				el = request.UnsanitizedRelativeURI.IndexOf("/",request.UnsanitizedRelativeURI.IndexOf("/")+1)-1;
				}
				appname = request.UnsanitizedRelativeURI.Substring(request.UnsanitizedRelativeURI.IndexOf("/")+1,el);
				}else {
					appname = "home";
				}
				doRequest:
				if(instances.ContainsKey(appname)) {
				try {
				    instances[appname].ntfyRequest(request);
				}catch(RemotingException er) {
				Console.WriteLine("Remoting instance for "+appname+" has died. Respawning application....");
					lock(instances) {
					AppDomain.Unload(domains[appname]);
						instances.Remove(appname);
						domains.Remove(appname);
					}
					goto doRequest;
				}
					}else {
				//Search for application
					bool isfound = false;
					foreach(string et in Directory.GetDirectories(Environment.CurrentDirectory)) {
					string it = et.Replace("\\","/");
					string ix = it.Substring(it.LastIndexOf("/")+1);
					
					if(ix == appname) {
						isfound = true;
						}
					}
					if(!isfound) {
					StreamWriter mwriter = new StreamWriter(request.stream);
						mwriter.WriteLine("<html>");
						mwriter.WriteLine("<head>");
						mwriter.WriteLine("<title>Application not found</title>");
						mwriter.WriteLine("</head>");
						mwriter.WriteLine("<body>");
						mwriter.WriteLine("<h1>Unable to locate application "+appname+"</h1><hr />The application you are looking for does not exist on this server.");
						mwriter.WriteLine("</body>");
						mwriter.WriteLine("</html>");
						mwriter.Flush();
					}else {
					Stream mstr = File.Open(Environment.CurrentDirectory+"\\"+appname+"\\"+appname+".dll",FileMode.Open);
						byte[] buffer = new byte[mstr.Length];
						mstr.Read(buffer,0,buffer.Length);
						LoadApplication(buffer,appname);
						goto doRequest;
					}
				}
			byte[] mbuffer = new byte[16384];
			int total = 0;
			try {
			if(request.ExpectedContentLength>0) {
			while(true) {
			int count = request.stream.Read(mbuffer,0,mbuffer.Length);
						
						total+=count;
				if(count <=0 || total>=request.ExpectedContentLength) {
				break;
				}
				
			}
			}
			}catch(Exception er) {
			}
			
				request.stream.Dispose();
			
		}
		public void LoadApplication(byte[] assembly, string appName) {
		AppDomain md = AppDomain.CreateDomain(appName);
			VMInstance app = md.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().FullName,"VMLib.VMInstance") as VMInstance;
			app.StartApplication(assembly,this);
			lock(instances) {
			instances.Add(appName,app);
				domains.Add(appName,md);
			}
		}
	}
}
