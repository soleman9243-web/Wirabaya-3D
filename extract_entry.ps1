Add-Type -AssemblyName System.IO.Compression.FileSystem

$filePath = $args[0]
$entryName = $args[1]  # e.g. "word/header1.xml"
$zip = [System.IO.Compression.ZipFile]::OpenRead($filePath)
$entry = $zip.Entries | Where-Object { $_.FullName -eq $entryName }
if ($entry) {
    $stream = $entry.Open()
    $reader = New-Object System.IO.StreamReader($stream)
    $content = $reader.ReadToEnd()
    $reader.Close()
    $stream.Close()
    
    $xml = [xml]$content
    $sw = New-Object System.IO.StringWriter
    $writer = New-Object System.Xml.XmlTextWriter($sw)
    $writer.Formatting = [System.Xml.Formatting]::Indented
    $xml.WriteTo($writer)
    $writer.Flush()
    $sw.ToString()
} else {
    Write-Host "Entry not found: $entryName"
}
$zip.Dispose()
