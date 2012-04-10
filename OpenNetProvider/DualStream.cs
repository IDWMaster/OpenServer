using System;
using System.IO;
namespace OpenNet
{
	public class DualStream:Stream
	{
		Stream _writer;
		Stream _reader;
		public DualStream (Stream writer, Stream reader)
		{
			_reader = reader;
			_writer = writer;
		}

		#region implemented abstract members of System.IO.Stream
		public override void Flush ()
		{
			_writer.Flush();
		}

		public override long Seek (long offset, SeekOrigin origin)
		{
			throw new NotImplementedException ();
		}

		public override void SetLength (long value)
		{
			throw new NotImplementedException ();
		}

		public override int Read (byte[] buffer, int offset, int count)
		{
			return _reader.Read(buffer,offset,count);
				
		}

		public override void Write (byte[] buffer, int offset, int count)
		{
			_writer.Write(buffer,offset,count);
		}

		public override bool CanRead {
			get {
				return true;
			}
		}

		public override bool CanSeek {
			get {
				return false;
			}
		}

		public override bool CanWrite {
			get {
				return true;
			}
		}

		public override long Length {
			get {
				throw new NotImplementedException ();
			}
		}

		public override long Position {
			get {
				throw new NotImplementedException ();
			}
			set {
				throw new NotImplementedException ();
			}
		}
		#endregion
	}
}

