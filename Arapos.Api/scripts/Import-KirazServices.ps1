param(
    [string]$ExcelPath = "",
    [string]$SqlServer = "(localdb)\MSSQLLocalDB",
    [string]$Database = "ARAPOS",
    [Guid]$TenantId = "2CC3C972-8E4B-424E-B3B8-33AC7B6ACDFF",
    [Guid]$BranchId = "852E29AA-3AE9-43BD-9C04-13BE9506FBB0",
    [string]$CurrencyCode = "AED"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-XlsxRows {
    param([string]$Path)

    Add-Type -AssemblyName System.IO.Compression.FileSystem

    $zip = [System.IO.Compression.ZipFile]::OpenRead($Path)
    try {
        $sharedStrings = @()
        $sharedEntry = $zip.Entries | Where-Object { $_.FullName -eq "xl/sharedStrings.xml" }
        if ($sharedEntry) {
            $reader = [System.IO.StreamReader]::new($sharedEntry.Open())
            try {
                $xml = [xml]$reader.ReadToEnd()
            }
            finally {
                $reader.Dispose()
            }

            foreach ($si in $xml.sst.si) {
                $parts = @()
                $plainText = $si.PSObject.Properties["t"]
                $runs = $si.PSObject.Properties["r"]

                if ($null -ne $plainText -and $null -ne $plainText.Value) {
                    $parts += [string]$si.t
                }

                if ($null -ne $runs -and $null -ne $runs.Value) {
                    foreach ($run in $runs.Value) {
                        $runText = $run.PSObject.Properties["t"]
                        if ($null -ne $runText -and $null -ne $runText.Value) {
                            $parts += [string]$run.t
                        }
                    }
                }

                $sharedStrings += ($parts -join "")
            }
        }

        $sheetEntry = $zip.Entries | Where-Object { $_.FullName -eq "xl/worksheets/sheet1.xml" }
        if (-not $sheetEntry) {
            throw "Worksheet sheet1.xml was not found in the Excel file."
        }

        $sheetReader = [System.IO.StreamReader]::new($sheetEntry.Open())
        try {
            $sheetXml = [xml]$sheetReader.ReadToEnd()
        }
        finally {
            $sheetReader.Dispose()
        }

        $ns = [System.Xml.XmlNamespaceManager]::new($sheetXml.NameTable)
        $ns.AddNamespace("x", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")

        $rows = $sheetXml.SelectNodes("//x:sheetData/x:row", $ns)
        foreach ($row in $rows) {
            $values = @()
            foreach ($cell in $row.c) {
                $value = ""
                $cellType = $cell.PSObject.Properties["t"]
                $cellValue = $cell.PSObject.Properties["v"]
                $inlineString = $cell.PSObject.Properties["is"]

                if ($null -ne $cellType -and $cellType.Value -eq "s") {
                    $index = [int]$cellValue.Value
                    if ($index -lt $sharedStrings.Count) {
                        $value = $sharedStrings[$index]
                    }
                }
                elseif ($null -ne $inlineString -and $null -ne $inlineString.Value) {
                    $inlineText = $inlineString.Value.PSObject.Properties["t"]
                    if ($null -ne $inlineText -and $null -ne $inlineText.Value) {
                        $value = [string]$inlineText.Value
                    }
                }
                elseif ($null -ne $cellValue -and $null -ne $cellValue.Value) {
                    $value = [string]$cellValue.Value
                }

                $values += $value
            }

            if ($values.Count -gt 0) {
                [pscustomobject]@{
                    NameAr = if ($values.Count -ge 1) { $values[0].Trim() } else { "" }
                    NameEn = if ($values.Count -ge 2) { $values[1].Trim() } else { "" }
                    PriceText = if ($values.Count -ge 3) { $values[2].Trim() } else { "" }
                }
            }
        }
    }
    finally {
        $zip.Dispose()
    }
}

function ConvertTo-BasePrice {
    param([string]$PriceText)

    if ([string]::IsNullOrWhiteSpace($PriceText)) {
        return $null
    }

    $matches = [regex]::Matches($PriceText, "\d+(\.\d+)?")
    if ($matches.Count -eq 0) {
        return $null
    }

    $numbers = foreach ($match in $matches) {
        [decimal]::Parse($match.Value, [System.Globalization.CultureInfo]::InvariantCulture)
    }

    ($numbers | Measure-Object -Minimum).Minimum
}

function Escape-SqlText {
    param([string]$Value)

    if ($null -eq $Value) {
        return ""
    }

    $Value.Replace("'", "''")
}

if ([string]::IsNullOrWhiteSpace($ExcelPath)) {
    $downloadMatch = Get-ChildItem -LiteralPath "C:\Users\kiraz\Downloads" -Filter "*.xlsx" |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if (-not $downloadMatch) {
        throw "Excel file was not provided and no workbook was found in Downloads."
    }

    $ExcelPath = $downloadMatch.FullName
}

if (-not (Test-Path -LiteralPath $ExcelPath)) {
    throw "Excel file not found: $ExcelPath"
}

$rows = Get-XlsxRows -Path $ExcelPath |
    Where-Object {
        -not [string]::IsNullOrWhiteSpace($_.NameAr) -and
        -not [string]::IsNullOrWhiteSpace($_.NameEn) -and
        $_.NameEn -ne "English"
    } |
    ForEach-Object {
        $basePrice = ConvertTo-BasePrice -PriceText $_.PriceText
        if ($null -eq $basePrice) {
            return
        }

        [pscustomobject]@{
            NameAr = $_.NameAr
            NameEn = $_.NameEn
            Price = $basePrice
            PriceCents = [int][Math]::Round($basePrice * 100, [System.MidpointRounding]::AwayFromZero)
            PriceText = $_.PriceText
        }
    } |
    Sort-Object NameAr, NameEn -Unique

if (-not $rows -or $rows.Count -eq 0) {
    throw "No service rows were parsed from the Excel file."
}

$sqlLines = @(
    "SET NOCOUNT ON;",
    "DECLARE @TenantId uniqueidentifier = '$TenantId';",
    "DECLARE @BranchId uniqueidentifier = '$BranchId';"
)

$rowIndex = 0
foreach ($row in $rows) {
    $rowIndex++
    $nameAr = Escape-SqlText $row.NameAr
    $nameEn = Escape-SqlText $row.NameEn
    $currency = Escape-SqlText ($CurrencyCode.ToUpperInvariant())
    $serviceVar = "@ServiceId$rowIndex"
    $servicePriceVar = "@ServicePriceId$rowIndex"

    $sqlLines += @(
        "",
        "IF NOT EXISTS (",
        "    SELECT 1",
        "    FROM Services",
        "    WHERE TenantId = @TenantId",
        "      AND BranchId = @BranchId",
        "      AND ISNULL(NameAr, N'') = N'$nameAr'",
        "      AND ISNULL(NameEn, '') = '$nameEn'",
        ")",
        "BEGIN",
        "    DECLARE $serviceVar uniqueidentifier = NEWID();",
        "    DECLARE $servicePriceVar uniqueidentifier = NEWID();",
        "",
        "    INSERT INTO Services (Id, TenantId, BranchId, NameAr, NameEn, DurationMin, IsActive, CreatedAt)",
        "    VALUES ($serviceVar, @TenantId, @BranchId, N'$nameAr', '$nameEn', NULL, 1, SYSUTCDATETIME());",
        "",
        "    INSERT INTO ServicePrices (Id, TenantId, BranchId, ServiceId, PriceCents, CurrencyCode, IsActive, CreatedAt)",
        "    VALUES ($servicePriceVar, @TenantId, @BranchId, $serviceVar, $($row.PriceCents), '$currency', 1, SYSUTCDATETIME());",
        "END"
    )
}

$tempSqlPath = Join-Path $PSScriptRoot "import-kiraz-services.generated.sql"
$sqlLines | Set-Content -LiteralPath $tempSqlPath -Encoding UTF8

Write-Host ("Prepared {0} services for import into Kiraz / Abu Dhabi." -f $rows.Count)
Write-Host ("Generated SQL file: {0}" -f $tempSqlPath)

& sqlcmd -S $SqlServer -d $Database -i $tempSqlPath

Write-Host "Import command finished."
