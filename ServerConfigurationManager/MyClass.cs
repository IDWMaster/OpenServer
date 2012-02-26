using System;
using VMLib;
using HttpServer;
using System.IO;
using System.Drawing;
using System.Text;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
namespace ServerConfigurationManager
{
	
	public class ConfigManager
	{
		public static string getMimeType(string filename) {
			string mimetype;
				string str = filename.Substring(filename.LastIndexOf("."));
				if(engine.mimetypes.ContainsKey(str)) {
				mimetype = engine.mimetypes[str];
				}else {
				mimetype = "application/octet-stream";
				}
			return mimetype;
		}
		RequestHelpers reqManager = new RequestHelpers();
		byte[] bitmapData = null;
		byte[] transparentBitmap = null;
		public void onRequest(ClientWebRequest request) {
			try {
			if(request.UnsanitizedRelativeURI.Contains("transparent.png")) {
			if(transparentBitmap == null) {
				Bitmap mmap = new Bitmap(4,4);
					Graphics mfix = Graphics.FromImage(mmap);
					mfix.Clear(Color.FromArgb(106,0,0,255));
					mfix.Dispose();
					MemoryStream mstream = new MemoryStream();
					mmap.Save(mstream,ImageFormat.Png);
					mstream.Position = 0;
					transparentBitmap = mstream.ToArray();
					mstream.Dispose();
					mmap.Dispose();
				}
				ClientHttpResponse response = new ClientHttpResponse();
				response.ContentType = "image/png";
				response.len = transparentBitmap.Length;
				response.StatusCode = "200 OK";
				response.WriteHeader(request.stream);
				request.stream.Write(transparentBitmap,0,transparentBitmap.Length);
				
			}
		if(request.UnsanitizedRelativeURI.Contains("backgroundImg.jpg")) {
			if(bitmapData == null) {
				Bitmap mmap = new Bitmap(1024,1024);
				Graphics mfix = Graphics.FromImage(mmap);
				mfix.Clear(Color.Black);
				mfix.FillRectangle(new LinearGradientBrush(new Point(0,0),new Point(0,512),Color.Blue,Color.Black),new Rectangle(0,0,1024,512));
				mfix.DrawString("OpenServer 2012 - Administration Console\nApplication startup time: "+DateTime.Now.ToString(),new Font(FontFamily.GenericMonospace,24),Brushes.White,new Point(0,0));
					mfix.Dispose();
				
					MemoryStream mstream = new MemoryStream();
					mmap.Save(mstream,ImageFormat.Jpeg);
					mstream.Position = 0;
					bitmapData = mstream.ToArray();
					mstream.Dispose();
					
				}
				
				ClientHttpResponse response = new ClientHttpResponse();
				response.ContentType = "image/jpeg";
				response.len = bitmapData.Length;
				response.StatusCode = "200 OK";
				response.WriteHeader(request.stream);
				request.stream.Write(bitmapData,0,bitmapData.Length);
				request.stream.Flush();
				return;
			}
			string path = Environment.CurrentDirectory+request.UnsanitizedRelativeURI.Replace("/",Path.DirectorySeparatorChar.ToString());
			if(path.IndexOf("?")>1) {
			path = path.Substring(0,path.IndexOf("?"));
			}
			if(File.Exists(path)) {
				
			using(Stream fstr = File.Open(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite)) {
				
				string mimetype;
				string str = path.Substring(path.LastIndexOf("."));
				if(engine.mimetypes.ContainsKey(str)) {
				mimetype = engine.mimetypes[str];
				}else {
				mimetype = "application/octet-stream";
				}
					
					if(mimetype == "text/html") {
					StreamReader mreader = new StreamReader(fstr);
						string httext = mreader.ReadToEnd();
						reqManager.ParseHTMLDocument(ref httext,request);
						MemoryStream mstream = new MemoryStream();
						StreamWriter mwriter = new StreamWriter(mstream);
						mwriter.Write(httext);
						mwriter.Flush();
						mstream.Position = 0;
						if(request.ContinueProcessing) {
						ClientHttpResponse response = new ClientHttpResponse();
							response.ContentType = mimetype;
							response.len = mstream.Length;
							response.StatusCode = "200 OK";
							response.WriteHeader(request.stream);
							response.WriteStream(mstream,request.stream,16384);
						}else {
						return;
						}
					}else {
			
				ClientHttpResponse response = new ClientHttpResponse();
				response.ContentType = mimetype;
				response.len = fstr.Length;
						response.StatusCode = "200 OK";
					response.WriteHeader(request.stream);
					response.WriteStream(fstr,request.stream,16384);
					
					}
				}
			}else {
			ClientHttpResponse response = new ClientHttpResponse();
				response.ContentType = "text/html";
				response.len = notfoundpage.Length;
				response.StatusCode = "404 Not Found";
				response.WriteHeader(request.stream);
				request.stream.Write(notfoundpage,0,notfoundpage.Length);
				request.stream.Flush();
				
			}
			}catch(Exception er) {
			Console.WriteLine(er);
			}
		}
		byte[] notfoundpage;
		static VMExecutionEngine engine;
		public ConfigManager (VMExecutionEngine _engine)
		{
			engine = _engine;
			MemoryStream mstream = new MemoryStream();
			StreamWriter mwriter= new StreamWriter(mstream);
			mwriter.WriteLine("<html>");
			mwriter.WriteLine("<head>");
			mwriter.WriteLine("<title>Page not found</title>");
			mwriter.WriteLine("</head>");
			mwriter.WriteLine("<body>");
			mwriter.WriteLine("The requested URL could not be located in the server administration console.");
			mwriter.WriteLine("</body>");
			mwriter.WriteLine("</html>");
			mwriter.Flush();
			mstream.Position = 0;
			notfoundpage = new byte[mstream.Length];
			mstream.Read(notfoundpage,0,notfoundpage.Length);
		}
		
	}
}

