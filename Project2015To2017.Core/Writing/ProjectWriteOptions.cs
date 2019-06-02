using System;
using System.IO;

namespace Project2015To2017.Writing
{
	public class ProjectWriteOptions
	{
		/// <summary>
		/// Make backup copies of files before their modification or deletion
		/// </summary>
		public bool MakeBackups { get; set; }

		/// <summary>
		/// The operation to use to delete a file.
		/// Certain source control systems may require the deletion to be done using a special command
		/// </summary>
		public Action<FileSystemInfo> DeleteFileOperation { get; set; } = x => x.Delete();

		/// <summary>
		/// The operation to use to checkout a file (if required)
		/// </summary>
		public Action<FileSystemInfo> CheckoutOperation { get; set; } = _ => { };
	}
}
