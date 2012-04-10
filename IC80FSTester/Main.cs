#define use_crypto
using System;
using IC80v3;
using System.IO;
namespace IC80FSTester
{
	class MainClass
	{

		public static void Main (string[] args)
		{
			Stream mstream = File.Open("fs",FileMode.Create);
			byte[] tbuff = new byte[16384];
			IndexedFS tfs = new IndexedFS(new Filesystem(mstream,16384,1024*1024*500));
		    int i = 0;
			while(true) {
				Console.WriteLine(i);
			tfs.CreateFile("somefile");
				Stream stream = tfs.OpenFile("somefile");
			Console.WriteLine("Free space: "+tfs.FreeSpace.ToString());
				if(i ==2) {
				Console.WriteLine();
				}
				IndexedFS mfs = new IndexedFS(new Filesystem(SeekableCryptoStream.CreateUltraSecureStream("password",16384,stream),16384,1024*1024*50));
				mfs.Dispose();
				tfs.Delete("somefile");
				Console.WriteLine("Free space should be :"+tfs.FreeSpace.ToString());
				tfs.Dispose();
				mstream = File.Open("fs",FileMode.Open);
				tfs = new IndexedFS(new Filesystem(mstream,16384,0));
				i++;
			}
		}
	}
}
