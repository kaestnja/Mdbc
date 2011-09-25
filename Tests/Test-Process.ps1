
<#
.SYNOPSIS
	Tests the Mdbc module with some process data

.NOTES
	TotalProcessorTime
		It can be null.
		Save as double, TimeSpan is not BSON type.
#>

Set-StrictMode -Version 2

Import-Module Mdbc
$collection = Connect-Mdbc mongodb://localhost test process -NewCollection

# Input: [System.Diagnostics.Process]
# Output: document with process and its module data
filter New-Document
{
	# create a document explicitly by New-MdbcData
	$process = New-MdbcData -DocumentId $_.Id
	$process.Name = $_.Name
	$process.HandleCount = $_.HandleCount
	$process.WorkingSet = $_.WorkingSet
	$process.PrivateMemorySize = $_.PrivateMemorySize
	$process.StartTime = $_.StartTime
	$span = $_.TotalProcessorTime
	if ($span) {
		$process.TotalProcessorTime = $_.TotalProcessorTime.TotalMinutes
	}
	# array of nested documents for modules
	# documents are created implicitly from dictionaries
	$process.Modules = @(
		$_.Modules | %{
			try {
				@{
					FileName = $_.FileName
					ModuleMemorySize = $_.ModuleMemorySize
				}
			}
			catch {}
		}
	)
	# output the document
	$process
}

### Insert
Get-Process | New-Document | Add-MdbcData $collection -ea Continue

### Upsert
Get-Process | New-Document | Add-MdbcData -Update $collection -ea Continue

### Count
$collection | Format-List | Out-String
$n1 = $collection.FindAll().Count()
$n2 = Get-MdbcData $collection -Count
if ($n1 -ne $n2) { throw }
"Count : $n1"

### Get by name
$1 = Get-MdbcData $collection (query Name svchost) -Count
$2 = $collection.Find([MongoDB.Driver.QueryDocument]@{ Name = 'svchost' }).Count()
$3 = $collection.Find([MongoDB.Driver.Builders.Query]::EQ('Name', 'svchost')).Count()
if ($1 -ne $2) { throw }
if ($1 -ne $3) { throw }
"Find svchost : $1"

### Get by pattern/where
$1 = Get-MdbcData $collection (query Name -Match '^svc|^mon') -Count
$2 = $collection.Find([MongoDB.Driver.Builders.Query]::Matches('Name', '^svc|^mon')).Count()
$3 = $collection.Find([MongoDB.Driver.QueryDocument]@{ '$where' = 'this.Name == "svchost" || this.Name == "mongod"' }).Count()
if ($1 -ne $2) { throw }
if ($1 -ne $3) { throw }
"Find regex : $$"
"Find where : $$"

Get-MdbcData $collection (query Name mongod) | Update-MdbcData $collection (update HandleCount -Increment 1)
$document = Get-MdbcData $collection (query Name mongod)

$document | Convert-MdbcData | Format-List | Out-String
$document | Convert-MdbcJson