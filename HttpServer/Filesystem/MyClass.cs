/*
 * Originally created by Brian Bosak
 */



/*UTF-32 uses 4 times as much space as ASCII
 * For the sake of political correctness, and to avoid lawsuits from
 * various civil rights groups, we will use UTF-32 anyways
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
namespace IC80v3
{
	public class FStream:Stream {
		long filehandle;
		Filesystem msys;
		public FStream(long file, Filesystem _msys) {
		msys = _msys;
			filehandle = msys.OpenFile(file);
			filename = file;
		}
		long filename;
		#region implemented abstract members of System.IO.Stream
		public override void Flush ()
		{
			//Not applicable.
		}

		public override long Seek (long offset, SeekOrigin origin)
		{
			throw new NotImplementedException ();
		}
        protected override void Dispose(bool disposing)
        {
            msys.CloseFile(filehandle);
            base.Dispose(disposing);
        }
		public override void SetLength (long value)
		{
			if(value>Length) {
				if(value-Length == 0) {
				Console.WriteLine("WARN:NOCHANGE");
				}
				msys.AllocSpace(filename,value-Length);
			}else {
			throw new NotSupportedException ("Shrinking a file is not supported on the IC80FS. Please consider deleting, and over-writing the file instead.");
			}
		}

		public override int Read (byte[] buffer, int offset, int count)
		{
            
			byte[] dgram;
			int data2read = count;
			int dataread = 0;
			while(data2read>0) {
                //TODO: Something's wrong here......
			dgram = msys.ReadBlock(filehandle,data2read);
			data2read-=dgram.Length;
            
				
				Buffer.BlockCopy(dgram,0,buffer,offset+dataread,dgram.Length);
				dataread+=dgram.Length;
                if (Length - (Position+dataread) == 0)
                {
                    break;
                }
			}
			cpos+=dataread;
			return dataread;
		}
		long cpos = 0;
		public override void Write (byte[] buffer, int offset, int count)
		{
			if(Length-Position<count) {
			//Increase stream size
				
				msys.AllocSpace(filename,(count-(Length-Position))+msys._seglen);
				
			}
			if(Length-Position<count) {
			throw new Exception("Whut?");
			}
			msys.WriteBlock(filehandle,buffer,offset,count);
			cpos+=count;
		}

		public override bool CanRead {
			get {
				return true;
			}
		}

		public override bool CanSeek {
			get {
				return true;
			}
		}

		public override bool CanWrite {
			get {
				return true;
			}
		}

		public override long Length {
			get {
				return msys.GetLen(filehandle);
			}
		}

		public override long Position {
			get {
				return cpos;
			}
			set {

                if(value>Length) {
                SetLength(value);
                }
                msys.Seek(filehandle, value);
                cpos = value;
               
			}
		}
		#endregion
	
	}
	
	
	
	public class Filesystem:IDisposable
	{
		Stream fstream;
        public long Data2Write
        {
            get {
                SeekableCryptoStream mstream = fstream as SeekableCryptoStream;
                if (mstream == null)
                {
					
                    return 0;
                }
                else
                {
                    return mstream.Data2Write;
                }
            }
        }
		class IC80Fragment {
			public long name;
			public long size;
			public long FragmentPosition;
			
			public bool hasNextFile = false;
			public bool haschanged = true;
			public void Read(BinaryReader mreader) {
				//UTF32 uses 4 bytes per character
                try
                {
                    FragmentPosition = mreader.BaseStream.Position;
                    name = mreader.ReadInt64();
                    size = mreader.ReadInt64();
                    hasNextFile = mreader.ReadBoolean();
                    haschanged = false;
                }
                catch (Exception er)
                {
                    //Rethrow due to treaty with natives
                    throw er;
                }
			}
			public void Serialize(BinaryWriter mwriter) {
			mwriter.Write(name);
				mwriter.Write(size);
				mwriter.Write(hasNextFile);
				
			}
			public static long GetSize() {
			return sizeof(long)+sizeof(long)+sizeof(bool);
			}
		}

      
        public void Commit()
        {
			BinaryWriter mwriter = new BinaryWriter(fstream);
		foreach(KeyValuePair<long,List<IC80Fragment>> et in fragments) {
           
			foreach(IC80Fragment ett in et.Value) {
				if(ett.haschanged) {
					ett.haschanged = false;
						fstream.Position = ett.FragmentPosition;
					ett.Serialize(mwriter);
					}
				}
			}
			mwriter.Flush();
		}
		public long FreeSpace {
		get {
				long total =0;
			foreach(IC80Fragment et in fragments[-1]) {
				total+=et.size;
				}
				return total;
			}
		}
		/// <summary>
		/// Commit the file if safety is enabled
		/// </summary>
		void commit() {
			Commit();
		}
		public Dictionary<long,List<long>> handleMappings = new Dictionary<long, List<long>>();
		public void DeleteFile(long filename) {
			if(handleMappings.ContainsKey(filename)) {
				
				foreach(long et in handleMappings[filename]) {
					fileHandles.Remove(et);
					}
				handleMappings.Remove(filename);
				
		Console.WriteLine("WARNING: Deleted file with open handles");
			}
			foreach(IC80Fragment et in fragments[filename]) {
			et.name = -1;
				
				et.haschanged = true;
				
			}
			fragments[-1].AddRange(fragments[filename]);
			fragments.Remove(filename);
			commit();
		}
        public bool HasFile(long filename)
        {
            lock (fragments)
            {
                return fragments.ContainsKey(filename);
            }
        }
		IC80Fragment GetAdjacentFragment(IC80Fragment src) {
		
			foreach(KeyValuePair<long,List<IC80Fragment>> et in fragments) {
			foreach(IC80Fragment ett in et.Value) {
				if(ett.FragmentPosition >= src.FragmentPosition+src.size+IC80Fragment.GetSize()) {
					return ett;
					}
				}
			}
			return null;
		}
		Dictionary<long,List<IC80Fragment>> fragments = new Dictionary<long, List<IC80Fragment>>();
		/// <summary>
		/// Allocs a contiguous region of free space
		/// </summary>
		/// <param name='file'>
		/// File name
		/// </param>
		/// <param name='len'>
		/// Length to allocate (in bytes)
		/// </param>
		public void AllocSpace(long file,long len) {
		
		
		if(!fragments.ContainsKey(file)) {
			fragments.Add(file,new List<IC80Fragment>());
			}
            lock(fragments[-1]) {
				
                if (fragments[file].Count > 0)
                {
                    IC80Fragment endfrag = null;
					long maxpos = -8;
					foreach(IC80Fragment et in fragments[file]) {
					if(et.FragmentPosition>maxpos) {
						maxpos = et.FragmentPosition;
							endfrag = et;
						}
					}
                    long lastfrag_end = endfrag.FragmentPosition + endfrag.size + IC80Fragment.GetSize();
                    foreach (IC80Fragment et in fragments[-1])
                    {
                    //TODO: This has been disabled for testing purposes
								break;
						bool foundbug = false;
                        if (et.FragmentPosition == lastfrag_end && et.size>len)
                        {
							
                            //Resize existing fragment, without creating a new one
							foreach(KeyValuePair<long,List<IC80Fragment>> fraglist in fragments) {
								
							foreach(IC80Fragment frag in fraglist.Value) {
								if(frag !=et) {
									if(et.FragmentPosition+len>=frag.FragmentPosition && et.size+et.FragmentPosition+len<=frag.size+frag.FragmentPosition+IC80Fragment.GetSize()) {
									
											foundbug = true;
											break;
									}
								}
								}
								if(foundbug) {
								break;
								}
							}
							if(foundbug) {
							break;
							}
                            et.FragmentPosition += len;
                            et.size -= len;
                            endfrag.size += len;
                            endfrag.haschanged = true;
                            et.haschanged = true;
							commit();
                            return;
                        }
                    }
                }
                foreach (IC80Fragment et in fragments[-1])
                {
                    if (et.size > len + IC80Fragment.GetSize()+IC80Fragment.GetSize())
                    {
						//Mark free space as being occupied by Wall Street
                        fragments[-1].Remove(et);
                        et.name = file;
                        et.haschanged = true;
						//Create new free democracy
                        IC80Fragment freefrag = new IC80Fragment();
                        freefrag.name = -1;
                        freefrag.haschanged = true;
						
                        //Compute fragment position. Immediately after this file
                        freefrag.FragmentPosition = len + et.FragmentPosition + IC80Fragment.GetSize();
                        //We use some free space for our file fragment, and also free space is used for the free space segment
						//itself.
						freefrag.size = et.size - len-IC80Fragment.GetSize();
						et.hasNextFile = true;
						IC80Fragment adjfrag = GetAdjacentFragment(freefrag);
						if(adjfrag !=null) {
							freefrag.hasNextFile = true;
						}

                        et.size = len;
						
                        fragments[file].Add(et);
                        
                        fragments[-1].Add(freefrag);
                     if(GetAdjacentFragment(et) == null) {
						throw new Exception("Well here's your problem!");
						}
                        break;
                    }
                }
			}
			commit();
			
			
		}
		
		public long GetLen(long fileHandle) {
			lock(fileHandles) {
				if(!fileHandles.ContainsKey(fileHandle)) {
				throw new Exception("Invalid handle");
				}
		return fileHandles[fileHandle].GetLen();
			}
		}
		long currentHandle = 0;
		object handleSync  = new object();
		/// <summary>
		/// Opens the specified file.
		/// </summary>
		/// <returns>
		/// A pointer to the file.
		/// </returns>
		/// <param name='fileID'>
		/// The file name to open
		/// </param>
		public long OpenFile(long fileID) {
		SegmentReader mreader = new SegmentReader(fstream,fragments[fileID],fileID);
        long keyval = 0;
        lock (fileHandles)
        {
                while (fileHandles.ContainsKey(keyval))
                {
                    keyval++;
                }
                fileHandles.Add(keyval, mreader);
            
            
				if(!handleMappings.ContainsKey(fileID)) {
				handleMappings.Add(fileID,new List<long>());
				}
				handleMappings[fileID].Add(keyval);
				
			return keyval;
            }
		}
		public void CloseFile(long fileHandle) {
            lock (fileHandles)
            {
               handleMappings[fileHandles[fileHandle]._fileid].Remove(fileHandle);
				if(handleMappings[fileHandles[fileHandle]._fileid].Count == 0) {
				handleMappings.Remove(fileHandles[fileHandle]._fileid);
				}
                fileHandles.Remove(fileHandle);
                
            }
		}
		public void WriteBlock(long fileHandle,byte[] data,int _offset, int count) {
			int offset = _offset;
			int dcount = count;
			while(offset<count) {
				int bytesread = fileHandles[fileHandle].WriteBlock(data,offset,dcount);
		    dcount-= bytesread;
				offset+=bytesread;
			}
		}
		Dictionary<long,SegmentReader> fileHandles = new Dictionary<long, SegmentReader>();
		/// <summary>
		/// Reads a contiguous block from the specified file.
		/// </summary>
		/// <returns>
		/// The block.
		/// </returns>
		/// <param name='fileHandle'>
		/// A file handle, obtained from a call to OpenFile
		/// </param>
		public byte[] ReadBlock(long fileHandle, int count) {
		return fileHandles[fileHandle].ReadBlock(count);
			
		}
        public void Seek(long filehandle, long position)
        {
            fileHandles[filehandle].seek(position);
        }
		public IEnumerable<long> EnumFiles() {
		List<long> retval = new List<long>();
			foreach(KeyValuePair<long,List<IC80Fragment>> et in fragments) {
			if(et.Key !=-1) {
				yield return et.Key;
				}
			}
		}
		internal int _seglen;
        public Filesystem(Stream basestream, string pswd, int seglen, long partitionLen)
        {
			_seglen = seglen;
            bool empty = basestream.Length <16389;
			
            Stream mstream = SeekableCryptoStream.CreateUltraSecureStream(pswd,seglen, basestream);
            fstream = mstream;
            if (empty)
            {

                BinaryWriter mwriter = new BinaryWriter(fstream);
                mwriter.Write("IC80v3");
                long cpos = fstream.Position;
                mwriter.Write(new char[256]);
                mwriter.Write(partitionLen);
                mwriter.Write(false);
                fragments.Add(-1, new List<IC80Fragment>());

                IC80Fragment lament = new IC80Fragment();
                lament.name = -1;
                lament.size = partitionLen;
                lament.FragmentPosition = cpos;
                fragments[-1].Add(lament);
                mwriter.Flush();

            }
            else
            {

                BinaryReader mreader = new BinaryReader(fstream);
				string xt = mreader.ReadString();
                if (xt != "IC80v3")
                {
                    throw new Exception("This library only supports IC80 version 3 file systems.");
                }
                while (true)
                {
                    IC80Fragment mf = new IC80Fragment();
                    mf.Read(mreader);

                    if (!fragments.ContainsKey(mf.name))
                    {
                        fragments.Add(mf.name, new List<IC80Fragment>());
                    }
                    fragments[mf.name].Add(mf);

                    if (!mf.hasNextFile)
                    {
                        break;
                    }else {
					fstream.Position +=mf.size;
					}
                }
            }
			FreeSpace.ToString();
        }
			
            public Filesystem (Stream basestream, int seglen, long partitionLen)
            {
		 bool empty = basestream.Length <16389;
		
            fstream = basestream;
            if (empty)
            {

                BinaryWriter mwriter = new BinaryWriter(fstream);
                mwriter.Write("IC80v3");
                long cpos = fstream.Position;
                mwriter.Write(new char[256]);
                mwriter.Write(partitionLen);
                mwriter.Write(false);
                fragments.Add(-1, new List<IC80Fragment>());

                IC80Fragment lament = new IC80Fragment();
                lament.hasNextFile = false;
				lament.name = -1;
                lament.size = partitionLen;
                lament.FragmentPosition = cpos;
                fragments[-1].Add(lament);
                mwriter.Flush();

            }
            else
            {

                BinaryReader mreader = new BinaryReader(fstream);
                if (mreader.ReadString() != "IC80v3")
                {
                    throw new Exception("This library only supports IC80 version 3 file systems.");
                }
                while (true)
                {
                    IC80Fragment mf = new IC80Fragment();
                    mf.Read(mreader);

                    if (!fragments.ContainsKey(mf.name))
                    {
                        fragments.Add(mf.name, new List<IC80Fragment>());
                    }
                    fragments[mf.name].Add(mf);

                    if (!mf.hasNextFile)
                    {
                        break;
                    }else {
					fstream.Position += mf.size;
					}
					
                }
            }
			FreeSpace.ToString();
        }
		
	class SegmentReader {
        internal long _fileid;
	public SegmentReader(Stream _basestream, List<IC80Fragment> _fragments, long fileID) {
				fragments = _fragments;
				basestream = _basestream;
                _fileid = fileID;		
    }
			Stream basestream;
			List<IC80Fragment> fragments;
            
			long segoffset = 0;
			int blockOffset = 0;
			/// <summary>
			/// Reads a block of contiguous data from the stream.
			/// </summary>
			/// <returns>
			/// The block.
			/// </returns>
			/// <param name='file'>
			/// The file to read from
			/// </param>
			/// <param name='count'>
			/// The number of bytes to read (at maximum) from the contiguous fragment
			/// </param>
			public byte[] ReadBlock(int count) {
				
				BinaryReader mreader = new BinaryReader(basestream);
				if(!(fragments.Count>blockOffset)) {
				return new byte[0];
				}
				basestream.Position = fragments[blockOffset].FragmentPosition+IC80Fragment.GetSize();
				
				//Read the fragment (or as much of it as required)
				//after seeking to segoffset
				basestream.Position+=segoffset;
				if(fragments[blockOffset].size-segoffset>=count) {
				    
					byte[] retval = new byte[count];
					
					basestream.Read(retval,0,retval.Length);
					segoffset+=retval.Length;
					return retval;
				}else {
				byte[] retval = new byte[fragments[blockOffset].size-segoffset];
					basestream.Read(retval,0,retval.Length);
					blockOffset++;
					segoffset=0;
					return retval;
				}
				
			}
            public void seek(long position)
            {
                long dpos = position;
                long cpos = 0;
                blockOffset = 0;
				segoffset = 0;
                while (dpos != cpos)
                {
                    if (fragments[blockOffset].size + cpos < dpos)
                    {
                        cpos += fragments[blockOffset].size;
                        blockOffset++;
                    }
                    else
                    {
                        segoffset = dpos - cpos;
                        cpos = dpos;
                    }
                    
                }
            }
			/// <summary>
			/// Writes the block.
			/// </summary>
			/// <returns>
			/// The number of bytes successfully written. If this is less than count, you should maybe try this function again.
			/// </returns>
			/// <param name='block'>
			/// Block.
			/// </param>
			/// <param name='count'>
			/// Count.
			/// </param>
			public int WriteBlock(byte[] block, int offset, int count) {
				int written = 0;
				basestream.Position = fragments[blockOffset].FragmentPosition+IC80Fragment.GetSize();
				basestream.Position+=segoffset;
				if(fragments[blockOffset].size-segoffset>=count) {
				basestream.Write(block,offset,count);
					written = count;
					segoffset+=written;
				} else {
					
				basestream.Write(block,offset,(int)(fragments[blockOffset].size-segoffset));
				written = (int)(fragments[blockOffset].size-segoffset);
					blockOffset++;
					segoffset = 0;
				}
				return written;
				
			}
			public long GetLen() {
				long total = 0;
			foreach(IC80Fragment et in fragments) {
				total+=et.size;
				}
				return total;
			}
		}

    public void Dispose()
    {
		this.fstream.Flush();
        this.fstream.Close();
    }
    }
}
