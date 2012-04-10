using System;
using System.Collections.Generic;
using HttpServer;
using OpenNet;
using VMLib;
using System.IO;
using Tester;
namespace OpenNetProvider
{
	
	public class NetProvider
	{
		public void onRequest(ClientWebRequest request) {
		if(request.Method == "GET") {
			//Initiate session
			ClientHttpResponse response = new ClientHttpResponse();
				response.ContentType = "text/html";
				response.len = 1024*1024*300;
				response.StatusCode = "200 OK";
				response.WriteHeader(request.stream);
				ClientSession session = new ClientSession();
				session.sessionID = Guid.NewGuid();
				TrashyStream garbage = new TrashyStream(request.stream);
				session.writer = garbage;
				BinaryWriter mwriter = new BinaryWriter(garbage);
				mwriter.Write(session.sessionID.ToByteArray());
				mwriter.Flush();
				lock(ClientSession.sessions) {
				ClientSession.sessions.Add(session.sessionID,session);
				}
				session.WaitHandle.WaitOne();
			}else {
				try {
			TrashyStream reader = new TrashyStream(request.stream);
				BinaryReader mreader = new BinaryReader(reader);
				ClientSession currentSession = ClientSession.sessions[new Guid(mreader.ReadBytes(16))];
				currentSession.reader = reader;
				BinaryWriter mwriter = new BinaryWriter(currentSession.writer);
				byte[] ourprivatekey = db[0];
				byte[] ourpubkey = db.GetPublicKeyOnly(ourprivatekey);
				mwriter.Write(ourpubkey.Length);
				mwriter.Write(ourpubkey);
				mwriter.Flush();
				byte[] theirpubkey = mreader.ReadBytes(mreader.ReadInt32());
				if(!db.IsKeyTrusted(theirpubkey)) {
				db.AddPublicKey(theirpubkey);
					db.Commit();
				}
				currentSession.securedStream = new TrashyStream(db.CreateAuthenticatedStream(ourprivatekey,new DualStream(currentSession.writer,currentSession.reader),32));
				Console.WriteLine("Secure stream negotiated");
				currentSession.pubKey = BitConverter.ToString(theirpubkey);
				OpenNetProtocolDriver driver = new OpenNetProtocolDriver(currentSession);
				}catch(Exception er) {
				Console.WriteLine(er);
				}
			}
		}
		PubKeyDatabase db;
		public VMExecutionEngine _engine;
		public static NetProvider instance;
		public NetProvider (VMExecutionEngine engine)
		{
			instance = this;
			_engine = engine;
			if(!File.Exists("OpenNetDB")) {
			File.Create("OpenNetDB").Dispose();
				try {
				File.Encrypt("OpenNetDB");
				}catch(Exception) {
				Console.WriteLine("WARN: File system encryption is not supported on your computer. the private key will remain in un-encrypted format");
				}
			}
			db = new PubKeyDatabase("default",File.Open("OpenNetDB",FileMode.Open));
			if(db.Length == 0) {
			db.AddPublicKey(db.GenPublicPrivateKey(2048));
			db.GetPublicKeyOnly(db[0]);
			}
			
		}
		
	}
	public class ClientSession {
		public System.Threading.ManualResetEvent WaitHandle = new System.Threading.ManualResetEvent(false);
		public static Dictionary<Guid,ClientSession> sessions = new Dictionary<Guid, ClientSession>();
		/// <summary>
		/// Represents an unsecured input stream.
		/// </summary>
	public Stream reader;
		/// <summary>
		/// Represents an unsecured output stream.
		/// </summary>
		public Stream writer;
		/// <summary>
		/// Represents a secure stream.
		/// </summary>
		public Stream securedStream;
		public Guid sessionID;
		public string pubKey;
	}
}

