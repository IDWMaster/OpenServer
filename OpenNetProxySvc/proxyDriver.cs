using System;
using System.Collections.Generic;
using GlobalGridV12;
using System.IO;
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
        public void OpenStream()
        {
            lock (_underlyingstream)
            {
                BinaryWriter mwriter = new BinaryWriter(_underlyingstream);
                mwriter.Write((byte)2);
                mwriter.Flush();
            }
        }
        public delegate void ConnectionEstablishedEventArgs(XStream stream);
        public event ConnectionEstablishedEventArgs onConnectionEstablished;
        Stream _underlyingstream;
		public OpenNetProtocolDriver (Stream basestream):base("null")
		{
            _underlyingstream = basestream;
            System.Threading.Thread mthread = new System.Threading.Thread(delegate()
            {
                BinaryReader mreader = new BinaryReader(_underlyingstream);
                while (true)
                {
                    
                    byte opcode = mreader.ReadByte();
                    if (opcode == 0)
                    {
                        return;
                    }
                    if (opcode == 1)
                    {
                        //XMIT
                       
                        int conid = mreader.ReadInt32();
                        Console.WriteLine(conid);
                        sockets[conid].ntfyDgram(mreader.ReadBytes(mreader.ReadInt32()));
                        Console.WriteLine("PACKET RECEIVED");
                    }
                    if (opcode == 2)
                    {
                        //Establish network connection
                        Console.WriteLine("ILLEGAL OPCODE --- Server OPCODE specified for client");


                    }
                    if (opcode == 3)
                    {
                        Console.WriteLine("CONNECTION ESTABLISHED");
                        
                        int conid = mreader.ReadInt32();
                        sockets.Add(conid, new ParallelSocket(this, _underlyingstream, conid));
                        ParallelSocket socket = sockets[conid];
                        System.Threading.ThreadPool.QueueUserWorkItem(delegate(object sender)
                        {
                            onConnectionEstablished.Invoke(new XStream(socket));
                        });
                    }
                }
            });
            mthread.Start();
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

