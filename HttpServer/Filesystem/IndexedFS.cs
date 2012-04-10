/*
 * Work items:
 * TODO: Create a volume shadow copy service, to create backups of files automatically
 * and manage revisions of files
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
namespace IC80v3
{
   public class IndexedFS
    {
		
		
        class ObservableStream : Stream
        {
            public ObservableStream(long fid, Filesystem msys)
            {
                _msys = msys;
                _fid = fid;
                lock (msys)
                {
                    _basestream = new FStream(fid, msys);
                }
            }
            long _fid;
            public delegate void FileUpdateEventArgs(ObservableStream stream,long fpos, byte[] data);
            public event FileUpdateEventArgs OnDataWritten;
            public event FileUpdateEventArgs OnFileCommit;
            FStream _basestream;
            Filesystem _msys;
            
            protected override void Dispose(bool disposing)
            {
                if (OnFileCommit != null)
                {
                    OnFileCommit.Invoke(this, Position, null);

                }
                lock (_msys)
                {
                    _basestream.Close();
                }
                base.Dispose(disposing);
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
				lock(_msys) {
				_basestream.Flush();
				}
            }
				

            public override long Length
            {
                get { return _basestream.Length; }
            }

            public override long Position
            {
                get
                {
                    return _basestream.Position;
                }
                set
                {
                    lock (_msys)
                    {
                        _basestream.Position = value;
                    }
                }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                lock (_msys)
                {
                    return _basestream.Read(buffer, offset, count);
                }
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                lock (_msys)
                {
                    return _basestream.Seek(offset, origin);
                }
            }

            public override void SetLength(long value)
            {
                lock (_msys)
                {
                    _basestream.SetLength(value);
                }
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                byte[] bc = new byte[count];
                Buffer.BlockCopy(buffer, offset, bc, 0, count);
                if (OnDataWritten != null)
                {
                    OnDataWritten.Invoke(this, Position, buffer);
                }
                lock (_msys)
                {
                    _basestream.Write(buffer, offset, count);
                }
            }
        }


        Filesystem _msys;

        public List<string> Files
        {
            get
            {
                List<string> rval = new List<string>();
                lock (filemappings)
                {
                    foreach (KeyValuePair<string, long> et in filemappings)
                    {
                        rval.Add(et.Key);
                    }
                }
                return rval;
            }
        }
        public List<string> Directories
        {
            get
            {
            
                List<string> rval = new List<string>();
                lock (dirmappings)
                {
                    foreach (KeyValuePair<string, long> et in dirmappings)
                    {
                        rval.Add(et.Key);
                    }
                }
                return rval;
            
            }
        }
        public IndexedFS OpenDir(string dirname)
        {
            IndexedFS ms = new IndexedFS(new Filesystem(new ObservableStream(dirmappings[dirname],_msys),16384, _msys.FreeSpace));
            ms.parent = this;
            ms.name = dirname;
            return ms;
        }
        public void Delete(string filename)
        {
            lock (filemappings)
            {
                lock (_msys)
                {
                    _msys.DeleteFile(filemappings[filename]);
					lock(filemappings) {
						
                    filemappings.Remove(filename);
					}
					
                }
            }
			Commit();
        }
		public long FreeSpace {
		get {
			lock(_msys) {
				return _msys.FreeSpace;
				}
			}
		}
        public void CreateFile(string filename)
        {
            lock (filemappings)
            {
                if (filemappings.ContainsKey(filename))
                {
                        throw new IOException("Allocation failed -- File already exists");
                    
                }
                else
                {
                    lock (_msys)
                {
                    while (_msys.HasFile(cval))
                    {
                        cval++;
                    }
                    if (_msys.HasFile(cval))
                    {
                        throw new IOException("File allocation error - Critical");
                    }
                    _msys.AllocSpace(cval, 16384);
                }
                    filemappings.Add(filename, cval);
                }
                
                    cval++;
            }
        }
        public void CreateDirectory(string dirname)
        {
            lock (dirmappings)
            {
				lock (_msys)
                {
					while(_msys.HasFile(cval)) {
					cval++;
					}
                dirmappings.Add(dirname, cval);
                
                    _msys.AllocSpace(cval, 16384);
                }
                cval++;
            }
        }
        long cval = 1;
        public Stream OpenFile(string filename)
        {
            return new ObservableStream(filemappings[filename],_msys);
        }
        public string name = "/";
        public void Commit()
        {
            lock (dirmappings)
            {
                lock (filemappings)
                {
                    lock (_msys)
                    {
                        Stream bstr = new ObservableStream(0, _msys);
                        BinaryWriter mwriter = new BinaryWriter(bstr);
                        mwriter.Write(filemappings.Count+dirmappings.Count);

                        foreach (KeyValuePair<string, long> et in filemappings)
                        {
                            mwriter.Write(true);
                            mwriter.Write(et.Key);
                            mwriter.Write(et.Value);
                        }
                        foreach (KeyValuePair<string, long> et in dirmappings)
                        {
                            mwriter.Write(false);
                            mwriter.Write(et.Key);
                            mwriter.Write(et.Value);

                        }
                        _msys.Commit();
						bstr.Close();
                    }
                }
            }
			if(parent !=null) {
			parent.Commit();
			}
        }
        public IndexedFS parent;
        Dictionary<string, long> filemappings = new Dictionary<string, long>();
        Dictionary<string, long> dirmappings = new Dictionary<string, long>();
        void Destroy() {
			lock(_msys) {
		_msys.Dispose();
			}
		}
		public void Dispose()
        {
			Destroy();
			GC.SuppressFinalize(this);
        }
        public IndexedFS(Filesystem msys)
        {
            _msys = msys;
            if (!msys.EnumFiles().GetEnumerator().MoveNext())
            {
            //Create index
                msys.AllocSpace(0,512);
                Stream mstream = new ObservableStream(0, msys);
                BinaryWriter mwriter = new BinaryWriter(mstream);
                
                mwriter.Write(filemappings.Count);
                cval++;
            }
            ObservableStream fstr = new ObservableStream(0, msys);
            BinaryReader mreader = new BinaryReader(fstr);
            int count = mreader.ReadInt32();
           
            for (int i = 0; i < count; i++)
            {
				
                if (mreader.ReadBoolean())
                {
                    filemappings.Add(mreader.ReadString(), mreader.ReadInt64());
                }
                else
                {
                    dirmappings.Add(mreader.ReadString(), mreader.ReadInt64());

                }
                
                cval++;
            }
			
        }
		
    }
}
