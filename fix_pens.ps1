Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$srcPath = "D:\Penyimpaan Internal\Download\PENS.docx"
$dstPath = "D:\Penyimpaan Internal\Download\PENS_fixed.docx"

# Copy the file
Copy-Item -Path $srcPath -Destination $dstPath -Force

# Open the docx for update
$zip = [System.IO.Compression.ZipFile]::Open($dstPath, [System.IO.Compression.ZipArchiveMode]::Update)

# Read document.xml
$entry = $zip.Entries | Where-Object { $_.FullName -eq 'word/document.xml' }
$stream = $entry.Open()
$reader = New-Object System.IO.StreamReader($stream)
$content = $reader.ReadToEnd()
$reader.Close()
$stream.Close()

$xml = [xml]$content
$ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
$ns.AddNamespace("w", "http://schemas.openxmlformats.org/wordprocessingml/2006/main")
$ns.AddNamespace("w14", "http://schemas.microsoft.com/office/word/2010/wordml")

$body = $xml.SelectSingleNode("//w:body", $ns)

# Helper function: check if a paragraph is empty (no visible text content)
function Is-EmptyParagraph($p) {
    $texts = $p.SelectNodes(".//w:t", $ns)
    foreach ($t in $texts) {
        if ($t.InnerText.Trim() -ne "") {
            return $false
        }
    }
    return $true
}

# Remove the first 3 empty/spacer paragraphs at the top (before "Surabaya,")
# These are: P1 (empty), P2 (empty), P3 (tab only)
$removedTop = 0
while ($removedTop -lt 3) {
    $firstChild = $body.FirstChild
    if ($firstChild -and $firstChild.LocalName -eq "p") {
        if (Is-EmptyParagraph $firstChild) {
            Write-Host "Removing top empty paragraph"
            $body.RemoveChild($firstChild) | Out-Null
            $removedTop++
        } else {
            break
        }
    } else {
        break
    }
}
Write-Host "Removed $removedTop empty paragraphs from top"

# Remove the empty paragraph between "Kota Surabaya..." and "Dengan hormat" (P14 area)  
# Find paragraph with "Kota Surabaya" text and check next paragraph
$allPs = $body.SelectNodes("w:p", $ns)
$toRemoveMiddle = @()
for ($i = 0; $i -lt $allPs.Count; $i++) {
    $p = $allPs[$i]
    $texts = $p.SelectNodes(".//w:t", $ns)
    $line = ""
    foreach ($t in $texts) { $line += $t.InnerText }
    
    # Find empty paragraphs between address and "Dengan hormat"
    # Also find empty paragraph after "Dengan hormat" content area
    if ($line -match "Kota Surabaya") {
        # Check next paragraph - if it's just spaces/empty, remove it
        if ($i + 1 -lt $allPs.Count) {
            $nextP = $allPs[$i + 1]
            $nextTexts = $nextP.SelectNodes(".//w:t", $ns)
            $nextLine = ""
            foreach ($t in $nextTexts) { $nextLine += $t.InnerText }
            if ($nextLine.Trim() -eq "") {
                $toRemoveMiddle += $nextP
                Write-Host "Marking empty paragraph after 'Kota Surabaya' for removal"
            }
        }
    }
}

foreach ($p in $toRemoveMiddle) {
    $body.RemoveChild($p) | Out-Null
}
Write-Host "Removed $($toRemoveMiddle.Count) empty paragraphs from middle"

# Remove the empty paragraph right before the "Surabaya, " date line
# This is the paragraph that only has tabs
$allPs2 = $body.SelectNodes("w:p", $ns)
for ($i = 0; $i -lt $allPs2.Count; $i++) {
    $p = $allPs2[$i]
    $texts = $p.SelectNodes(".//w:t", $ns)
    $line = ""
    foreach ($t in $texts) { $line += $t.InnerText }
    
    if ($line -match "Surabaya,") {
        # Check if previous paragraph is just tabs/empty
        if ($i -gt 0) {
            $prevP = $allPs2[$i - 1]
            if (Is-EmptyParagraph $prevP) {
                Write-Host "Removing empty paragraph before 'Surabaya,'"
                $body.RemoveChild($prevP) | Out-Null
            }
        }
        break
    }
}

# Also set spacing after to 0 on all paragraphs
$allSpacings = $xml.SelectNodes("//w:p/w:pPr/w:spacing", $ns)
foreach ($spacing in $allSpacings) {
    $spacing.SetAttribute("after", "http://schemas.openxmlformats.org/wordprocessingml/2006/main", "0")
    $spacing.SetAttribute("before", "http://schemas.openxmlformats.org/wordprocessingml/2006/main", "0")
}

# Reduce page margins
$sectPr = $xml.SelectSingleNode("//w:sectPr/w:pgMar", $ns)
if ($sectPr) {
    # Reduce top margin (was 1440 = 1 inch, set to 720 = 0.5 inch)
    $sectPr.SetAttribute("top", "http://schemas.openxmlformats.org/wordprocessingml/2006/main", "720")
    # Reduce bottom margin 
    $sectPr.SetAttribute("bottom", "http://schemas.openxmlformats.org/wordprocessingml/2006/main", "720")
    Write-Host "Margins adjusted: top=720, bottom=720"
}

# Write back the modified XML
$entry2 = $zip.GetEntry('word/document.xml')
$stream2 = $entry2.Open()
$stream2.SetLength(0)
$writer = New-Object System.IO.StreamWriter($stream2, [System.Text.Encoding]::UTF8)
$xml.Save($writer)
$writer.Close()
$stream2.Close()

$zip.Dispose()

Write-Host ""
Write-Host "Fixed file saved to: $dstPath"
