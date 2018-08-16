# doit
This tool runs a xml script to automate recurring tasks. Useful on backup scenarios.

## Index
1. [The Configuration File](#TheConfigurationFile)
    1. [Example](#TheConfigurationFileExample)
    2. [Encryption](#TheConfigurationFileEncryption)
2. [Settings](#Settings)
    1. [LogFile](#SettingsLogFile)
    2. [ConnectionStrings](#SettingsConnectionStrings)
    3. [Exceptions](#SettingsExceptions)
3. [Execute Commands](#ExecuteCommands)
    1. [Database](#ExecuteDatabase)
        * [Backup](#ExecuteDatabaseBackup)
        * BackupLog
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
        * [Filter](#ExecuteDataTableFilter)
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
        * [Copy](#ExecuteStorageCopy)
        * [SetMetadata] _(Waiting documentation)_
        * [Snapshot] _(Waiting documentation)_
    16. [Condition](#ExecuteCondition)
    17. [Ftp](#ExecuteFtp)
        * [List](#ExecuteFtpList)
        * [Download](#ExecuteFtpDownload)
        * [Upload](#ExecuteFtpUpload)
        * [CreateFolder](#ExecuteFtpCreateFolder)
        * [DeleteFolder](#ExecuteFtpDeleteFolder)
        * [DeleteFile](#ExecuteFtpDeleteFile)
    18. Services _(To-Do)_
        * Start
        * Stop

## <a id="TheConfigurationFile">The Configuration File</a>
The default configuration file is called "DoIt.config.xml". Its main sections are "Settings" and "Execute", which contains the settings used when executing and the steps to run, respectively.

If you want to use another configuration file you can use:
```shell
C:\DoIt\DoIt.exe /config="C:\DoIt\AnotherConfigFile.config.xml"
```

### <a id="TheConfigurationFileExample">Example</a>
Here is a configuration file example.
Please use the full documentation for more commands or options.
```xml
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>

  <!-- Here we load some data to use when executing -->
  <Settings>

    <ConnectionStrings>
      <Database id="1">Data Source=localhost\sql2016express; Initial Catalog=database; Integrated Security=false; User Id=sa; Password=123;</Database>
      <Storage id="1">DefaultEndpointsProtocol=https;AccountName=my_account;AccountKey=871oQKMifslemflIwq54e0fd8sJskdmw98348dMF0suJ0WODK73lMlwiehf34u0mm5ez6MdiewklFH3/w2/IEK==</Storage>
      <MailServer id="1">host=smtp.domain.com; from=user@domain.com; port=587; ssl=true; user=user@domain.com; pass=123;</MailServer>
    </ConnectionStrings>

    <Exceptions mailServer="1" attachLogFile="true">
      <Mail>admin1@company.com</Mail>
      <Mail>admin2@company.com</Mail>
    </Exceptions>

    <LogFile toVar="logFile">%programdata%\DoIt\DoIt_{now:yyyy-MM-dd}.log</LogFile>

  </Settings>

  <!-- Here we put the script steps -->
  <Execute>

    <Log>We can use variables!</Log>
    <SetValue>
      <String to="my_var1" value="{now}" />
    </SetValue>
    <Log>Today is: {my_var1:yyyy-MM-dd}.</Log>
    
    <Log>Load the files from a directory to a variable</Log>
    <LocalDisk>
      <ListFiles to="files_list" path="C:\MyFolder" searchPattern="*.*" allDirectories="false" fetchAttributes="false" where="" sort="" regex="" />
    </LocalDisk>

    <Log>We can also use loops!</Log>
    <ForEach itemFrom="files_list" where="" sort="">
      <Log>File: {files_list.filename}</Log>
    </ForEach>

    <Log>Here is how to execute a SQL command to the database with id=1</Log>
    <Sql database="1">
      <Execute timeout="30">insert into backups (start_date) values (getdate())</Execute>
    </Sql>

    <Log>Create a database backup and save the filename to the variable bak1</Log>
    <Database id="1">
      <Backup toFile="%programdata%\DoIt\MyDatabase_{now:yyyy-MM-dd_HH-mm}.bak" type="bak" toVar="bak1" />
    </Database>
    
    <Log>Upload the bak1 file to the storage with id=1</Log>
    <Storage id="1">
      <Upload file="{bak1}" toBlob="backups/Backup_{now:yyyy-MM-dd}/{bak1:filename}" deleteSource="true" />
    </Storage>

  </Execute>
</Configuration>
```

### <a id="TheConfigurationFileEncryption">Encryption</a>
You can encrypt/decrypt the connection strings from the configuration file using the following commands:
```shell
C:\DoIt\DoIt.exe /config="C:\DoIt\AnotherConfigFile.config.xml" /encryptionKey="test123"
Configuration file was encrypted.

C:\DoIt\DoIt.exe /config="C:\DoIt\AnotherConfigFile.config.xml" /decryptionKey="test123"
Configuration file was decrypted.
```

To use an encrypted configuration file without decrypting it, you should use the following command:
```shell
C:\DoIt\DoIt.exe /config="C:\DoIt\AnotherConfigFile.config.xml" /cryptKey="test123"
```

## <a id="Settings">Settings</a>

### <a id="SettingsLogFile">LogFile</a>
Use this tag to specify the log path and variable.

*Tag Location: Configuration > Settings > LogFile*
```xml
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Settings>
    <LogFile toVar="logFile">%programdata%\DoIt\DoIt_{now:yyyy-MM-dd}.log</LogFile>
  </Settings>
</Configuration>
```

### <a id="SettingsConnectionStrings">ConnectionStrings</a>
This tag set database and azure storage account connection strings.

*Tag Location: Configuration > Settings > ConnectionStrings*
```xml
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Settings>
    <ConnectionStrings>
      <Database id="1">Data Source=localhost\sql2016express; Initial Catalog=database; Integrated Security=false; User Id=sa; Password=123;</Database>
      <Storage id="1">DefaultEndpointsProtocol=https;AccountName=my_account;AccountKey=871oQKMifslemflIwq54e0fd8sJskdmw98348dMF0suJ0WODK73lMlwiehf34u0mm5ez6MdiewklFH3/w2/IEK==</Storage>
      <MailServer id="1">host=smtp.domain.com; from=user@domain.com; port=587; ssl=true; user=user@domain.com; pass=123;</MailServer>
    </ConnectionStrings>
  </Settings>
</Configuration>
```

### <a id="SettingsExceptions">Exceptions</a>
Use this tag to mail users if an exception occurs.

*Tag Location: Configuration > Settings > Exceptions*
```xml
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Settings>
    <Exceptions mailServer="1" attachLogFile="true">
      <Mail>admin1@company.com</Mail>
      <Mail>admin2@company.com</Mail>
    </Exceptions>
  </Settings>
</Configuration>
```

## <a id="ExecuteCommands">Execute Commands</a>

### <a id="ExecuteDatabase">Database</a>

#### <a id="ExecuteDatabaseBackup">Backup</a>
Backup SQL Server databases.

*Tag Location: Configuration > Execute > Database > Backup*
```xml
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
```xml
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
```xml
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
```xml
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
```xml
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
```xml
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
```xml
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
```xml
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
```xml
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
```xml
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
```xml
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <Mail server="1" to="other_user@domain.com" subject="Hello World">
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
```xml
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
```xml
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
```xml
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
```xml
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
```xml
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
```xml
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
```xml
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
```xml
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
```xml
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
```xml
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
```xml
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
```xml
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
```xml
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
```xml
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <DataTable>
      <SetRowValue data="users_list" where="">
        <Column name="is_active" type="int">1</Column>
      </SetRowValue>
    </DataTable>
  </Execute>
</Configuration>
```

#### <a id="ExecuteDataTableGetDataRow">GetDataRow</a>
Find the rows that matches the where condition and set one of them to the specified variable.

*Tag Location: Configuration > Execute > DataTable > GetDataRow*
```xml
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
```xml
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
```xml
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
```xml
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
```xml
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
```xml
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

#### <a id="ExecuteDataTableFilter">Filter</a>
Filter rows in the specified datatable.

*Tag Location: Configuration > Execute > DataTable > Filter*
```xml
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <DataTable>
      <Filter data="users_list" where="is_active=1" to="active_users_list" />
    </DataTable>
  </Execute>
</Configuration>
```

### <a id="ExecuteSetValue">SetValue</a>

#### <a id="ExecuteSetValueCalc">Calc</a>
Execute a simple operation using the specified values.

*Tag Location: Configuration > Execute > SetValue > Calc*
```xml
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
```xml
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
```xml
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
```xml
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
```xml
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
```xml
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
```xml
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
```xml
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
```xml
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
```xml
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
```xml
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
```xml
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
```xml
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <Storage id="1">
      <ListBlobs to="blobs_list" container="container{now:yyyyMM}" prefix="" fetchAttributes="false" details="none|snapshots|metadata" where="" sort="" regex="" />
    </Storage>
  </Execute>
</Configuration>
```

#### <a id="ExecuteStorageCopy">Copy</a>
Copies a blob from one storage account to another.

*Tag Location: Configuration > Execute > Storage > Copy*
```xml
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <Storage id="1">
      <Copy blob="my_container1/my_blob.txt" toStorage="2" toBlob="my_container2/my_blob.txt" />
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
```xml
<Condition type="has-disk-space" drive="C:\" min="10000">
  <True>
    <Log>True Result</Log>
  </True>
</Condition>
```

#### Sample2 - Condition Type: file-exists
```xml
<Condition type="file-exists" path="C:\MyFile.txt">
  <True>
    <Log>True Result</Log>
  </True>
</Condition>
```

#### Sample3 - Condition Type folder-exists
```xml
<Condition type="folder-exists" path="C:\MyFolder">
  <True>
    <Log>True Result</Log>
  </True>
</Condition>
```

#### Sample4 - Condition Type: has-rows
```xml
<Condition type="has-rows" data="customers_list">
  <True>
    <Log>True Result</Log>
  </True>
</Condition>
```

#### Sample5 - Condition Type: is-datetime
```xml
<Condition type="is-datetime" days="all|mon|wed|fri|1|15" time="08-12">
  <True>
    <Log>True Result</Log>
  </True>
</Condition>
```

#### Sample6 - Condition Type: if
```xml
<Condition type="if" value1="{files_count}" value2="0" comparison="greater" valueType="numeric">
  <True>
    <Log>True Result</Log>
  </True>
  <False>
    <Log>False Result</Log>
  </False>
</Condition>
```

### <a id="ExecuteFtp">Ftp</a>

#### <a id="ExecuteFtpList">List</a>
List all files of a given folder using the connection data provided. The returned datatable contains the following columns:

* name (string)
* type (string) = "folder" | "file"
* length (long)
* datetime (DateTime)

*Tag Location: Configuration > Execute > Ftp > List*
```xml
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <Ftp host="ftps://mydomain.com" port="21" user="user_name" password="password123">
      <List path="wwwroot" to="files_list" />
    </Ftp>
  </Execute>
</Configuration>
```

#### <a id="ExecuteFtpDownload">Download</a>
Download the specified file using the connection data provided.

*Tag Location: Configuration > Execute > Ftp > Download*
```xml
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <Ftp host="ftps://mydomain.com" port="21" user="user_name" password="password123">
      <Download path="wwwroot/myfile.zip" toFile="%programdata%\DoIt\myfile.zip" />
    </Ftp>
  </Execute>
</Configuration>
```

#### <a id="ExecuteFtpUpload">Upload</a>
Upload the specified file using the connection data provided.

*Tag Location: Configuration > Execute > Ftp > Upload*
```xml
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <Ftp host="ftps://mydomain.com" port="21" user="user_name" password="password123">
      <Upload file="%programdata%\DoIt\myfile.zip" toPath="wwwroot/myfile.zip"  />
    </Ftp>
  </Execute>
</Configuration>
```

#### <a id="ExecuteFtpCreateFolder">CreateFolder</a>
Create the specified folders using the connection data provided.

*Tag Location: Configuration > Execute > Ftp > CreateFolder*
```xml
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <Ftp host="ftps://mydomain.com" port="21" user="user_name" password="password123">
      <CreateFolder path="wwwroot/myfolder/level2/level3" />
    </Ftp>
  </Execute>
</Configuration>
```

#### <a id="ExecuteFtpDeleteFolder">DeleteFolder</a>
Delete the specified folder using the connection data provided.

*Tag Location: Configuration > Execute > Ftp > DeleteFolder*
```xml
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <Ftp host="ftps://mydomain.com" port="21" user="user_name" password="password123">
      <DeleteFolder path="wwwroot/myfolder" />
    </Ftp>
  </Execute>
</Configuration>
```

#### <a id="ExecuteFtpDeleteFile">DeleteFile</a>
Delete the specified file using the connection data provided.

*Tag Location: Configuration > Execute > Ftp > DeleteFile*
```xml
<?xml version="1.0" encoding="utf-16" ?>
<Configuration>
  <Execute>
    <Ftp host="ftps://mydomain.com" port="21" user="user_name" password="password123">
      <DeleteFile path="wwwroot/myfile.zip" />
    </Ftp>
  </Execute>
</Configuration>
```
