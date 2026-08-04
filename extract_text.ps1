Add-Type -AssemblyName System.IO.Compression.FileSystem

$filePath = $args[0]
$zip = [System.IO.Compression.ZipFile]::OpenRead($filePath)
$entry = $zip.Entries | Where-Object { $_.FullName -eq 'word/document.xml' }
$stream = $entry.Open()
$reader = New-Object System.IO.StreamReader($stream)
$content = $reader.ReadToEnd()
$reader.Close()
$stream.Close()
$zip.Dispose()

$xml = [xml]$content
$ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
$ns.AddNamespace("w", "http://schemas.openxmlformats.org/wordprocessingml/2006/main")

# Extract text from each paragraph
$paragraphs = $xml.SelectNodes("//w:p", $ns)
$lineNum = 0
foreach ($p in $paragraphs) {
    $lineNum++
    $texts = $p.SelectNodes(".//w:t", $ns)
    $line = ""
    foreach ($t in $texts) {
        $line += $t.InnerText
    }
    if ($line -ne "") {
        Write-Host "P${lineNum}: $line"
    } else {
        Write-Host "P${lineNum}: [empty]"
    }
}
