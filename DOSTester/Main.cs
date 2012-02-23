using System;
using System.Net;
using System.Net.Sockets;
namespace DOSTester
{
	class MainClass
	{
		static void thetar() {
		WebClient mclient = new WebClient();
			while(true) {
			try {
				mclient.DownloadData(new Uri("http://127.0.0.1:82/ServerConfigurationManager/backgroundImg.jpg"));
				}catch(Exception er) {
				Console.WriteLine("URL request failed.");
				}
				}
		}
		public static void Main (string[] args)
		{
			for(int i = 0;i<30;i++) {
			System.Threading.Thread mthread = new System.Threading.Thread(thetar);
				
				mthread.Start();
			}
		}
	}
}
