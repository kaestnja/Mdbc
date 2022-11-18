# Mdbc Release Notes 4.0.0 - 5.2.2

## v5.2.2

Amend manifest of PSGallery package:

    PowerShellVersion = '5.1'
    DotNetFrameworkVersion = '4.7.1'

## v5.2.1

.NET driver 2.9.2.

## v5.2.0

.NET driver 2.9.1.

**Potentially breaking package change.**
The PSGallery package is built for PowerShell v5 and PowerShell Core.

The NuGet package is built for PowerShell v3, v4, v5 (same as before).

## v5.1.5

.NET driver 2.8.0.
Note that it requires net452 instead of the old minimum net45.

## v5.1.4

.NET driver 2.7.3

## v5.1.3

- .NET driver 2.7.2
- Remove `Add-MdbcCollection` switch `AutoIndexId` (deprecated by the driver).

## v5.1.2

- .NET driver 2.6.1
- Convert the project to .NET SDK

## v5.1.1

Fix #19, `New-MdbcData` with `Id` as script block.

## v5.1.0

MongoDB 3.6 and .NET driver 2.5.0

**Potentially incompatible change:** `GuidRepresentation.Standard` is used for
GUID's as the default value instead of `CSharpLegacy`. In order to use the old
default value, set the environment variable `Mdbc_GuidRepresentation` to
`CSharpLegacy` before using Mdbc.

Updates in expressions:

- retired `$pushAll` is still supported in Mdbc but internally uses `$push $each`.
- `$push` supports negative positions as counted from the end.
- `$pop` accepts 1 and -1.

## v5.0.1

- Require PS 3.0 in the module manifest.
- Add missing */Scripts* to the package.
- Tidy up after moving to new driver.

## v5.0.0

C# driver 2.4.4 and .NET Framework 4.5

## v4.8.9

Adjust QueryCompiler $size invalid cases to Mongo 3.4.1

## v4.8.8

Packaged and published as PSGallery module.

Fixed potential variable conflicts in `Mdbc.ArgumentCompleters.ps1`.

## v4.8.7

New `Mdbc.ArgumentCompleters.ps1` for v5 native, TabExpansionPlusPlus, and the
latest TabExpansion2.ps1. It replaces `TabExpansion.Mdbc.ps1` which works with
TabExpansion2.ps1 only.

## v4.8.6

`New-MdbcQuery`: new parameter `TypeAlias` for type aliases (MongoDB 3.2).

## v4.8.5

C# driver 1.11, tested with MongoDB 3.2

## v4.8.4

C# driver 1.10.1, tested with MongoDB 3.0.5

## v4.8.3

C# driver 1.10, MongoDB 3.0.0-rc7

Possibly incompatible change. Due to the server and driver changes cmdlets do
not write server results on exceptions. Similar server data are available in
error inner exceptions, e.g. the property `Result` (`BsonDocument`).

## v4.8.2

C# driver v1.9.2

## v4.8.1

Fixed #3 (`Get-MdbcData -SortBy` with file collections).

## v4.8.0

Resolved #2. `Mdbc.Dictionary` gets null on `$data['missing-key']`.

## v4.7.3

C# driver v1.9.1

## v4.7.2

`Connect-Mdbc`: New parameter `Timeout`: Determines the maximum time to wait before timing out.

## v4.7.0

C# driver v1.9. Tested with MongoDB v2.6.0-rc3

*File collections*

- Update operations check for invalid element names, as in the core.
- Adjusted operation behaviour to corresponding changes in the core.

`Invoke-MdbcAggregate`

- Uses the cursor output mode.
- New parameters `BatchSize`, `MaxTime`, `AllowDiskUse`.
- Renamed parameter `Operation` to `Pipeline`, as in the driver.

`New-MdbcUpdate`

- New parameters: `Mul`, `Xor`, `Min`, `Max`, `CurrentDate`.

## v4.6.0

New parameter `Retry` of `Export-MdbcData` tells to retry on failures to open
the file and specifies one or two arguments. The first is the retry timeout.
The second is the retry interval, the default is 50 milliseconds.

For example, it can be used together with `Append` for adding data to the same
file by several writers simultaneously.

## v4.5.1

Input JSON format. It does not have to be one object per line. It is a sequence
of objects and arrays of objects. Arrays are unrolled. Top objects and arrays
are optionally separated by spaces, tabs, and new lines.

As a result, output of PowerShell `ConvertTo-Json` (V3) saved to a file can be
read and processed by Mdbc cmdlets:

    Get-Process | ConvertTo-Json -Depth 1 | Set-Content process.json
    Open-MdbcFile process.json -Simple
    Get-MdbcData @{Name='mongod'}

## v4.5.0

**JSON file collections and data files**

Cmdlets `Open-MdbcFile`, `Save-MdbcFile`, `Import-MdbcData`, `Export-MdbcData`
support JSON format in addition to BSON. New parameter `FileFormat` specifies
`Auto`, `Bson`, or `Json`. `Auto` is the default, format is defined by the file
extension: *.json* is for JSON, others are for BSON.

## v4.4.6

Fixed a case with Mdbc serialization registered too late.
All cmdlets now ensure this registration first of all.

## v4.4.5

`Export-MdbcData`

- The parameter `InputObject` is positional, similar to `Add-MdbcData` and
  `New-MdbcData`

`Script\TabExpansionProfile.Mdbc.ps1`

- Added completers for `Property` arguments of `Add-MdbcData`, `New-MdbcData`,
  and `Export-MdbcData`. If you use the suggested `TabExpansion2.ps1` update
  it, too.

## v4.4.4

`IConvertibleToBsonDocument`

- `Mdbc.Dictionary` implements `MongoDB.Bson.IConvertibleToBsonDocument`.
- `IConvertibleToBsonDocument` is used instead of `Mdbc.Dictionary`, where
  appropriate. Thus, more types are supported by some operations, e.g.
  `Add-MdbcData`.
- Renamed `Mdbc.Dictionary.Document()` to `Mdbc.Dictionary.ToBsonDocument()`.
  This change is potentially breaking. But normally scripts should not use
  this method.

## v4.4.3

New `Script\TabExpansionProfile.Mdbc.ps1` adds TabExpansion2 completers for
arguments of `Connect-Mdbc` parameters `DatabaseName` and `CollectionName`
(requires PowerShell v3).

## v4.4.1

`Remove-MdbcData`, `Update-MdbcData`

- Removed obsolete parameter `Modes`, use switches `One` in `Remove-MdbcData`,
  `Add` and `All` in `Update-MdbcData`. Note that defaults (all and one) are
  different, Mdbc follows the driver API in this case.

`Scripts\Update-MongoFiles.ps1`

- Parameter `Path` accepts one or more paths.

## v4.4.0

Implicit conversion of `Mdbc.Dictionary` and `BsonDocument` objects to `_id`
queries is not supported. Objects are converted to queries with all elements.
Old queries may be broken or not but they should be redesigned in any case.

`Remove-MdbcData`

- New switch `One` is used instead of `Modes`.
- `Modes` will be removed in the next release.

`Update-MdbcData`

- New switches `Add` and `All` are used instead of `Modes`.
- `Modes` will be removed in the next release.

`Get-MdbcData`

- New parameter `ResultVariable`.

File collections

- Fixed distinct queries with document values.
- Optimized `_id` queries for normal file collections.

`Scripts\Update-MongoFiles.ps1`

- New switch `Log` tells to log changes to *files_log*.
- The script outputs an object with some statistics.

*Mdbc.Format.ps1xml*

- Removed `BsonValue` type formats from the module. Need in direct use of these
  types is gradually reduced to minimum, scripts normally should not use them.

## v4.3.1

File collection queries:

- Support `$in` and `$nin` with regular expressions and `$all` with
  `$elemMatch` and regular expressions.
- `$mod` arguments are processed like in MongoDB.
- Improved input validation and error messages.

## v4.3.0

The new concept of bson file collections is stabilized.

`Get-MdbcData`

Removed the parameter `Cursor` as not adding much value. Cursors can be
obtained from  a collection and then used exactly in the same way as before.

`Add-MdbcData`, `Remove-MdbcData`, `Update-MdbcData`

- Parameter `Result` supports file collections and gets similar information.
- Reduced error processing differences between MongoDB and file collections.

`Scripts\Mdbc.ps1`

- Without parameters it just loads the helpers and does not connect.
- Operator shortcut variables are read only.

## v4.2.0

### Bson file collections

This release introduces bson data file collections which do not require
MongoDB. They are opened and saved by new cmdlets `Open-MdbcFile` and
`Save-MdbcFile` and support commands `Get-MdbcData`, `Add-MdbcData`,
`Remove-MdbcData`, and `Update-MdbcData`.

See FILE COLLECTIONS in help:

    Import-Module Mdbc; help about_Mdbc

The concept is yet experimental and features may change.

### Other changes

`New-MdbcQuery`:

- Parameter `Mod` accepts `long` values
- Some parameters accept nulls if they are accepted in native queries

`Get-MdbcData`

- Parameters `Remove` and `Update` can be used with `As`.
- Parameters `First` and `Last` are not used together.
- Parameter `Property` also accepts `IMongoFields`.
- Fixed `Last` with `Count`.

`Add-MdbcData`, `Remove-MdbcData`, `Update-MdbcData`

- If `Result` is specified then it is written on non terminating errors.

`Invoke-MdbcCommand`

- The response is written on non terminating errors.

`Update-MongoFiles.ps1` and `Get-MongoFile.ps1`:

- New named parameter `CollectionName`
- The current PowerShell path is used instead of the current directory

## v4.1.0

### New cmdlet `Invoke-MdbcAggregate`

The driver currently provides just a raw API for aggregate operations. So does
this cmdlet. When the API change the cmdlet will be redesigned.

### `Get-MdbcData`, `Import-MdbcData`, `Invoke-MdbcMapReduce`

Removed `AsCustomObject`. The parameter `As` is used for all types, including
two new, `Lazy` and `Raw`. The argument is either a type or a shortcut enum.

The result cursor always represents documents according to the type `As`.

### `New-MdbcData`, `Add-MdbcData`, `Export-MdbcData`

If `_id` is presented more than once by `Id`, `NewId`, `Property` or an input
object then an exception is thrown.

## v4.0.0

C# driver 1.8.3

This release introduces breaking changes in cmdlets redesigned for cleaner and
easier to use interface. Also, some parameters were renamed in order to follow
C# driver method names.

### `New-MdbcQuery`

Query operator parameters cannot be used together in a single call.

Renamed parameters:

- `GE` -> `GTE`
- `LE` -> `LTE`
- `Match` -> `Matches`
- `Matches` -> `ElemMatch`

Changed parameters:

- switch `Not` becomes a parameter with an argument
- parameter `Exists <bool>` is replaced by switches `Exists`, `NotExists`

### `New-MdbcUpdate`

Changed parameters so that they all can be used together and each parameter can
specify several fields with arguments. As a result, a single call with several
parameters and/or arguments may create complex updates.

Renamed parameters:

- `Band` -> `BitwiseAnd`
- `Bor` -> `BitwiseOr`
- `Increment` -> `Inc`

### `New-MdbcData`

Parameter `Value` makes `BsonValue` only, including `BsonDocument`, `BsonArray`.

### Other changes

`Get-MdbcData` and `Update-MdbcData` catch `MongoException` and write it as non
terminating error, i.e. depending on the error action processing may continue.

If none of parameters `ConnectionString`, `DatabaseName`, `CollectionName` is
specified for `Connect-Mdbc` then they are assumed to be `.`, `test`, `test`.

Fixed several issues with null arguments in cmdlets and nulls in documents.

Fixed false positive cyclic reference errors.

----

[Other versions](https://github.com/nightroman/Mdbc/tree/main/About)
