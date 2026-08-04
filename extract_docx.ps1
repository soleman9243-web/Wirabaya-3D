Add-Type -AssemblyName System.IO.Compression.FileSystem

$srcPath = $args[0]
$dstPath = $args[1]

# Copy file first to avoid lock issues
Copy-Item -Path $srcPath -Destination $dstPath -Force

$zip = [System.IO.Compression.ZipFile]::OpenRead($dstPath)

# List all entries
Write-Host "=== Entries in ZIP ==="
foreach ($e in $zip.Entries) {
    Write-Host $e.FullName
}

$entry = $zip.Entries | Where-Object { $_.FullName -eq 'word/document.xml' }
$stream = $entry.Open()
$reader = New-Object System.IO.StreamReader($stream)
$content = $reader.ReadToEnd()
$reader.Close()
$stream.Close()
$zip.Dispose()

# Format XML for readability
$xml = [xml]$content
$sw = New-Object System.IO.StringWriter
$writer = New-Object System.Xml.XmlTextWriter($sw)
$writer.Formatting = [System.Xml.Formatting]::Indented
$xml.WriteTo($writer)
$writer.Flush()
$sw.ToString()
