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
        internal ClientWebRequest(Stream str, List<string> hed)
        {
            headers = hed;
            stream = str;
        }
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
        public string UnsanitizedRelativeURI
        {
            get
            {
                return headers[0].Split(" "[0])[1];

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
                throw new KeyNotFoundException("The specified value was not sent by the client");
            }
        }
        Dictionary<string, string> mpairs;
        string ParsePostValue(string value)
        {
            string mval = value.Replace("+", " ");
            return mval;
        }
        public Dictionary<string, string> Form
        {
            get
            {

                if (mpairs == null)
                {
					mpairs = new Dictionary<string, string>();
                    byte[] formvalue = null;

                    formvalue = new byte[ExpectedContentLength];
                    stream.Read(formvalue, 0, formvalue.Length);

                    MemoryStream mstream = new MemoryStream(formvalue);
                    StreamReader mreader = new StreamReader(mstream);
                    string txt = mreader.ReadToEnd();
                    string[] vals = txt.Split('&');
                    foreach (string et in vals)
                    {
                        string key = et.Substring(0, et.IndexOf("="));
                        string value = ParsePostValue(et.Substring(et.IndexOf("=") + 1));
                        mpairs.Add(key, value);
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
        string ReadLine(BinaryReader mreader)
        {
            StringBuilder mb = new StringBuilder();
            while (true)
            {
                char mchar = (char)mreader.Read();
                if (mchar == '\n')
                {
                    return mb.ToString();
                }
                mb.Append(mchar);
            }
        }
		public static bool resetConnections = false;
        TcpListener mlist;
        void onclientconnect(object sender)
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
                    if (txt.Length < 3)
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
        public bool isrunning = true;
        public HttpListener(int port)
        {
            mlist = new TcpListener(IPAddress.Any, port);
            mlist.Start();
            System.Threading.Thread mthread = new System.Threading.Thread(thetar);
            mthread.Start();
        }

    }
}
