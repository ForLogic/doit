using ICSharpCode.SharpZipLib.Zip;
using Microsoft.WindowsAzure.Storage;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DoIt.Functions
{
	internal class ZipFunctions : FunctionsNodeHandlerBase
	{
		public ZipFunctions(XmlNode node):base(node){}

		public override bool Execute()
		{
			if (Node == null)
				return false;
			var path = Program.Shared.ReplaceTags(Util.GetStr(Node, "path"));
			var mode = Util.GetStr(Node, "mode", "write").ToLower();
			var toVar = Util.GetStr(Node, "toVar");
			if (!string.IsNullOrEmpty(toVar))
				lock (Program.Shared.LockVariables) { Program.Shared.Variables[toVar + ";" + Program.Shared.GetSequence()] = path; }
			switch (mode){
				case "read": ReadFile(path, Node); break;
				case "write": WriteFile(path, Node); break;
			}
			return true;
		}

		void ReadFile(string path, XmlNode node){
			if (!File.Exists(path))
				return;
			foreach (var n in Util.GetChildNodes(node, "Extract")){
				var toFolder = Program.Shared.ReplaceTags(Util.GetStr(n, "toFolder"));
				var counter = 0;
				using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
				using (var zipStream = new ZipInputStream(fs)){
					var entry = zipStream.GetNextEntry();
					while (entry != null){
						var file = Path.Combine(toFolder, entry.Name);
						var folder = Path.GetDirectoryName(file);
						if (!Directory.Exists(folder))
							Directory.CreateDirectory(folder);
						using (var newFs = File.Create(file))
							zipStream.CopyTo(newFs);
						entry = zipStream.GetNextEntry();
						counter++;
					}
				}
				Program.Shared.WriteLogLine("Zip file extracted to folder {0} (Files: {1})", toFolder, counter);
			}
		}

		void WriteFile(string path, XmlNode node){
			if (File.Exists(path))
				File.Delete(path);
			Program.Shared.WriteLogLine("Creating zip file: {0}", path);
			using (var zipStream = new ZipOutputStream(File.Create(path))){

				// folder
				//foreach (var n in GetChildNodes(node, "Folder")){
				//	var name = ReplaceTags(GetStr(n, "name"));
				//	var toFile = ReplaceTags(GetStr(n, "toFile"));
				//	var toFolder = ReplaceTags(GetStr(n, "toFolder"));
				//	var pattern = GetStr(n, "pattern") ?? "*.*";
				//	var deleteSource = GetStr(n, "deleteSource", "false").ToLower() == "true";
				//	var multiFiles = GetStr(n, "multiFiles") == "true";
				//	var lstZipFiles = ZipFolderAs(name, toFile, toFolder, multiFiles, pattern, deleteSource);
				//	foreach (var zip in lstZipFiles){
				//		ZipFiles.Add(zip);
				//		WriteLogLine(String.Format("Finished Zipping Folder (Folder: {0}; ZipFile: {1}; Size: {2}).", name, zip, GetFileSize(new FileInfo(zip).Length)));
				//	}
				//}

				var lstNodes = Util.GetChildNodes(node, "AddFile", "AddBlob");
				foreach (var n in lstNodes)
					switch (n.Name.ToLower()){
						case "addfile": AddFile(zipStream, n); break;
						case "addblob": AddBlob(zipStream, n); break;
					}

				zipStream.Finish();
			}
			Program.Shared.WriteLogLine("Finished creating zip file {0} (Size: {1})", path, Util.GetFileSize(new FileInfo(path).Length));
		}

		string GetZipEntry(XmlNode n, Dictionary<string, DataRow> lstCurrentRows, string defaultValue){
			var zipEntry = Program.Shared.ReplaceTags(Util.GetStr(n, "zipEntry"), lstCurrentRows);
			var zipFolder = Program.Shared.ReplaceTags(Util.GetStr(n, "zipFolder"), lstCurrentRows);
			var zipFilename = Program.Shared.ReplaceTags(Util.GetStr(n, "zipFilename"), lstCurrentRows);
			if (!string.IsNullOrEmpty(zipEntry))
				zipEntry = zipEntry.Split(new char[]{'/','\\'}, StringSplitOptions.RemoveEmptyEntries).Select(s => s.GetFileName()).Concat(s => s, "/");
			else if (!string.IsNullOrEmpty(zipFilename)){
				if(!string.IsNullOrEmpty(zipFolder))
					zipFolder = zipFolder.Split(new char[]{'/','\\'}, StringSplitOptions.RemoveEmptyEntries).Select(s => s.GetFileName()).Concat(s => s, "/");
				zipEntry = string.IsNullOrEmpty(zipFolder) ? zipFilename : zipFolder+"/"+zipFilename.GetFileName();
			}
			if (string.IsNullOrEmpty(zipEntry))
				zipEntry = defaultValue;
			if (string.IsNullOrEmpty(zipEntry))
				return null;
			return string.Join("/", zipEntry.Split(new char[]{Path.DirectorySeparatorChar,'/'},StringSplitOptions.RemoveEmptyEntries).Select(str => str.GetFileName()));
		}

		void AddFile(ZipOutputStream zipStream, XmlNode n){
			var forEach = Util.GetStr(n, "forEach");
			var where = Program.Shared.ReplaceTags(Util.GetStr(n, "where"));
			var dt = string.IsNullOrEmpty(forEach) ? null : Program.Shared.GetDataTable(Program.Shared.ThreadID(), forEach);
			var lstRows = dt==null ? new DataRow[0] : string.IsNullOrEmpty(where) ? dt.Rows.Cast<DataRow>().ToArray() : dt.Select(where);
			var rowsCount = dt == null ? 1 : lstRows.Length;
			for(var x=0; x<rowsCount; x++){
				var lstCurrentRows = dt == null ? null : new Dictionary<string, DataRow>(){{forEach,lstRows[x]}};
				var name = Program.Shared.ReplaceTags(Util.GetStr(n, "name"), lstCurrentRows);
				var deleteSource = Util.GetStr(n, "deleteSource", "false").ToLower() == "true";
				var zipEntry = GetZipEntry(n, lstCurrentRows, Path.GetFileName(name));
				var fi = new FileInfo(name);
				zipStream.PutNextEntry(new ZipEntry(zipEntry){DateTime=fi.LastWriteTime, Size=fi.Length});
				using (var fs = new FileStream(name, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					fs.CopyTo(zipStream);
				if (deleteSource)
					File.Delete(name);
				Program.Shared.WriteLogLine(String.Format("Add File to Zip (File: {0}; File Size: {1}).", name, Util.GetFileSize(fi.Length)));
			}
		}

		void AddBlob(ZipOutputStream zipStream, XmlNode n){
			var fromStorage = Util.GetStr(n, "fromStorage");
			var forEach = Util.GetStr(n, "forEach");
			var where = Program.Shared.ReplaceTags(Util.GetStr(n, "where"));
			var dt = string.IsNullOrEmpty(forEach) ? null : Program.Shared.GetDataTable(Program.Shared.ThreadID(), forEach);
			var lstRows = dt==null ? new DataRow[0] : string.IsNullOrEmpty(where) ? dt.Rows.Cast<DataRow>().ToArray() : dt.Select(where);
			var rowsCount = dt == null ? 1 : lstRows.Length;
			for(var x=0; x<rowsCount; x++){
				var lstCurrentRows = dt == null ? null : new Dictionary<string, DataRow>(){{forEach,lstRows[x]}};
				var snapshotTime = Util.ParseDateTime(Program.Shared.ReplaceTags(Util.GetStr(n, "snapshotTime"), lstCurrentRows));
				var blobClient = CloudStorageAccount.Parse(Program.Shared.Storages[fromStorage]).CreateCloudBlobClient();
				var blobName = Program.Shared.ReplaceTags(Util.GetStr(n, "name"), lstCurrentRows);
				var blobContainer = blobClient.GetContainerReference(blobName.Remove(blobName.IndexOf("/")));
				var blob = blobContainer.GetBlockBlobReference(blobName.Substring(blobName.IndexOf("/")+1), new DateTimeOffset(snapshotTime.Value, TimeSpan.Zero));
				var dateTime = Util.ParseDateTime(Program.Shared.ReplaceTags(Util.GetStr(n, "dateTime"), lstCurrentRows));
				var size = Convert.ToInt64(Program.Shared.ReplaceTags(Util.GetStr(n, "size", "0"), lstCurrentRows));
				var zipEntry = GetZipEntry(n, lstCurrentRows, blob.Name);
				if (!blob.Exists())
					continue;
				if (size == 0){
					blob.FetchAttributes();
					dateTime = blob.Properties.LastModified.Value.DateTime;
					size = blob.Properties.Length;
				}
				zipStream.PutNextEntry(new ZipEntry(zipEntry){DateTime=dateTime??DateTime.Now, Size=size});
				blob.DownloadToStream(zipStream);
				Program.Shared.WriteLogLine(String.Format("Add Blob to Zip (Blob: {0}; Blob Size: {1}).", blob.Uri.ToString(), Util.GetFileSize(size)));
			}
		}
	}
}
