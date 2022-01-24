using ICSharpCode.SharpZipLib.Zip;
using LumenWorks.Framework.IO.Csv;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using static DoIt.Program;

namespace DoIt.Functions
{
	internal class StorageFunctions : FunctionsNodeHandlerBase
	{
		public StorageFunctions(XmlNode node):base(node){}

		public override bool Execute()
		{
			if (Node == null)
				return false;
			var id = Util.GetStr(Node, "id");
			//if (string.IsNullOrEmpty(id))
			//	return false;
			var lstNodes = Util.GetChildNodes(Node, "Upload", "Download", "ListBlobs", "ListContainers", "Copy", "SetMetadata", "Snapshot", "DeleteMultipleBlobs", "DeleteBlob", "DeleteSnapshot", "DeleteContainers");
			foreach (var n in lstNodes)
				switch (n.Name.ToLower()){
					case "upload": Upload(id, n); break;
					case "download": Download(id, n); break;
					case "listblobs": ListBlobs(id, n); break;
					case "listcontainers": ListContainers(id, n); break;
					case "copy": Copy(id, n); break;
					case "setmetadata": SetMetadata(id, n); break;
					case "snapshot": Snapshot(id, n); break;
					case "deletemultipleblobs": DeleteMultipleBlobs(id, n); break;
					case "deleteblob": DeleteBlob(id, n); break;
					case "deletesnapshot": DeleteSnapshot(id, n); break;
					case "deletecontainers": DeleteContainers(id, n); break;
				}
			return true;
		}

		// upload
		void Upload(string id, XmlNode n){
			var file = Program.Shared.ReplaceTags(Util.GetStr(n, "file"));
			var folder = Program.Shared.ReplaceTags(Util.GetStr(n, "folder"));
			var toBlob = Program.Shared.ReplaceTags(Util.GetStr(n, "toBlob"));
			var toFolder = Program.Shared.ReplaceTags(Util.GetStr(n, "toFolder"));
			var pattern = Util.GetStr(n, "pattern", "*.*");
			var deleteSource = Util.GetStr(n, "deleteSource", "false").ToLower() == "true";
			var async = Util.GetStr(n, "async", "false").ToLower() == "true";
			var lstFiles = string.IsNullOrEmpty(file) && !string.IsNullOrEmpty(folder) ? Util.GetFiles(folder, pattern) : new string[]{file};
			var blobClient = CloudStorageAccount.Parse(Program.Shared.Storages[id]).CreateCloudBlobClient();
			var blobContainer = blobClient.GetContainerReference((toBlob??toFolder).Remove(toBlob.IndexOf("/")));
			if (blobContainer.CreateIfNotExists())
				blobContainer.SetPermissions(new BlobContainerPermissions(){PublicAccess=BlobContainerPublicAccessType.Off});
			foreach (var f in lstFiles){
				if (!File.Exists(f)){
					Program.Shared.WriteLogLine(String.Format("File Not Found: {0}.", f));
					continue;
				}
				var blobName = string.IsNullOrEmpty(toBlob) ? toFolder.Substring(toFolder.IndexOf("/") + 1) + "/" + Path.GetFileName(f) : toBlob.Substring(toBlob.IndexOf("/") + 1) + (string.IsNullOrEmpty(file) && !string.IsNullOrEmpty(folder) ? "/" + Path.GetFileName(f) : null);
				var blob = blobContainer.GetBlockBlobReference(blobName);
				blob.Properties.ContentType = Util.GetContentType(f);
				Program.Shared.WriteLogLine(String.Format("Sending File To Storage (File: {0}; Size: {1}).", Path.GetFileName(f), Util.GetFileSize(new FileInfo(f).Length)));
				if (async){
					var task = blob.UploadFromFileAsync(f);
					Program.Shared.StorageUploadTasks.Add(task);
					task.ContinueWith(t => {
						if (deleteSource && File.Exists(f)){
							File.Delete(f);
							Program.Shared.WriteLogLine(String.Format("File Deleted: {0}.", f));
						}
					});
				}else{
					blob.UploadFromFile(f);
					if (deleteSource && File.Exists(f)){
						File.Delete(f);
						Program.Shared.WriteLogLine(String.Format("File Deleted: {0}.", f));
					}
				}
			}
		}

		// download
		void Download(string id, XmlNode n){
			var blobName = Program.Shared.ReplaceTags(Util.GetStr(n, "blob"));
			var blobUri = Program.Shared.ReplaceTags(Util.GetStr(n, "uri"));
			var toFile = Program.Shared.ReplaceTags(Util.GetStr(n, "toFile"));
			var sas = Program.Shared.ReplaceTags(Util.GetStr(n, "sharedAccessSignature"));
			var snapshotTime = Util.ParseDateTimeOffset(Program.Shared.ReplaceTags(Util.GetStr(n, "snapshotTime")));
			if (!string.IsNullOrEmpty(blobName)){
				var blobClient = CloudStorageAccount.Parse(Program.Shared.Storages[id]).CreateCloudBlobClient();
				var blobContainer = blobClient.GetContainerReference(blobName.Remove(blobName.IndexOf("/")));
				var blob = GetBlobReference(blobContainer, blobName.Substring(blobName.IndexOf("/") + 1), sas, snapshotTime);
				if (blob.Exists()){
					var dir = Path.GetDirectoryName(toFile);
					if (!Directory.Exists(dir))
						Directory.CreateDirectory(dir);
					blob.DownloadToFile(toFile, FileMode.Create);
				}
			}
			if(!string.IsNullOrEmpty(blobUri)){
				var blob = GetBlobReference(blobUri, sas, snapshotTime);
				if (blob.Exists()){
					var dir = Path.GetDirectoryName(toFile);
					if (!Directory.Exists(dir))
						Directory.CreateDirectory(dir);
					blob.DownloadToFile(toFile, FileMode.Create);
				}
			}
		}

		CloudBlockBlob GetBlobReference(CloudBlobContainer container, string blobName, string sasKey = null, DateTimeOffset? snapshotTime = null)
		{
			if (container == null || string.IsNullOrEmpty(blobName))
				return null;
			var sasString = !string.IsNullOrEmpty(sasKey) && Program.Shared.SharedAccessSignatures.ContainsKey(sasKey) ? Program.Shared.SharedAccessSignatures[sasKey] : "";
			return snapshotTime == null ?
				container.GetBlockBlobReference(blobName + sasString):
				container.GetBlockBlobReference(blobName + sasString, snapshotTime);
		}

		CloudBlockBlob GetBlobReference(string blobUri, string sasKey = null, DateTimeOffset? snapshotTime = null)
		{
			var snapshotString = snapshotTime == null ? "" : string.Format("?snapshot={0:yyyy-MM-ddTHH:mm:ss.fffffffZ}", snapshotTime.Value.UtcDateTime);
			var sasString = string.IsNullOrEmpty(sasKey) || !Program.Shared.SharedAccessSignatures.ContainsKey(sasKey) ? "" : Program.Shared.SharedAccessSignatures[sasKey];
			if (!string.IsNullOrEmpty(snapshotString) && !string.IsNullOrEmpty(sasString) && sasString.StartsWith("?"))
				sasString = "&" + sasString.Remove(0, 1);
			var uri = new Uri(blobUri + snapshotString + sasString);
			return new CloudBlockBlob(uri);
		}

		// list blobs
		void ListBlobs(string id, XmlNode n){
			var to = Util.GetStr(n, "to");
			var container = Program.Shared.ReplaceTags(Util.GetStr(n, "container"));
			var uri = Program.Shared.ReplaceTags(Util.GetStr(n, "uri"));
			var prefix = Program.Shared.ReplaceTags(Util.GetStr(n, "prefix"));
			var fetchAttributes = Util.GetStr(n, "fetchAttributes", "false").ToLower() == "true";
			var details = Util.GetEnumValue<BlobListingDetails>(Util.GetStr(n, "details", "none"), BlobListingDetails.None);
			var flat = Util.GetStr(n, "flat", details.HasFlag(BlobListingDetails.Snapshots) ? "true" : "false").ToLower() == "true";
			var where = Program.Shared.ReplaceTags(Util.GetStr(n, "where"));
			var sort = Program.Shared.ReplaceTags(Util.GetStr(n, "sort"));
			var regex = Program.Shared.ReplaceTags(Util.GetStr(n, "regex"));
			var sas = Program.Shared.ReplaceTags(Util.GetStr(n, "sharedAccessSignature"));
			var lst = null as CloudBlob[];
			var blobClient = null as CloudBlobClient;
			var blobContainer = null as CloudBlobContainer;
			if (string.IsNullOrEmpty(uri)){
				blobClient = CloudStorageAccount.Parse(Program.Shared.Storages[id]).CreateCloudBlobClient();
				blobContainer = string.IsNullOrEmpty(container) ? null : blobClient.GetContainerReference(container + (!string.IsNullOrEmpty(sas) && Program.Shared.SharedAccessSignatures.ContainsKey(sas) ? Program.Shared.SharedAccessSignatures[sas] : null));
				lst = (blobContainer == null ? blobClient.ListBlobs(prefix, flat, details) : (blobContainer.Exists() ? blobContainer.ListBlobs(prefix, flat, details) : new CloudBlob[0])).Where(b => b is CloudBlob).Cast<CloudBlob>().ToArray();
			} else {
				blobContainer = new CloudBlobContainer(new Uri(uri + (Program.Shared.SharedAccessSignatures.ContainsKey(sas) ? Program.Shared.SharedAccessSignatures[sas] : null)));
				lst = blobContainer.ListBlobs(prefix, flat, details).Where(b => b is CloudBlob).Cast<CloudBlob>().ToArray();
			}
			if (!string.IsNullOrEmpty(regex))
				lst = lst.Where(b => Regex.IsMatch(b.Uri.PathAndQuery, regex)).ToArray();
			var lstMetadata = Util.GetChildNodes(n, "Metadata").Select(n2 => new { Name = Util.GetStr(n2, "name"), Type = Util.GetStr(n2, "type"), Format = n2.InnerXml }).Where(m => !string.IsNullOrEmpty(m.Name) && !string.IsNullOrEmpty(m.Type)).ToArray();
			var dt = new DataTable();
			dt.Columns.Add("blob_name", typeof(string));
			dt.Columns.Add("blob_extension", typeof(string));
			dt.Columns.Add("blob_container", typeof(string));
			dt.Columns.Add("blob_uri", typeof(string));
			dt.Columns.Add("blob_length", typeof(long));
			dt.Columns.Add("blob_last_modified", typeof(DateTimeOffset));
			dt.Columns.Add("blob_last_modified_utc", typeof(DateTime));
			dt.Columns.Add("blob_content_type", typeof(string));
			dt.Columns.Add("blob_content_md5", typeof(string));
			dt.Columns.Add("blob_is_snapshot", typeof(bool));
			dt.Columns.Add("blob_snapshot_time", typeof(DateTimeOffset));
			foreach (var blob in lst){
				if (fetchAttributes){
					blob.FetchAttributes();
					Program.Shared.WriteLogLine("Fetching blob attributes: " + blob.Uri.ToString());
				}
				var lstRowData = new List<object>(){blob.Name, blob.Name.GetFileExtension(), blob.Container.Name, blob.Uri.ToString(), blob.Properties.Length, blob.Properties.LastModified, blob.Properties.LastModified?.UtcDateTime, blob.Properties.ContentType, blob.Properties.ContentMD5, blob.IsSnapshot, blob.SnapshotTime};
				if(blob.Metadata != null){
					foreach (var k in blob.Metadata.Keys)
						if (!dt.Columns.Contains("metadata_" + k)){
							var metadata = lstMetadata.FirstOrDefault(m => m.Name == k);
							if (metadata == null){
								dt.Columns.Add("metadata_" + k, typeof(string));
								continue;
							}
							switch (metadata.Type.ToLower()){
								case "datetime": dt.Columns.Add("metadata_" + k, typeof(DateTime)); break;
								case "datetimeoffset": dt.Columns.Add("metadata_" + k, typeof(DateTimeOffset)); break;
								case "short": dt.Columns.Add("metadata_" + k, typeof(short)); break;
								case "int": dt.Columns.Add("metadata_" + k, typeof(int)); break;
								case "long": dt.Columns.Add("metadata_" + k, typeof(long)); break;
								case "decimal": dt.Columns.Add("metadata_" + k, typeof(decimal)); break;
								case "float": dt.Columns.Add("metadata_" + k, typeof(float)); break;
								case "double": dt.Columns.Add("metadata_" + k, typeof(double)); break;
								case "bool": dt.Columns.Add("metadata_" + k, typeof(bool)); break;
							}
						}
					foreach (var k in blob.Metadata.Keys){
							var metadata = lstMetadata.FirstOrDefault(m => m.Name == k);
							if (metadata == null){
								lstRowData.Add(blob.Metadata[k]);
								continue;
							}
							switch (metadata.Type.ToLower()){
								case "datetime": lstRowData.Add(string.IsNullOrEmpty(blob.Metadata[k]) ? null : new Nullable<DateTime>(DateTime.ParseExact(blob.Metadata[k], metadata.Format, null))); break;
								case "datetimeoffset": lstRowData.Add(string.IsNullOrEmpty(blob.Metadata[k]) ? null : new Nullable<DateTimeOffset>(DateTimeOffset.ParseExact(blob.Metadata[k], metadata.Format, null))); break;
								case "short": lstRowData.Add(string.IsNullOrEmpty(blob.Metadata[k]) ? null : new Nullable<short>(Convert.ToInt16(blob.Metadata[k]))); break;
								case "int": lstRowData.Add(string.IsNullOrEmpty(blob.Metadata[k]) ? null : new Nullable<int>(Convert.ToInt32(blob.Metadata[k]))); break;
								case "long": lstRowData.Add(string.IsNullOrEmpty(blob.Metadata[k]) ? null : new Nullable<long>(Convert.ToInt64(blob.Metadata[k]))); break;
								case "decimal": lstRowData.Add(string.IsNullOrEmpty(blob.Metadata[k]) ? null : new Nullable<decimal>(Convert.ToDecimal(blob.Metadata[k]))); break;
								case "float": lstRowData.Add(string.IsNullOrEmpty(blob.Metadata[k]) ? null : new Nullable<float>(Convert.ToSingle(blob.Metadata[k]))); break;
								case "double": lstRowData.Add(string.IsNullOrEmpty(blob.Metadata[k]) ? null : new Nullable<double>(Convert.ToDouble(blob.Metadata[k]))); break;
								case "bool": lstRowData.Add(string.IsNullOrEmpty(blob.Metadata[k]) ? null : new Nullable<bool>(Convert.ToBoolean(blob.Metadata[k]))); break;
							}
					}
				}
				dt.Rows.Add(lstRowData.ToArray());
			}
			if (!string.IsNullOrEmpty(where)||!string.IsNullOrEmpty(sort)){
				var lstRows = dt.Select(where, sort);
				var lstRowsToRemove = dt.Rows.Cast<DataRow>().Where(r => !lstRows.Contains(r)).ToList();
				foreach (var r in lstRowsToRemove)
					dt.Rows.Remove(r);
			}
			lock (Program.Shared.LockDataTables){
				Program.Shared.DataTables[to+";"+Program.Shared.GetSequence()] = dt;
			}
			Program.Shared.WriteLogLine("List blobs completed: {0} - {1} blob(s) found{2}", blobContainer == null ? blobClient.BaseUri.ToString() : blobContainer.Uri.ToString(), lst.Length, string.IsNullOrEmpty(where) ? null : " and "+dt.Rows.Count+" match(es) the \"where\" condition");
		}

		// listcontainers
		void ListContainers(string id, XmlNode n)
		{
			var to = Util.GetStr(n, "to");
			var prefix = Program.Shared.ReplaceTags(Util.GetStr(n, "prefix"));
			var listingDetails = Util.GetEnumValue(Util.GetStr(n, "listingDetails", "none"), ContainerListingDetails.None);
			var where = Program.Shared.ReplaceTags(Util.GetStr(n, "where"));
			var sort = Program.Shared.ReplaceTags(Util.GetStr(n, "sort"));
			var regex = Program.Shared.ReplaceTags(Util.GetStr(n, "regex"));
			var blobClient = CloudStorageAccount.Parse(Program.Shared.Storages[id]).CreateCloudBlobClient();

			var dt = new DataTable();
			dt.Columns.Add("name", typeof(string));
			dt.Columns.Add("public_access", typeof(string));
			dt.Columns.Add("etag", typeof(string));
			dt.Columns.Add("last_modified", typeof(DateTimeOffset));
			dt.Columns.Add("uri", typeof(string));

			var lst = blobClient.ListContainers(prefix, listingDetails);
			var totalCount = lst.Count();
			if (!string.IsNullOrEmpty(regex))
				lst = lst.Where(c => Regex.IsMatch(c.Name, regex)).ToArray();
			foreach (var c in lst)
				dt.Rows.Add(c.Name, c.Properties?.PublicAccess, c.Properties?.ETag, c.Properties?.LastModified, c.Uri.ToString());
			if (!string.IsNullOrEmpty(where) || !string.IsNullOrEmpty(sort))
            {
				var lstRows = dt.Select(where, sort);
				var dt2 = dt.Clone();
				foreach (var r in lstRows)
					dt2.Rows.Add(r.ItemArray);
				dt = dt2;
			}

			lock (Program.Shared.LockDataTables)
				Program.Shared.DataTables[to + ";" + Program.Shared.GetSequence()] = dt;
			Program.Shared.WriteLogLine("List containers completed: {0} - {1} containers(s) found{2}", blobClient.BaseUri.ToString(), totalCount, string.IsNullOrEmpty(where) && string.IsNullOrEmpty(regex) ? null : " and " + dt.Rows.Count + " matched the conditions");
		}

		// copy
		void Copy(string id, XmlNode n){
			var blob = Program.Shared.ReplaceTags(Util.GetStr(n, "blob"));
			var toBlob = Program.Shared.ReplaceTags(Util.GetStr(n, "toBlob"));
			var toStorage = Util.GetStr(n, "toStorage");
			var blobClient1 = CloudStorageAccount.Parse(Program.Shared.Storages[id]).CreateCloudBlobClient();
			var blobContainer1 = blobClient1.GetContainerReference(blob.Remove(blob.IndexOf("/")));
			var blob1 = blobContainer1.GetBlockBlobReference(blob.Substring(blob.IndexOf("/") + 1));
			if (!blob1.Exists())
				return;
			var blobClient2 = CloudStorageAccount.Parse(Program.Shared.Storages[toStorage]).CreateCloudBlobClient();
			var blobContainer2 = blobClient2.GetContainerReference(toBlob.Remove(toBlob.IndexOf("/")));
			if (blobContainer2.CreateIfNotExists())
				blobContainer2.SetPermissions(new BlobContainerPermissions() { PublicAccess = BlobContainerPublicAccessType.Off });
			var blob2 = blobContainer2.GetBlockBlobReference(toBlob.Substring(toBlob.IndexOf("/") + 1));
			var exists = blob2.Exists();
			if (exists && blob1.Properties != null && blob2.Properties != null && blob2.Properties.Length == blob1.Properties.Length && blob2.Properties.ContentMD5 == blob1.Properties.ContentMD5)
				return;
			if (exists)
				blob2.Delete();
			var sig = blob1.GetSharedAccessSignature(new SharedAccessBlobPolicy() { SharedAccessExpiryTime = DateTime.UtcNow.AddMinutes(30), Permissions = SharedAccessBlobPermissions.Read });
			blob2.StartCopy(new Uri(blob1.Uri.AbsoluteUri + sig));
			//Program.Shared.WriteLogLine("Copy blob: {0} -> {1}", blob1.Uri.ToString(), blob2.Uri.ToString());
		}

		// set metadata
		void SetMetadata(string id, XmlNode n){
			var fromBlob = Program.Shared.ReplaceTags(Util.GetStr(n, "fromBlob"));
			var clear = Util.GetStr(n, "clear", "false").ToLower() == "true";
			var blobClient = CloudStorageAccount.Parse(Program.Shared.Storages[id]).CreateCloudBlobClient();
			var blobContainer = blobClient.GetContainerReference(fromBlob.Remove(fromBlob.IndexOf("/")));
			var blobRef = blobContainer.GetBlockBlobReference(fromBlob.Substring(fromBlob.IndexOf("/") + 1));
			if (!blobRef.Exists())
				return;
			var lstMetadata = Util.GetChildNodes(n, "Field").Select(n2 => new { Name = Util.GetStr(n2, "name"), Value = Extensions.OnlyChars(Extensions.RemoveAccents(Program.Shared.ReplaceTags(n2.InnerXml)), "0123456789abcdefghijklmnopqrstuvxwyzABCDEFGHIJKLMNOPQRSTUVXWYZ_- ().:/", "_")}).Where(n2 => !string.IsNullOrEmpty(n2.Value)).ToDictionary(n2 => n2.Name, n2 => n2.Value);
			if (!clear)
				blobRef.FetchAttributes();
			foreach (var k in lstMetadata.Keys)
				blobRef.Metadata[k] = lstMetadata[k];
			blobRef.SetMetadata();
			//WriteLogLine("Metadata was set for blob: {0}", blobRef.Uri.ToString());
		}

		// snapshot
		void Snapshot(string id, XmlNode n){
			var blob = Program.Shared.ReplaceTags(Util.GetStr(n, "blob"));
			var blobClient = CloudStorageAccount.Parse(Program.Shared.Storages[id]).CreateCloudBlobClient();
			var blobContainer = blobClient.GetContainerReference(blob.Remove(blob.IndexOf("/")));
			var blobRef = blobContainer.GetBlockBlobReference(blob.Substring(blob.IndexOf("/") + 1));
			var lstMetadata = Util.GetChildNodes(n, "Metadata").Select(n2 => new { Name = Util.GetStr(n2, "name"), Value = Extensions.OnlyChars(Extensions.RemoveAccents(Program.Shared.ReplaceTags(n2.InnerXml)), "0123456789abcdefghijklmnopqrstuvxwyzABCDEFGHIJKLMNOPQRSTUVXWYZ_- ().:/", "_")}).Where(n2 => !string.IsNullOrEmpty(n2.Value)).ToDictionary(n2 => n2.Name, n2 => n2.Value);
            if (!blobRef.Exists())
                return;
            var snapshot = blobRef.CreateSnapshot(lstMetadata.Count == 0 ? null : lstMetadata);
			Program.Shared.WriteLogLine("Snapshot time: {0:yyyy-MM-dd HH:mm:ss.fffffff zzz}", snapshot.SnapshotTime);
			//var snapshot = blobRef.Snapshot(lstMetadata.Count == 0 ? null : lstMetadata);
            var snapshotTimeToVar = Program.Shared.ReplaceTags(Util.GetStr(n, "snapshotTimeToVar"));
            var snapshotTimeToRow = Program.Shared.ReplaceTags(Util.GetStr(n, "snapshotTimeToRow"));
            if (!string.IsNullOrEmpty(snapshotTimeToVar))
                lock (Program.Shared.LockVariables)
                    Program.Shared.Variables[snapshotTimeToVar + ";" + Program.Shared.GetSequence()] = snapshot.SnapshotTime;
            if (!string.IsNullOrEmpty(snapshotTimeToRow)){
                var lstRows = Program.Shared.GetCurrentRows(Program.Shared.ThreadID());
                var index = snapshotTimeToRow.IndexOf(".");
                if (index == -1)
                    return;
                var table = snapshotTimeToRow.Remove(index);
                var column = snapshotTimeToRow.Substring(index+1);
                var dt = Program.Shared.GetDataTable(Program.Shared.ThreadID(), table);
                lock (Program.Shared.LockDataTables){
					if (!dt.Columns.Contains(column))
                        dt.Columns.Add(column, typeof(DateTimeOffset));
                    lstRows[table][column] = snapshot.SnapshotTime;
                }
            }
        }

		// delete multiple blobs
		void DeleteMultipleBlobs(string id, XmlNode n){
			var regex = Program.Shared.ReplaceTags(Util.GetStr(n, "regex"));
			var container = Program.Shared.ReplaceTags(Util.GetStr(n, "container"));
			var name = Program.Shared.ReplaceTags(Util.GetStr(n, "name"));
			var prefix = Util.GetStr(n, "prefix");
			var olderThan = Util.GetStr(n, "olderThan");
			var limit = Util.GetDateTimeOffset(olderThan);
			var regexDateGroup = Util.GetStr(n, "regexDateGroup");
			var regexDateFormat = Util.GetStr(n, "regexDateFormat");
			var blobClient = CloudStorageAccount.Parse(Program.Shared.Storages[id]).CreateCloudBlobClient();
			var blobContainer = blobClient.GetContainerReference(container);
			var lstBlobs = string.IsNullOrEmpty(name) ? ((string.IsNullOrEmpty(container) ? blobClient.ListBlobs(prefix, true) : (blobContainer.Exists() ? blobContainer.ListBlobs(prefix, true) : new CloudBlob[0])).Where(b => b is CloudBlob).Cast<CloudBlob>().ToList()) : new List<CloudBlob>(){blobContainer.GetBlockBlobReference(name)};
			if (!string.IsNullOrEmpty(regex)){
				lstBlobs = lstBlobs.Where(item => {
					var m = Regex.Match(item.Uri.PathAndQuery, regex);
					if (string.IsNullOrEmpty(regexDateGroup) || string.IsNullOrEmpty(regexDateFormat) || m.Groups[regexDateGroup] == null || string.IsNullOrEmpty(m.Groups[regexDateGroup].Value))
						return m.Success;
					return m.Success && DateTime.ParseExact(m.Groups[regexDateGroup].Value, regexDateFormat, null) < limit;
				}).ToList();
			}
			Parallel.ForEach(lstBlobs, blob => {
				if(!string.IsNullOrEmpty(regex) && !string.IsNullOrEmpty(regexDateGroup) && !string.IsNullOrEmpty(regexDateFormat)){
					if (blob.DeleteIfExists())
						Program.Shared.WriteLogLine($"Blob deleted: {blob.Uri.ToString()}");
				}else{
					blob.FetchAttributes();
					var modified = blob.Properties.LastModified == null ? DateTime.Now : blob.Properties.LastModified.Value;
					if (modified < limit)
						if (blob.DeleteIfExists())
							Program.Shared.WriteLogLine($"Blob deleted: {blob.Uri.ToString()}");
				}
			});
		}

		// delete blob
		void DeleteBlob(string id, XmlNode n)
		{
			var container = Program.Shared.ReplaceTags(Util.GetStr(n, "container"));
			var blobName = Program.Shared.ReplaceTags(Util.GetStr(n, "name"));
			if (string.IsNullOrEmpty(container) || string.IsNullOrEmpty(blobName))
				return;
			var blobClient = CloudStorageAccount.Parse(Program.Shared.Storages[id]).CreateCloudBlobClient();
			var blobContainer = blobClient.GetContainerReference(container);
			var blob = blobContainer.GetBlobReference(blobName);
			blob.DeleteIfExists();
		}

		// delete snapshot
		void DeleteSnapshot(string id, XmlNode n){
			var container = Program.Shared.ReplaceTags(Util.GetStr(n, "container"));
			var name = Program.Shared.ReplaceTags(Util.GetStr(n, "name"));
			var time = Util.ParseDateTime(Program.Shared.ReplaceTags(Util.GetStr(n, "time")));
			if (container == null || name == null || time == null)
				return;
			var blobClient = CloudStorageAccount.Parse(Program.Shared.Storages[id]).CreateCloudBlobClient();
			var blobContainer = blobClient.GetContainerReference(container);
			var blob = blobContainer.GetBlockBlobReference(name, new DateTimeOffset(time.Value));
			if (blob.IsSnapshot && blob.Exists())
				blob.Delete();
		}

		// delete containers
		void DeleteContainers(string id, XmlNode n){
			var prefix = Util.GetStr(n, "prefix");
			var regex = Program.Shared.ReplaceTags(Util.GetStr(n, "regex"));
			var name = Program.Shared.ReplaceTags(Util.GetStr(n, "name"));
			var blobClient = CloudStorageAccount.Parse(Program.Shared.Storages[id]).CreateCloudBlobClient();
			var lstContainers = new List<CloudBlobContainer>();
			if (string.IsNullOrEmpty(name))
				lstContainers.AddRange(blobClient.ListContainers(prefix));
			else
				lstContainers.Add(blobClient.GetContainerReference(name));
			if (!string.IsNullOrEmpty(regex))
				lstContainers.Remove(c => Regex.IsMatch(c.Name, regex));
			foreach (var c in lstContainers)
				if (c.Exists())
					c.Delete();
		}
	}
}
