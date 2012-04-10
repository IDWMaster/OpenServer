using System;
using System.Collections.Generic;
using GlobalGridV12;
using System.IO;
using HttpServer;
namespace OpenNetProvider
{
	/// <summary>
	/// Implementation of the OpenNet Protocol Driver for the Global Grid
	/// The Global Grid technology is patent-pending and is proprietary technology
	/// This file is open-source, and simply calls into that library. 
	/// To utilize this technology, the Global Grid must be obtained as a separate library
	/// and may not be shipped with this program. The Global Grid service may be obtained from Brian Bosak,
	/// the copyright holder of this technology.
	/// </summary>
	public class OpenNetProtocolDriver:P2PConnectionManager
	{
		Dictionary<long,ParallelSocket> sockets = new Dictionary<long, ParallelSocket>();
		internal static Guid protocolID = new Guid("a21c578c-66f5-4961-bc68-5c844f598788");
		int currentID = 0;
		void thetar(object sender) {
            try
            {
                ParallelSocket msock = sender as ParallelSocket;
                XStream mstream = new XStream(msock);
                List<string> headers = new List<string>();
                BinaryReader mreader = new BinaryReader(mstream);
                int count = mreader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    headers.Add(mreader.ReadString());
                }
                //Create a new ClientWebRequest. Note that WE will be the server for this remoting object
                //as it is created on our end. So unlike most reuqests, we'll have a really complex route
                //Execution route: server->Protocol driver->Server->Application
                ClientWebRequest request = new ClientWebRequest(mstream, headers);
                request.SecureConnection = true;
                request.SecurityIdentifier = _session.pubKey;
                //Inject the request into the server execution framework
                NetProvider.instance._engine.NtfyConnection(request);
            }
            catch (Exception er)
            {
                Console.WriteLine(er);
            }
		}
		ClientSession _session;
		public OpenNetProtocolDriver (ClientSession session):base("null")
		{
			_session = session;
			BinaryReader mreader = new BinaryReader(session.securedStream);
			while(true) {
			byte opcode = mreader.ReadByte();
				if(opcode == 0) {
				return;
				}
				if(opcode == 1) {
				//XMIT
					
					int conid = mreader.ReadInt32();
					Console.WriteLine(conid);
					sockets[conid].ntfyDgram(mreader.ReadBytes(mreader.ReadInt32()));
                    Console.WriteLine("PACKET RECEIVED");
				}
				if(opcode == 2) {
				//Establish network connection
					BinaryWriter mwriter = new BinaryWriter(session.securedStream);
					lock(session.securedStream) {
                        Console.WriteLine("OPCODE 3");
					mwriter.Write((byte)3);
						mwriter.Write(currentID);
					mwriter.Flush();
					}
					lock(sockets) {
						ParallelSocket msock = new ParallelSocket(this,session.securedStream,currentID);
					sockets.Add(currentID,msock);
						System.Threading.ThreadPool.QueueUserWorkItem(thetar,msock);
					currentID++;
					}
					
					
				}
			}
			
		}

		#region implemented abstract members of GlobalGridV12.P2PConnectionManager
		protected override void InitializeSockets ()
		{
	
		}

		protected override VSocket[] KnownSockets {
			get {
				return null;
			}
		}
		#endregion
	}
	public class ParallelSocket:VSocket {
		Stream _underlyingstream;
		P2PConnectionManager _manager;
		int _id;
		public ParallelSocket(P2PConnectionManager manager, Stream ustr, int connectionID):base(manager) {
		_underlyingstream = ustr;
			_manager = manager;
			_id = connectionID;
		}
		#region implemented abstract members of GlobalGridV12.VSocket
		protected override byte[] _SerializeAlternate ()
		{
            return new byte[1];
		}

		protected override byte[] _Serialize ()
		{
            return new byte[1];
		}
		public void ntfyDgram(byte[] dgram) {
	    invokeRecvDgate(dgram);
		}
		public override void Send (byte[] dgram, int timeout)
		{
			lock(_underlyingstream) {
			BinaryWriter mwriter = new BinaryWriter(_underlyingstream);
				mwriter.Write((byte)1);
				mwriter.Write(_id);
				mwriter.Write(dgram.Length);
				mwriter.Write(dgram);
				mwriter.Flush();

			}
			Console.WriteLine("XMIT"+_id.ToString());
		}

		public override Guid ProtocolID {
			get {
				return _manager.ID;
			}
		}
		#endregion
	
		#region implemented abstract members of GlobalGridV12.VSocket
		public override void Dispose ()
		{
			
		}
		#endregion
	}
}

