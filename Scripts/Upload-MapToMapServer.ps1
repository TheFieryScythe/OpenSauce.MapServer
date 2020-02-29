[CmdletBinding(SupportsShouldProcess)]
Param(
    [Parameter(Mandatory, ValueFromPipeline)]
    [System.IO.FileInfo]$File,
    [Parameter(Mandatory)]
    [string]$ResourceGroupName,
    [Parameter(Mandatory)]
    [string]$StorageAccountName,
    [Parameter(Mandatory)]
    [string]$StorageContainerName
)
begin {
    Import-Module Az

    Connect-AzAccount
    $storageAccount = Get-AzStorageAccount -ResourceGroupName $ResourceGroupName -Name $StorageAccountName
    $context = $storageAccount.Context

    $script:existingMaps = Get-AzStorageBlob -Container $StorageContainerName -Blob "*.zip" -Context $Context |
    ForEach-Object {
        $mapName = $_.ICloudBlob.Metadata["UncompressedName"]
        if ($mapName -and ($mapName -like "*.map" -or $mapName -like "*.yelo" )) {
            $mapName
        }
    }
}
process {
    Write-Verbose "Processing: $($File.Name)"
    if ($script:existingMaps -contains $File.Name) {
        Write-Verbose "Map already present in storage"
    }
    else {
        $archivePath = [IO.Path]::ChangeExtension($File.FullName, "zip")
        Write-Verbose "Creating archive $archivePath"
        $File | Compress-Archive -DestinationPath $archivePath -Force
        $archiveFile = Get-Item -Path $archivePath

        $archiveFile | Set-AzStorageBlobContent -Container $StorageContainerName -Blob $archiveFile.Name -Context $context -Force -Metadata @{
            UncompressedName = $File.Name.ToLowerInvariant()
            UncompressedMD5  = ($File | Get-FileHash -Algorithm MD5).Hash
            UncompressedSize = $File.Length
            CompressedName   = $archiveFile.Name
            CompressedMD5    = ($archiveFile | Get-FileHash -Algorithm MD5).Hash
            CompressedSize   = $archiveFile.Length
        }
    }
}