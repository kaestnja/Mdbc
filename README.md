
[![NuGet](https://buildstats.info/nuget/Mdbc)](https://www.nuget.org/packages/Mdbc)
[![PSGallery](https://img.shields.io/powershellgallery/dt/Mdbc.svg)](https://www.powershellgallery.com/packages/Mdbc)

# Mdbc

MongoDB Cmdlets for PowerShell

***

Mdbc is the PowerShell module based on the official [MongoDB C# driver](https://github.com/mongodb/mongo-csharp-driver).
Mdbc makes MongoDB data and operations PowerShell friendly.

- The PSGallery package is for PowerShell Core and PowerShell v5.1 .NET 4.7.2
- The NuGet package is for PowerShell v3-v5.1, .NET 4.7.2

## Quick start

**Step 1:** Get and install

**Package from PSGallery**

Mdbc for PowerShell Core and v5.1 is published as the PSGallery module [Mdbc](https://www.powershellgallery.com/packages/Mdbc).

You can install the module by this command:

```powershell
Install-Module Mdbc
```

**Package from NuGet**

Mdbc for PowerShell v3-v5.1 is published as the NuGet package [Mdbc](https://www.nuget.org/packages/Mdbc).
Download it by NuGet tools or [directly](http://nuget.org/api/v2/package/Mdbc).
In the latter case save it as *".zip"* and unzip. Use the package subdirectory *"tools/Mdbc"*.

Copy the directory *Mdbc* to one of the PowerShell module directories, see
`$env:PSModulePath`, for example like this:

    C:/Users/<User>/Documents/WindowsPowerShell/Modules/Mdbc

**Step 2:** In a PowerShell command prompt import the module:

```powershell
Import-Module Mdbc
```

**Step 3:** Take a look at help and available commands:

```powershell
help about_Mdbc
help Connect-Mdbc -Full
Get-Command -Module Mdbc
```

**Step 4:** Make sure mongod is running and try some commands:

```powershell
# Load the module
Import-Module Mdbc

# Connect the new collection test.test
Connect-Mdbc . test test -NewCollection

# Add two documents
@{_id = 1; value = 42}, @{_id = 2; value = 3.14} | Add-MdbcData

# Get documents as PS objects
Get-MdbcData -As PS | Format-Table

# Get the document by _id
Get-MdbcData @{_id = 1}

# Update the document, set 'value' to 100
Update-MdbcData @{_id = 1} @{'$set' = @{value = 100}}

# Get the document again, 'value' is 100
$doc = Get-MdbcData @{_id = 1}

# Remove the document
$doc | Remove-MdbcData

# Count documents, 1
Get-MdbcData -Count
```

## Next Steps

[HowTo.test.ps1]: https://github.com/nightroman/Mdbc/blob/master/Tests/HowTo.test.ps1
[Classes.lib.ps1]: https://github.com/nightroman/Mdbc/blob/master/Tests/Classes.lib.ps1
[Classes.test.ps1]: https://github.com/nightroman/Mdbc/blob/master/Tests/Classes.test.ps1
[BsonFile]: https://github.com/nightroman/BsonFile

Read cmdlet help topics and take a look at examples for some basic use cases.

Use *Scripts/Mdbc.ArgumentCompleters.ps1* for database and collection name completion and property completion.
*Scripts/Update-MongoFiles.ps1* is a toy for making test data but may be useful for tracking file changes.
See also tests, for example:

- [HowTo.test.ps1] - assorted how-to samples
- [Classes.lib.ps1] - PowerShell classes for saving and reading
- [Classes.test.ps1] - tests using the above classes are how-to samples
- [BsonFile] - Mdbc based script module for BSON/JSON file collections

## Driver methods and module commands

| Driver | Module  | Output
| :----- | :-----  | :-----
| **Client** | |
| MongoClient | Connect-Mdbc | $Client $Database $Collection
| GetDatabase | Get-MdbcDatabase | database(s)
| DropDatabase | Remove-MdbcDatabase | none
| Transactions | Use-MdbcTransaction | -
| Watch | Watch-MdbcChange -Client | cursor
| **Database** | |
| RunCommand | Invoke-MdbcCommand | document
| GetCollection | Get-MdbcCollection | collection(s)
| CreateCollection | Add-MdbcCollection | none
| RenameCollection | Rename-MdbcCollection | none
| DropCollection | Remove-MdbcCollection | none
| Watch | Watch-MdbcChange -Database | cursor
| **Collection** | |
| InsertOne | Add-MdbcData | none
| Find | Get-MdbcData | documents
| CountDocuments | Get-MdbcData -Count | count
| Distinct | Get-MdbcData -Distinct | values
| FindOneAndDelete | Get-MdbcData -Remove | old document
| FindOneAndReplace | Get-MdbcData -Set | old or new document
| FindOneAndUpdate | Get-MdbcData -Update | old or new document
| DeleteOne | Remove-MdbcData | none or info (-Result)
| DeleteMany | Remove-MdbcData -Many | none or info (-Result)
| ReplaceOne | Set-MdbcData | none or info (-Result)
| UpdateOne | Update-MdbcData | none or info (-Result)
| UpdateMany | Update-MdbcData -Many | none or info (-Result)
| Aggregate | Invoke-MdbcAggregate | documents
| Watch | Watch-MdbcChange -Collection | cursor

## See also

- [Mdbc Release Notes](https://github.com/nightroman/Mdbc/blob/master/Release-Notes.md)
- [about_Mdbc.help.txt](https://github.com/nightroman/Mdbc/blob/master/Module/en-US/about_Mdbc.help.txt)
- [Ldbc, similar project for LiteDB](https://github.com/nightroman/Ldbc)
