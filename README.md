# doit
This tool runs a xml script to automate recurring tasks. Useful on backup scenarios.

## Index
1. [The Configuration File](#TheConfigurationFile)
2. [Settings](#Settings)
    1. [LogFile](#SettingsLogFile)
    2. [Exceptions](#SettingsExceptions)
    3. [ConnectionStrings](#SettingsConnectionStrings)
3. [Execute Commands](#ExecuteCommands)
    1. [Database](#ExecuteDatabase)
        * [Backup](#ExecuteDatabaseBackup)
    2. [Zip](#ExecuteZip)
        * [AddFile](#ExecuteZipAddFile)
        * [AddBlob](#ExecuteZipAddBlob)
        * AddFolder _(To-Do)_
        * [Extract](#ExecuteZipExtract)
    3. [Process](#ExecuteProcess)
        * [Start](#ExecuteProcessStart)
        * [Kill](#ExecuteProcessKill)
        * [List](#ExecuteProcessList)
    4. [Sql](#ExecuteSql)
        * [Execute](#ExecuteSqlExecute)
        * [Select](#ExecuteSqlSelect)
        * [Scalar](#ExecuteSqlScalar)
    5. [Mail](#ExecuteMail)
    6. [ForEach](#ExecuteForEach)
    7. [Log](#ExecuteLog)
    8. [Sleep](#ExecuteSleep)
    9. [Exception](#ExecuteException)
    10. [Try](#ExecuteTry)
    11. [Csv](#ExecuteCsv)
        * [WriteLine](#ExecuteCsvWriteLine)
        * [WriteData](#ExecuteCsvWriteData)
        * [Load](#ExecuteCsvLoad)
    12. [DataTable](#ExecuteDataTable)
        * [Count](#ExecuteDataTableCount)
        * [Sum](#ExecuteDataTableSum)
        * [Avg](#ExecuteDataTableAvg)
        * [Min](#ExecuteDataTableMin)
        * [Max](#ExecuteDataTableMax)
        * [SetRowValue](#ExecuteDataTableSetRowValue)
        * [GetDataRow](#ExecuteDataTableGetDataRow)
        * [Diff](#ExecuteDataTableDiff)
        * [Join](#ExecuteDataTableJoin)
        * [Intersect](#ExecuteDataTableIntersect)
        * [RemoveRows](#ExecuteDataTableRemoveRows)
        * [InsertRow](#ExecuteDataTableInsertRow)
    13. [SetValue](#ExecuteSetValue)
        * [Calc](#ExecuteSetValueCalc)
        * [CalcDate](#ExecuteSetValueCalcDate)
        * [String](#ExecuteSetValueString)
        * [Date](#ExecuteSetValueDate)
    14. [LocalDisk](#ExecuteLocalDisk)
        * [ListFiles](#ExecuteLocalDiskListFiles)
        * [MoveFile](#ExecuteLocalDiskMoveFile)
        * [MoveFolder](#ExecuteLocalDiskMoveFolder)
        * [CopyFile](#ExecuteLocalDiskCopyFile)
        * [DeleteFile](#ExecuteLocalDiskDeleteFile)
        * [DeleteFolder](#ExecuteLocalDiskDeleteFolder)
    15. [Storage](#ExecuteStorage)
        * [Upload](#ExecuteStorageUpload)
        * [Download](#ExecuteStorageDownload)
        * [ListBlobs](#ExecuteStorageListBlobs)
    16. [Condition](#ExecuteCondition)
    17. Ftp _(To-Do)_
        * Download
        * Upload
        * ListFiles
    18. Services _(To-Do)_
        * Start
        * Stop

## <a id="TheConfigurationFile">The Configuration File</a>
The default configuration file is called "DoIt.config.xml". Its main sections are "Settings" and "Execute", which contains the settings used when executing and the steps to run, respectively.

If you want to use another configuration file you can use:
```shell
C:\DoIt\DoIt.exe /config="C:\DoIt\AnotherConfigFile.config.xml"
```

Below you can see how to prepare the configuration file.

## <a id="Settings">Settings</a>

### <a id="SettingsLogFile">LogFile</a>
Use this tag to specify the log path and variable.

*Tag Location: Configuration > Settings > LogFile*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Settings>
    <LogFile toVar="logFile">%programdata%\DoIt\DoIt_{now:yyyy-MM-dd}.log</LogFile>
  </Settings>
</Configuration>
```

### <a id="SettingsExceptions">Exceptions</a>
Use this tag to mail users if an exception occurs.

*Tag Location: Configuration > Settings > Exceptions*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Settings>
    <Exceptions smtp="host=smtp.company.com; user=mail_user@company.com; pass=abc123; port=587; ssl=true; from=mail_user@company.com;" attachLogFile="true">
      <Mail>admin@company.com</Mail>
    </Exceptions>
  </Settings>
</Configuration>
```

### <a id="SettingsConnectionStrings">ConnectionStrings</a>
This tag set database and azure storage account connection strings.

*Tag Location: Configuration > Settings > ConnectionStrings*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Settings>
    <ConnectionStrings>
      <Database id="1">Data Source=localhost\sql2016express; Initial Catalog=database; Integrated Security=false; User Id=sa; Password=123;</Database>
      <Storage id="1">DefaultEndpointsProtocol=https;AccountName=my_account;AccountKey=871oQKMifslemflIwq54e0fd8sJskdmw98348dMF0suJ0WODK73lMlwiehf34u0mm5ez6MdiewklFH3/w2/IEK==</Storage>
    </ConnectionStrings>
  </Settings>
</Configuration>
```
## <a id="ExecuteCommands">Execute Commands</a>

### <a id="ExecuteDatabase">Database</a>

#### <a id="ExecuteDatabaseBackup">Backup</a>
Backup SQL Server databases.

*Tag Location: Configuration > Execute > Database > Backup*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <Database id="1">
      <Backup toFile="%programdata%\DoIt\MyDatabase_{now:yyyy-MM-dd_HH-mm}.bak" type="bak" withOptions="with compression" timeout="1800" toVar="bak1" />
    </Database>
  </Execute>
</Configuration>
```

### <a id="ExecuteZip">Zip</a>

#### <a id="ExecuteZipAddFile">AddFile</a>
Add a new file to the specified zip package.

*Tag Location: Configuration > Execute > Zip > AddFile*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <Zip path="C:\MyFiles.zip" mode="write">
      <AddFile name="C:\MyFile1.txt" deleteSource="true" zipFolder="" zipFilename="MainData.txt" />
      <AddFile forEach="users_list" where="is_active=1" name="C:\Users\UserData{users_list.id}.csv" deleteSource="false" />
    </Zip>
  </Execute>
</Configuration>
```

#### <a id="ExecuteZipAddBlob">AddBlob</a>
Download blob from a storage account and add it to the specified zip package.

*Tag Location: Configuration > Execute > Zip > AddBlob*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <Zip path="C:\MyFiles.zip" mode="write">
      <AddBlob fromStorage="1" name="my_container/myblob1.txt" zipFolder="" zipFilename="File.txt" />
      <AddBlob forEach="blobs_list" where="blob_length <= 5*1024*1024" fromStorage="1" name="{blobs_list.blob_container}/{blobs_list.blob_name}" snapshotTime="" dateTime="{blobs_list.blob_last_modified}" size="{blobs_list.blob_length}" />
    </Zip>
  </Execute>
</Configuration>
```

#### <a id="ExecuteZipExtract">Extract</a>
Extract a zip package to the specified folder.

*Tag Location: Configuration > Execute > Zip > Extract*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <Zip path="C:\MyFiles.zip" mode="read">
      <Extract toFolder="C:\MyFolder" />
    </Zip>
  </Execute>
</Configuration>
```

### <a id="ExecuteProcess">Process</a>

#### <a id="ExecuteProcessStart">Start</a>
Starts an external application.

*Tag Location: Configuration > Execute > Process > Start*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <Process>
      <Start path="C:\MyApp.exe" args="" wait="true" time="" />
    </Process>
  </Execute>
</Configuration>
```

#### <a id="ExecuteProcessKill">Kill</a>
Kills an executing process.

*Tag Location: Configuration > Execute > Process > Kill*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <Process>
      <Kill id="" name="chrome" />
    </Process>
  </Execute>
</Configuration>
```

#### <a id="ExecuteProcessList">List</a>
List the executing processes. The returned datatable will contain the following columns:

* id (int)
* session_id (int)
* name (string)
* machine (string)
* start (DateTime)
* filename (string)

Tag Location: Configuration > Execute > Process > List
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <Process>
      <List name="" machine="" regex="" to="process_list" />
    </Process>
  </Execute>
</Configuration>
```

### <a id="ExecuteSql">Sql</a>

#### <a id="ExecuteSqlExecute">Execute</a>
Execute SQL commands.

*Tag Location: Configuration > Execute > Sql > Execute*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <Sql database="1">
      <Execute timeout="30">insert into backups (start_date) values (getdate())</Execute>
    </Sql>
  </Execute>
</Configuration>
```

#### <a id="ExecuteSqlSelect">Select</a>
Execute SQL queries and set the results to the specified variable.

*Tag Location: Configuration > Execute > Sql > Select*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <Sql database="1">
      <Select to="users_table" timeout="30">
        select us.id, us.name, us.email from users us where us.removed=0
      </Select>
    </Sql>
  </Execute>
</Configuration>
```

#### <a id="ExecuteSqlScalar">Scalar</a>
Execute the SQL command/query and set the result to the specified variable.

*Tag Location: Configuration > Execute > Sql > Scalar*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <Sql database="1">
      <Scalar to="user_id" timeout="30">
        select us.id from users us where us.email='user@mycompany.com'
      </Scalar>
    </Sql>
  </Execute>
</Configuration>
```

### <a id="ExecuteMail">Mail</a>
Send an e-mail.

*Tag Location: Configuration > Execute > Mail*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <Mail smtp="host=smtp.domain.com; from=user@domain.com; port=587; ssl=true; user=user@domain.com; pass=123;" to="other_user@domain.com" subject="Hello World">
      <Body>
        Here is my mail body.
      </Body>
      <Attachments>
        <File path="C:\MyFileToSend.txt" />
        <File path="C:\My2ndAttachment.txt" />
        <SqlQuery database="1" dataFormat="csv|json|xml">
          select t.id, t.total, t.date from orders t where t.date>=cast(getdate() as date)
        </SqlQuery>
      </Attachments>
    </Mail>
  </Execute>
</Configuration>
```

### <a id="ExecuteForEach">ForEach</a>
Loop throught the rows in the specified table.

*Tag Location: Configuration > Execute > ForEach*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <ForEach itemFrom="users_list" where="" sort="" parallel="1">
      <Log>User Name: {users_list.name}.</Log>
    </ForEach>
  </Execute>
</Configuration>
```

### <a id="ExecuteLog">Log</a>
Write a new line to the previously specified log file.

*Tag Location: Configuration > Execute > Log*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <Log>Hello World!</Log>
  </Execute>
</Configuration>
```

### <a id="ExecuteSleep">Sleep</a>
Block the current thread for the specified time.

*Tag Location: Configuration > Execute > Sleep*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <Sleep time="30 seconds" />
  </Execute>
</Configuration>
```

### <a id="ExecuteException">Exception</a>
Throw a new exception.

*Tag Location: Configuration > Execute > Exception*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <Exception assembly="" type="System.Exception" message="Oh no, something is wrong!" />
  </Execute>
</Configuration>
```

### <a id="ExecuteTry">Try</a>
Try to run some commands for the specified retry times.

*Tag Location: Configuration > Execute > Try*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <Try retry="3" sleep="40 seconds">
      <Catch>
        <Exception type="System.Net.WebException" withMessage="(409) Conflict" />
        <Exception type="Microsoft.WindowsAzure.Storage.StorageException" withMessage="(409) Conflict" />
      </Catch>
      <Execute>
        <Log>The commands to run are here!</Log>
      </Execute>
      <Fail>
        <Log>Command failed :(</Log>
      </Fail>
    </Try>
  </Execute>
</Configuration>
```

### <a id="ExecuteCsv">Csv</a>

#### <a id="ExecuteCsvWriteLine">WriteLine</a>
Write a new line in the specified csv file.

*Tag Location: Configuration > Execute > Csv > WriteLine*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <Csv path="C:\MyFile.csv" separator=";">
      <WriteLine append="false">
        <Column>id</Column>
        <Column>name</Column>
        <Column>email</Column>
      </WriteLine>
    </Csv>
    <ForEach itemFrom="users_list">
      <Csv path="C:\MyFile.csv" separator=";">
        <WriteLine append="true">
          <Column>{users_list.id}</Column>
          <Column>{users_list.name}</Column>
          <Column>{users_list.email}</Column>
        </WriteLine>
      </Csv>
    </ForEach>
  </Execute>
</Configuration>
```

#### <a id="ExecuteCsvWriteData">WriteData</a>
Write the datatable to the specified csv file.

*Tag Location: Configuration > Execute > Csv > WriteData*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <Csv path="C:\MyFile.csv" separator=";">
      <WriteData data="users_list" where="" append="false">
        <Column header="id">{users_list.id}</Column>
        <Column header="name">{users_list.name}</Column>
        <Column header="email">{users_list.email}</Column>
      </WriteData>
    </Csv>
  </Execute>
</Configuration>
```

#### <a id="ExecuteCsvLoad">Load</a>
Load the specified csv file to a new datatable.

*Tag Location: Configuration > Execute > Csv > Load*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <Csv path="C:\MyFile.csv" separator=";">
      <Load to="users_list" where="" hasHeaders="true" />
    </Csv>
  </Execute>
</Configuration>
```

### <a id="ExecuteDataTable">DataTable</a>

#### <a id="ExecuteDataTableCount">Count</a>
Count the rows found in the specified table.

*Tag Location: Configuration > Execute > DataTable > Count*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <DataTable>
      <Count data="users_list" where="" to="users_count" />
    </DataTable>
  </Execute>
</Configuration>
```

#### <a id="ExecuteDataTableSum">Sum</a>
Sum values from the rows in the specified table

*Tag Location: Configuration > Execute > DataTable > Sum*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <DataTable>
      <Sum data="products_list" column="total" where="" to="total_products" />
    </DataTable>
  </Execute>
</Configuration>
```

#### <a id="ExecuteDataTableAvg">Avg</a>
Calculate the average values from the rows in the specified table.

*Tag Location: Configuration > Execute > DataTable > Avg*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <DataTable>
      <Avg data="products_list" column="price" where="" to="avg_price" />
    </DataTable>
  </Execute>
</Configuration>
```

#### <a id="ExecuteDataTableMin">Min</a>
Get the min value from the rows in the specified table.

*Tag Location: Configuration > Execute > DataTable > Min*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <DataTable>
      <Min data="products_list" column="price" where="" to="min_price" />
    </DataTable>
  </Execute>
</Configuration>
```

#### <a id="ExecuteDataTableMax">Max</a>
Get the max value from the rows in the specified table.

*Tag Location: Configuration > Execute > DataTable > Max*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <DataTable>
      <Max data="products_list" column="price" where="" to="max_price" />
    </DataTable>
  </Execute>
</Configuration>
```

#### <a id="ExecuteDataTableSetRowValue">SetRowValue</a>
Set values on the specified columns/rows.

*Tag Location: Configuration > Execute > DataTable > SetRowValue*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <DataTable>
      <SetRowValue data="users_list" where="">
        <Column name="is_active" value="1" />
      </SetRowValue>
    </DataTable>
  </Execute>
</Configuration>
```

#### <a id="ExecuteDataTableGetDataRow">GetDataRow</a>
Find the rows that matches the where condition and set one of them to the specified variable.

*Tag Location: Configuration > Execute > DataTable > GetDataRow*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <DataTable>
      <GetDataRow fromData="users_list" to="user_row" where="id={orders.id_user}" index="0" />
    </DataTable>
  </Execute>
</Configuration>
```

#### <a id="ExecuteDataTableDiff">Diff</a>
Create a new datatable with the rows existing in one datatable and not in other.

*Tag Location: Configuration > Execute > DataTable > Diff*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <DataTable>
      <Diff inData="blobs_list1" notInData="blobs_list2" columns="blob_container, blob_name, blob_content_md5" to="new_blobs_list" />
    </DataTable>
  </Execute>
</Configuration>
```

#### <a id="ExecuteDataTableJoin">Join</a>
Create a new datatable with the rows from other datatables.

*Tag Location: Configuration > Execute > DataTable > Join*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <DataTable>
      <Join data="blobs_list1, blobs_list2" to="all_blobs_list" />
    </DataTable>
  </Execute>
</Configuration>
```

#### <a id="ExecuteDataTableIntersect">Intersect</a>
Create a new datatable only with the rows existing in all specified datatables.

*Tag Location: Configuration > Execute > DataTable > Intersect*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <DataTable>
      <Intersect data="blobs_list1, blobs_list2" columns="blob_name" rowsFrom="0" to="new_blobs_list" />
    </DataTable>
  </Execute>
</Configuration>
```

#### <a id="ExecuteDataTableRemoveRows">RemoveRows</a>
Remove rows from the datatable when it matches the where clause.

*Tag Location: Configuration > Execute > DataTable > RemoveRows*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <DataTable>
      <RemoveRows from="users_list" where="is_active=0" />
    </DataTable>
  </Execute>
</Configuration>
```

#### <a id="ExecuteDataTableInsertRow">InsertRow</a>
Insert a new row in the specified datatable.

*Tag Location: Configuration > Execute > DataTable > InsertRow*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <DataTable>
      <InsertRow to="files_list">
        <Column name="id" type="int">1</Column>
        <Column name="name" type="string">Test User</Column>
        <Column name="email" type="string">testuser@company.com</Column>
      </InsertRow>
    </DataTable>
  </Execute>
</Configuration>
```

### <a id="ExecuteSetValue">SetValue</a>

#### <a id="ExecuteSetValueCalc">Calc</a>
Execute a simple operation using the specified values.

*Tag Location: Configuration > Execute > SetValue > Calc*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <SetValue>
      <Calc operation="+|-|*|/" value1="2" value2="3" to="my_calc" />
    </SetValue>
  </Execute>
</Configuration>
```

#### <a id="ExecuteSetValueCalcDate">CalcDate</a>
Execute a date operation using the specified values.

*Tag Location: Configuration > Execute > SetValue > CalcDate*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <SetValue>
      <CalcDate to="limit_date" value="{today}" add="-6 months" operation="-|+" />
    </SetValue>
  </Execute>
</Configuration>
```

#### <a id="ExecuteSetValueString">String</a>
Set the value to the specified string variable.

*Tag Location: Configuration > Execute > SetValue > String*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <SetValue>
      <String value="User Name: {users_list.name}" to="user_name" />
    </SetValue>
  </Execute>
</Configuration>
```

#### <a id="ExecuteSetValueDate">Date</a>
Set the date/time value to the specified variable.

*Tag Location: Configuration > Execute > SetValue > Date*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <SetValue>
      <Date value="{now}" to="start_date" />
    </SetValue>
  </Execute>
</Configuration>
```

### <a id="ExecuteLocalDisk">LocalDisk</a>

#### <a id="ExecuteLocalDiskListFiles">ListFiles</a>
Query files from a folder. The returned datatable contains the following columns:

* full_path (string)
* directory (string)
* filename (string)
* extension (string)
* creation_time (DateTime)*
* last_write_time (DateTime)*
* length (long)*

The columns creation_time, last_write_time and length will only be filled if the parameter "fetchAttributes" is set to "true".

*Tag Location: Configuration > Execute > LocalDisk > ListFiles*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <LocalDisk>
      <ListFiles to="files_list" path="C:\MyFolder" searchPattern="*.*" allDirectories="false" fetchAttributes="false" where="" sort="" regex="" />
    </LocalDisk>
  </Execute>
</Configuration>
```

#### <a id="ExecuteLocalDiskMoveFile">MoveFile</a>
Change the name from the specified file or move it to another parent folder.

*Tag Location: Configuration > Execute > LocalDisk > MoveFile*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <LocalDisk>
      <MoveFile path="C:\MyFile1.txt" to="C:\Folder\MyMovedFile.txt" />
    </LocalDisk>
  </Execute>
</Configuration>
```

#### <a id="ExecuteLocalDiskMoveFolder">MoveFolder</a>
Change the name from the specified folder or move it to another parent folder.

*Tag Location: Configuration > Execute > LocalDisk > MoveFolder*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <LocalDisk>
      <MoveFolder path="C:\MyFolder1" to="C:\Folder\MyMovedFolder" />
    </LocalDisk>
  </Execute>
</Configuration>
```

#### <a id="ExecuteLocalDiskCopyFile">CopyFile</a>
Copy the specified file to another file.

*Tag Location: Configuration > Execute > LocalDisk > CopyFile*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <LocalDisk>
      <CopyFile path="C:\MyFile1.txt" to="C:\Folder\MyFileCopy.txt" overwrite="true" />
    </LocalDisk>
  </Execute>
</Configuration>
```

#### <a id="ExecuteLocalDiskDeleteFile">DeleteFile</a>
Delete the specified file.

*Tag Location: Configuration > Execute > LocalDisk > DeleteFile*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <LocalDisk>
      <DeleteFile path="C:\MyFile1.txt" />
    </LocalDisk>
  </Execute>
</Configuration>
```

#### <a id="ExecuteLocalDiskDeleteFolder">DeleteFolder</a>
Delete the specified folder.

*Tag Location: Configuration > Execute > LocalDisk > DeleteFolder*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <LocalDisk>
      <DeleteFolder path="C:\MyFolder" recursive="false" />
    </LocalDisk>
  </Execute>
</Configuration>
```

### <a id="ExecuteStorage">Storage</a>

#### <a id="ExecuteStorageUpload">Upload</a>
Upload a file to the specified Azure storage account.

*Tag Location: Configuration > Execute > Storage > Upload*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <Storage id="1">
      <Upload file="C:\MyFile_{now:yyyy-MM-dd}.csv" toBlob="backups/Backup_{now:yyyy-MM-dd}/MyFile.csv" deleteSource="true" async="true" />
    </Storage>
  </Execute>
</Configuration>
```

#### <a id="ExecuteStorageDownload">Download</a>
Download the specified file.

*Tag Location: Configuration > Execute > Storage > Download*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <Storage id="1">
      <Download blob="my_container/myblob.txt" toFile="C:\MyFile.txt" snapshotTime="" />
    </Storage>
  </Execute>
</Configuration>
```

#### <a id="ExecuteStorageListBlobs">ListBlobs</a>
Query blobs and set the resulting list to a variable. The returned datatable contains the following columns:

* blob_name (string)
* blob_extension (string)
* blob_container (string)
* blob_uri (string)
* blob_length (long)
* blob_last_modified (DateTime)
* blob_content_type (string)
* blob_content_md5 (string)
* blob_is_snapshot (bool)
* blob_snapshot_time (DateTime)
* metadata_name1 (string)*
* metadata_name2 (string)*

The columns with the name starting with "metadata_" will only be filled with the blob metadata if the parameter "details" contains the option "metadata" or the parameter "fetchAttributes" is set to "true".

*Tag Location: Configuration > Execute > Storage > ListBlobs*
```html
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <Storage id="1">
      <ListBlobs to="blobs_list" container="container{now:yyyyMM}" prefix="" fetchAttributes="false" details="none|snapshots|metadata" where="" sort="" regex="" />
    </Storage>
  </Execute>
</Configuration>
```

### <a id="ExecuteCondition">Condition</a>
Perform a condition and run only the "True" or "False" inner tag, according to the result. The available condition types are:
* has-disk-space
* file-exists
* folder-exists
* has-rows
* is-datetime
* if

*Tag Location: Configuration > Execute > Condition*

#### Sample1 - Condition Type: has-disk-space
```html
<Condition type="has-disk-space" drive="C:\" min="10000">
  <True>
    <Log>True Result</Log>
  </True>
</Condition>
```

#### Sample2 - Condition Type: file-exists
```html
<Condition type="file-exists" path="C:\MyFile.txt">
  <True>
    <Log>True Result</Log>
  </True>
</Condition>
```

#### Sample3 - Condition Type folder-exists
```html
<Condition type="folder-exists" path="C:\MyFolder">
  <True>
    <Log>True Result</Log>
  </True>
</Condition>
```

#### Sample4 - Condition Type: has-rows
```html
<Condition type="has-rows" data="customers_list">
  <True>
    <Log>True Result</Log>
  </True>
</Condition>
```

#### Sample5 - Condition Type: is-datetime
```html
<Condition type="is-datetime" days="all|mon|wed|fri|1|15" time="08-12">
  <True>
    <Log>True Result</Log>
  </True>
</Condition>
```

#### Sample6 - Condition Type: if
```html
<Condition type="if" value1="{files_count}" value2="0" comparison="greater" valueType="numeric">
  <True>
    <Log>True Result</Log>
  </True>
  <False>
    <Log>False Result</Log>
  </False>
</Condition>
```
