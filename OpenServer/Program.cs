using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FreeSocial;
using HttpServer;
using VMLib;
namespace OpenServer
{
    /// <summary>
    /// OpenServer - Dedicated to free Internet communication
    /// and open social networking
    /// </summary>
    class Program
    {
		static VMExecutionEngine engine;
        static Dictionary<string, string> mimemappings = new Dictionary<string, string>();
        static void Main(string[] args)
        {
			engine = new VMExecutionEngine();
			
            HttpListener mlist = new HttpListener(82);
            mlist.onClientConnect += new HttpListener.ConnectEventDgate(mlist_onClientConnect);
            mimemappings.Add(".htm", "text/html");
            mimemappings.Add(".jpg", "image/jpeg");
            mimemappings.Add(".html", "text/html");
            mimemappings.Add(".png", "image/png");
            mimemappings.Add(".js", "text/javascript");
            mimemappings.Add(".mp4", "video/mp4");
            mimemappings.Add(".ogg", "audio/ogg");
            mimemappings.Add(".ogv", "video/ogg");

        }
        static RequestHelpers helpers = new RequestHelpers();
        static void mlist_onClientConnect(ClientWebRequest request)
        {
            try
            {
			engine.NtfyConnection(request);	
            }
            catch (Exception er)
            {
				
                MemoryStream errstream = new MemoryStream();
                StreamWriter mwriter = new StreamWriter(errstream);
                mwriter.Write("<html><head><title>An error has occured</title></head><body><pre><h2>Whoops! We've had a 500 Internal Server Error! To the embarassment of the developer, the full error is shown below</h2><hr />"+er.ToString()+"</pre></body></html>");
                mwriter.Flush();
                errstream.Position = 0;
                ClientHttpResponse response = new ClientHttpResponse();
                response.len = errstream.Length;
                response.StatusCode = "500 Internal Server Error";
                response.ContentType = "text/html";
                response.WriteHeader(request.stream);
                response.WriteStream(errstream, request.stream, 16384);
				try {
				request.stream.Dispose();
				}catch(Exception err) {
				
				}
            }
        }
    }

}