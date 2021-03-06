﻿/*
 * Originally created by Brian Bosak
 */
//#define NO_CACHE
//#define test
#define SINGLE_THREADED
//#define test
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.Threading;
namespace IC80v3
{

    public class SeekableCryptoStream : Stream
    {
        protected override void Dispose(bool disposing)
        {
            _reader.Dispose();
            base.Dispose(disposing);
        }
        public long Data2Write
        {
            get
            {
                return _reader.outputbuffer.Count * _reader.fragsize;
            }
        }
		
        public static SeekableCryptoStream CreateUltraSecureStream(string pswd, int seglen, Stream btr)
        {
#if test
			Console.WriteLine("====================================");
			Console.WriteLine("TEST ONLY -- DO NOT USE IN PRODUCTION");
			Console.WriteLine("====================================");
			
#endif
			
            SHA1 mt = new SHA1Managed();
            byte[] hash = mt.ComputeHash(Encoding.UTF8.GetBytes(pswd));
            List<char> tlist = new List<char>(pswd.ToCharArray());
            tlist.Reverse();
            string reversed = new string(tlist.ToArray());
            //Create stream 0
            Aes rdale = new AesManaged();

            Rfc2898DeriveBytes derivitive = new Rfc2898DeriveBytes(reversed + pswd + hash[5].ToString(), hash);
            rdale.Key = derivitive.GetBytes(32);
            derivitive = new Rfc2898DeriveBytes(hash, Encoding.Unicode.GetBytes(pswd), 16);
            rdale.IV = derivitive.GetBytes(16);
            SeekableCryptoStream mstr = new SeekableCryptoStream(new SegmentReader(btr, rdale, seglen));
            //Create stream 1
            Aes secondale = new AesManaged();
            derivitive = new Rfc2898DeriveBytes(pswd + reversed + hash[2].ToString(), Encoding.BigEndianUnicode.GetBytes(pswd));
            secondale.Key = derivitive.GetBytes(32);
            derivitive = new Rfc2898DeriveBytes(hash, Encoding.Unicode.GetBytes(pswd), 16);
            secondale.IV = derivitive.GetBytes(16);
            //No clue why this has to be less than 16384, but it does....
            Console.WriteLine("WARNING: Insecure test algorithm. Uncomment line below to secure it");
           //TODO: DOESN't work with 2 for some reason....
            mstr = new SeekableCryptoStream(new SegmentReader(mstr, rdale, seglen));
            return mstr;
        }




        SegmentReader _reader;
        byte[] currentSegment = null;
        long cpos;
        int FragmentID
        {
            get
            {
                //1st segment is used for sizeof(stream)
                return (int)(cpos / _reader.fragsize) + 1;
            }
        }
        int SegPos
        {
            get
            {
                //Compute segment ID
                double fragID = ((double)cpos / (double)_reader.fragsize) + 1;
                //Now, compute the position within that fragment

                double fragpos = (fragID - (long)fragID);
                int retval = (int)(fragpos * (double)_reader.fragsize);

                return retval;
            }
        }
        public SeekableCryptoStream(SegmentReader reader)
        {
            _reader = reader;
			byte[] mb = new byte[sizeof(long)];
            reader.rawRead(0,mb);
            len = BitConverter.ToInt64(mb, 0);
			if(len>reader.basestream.Length) {
			len = 0;
			}
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override void Flush()
        {
            byte[] ival = BitConverter.GetBytes(len);
            _reader.rawWrite(0,ival);
        }
        long len = 0;
        public override long Length
        {
            get { return len; }
        }

        public override long Position
        {
            get
            {
                return cpos;
            }
            set
            {
                int prevfrag = FragmentID;

                cpos = value;
                if (prevfrag != FragmentID)
                {
                    //Mark as needing to be read, if necessary
                    currentSegment = null;
                }

            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (currentSegment == null)
            {
                currentSegment = _reader.ReadSegment(FragmentID,this);

            }
            int data2read = count;
            int dataRead = 0;
            while (data2read > 0)
            {
                if (data2read >= currentSegment.Length - SegPos)
                {
                    int cdat = currentSegment.Length - SegPos;
                    Buffer.BlockCopy(currentSegment, SegPos, buffer, offset + dataRead, cdat);
                    int pid = FragmentID;
                    cpos += cdat;
                    dataRead += cdat;
                    data2read -= cdat;

                    currentSegment = _reader.ReadSegment(FragmentID,this);
                }
                else
                {
                    int cdat = data2read;
                    Buffer.BlockCopy(currentSegment, SegPos, buffer, offset + dataRead, data2read);
                    data2read -= cdat;
                    cpos += cdat;
                    dataRead += cdat;

                }
            }
            return dataRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.Begin)
            {
                Position = offset;
            }
            if (origin == SeekOrigin.Current)
            {
                Position += offset;
            }
            if (origin == SeekOrigin.End)
            {
                Position = Position - offset;
            }
            return Position;
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            int data2read = count;
            int upos = 0;
            while (data2read > 0)
            {


                if (currentSegment == null)
                {
                    //Read in current fragment
                    currentSegment = _reader.ReadSegment(FragmentID,this);

                }
                int dataread;
                if (data2read > currentSegment.Length - SegPos)
                {
                    dataread = currentSegment.Length - SegPos;
                    Buffer.BlockCopy(buffer, offset + upos, currentSegment, SegPos, dataread);

                }
                else
                {
                    dataread = data2read;
                    Buffer.BlockCopy(buffer, offset + upos, currentSegment, SegPos, dataread);
                }
                int pid = FragmentID;
                cpos += dataread;
                upos += dataread;
                data2read -= dataread;
                //Write immediately
                _reader.WriteSegment(pid, currentSegment);
                if (FragmentID != pid)
                {
                    currentSegment = null;
                }

            }
			
                if (cpos > len)
                {
                    len = cpos;
                }
        }
    }
    public class SegmentReader:IDisposable
    {
        internal Stream basestream;
        public int fragsize;
       
        ICryptoTransform reader;
        Stream pwriter;
        Stream preader;
        AesManaged _rdale;
        public bool _test = false;
        public SegmentReader(Stream _bsstr, Aes rdale, int _fragsize)
        {
            
            _rdale = rdale as AesManaged;
            
            basestream = _bsstr;
            fragsize = _fragsize;
            reader = rdale.CreateDecryptor();
          
        }
        public static bool testmode = false;
        void TransformMultiByte(byte[] input, byte[] output, ICryptoTransform transform)
        {
#if !test
            if (input.Length != output.Length)
            {
                throw new Exception("Input and output size must match");
            }
            if (testmode)
            {
                Buffer.BlockCopy(input, 0, output, 0, output.Length);
                return;
            }
            int ib = transform.InputBlockSize;
			
            if (true)
            {
                transform.TransformBlock(input, 0, output.Length, output, 0);
            }else {
                for (int i = 0; i < output.Length; i += ib)
                {
					transform.TransformBlock(input, i, ib, output, i);
                }
            }
#else
			output = input;
#endif
        }

        ManualResetEvent mvent = new ManualResetEvent(false);
        /// <summary>
        /// Controlled by buffering system. Does nothing
        /// </summary>
		public void Flush()
        {
            
        }
        const int boff = 32;
        Random mrand = new Random();
       public class buffercontainer
        {
            public byte[] data;
            public int segID;
        
        }
		object syncObj = new object();
		public int pendingOperations = 0;
		ManualResetEvent completeNtfy = new ManualResetEvent(false);
        public List<buffercontainer> outputbuffer = new List<buffercontainer>();
		bool needsReset = false;
		Dictionary<int,byte[]> pendingSegments = new Dictionary<int, byte[]>();
		Dictionary<Thread,ICryptoTransform> transforms = new Dictionary<Thread, ICryptoTransform>();
		ManualResetEvent cment = new ManualResetEvent(false);
        public void WriteSegment(int segID, byte[] data)
        {
            
            if (!isrunning)
            {
                throw new InvalidOperationException("Crypto driver has been deactivated. Unable to write data to device.");
            }
#if !NO_CACHE
	
		mpt:
					bool cts = false;
			lock(pendingSegments) {
			if(pendingSegments.ContainsKey(segID)) {
					cment.Reset();
					
					cts = true;
					
				}else {
					pendingSegments.Add(segID,data);
					
				}
			}
			if(cts) {
			
			cment.WaitOne();
			goto mpt;	
			}
			lock (cachedsegments)
                {
            if (cachedsegments.ContainsKey(segID))
            {
                
                    cachedsegments[segID] = data;
                
            }
			
            else
            {
               
                    cachedsegments.Add(segID, data);
                
            }
			}
#endif

            //Seek to segpos

            byte[] buffer = new byte[data.Length + boff];
            Buffer.BlockCopy(data, 0, buffer, 0, data.Length);
          lock(syncObj) {
				if(needsReset) {
				completeNtfy.Reset();
					needsReset = false;
				}
			pendingOperations++;
			}
			
			
			thetar (new object[] {buffer,segID});
			//THIS DOESN'T
			//	System.Threading.ThreadPool.QueueUserWorkItem(thetar,new object[] {buffer,segID});
            //END EXPERIMENT
            mvent.Set();
            
        }
		void thetar(object sender) {
			object[] data = sender as object[];
			byte[] buffer = data[0] as byte[];
			int segID = (int)data[1];
				ICryptoTransform writer = null;
				Thread currentThread = Thread.CurrentThread;  
			lock(transforms) {
				if(!transforms.ContainsKey(currentThread)) {
				transforms.Add(currentThread,_rdale.CreateEncryptor());
				}
				writer = transforms[currentThread];
			}
                if (_test)
                {
                    pwriter = basestream;
                }

                byte[] output = new byte[buffer.Length];
			
				if(writer == null) {
				lock(syncObj) {
                writer = _rdale.CreateEncryptor();
				}
				}else {
				(writer as System.Security.Cryptography.RijndaelManagedTransform).Reset();
			
				
			}
                TransformMultiByte(buffer, output, writer);
				
				lock (basestream)
				{
					 basestream.Position = segID * (fragsize + boff);
                basestream.Write(output, 0, output.Length);
			}
			lock(pendingSegments) {
				pendingSegments.Remove(segID);
				}
				lock(syncObj) {
				pendingOperations--;
				cment.Set();
					if(pendingOperations == 0) {
					
					completeNtfy.Set();
						needsReset = true;
					
					
					}
				}
				
		}
#if !NO_CACHE
        Dictionary<int, byte[]> cachedsegments = new Dictionary<int, byte[]>();
#endif
        public static int MaxCacheSyze = 15;
		public void rawWrite(long position, byte[] data) {
		lock(basestream) {
			basestream.Position = position;
			basestream.Write(data,0,data.Length);
				basestream.Flush();
			}
			
		}
		public int rawRead(long position, byte[] data) {
		lock(basestream) {
			basestream.Position = 0;
				return basestream.Read(data,0,data.Length);
			}
		}
        public byte[] ReadSegment(int segID, Stream s)
        {
			if(segID>0) {
			if((segID) * (fragsize + boff)>=basestream.Length) {
					return new byte[fragsize];
				}
			}
			lock(pendingSegments) {
			if(pendingSegments.ContainsKey(segID)) {
				return pendingSegments[segID];
				}
			}
#if !NO_CACHE
			
            lock (cachedsegments)
            {
                if (cachedsegments.ContainsKey(segID))
                {

                    return cachedsegments[segID];
                }
            }
#endif
		
            lock (basestream)
            {
#if !NO_CACHE
                if (cachedsegments.Count > MaxCacheSyze)
                {
                    lock (cachedsegments)
                    {
                        Dictionary<int,byte[]>.Enumerator enumer = cachedsegments.GetEnumerator();
                        enumer.MoveNext();
                        enumer.Dispose();
                        cachedsegments.Remove(enumer.Current.Key);
                    }
                }
#endif
               

                basestream.Position = segID * (fragsize + boff);
                if (basestream.Length - basestream.Position <= 0)
                {
#if !NO_CACHE
                    lock (cachedsegments)
                    {
                        cachedsegments.Add(segID, new byte[fragsize]);
                    }
#endif
                    return new byte[fragsize];
                }
                else
                {


                    byte[] dgram = new byte[fragsize];
                    if (_test)
                    {
                        preader = basestream;
                    }


                    byte[] input = new byte[fragsize+boff];

                    basestream.Read(input, 0, input.Length);
                    reader = _rdale.CreateDecryptor();
                    byte[] output = new byte[fragsize+boff];
                 
                    TransformMultiByte(input, output, reader);
                    Buffer.BlockCopy(output, 0, dgram, 0, dgram.Length);
                    for (int i = 0; i < 4; i++)
                    {
                        //Chunk has not yet been allocated (nonzero garbage bytes, decryption failure). Return an empty chunk (all zeroes)
                        
                        if (output[i+fragsize] != 0)
                        {
#if !NO_CACHE
                            lock (cachedsegments)
                            {
                                cachedsegments.Add(segID, new byte[fragsize]);
                            }
#endif
                            return new byte[fragsize];
                        }
                    }
#if !NO_CACHE
                    lock (cachedsegments)
                    {
                        cachedsegments.Add(segID, dgram);
                    }
#endif
                    return dgram;

                }
            }
        }
        bool isrunning = true;
        ManualResetEvent threadtermevent = new ManualResetEvent(false);
        void Destroy() {
		if(pendingOperations>0) {
			Console.WriteLine("Waiting for IO flush");
				completeNtfy.WaitOne();
				
				Console.WriteLine("IO flush complete");
			}
			basestream.Flush();
            basestream.Close();
		}
		public void Dispose()
        {
			GC.SuppressFinalize(this);
			Destroy();
        }
    }
}

