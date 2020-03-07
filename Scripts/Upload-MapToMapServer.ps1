[CmdletBinding(SupportsShouldProcess)]
Param(
    [Parameter(Mandatory, ValueFromPipeline)]
    [System.IO.FileInfo]$File,
    [Parameter(Mandatory)]
    [string]$ResourceGroupName,
    [Parameter(Mandatory)]
    [string]$StorageAccountName,
    [Parameter(Mandatory)]
    [string]$StorageContainerName,
    [switch]$UpdateMetadata
)

begin {
    Import-Module Az
    $storageAccount = Get-AzStorageAccount -ResourceGroupName $ResourceGroupName -Name $StorageAccountName
    $context = $storageAccount.Context
}
process {
    function Get-MapArchive {
        Param([System.IO.FileInfo]$File)

        $archivePath = [IO.Path]::ChangeExtension($File.FullName, "zip")
        if (Test-Path -LiteralPath $archivePath) {
            Write-Verbose "Archive already exists $archivePath"
        }
        else {
            Write-Verbose "Creating archive $archivePath"

            [System.IO.Compression.ZipArchive]$zipFile = [System.IO.Compression.ZipFile]::Open($archivePath, [System.IO.Compression.ZipArchiveMode]::Create)
            [System.IO.Compression.ZipArchiveEntry]$zipEntry = $zipFile.CreateEntry($File.Name)
            [System.IO.Stream]$entryStream = $zipEntry.Open();
            [System.IO.FileStream]$fileStream = [System.IO.File]::OpenRead($File.FullName);
            $fileStream.CopyTo($entryStream);
            
            $entryStream.Close()
            $entryStream.Dispose()
            $fileStream.Close()
            $entryStream.Dispose()
            $fileStream.Dispose()
            $zipFile.Dispose()
        }
        Get-Item -LiteralPath $archivePath
    }

    function Get-PartMetadata {
        Param([System.IO.FileInfo]$File)

        $partData = [System.Byte[]]::new(1048576)
        $md5 = New-Object -TypeName System.Security.Cryptography.MD5CryptoServiceProvider

        $partIndex = 1
        [System.IO.FileStream]$fileStream = [System.IO.File]::OpenRead($File.FullName);
        while ($fileStream.Position -lt $fileStream.Length) {
            $partOffset = $fileStream.Position
            $partSize = 1048576
            if (($fileStream.Position + $partSize) -gt $fileStream.Length) {
                $partSize = $fileStream.Length - $fileStream.Position
            }

            $fileStream.Read($partData, 0, $partSize) | Out-Null
            $partMD5 = [System.BitConverter]::ToString($md5.ComputeHash($partData, 0, $partSize)) -replace "-", ""

            [PSCustomObject]@{
                Name        = "{0}.{1:d3}" -f $File.Name, $partIndex
                Index       = $partIndex
                Size        = $partSize
                MD5         = $partMD5
                StartOffset = $partOffset
            }

            $partIndex += 1
        }
    }

    function Get-MapMetadata {
        Param(
            [System.IO.FileInfo]$MapFile,
            [System.IO.FileInfo]$ArchiveFile
        )
        $mapFileMD5 = Get-FileHash -LiteralPath $MapFile.FullName -Algorithm MD5
        $archiveFileMD5 = Get-FileHash -LiteralPath $ArchiveFile.FullName -Algorithm MD5

        $metadataPath = [IO.Path]::ChangeExtension($File.FullName, "json")
        $metadata = [PSCustomObject]@{
            UncompressedName = $File.Name
            UncompressedMD5  = $mapFileMD5.Hash
            UncompressedSize = $File.Length
            CompressedName   = $ArchiveFile.Name
            CompressedMD5    = $archiveFileMD5.Hash
            CompressedSize   = $ArchiveFile.Length
            Parts            = Get-PartMetadata -File $ArchiveFile
        }

        $metadata | ConvertTo-Json | Out-File -LiteralPath $metadataPath -Force
        Get-Item -LiteralPath $metadataPath
    }

    Write-Verbose "Processing: $($File.Name)"

    $foundMetadata = Get-AzStorageBlob -Container $StorageContainerName -Blob "$($File.BaseName).json" -Context $Context -ErrorAction SilentlyContinue
    if ($null -ne $foundMetadata -and -not $UpdateMetadata) {
        Write-Verbose "Map already present in storage"
    }
    else {
        $archiveFile = Get-MapArchive -File $File

        $archiveBlob = Get-AzStorageBlob -Container $StorageContainerName -Blob $archiveFile.Name -Context $Context -ErrorAction SilentlyContinue
        if ($archiveBlob) {
            Write-Verbose "Map archive already present in storage"
        }
        else {
            Set-AzStorageBlobContent -File $archiveFile.FullName -Container $StorageContainerName -Blob $archiveFile.Name -Context $context -Force
        }

        $metadataFile = Get-MapMetadata -MapFile $File -ArchiveFile $archiveFile
        Set-AzStorageBlobContent -File $metadataFile.FullName -Container $StorageContainerName -Blob $metadataFile.Name -Context $context -Force
    }
}