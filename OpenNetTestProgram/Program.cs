using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tester;
using OpenNet;
namespace OpenNetTestProgram
{
    class Program
    {
        static string ReadPassword()
        {
            string thestring = "";
            while (true)
            {
                char thechar = Console.ReadKey(true).KeyChar;
                if (thechar.ToString() == "\b")
                {
                    try
                    {
                        thestring = thestring.Substring(0, thestring.Length - 1);
                        Console.Write("\b \b");
                    }
                    catch (Exception)
                    {
                    }
                }
                else
                {
                    if (thechar.ToString() == "\r")
                    {
                        break;
                    }
                    else
                    {
                        thestring += thechar.ToString();
                        Console.Write("*");
                    }
                }
            }
            return thestring;
        }
        static void Main(string[] args)
        {
            Console.WriteLine("Opening HTTP request");
            HttpWebRequest request = HttpWebRequest.Create("http://127.0.0.1/OpenNetProvider") as HttpWebRequest;
            request.AllowWriteStreamBuffering = false;
            Stream receiver = new TrashyStream(request.GetResponse().GetResponseStream());
            BinaryReader mreader = new BinaryReader(receiver);
            byte[] guid = mreader.ReadBytes(16);
            Console.WriteLine(BitConverter.ToString(guid));
            request = HttpWebRequest.Create("http://127.0.0.1/OpenNetProvider") as HttpWebRequest;
            request.Method = "POST";
            request.ContentLength = 9999999999;
            request.AllowWriteStreamBuffering = false;
            Stream sender = new TrashyStream(request.GetRequestStream());
            BinaryWriter mwriter = new BinaryWriter(sender);
            mwriter.Write(guid);
            mwriter.Flush();
            byte[] theirpubkey = mreader.ReadBytes(mreader.ReadInt32());
            Stream dbStr = File.Open("keyDB.db", FileMode.OpenOrCreate);
            Console.WriteLine("Enter system password");
            PubKeyDatabase db = new PubKeyDatabase(ReadPassword(), dbStr);
            if (db.Length == 0)
            {
                db.AddPublicKey(db.GenPublicPrivateKey(2048));
            }
            byte[] ourpublickey = db.GetPublicKeyOnly(db[0]);
            byte[] ourprivatekey = db[0];
            mwriter.Write(ourpublickey.Length);
            mwriter.Write(ourpublickey);
            mwriter.Flush();
            db.AddPublicKey(theirpubkey);
            Stream securedStream = new TrashyStream(db.CreateAuthenticatedStream(ourprivatekey, new DualStream(sender, receiver), 32));
            
            Console.WriteLine("Secure stream negotiated");

           driver  = new OpenNetProvider.OpenNetProtocolDriver(securedStream);
            Console.WriteLine("Driver initialized");
            driver.OpenStream();
           
            driver.onConnectionEstablished += new OpenNetProvider.OpenNetProtocolDriver.ConnectionEstablishedEventArgs(driver_onConnectionEstablished);
        }
        static OpenNetProvider.OpenNetProtocolDriver driver;
        static void driver_onConnectionEstablished(GlobalGridV12.XStream stream)
        {
            BinaryWriter mwriter = new BinaryWriter(stream);
            mwriter.Write(1);
            mwriter.Write("GET /JDeveloper/DevOS.htm HTTP/1.1");
            StreamReader mreader = new StreamReader(stream);
            while (true)
            {
				try {
                Console.WriteLine(mreader.ReadLine());
				}catch(Exception er) {
				Console.WriteLine(er);
				break;
				}
				
            }
			Console.WriteLine("Complete");
        }
    }
}
