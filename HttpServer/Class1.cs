using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
namespace HttpServer
{
    /// <summary>
    /// Represents an HTTP Response.
    /// </summary>
    public class ClientHttpResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Server.Interop.ClientHttpResponse"/> class.
        /// </summary>
        public ClientHttpResponse()
        {

        }
        /// <summary>
        /// The HTTP status code.
        /// </summary>
        public string StatusCode = "200 OK";
        /// <summary>
        /// The Content-Length.
        /// </summary>
        public long len = 0;
        /// <summary>
        /// The type of the content (Content-Type header).
        /// </summary>
        
		public string ContentType;
        /// <summary>
        /// Writes the HTML header.
        /// </summary>
        /// <param name='stream'>
        /// The stream to write the HTML header to and flushes the output stream
        /// </param>
        public void WriteHeader(Stream stream)
        {
            StreamWriter mwriter = new StreamWriter(stream);
            mwriter.Write("HTTP/1.1 " + StatusCode + "\r\n");
            if(!String.IsNullOrEmpty(ContentType)) {
			mwriter.Write("Content-Type: " + ContentType + "\r\n");
            mwriter.Write("Content-Length: " + len.ToString() + "\r\n");
			}
				mwriter.Write("server: IDWOS 2012 (Virtual OS)\r\n");
            foreach(string et in AdditionalHeaders) {
			mwriter.Write(et+"\r\n");
			}
			mwriter.Write("\r\n");
            mwriter.Flush();

        }
		public void AddHeader(string header) {
			AdditionalHeaders.Add(header);
		}
		List<string> AdditionalHeaders = new List<string>();
		/// <summary>
		/// Redirect the user to the specified location.
		/// </summary>
		/// <param name='location'>
		/// The location to redirect to.
		/// </param>
		/// <param name='permanent'>
		/// Whether or not the redirection is permanent.
		/// </param>
		public void Redirect(string location, bool permanent, Stream destination) {
			if(permanent) {
			StatusCode = "301 Moved Permanently";
			
			}else {
			StatusCode = "307 Temporary Redirect";
			}
			AdditionalHeaders.Add("Location: "+location);
			MemoryStream mstream = new MemoryStream();
			StreamWriter mwriter = new StreamWriter(mstream);
			mwriter.Write("<html><head><title>Redirect notice</title></head><body>This page has moved to <a href=\""+location+"\">here</a></body></html>");
			mwriter.Flush();
			mstream.Position = 0;
			ContentType = "text/html";
			len = mstream.Length;
			WriteHeader(destination);
			WriteStream(mstream,destination,16384);
			
		}
        /// <summary>
        /// Copies the contents of one stream to another stream and flushes the destination stream
        /// </summary>
        /// <param name="source">The source stream</param>
        /// <param name="destination">The destination stream</param>
        /// <param name="buffersize">The amount of data (in bytes) to buffer</param>
        public void WriteStream(Stream source, Stream destination, int buffersize)
        {
            byte[] buffer = new byte[buffersize];
            while (true)
            {
                int bytesread = source.Read(buffer, 0, buffer.Length);
                if (bytesread < 1)
                {
                    break;
                }
                destination.Write(buffer, 0, bytesread);

            }
            destination.Flush();
        }

    }
    /// <summary>
    /// Represents a web request from a client
    /// </summary>
    public class ClientWebRequest:MarshalByRefObject
    {
        /// <summary>
        /// The stream between the client and the server
        /// </summary>
        public Stream stream;
        /// <summary>
        /// The HTTP request headers sent by the client
        /// </summary>
        public List<string> headers = new List<string>();
        /// <summary>
        /// Creates a new ClientWeb request from a given stream and list of headers
        /// </summary>
        /// <param name="str">The stream to the client</param>
        /// <param name="hed">The HTTP headers</param>
        public ClientWebRequest(Stream str, List<string> hed)
        {
            headers = hed;
            stream = str;
        }
		bool isSecureConnection = false;
		public bool SecureConnection {
			get {
			return isSecureConnection;
			}set {
			isSecureConnection = value;
			}
		}
		string sid;
		public string SecurityIdentifier {
		get {
			return sid;
			}set {
			sid = value;
			}
		}
		public void SetUnmanagedConnection() {
		UnmanagedConnection = true;
		}
		public bool UnmanagedConnection = false;
		public bool ContinueProcessing = true;
		Dictionary<string,string> cachedQueryString = null;
        public Dictionary<string, string> QueryString
        {
            get
            {
				if(cachedQueryString == null) {
                Dictionary<string, string> mdict = new Dictionary<string, string>();
                Uri mri = new Uri("http:/" + UnsanitizedRelativeURI);
                string[] querystrings = mri.Query.Split('&');
                foreach (string et in querystrings)
                {
                    if (et.Length > 0)
                    {
                        mdict.Add(et.Replace("?", "").Substring(0, et.Replace("?", "").IndexOf("=")), Uri.UnescapeDataString(et.Replace("?", "").Substring(et.Replace("?", "").IndexOf("=") + 1)));

                    }
					}
					cachedQueryString = mdict;
                return cachedQueryString;
				}else {
				return cachedQueryString;
				}
            }
        }
        public int ExpectedContentLength
        {
            get
            {
                return Convert.ToInt32(this["Content-Length"]);
            }
        }
        public string Method
        {
            get
            {
                return headers[0].Split(" "[0])[0];
            }
        }
		bool proxy = false;
		public bool ProxyConnection {
		get {
			return proxy;
			}
		}
        public string UnsanitizedRelativeURI
        {
            get
            {
				if(!headers[0].Contains("http://")) {
                return headers[0].Split(" "[0])[1];
				}else {
					proxy = true;
				return new Uri(headers[0].Split(' ')[1]).ToString().Replace("http://","/");
				}

            }
        }
        /// <summary>
        /// Gets the requested header
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public string this[string value]
        {
            get
            {
                foreach (string et in headers)
                {
                    string[] partitions = et.Split(":"[0]);
                    if (partitions[0].ToLower() == value.ToLower())
                    {
                        return partitions[1];
                    }
                }
            return null;    
			}
        }
        Dictionary<string, string> mpairs;
        string ParsePostValue(string value)
        {
            string mval = value;
            return mval;
        }
		bool compareAt(int offset, byte[] data, byte[] comparison) {
		int x = 0;
			for(int i = offset;x<comparison.Length;i++) {
				if(i>=data.Length) {
				return false;
				}
			if(comparison[x] != data[i]) {
				return false;
				}
				x++;
			}
			return true;
		}
		
		public Stream GetUploadedFile(out long start, out long end) {
		//Save entire request to disk
			byte[] buffer = new byte[16384];
			Stream fstream = File.Open(Path.GetTempFileName(),FileMode.Open);
			while(true) {
				
			int count = stream.Read(buffer,0,16384);
				fstream.Write(buffer,0,count);
				if(fstream.Length == ExpectedContentLength) {
				break;
				}
			if(count<1) {
				break;
				}
				
			}
			//Is this really the best way to do it? This is gonna be terribly slow.
			//Unfortunately; it seems that HTTP uploading isn't well documented.
			
			BinaryReader mreader = new BinaryReader(fstream);
		    fstream.Position = 0;
		   HttpListener.ReadLine(mreader).Replace("-","").Replace("\r","").Replace("\n","");
		   long ct = fstream.Position-2;
			fstream.Position = 0;
			byte[] tempbuff = new byte[ct];
			fstream.Read(tempbuff,0,(int)ct);
			HttpListener.ReadLine(mreader);
			while(HttpListener.ReadLine(mreader).Length>1) {
			}
			start = fstream.Position;
			while(true) {
				long fpos = fstream.Position;
			fstream.Read(buffer,0,buffer.Length);
				for(int i = 0;i<buffer.Length;i++) {
				if(buffer[i] == tempbuff[0]) {
					if(compareAt(i,buffer,tempbuff)) {
						end = i+fpos;
							return fstream;
						}
					}
				}
			if(fstream.Length == fstream.Position) {
				throw new Exception("Illegal operation.");
				}
			}
			
		}
	
		/// <summary>
		/// Gets the form.
		/// </summary>
		/// <value>
		/// The form.
		/// </value>
        public Dictionary<string, string> Form
        {
            get
            {

                if (mpairs == null)
                {
					mpairs = new Dictionary<string, string>();
                    byte[] formvalue = null;

                    formvalue = new byte[ExpectedContentLength];
                    int cpos = 0;
					while(cpos != formvalue.Length) {
					int count = stream.Read(formvalue, cpos, formvalue.Length);
					if(count == 0) {
						break;
						}
						cpos+=count;
					}
                    MemoryStream mstream = new MemoryStream(formvalue);
                    StreamReader mreader = new StreamReader(mstream);
                    string txt = mreader.ReadToEnd();
                    string[] vals = txt.Split('&');
                    foreach (string et in vals)
                    {
                        string key = et.Substring(0, et.IndexOf("="));
                        string value = ParsePostValue(et.Substring(et.IndexOf("=") + 1));
                        mpairs.Add(key, Uri.UnescapeDataString(value));
                    }
                }
                return mpairs;
            }
        }
    }
    public class HttpListener
    {
        public delegate void ConnectEventDgate(ClientWebRequest request);
        public event ConnectEventDgate onClientConnect;
        internal static string ReadLine(BinaryReader mreader)
        {
            StringBuilder mb = new StringBuilder();
            while (true)
            {
                char mchar = (char)mreader.Read();
				if(mchar == 65535) {
				return mb.ToString();
				}
                if (mchar == '\n')
                {
                    return mb.ToString();
                }
                mb.Append(mchar);
            }
        }
		public static bool resetConnections = false;
        TcpListener mlist;
        public void onclientconnect(object sender)
        {
			
            Stream mstream = sender as Stream;
            if(resetConnections) {
			try {
					mstream.Dispose();
				}catch(Exception) {
				}
			}
			try
            {
                //Parse web request
                BinaryReader mreader = new BinaryReader(mstream);

                List<string> headers = new List<string>();

                while (true)
                {
                    string txt = ReadLine(mreader);
                    if (txt.Length < 2)
                    {
                        break;
                    }
                    headers.Add(txt);

                }

                ClientWebRequest mreq = new ClientWebRequest(mstream, headers);
                using (mreq.stream)
                {
                    if (onClientConnect != null)
                    {
                        onClientConnect.Invoke(mreq);
                    }
                }
            }
            catch (Exception)
            {
            }
        }
        void thetar()
        {
            while (isrunning)
            {
                try
                {
                    System.Threading.ThreadPool.QueueUserWorkItem(onclientconnect, mlist.AcceptTcpClient().GetStream());
                }
                catch (Exception er)
                {
                
                }
                }
        }
		public static HttpListener LastListener;
        public bool isrunning = true;
        public HttpListener(int port)
        {
			LastListener = this;
            mlist = new TcpListener(IPAddress.Any, port);
            mlist.Start();
            System.Threading.Thread mthread = new System.Threading.Thread(thetar);
            mthread.Start();
        }

    }
}
