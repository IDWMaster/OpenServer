using System;
using System.Collections.Generic;
using HttpServer;
using IC80v3;
using System.IO;
using OpenNet;
using System.Net;
using Tester;
using OpenNetProvider;
using System.Threading;
using GlobalGridV12;
namespace OpenNetProxySvc
{
	public class ProxyHandler
	{
		static PubKeyDatabase db = null;
		static Dictionary<string,OpenNetProvider.OpenNetProtocolDriver> drivers = new Dictionary<string, OpenNetProvider.OpenNetProtocolDriver>();
		public static void ProcessRequest(ClientWebRequest equest) {
			lock(syncobj) {
		if(db == null) {
			db = new PubKeyDatabase("default",File.Open("OpenNetDB_client",FileMode.OpenOrCreate));
				
		}
			if(db.Length == 0) {
			db.AddPublicKey(db.GenPublicPrivateKey(2048));
			db.GetPublicKeyOnly(db[0]);
			}
			Uri url = new Uri("http:/"+equest.UnsanitizedRelativeURI.Replace("idw.local.ids","127.0.0.1"));
		if(!drivers.ContainsKey(url.Host)) {
			Console.WriteLine("Connecting to "+url.Host);
				HttpWebRequest request = HttpWebRequest.Create("http://"+url.Host+"/OpenNetProvider") as HttpWebRequest;
            request.AllowWriteStreamBuffering = false;
            Stream receiver = new TrashyStream(request.GetResponse().GetResponseStream());
            BinaryReader mreader = new BinaryReader(receiver);
            byte[] guid = mreader.ReadBytes(16);
            Console.WriteLine(BitConverter.ToString(guid));
            request = HttpWebRequest.Create("http://"+url.Host+"/OpenNetProvider") as HttpWebRequest;
            request.Method = "POST";
            request.ContentLength = 9999999999;
            request.AllowWriteStreamBuffering = false;
            Stream sender = new TrashyStream(request.GetRequestStream());
            BinaryWriter mwriter = new BinaryWriter(sender);
            mwriter.Write(guid);
            mwriter.Flush();
            byte[] theirpubkey = mreader.ReadBytes(mreader.ReadInt32());
            Stream dbStr = File.Open("keyDB.db", FileMode.OpenOrCreate);
            
            byte[] ourpublickey = db.GetPublicKeyOnly(db[0]);
            byte[] ourprivatekey = db[0];
            mwriter.Write(ourpublickey.Length);
            mwriter.Write(ourpublickey);
            mwriter.Flush();
            db.AddPublicKey(theirpubkey);
            Stream securedStream = new TrashyStream(db.CreateAuthenticatedStream(ourprivatekey, new DualStream(sender, receiver), 32));
            
            Console.WriteLine("Secure stream negotiated");

            driver = new OpenNetProtocolDriver(securedStream);
            drivers.Add(url.Host,driver);
				Console.WriteLine("Driver initialized");
				
			}
				mvent.Reset();
			currentRequest = equest;
				currentURL = url;
				driver = drivers[url.Host];
				drivers[url.Host].onConnectionEstablished += HandleonConnectionEstablished;
			
				drivers[url.Host].OpenStream();
			
				mvent.WaitOne();
			
			}
			}
		static OpenNetProtocolDriver driver;
		static Uri currentURL;
		static ClientWebRequest currentRequest;
		static object syncobj = new object();
		static void HandleonConnectionEstablished (XStream stream)
		{
			driver.onConnectionEstablished-= HandleonConnectionEstablished;
			int reqcount = currentRequest.headers.Count;
			BinaryWriter mwriter = new BinaryWriter(stream);
			mwriter.Write(reqcount);
			mwriter.Write("GET /"+currentURL.PathAndQuery+" HTTP/1.0");
			for(int i = 1;i<reqcount;i++) {
			mwriter.Write(currentRequest.headers[i]);
			}
			mwriter.Flush();
			byte[] buffer = new byte[16384];
			while(true) {
			try {
				int count = stream.Read(buffer,0,buffer.Length);
					
					Console.WriteLine("DGRAM len: "+count.ToString());
				currentRequest.stream.Write(buffer,0,count);
				}catch(Exception er) {
				break;
				}
			}
			Console.WriteLine("Request complete");
			currentRequest.stream.Flush();
			mvent.Set();
		}
		static ManualResetEvent mvent = new ManualResetEvent(false);
	}
}

