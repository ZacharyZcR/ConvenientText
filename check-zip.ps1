Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead("C:\Users\Programmer_Nick\OneDrive\文档\Visual Studio 18 项目文件\ConvenientText - Openclaw\bin\Release\net8.0\ConvenientText.cipx")
$zip.Entries | ForEach-Object { Write-Host $_.FullName }
$zip.Dispose()
