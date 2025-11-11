# DecryptResources.ps1
# One-time script to decrypt all .cr files in externals/ folder
# Run this ONCE before removing encryption code

param(
    [string]$ExternalsPath = ".\rdpWrapper\externals"
)

Write-Host "RDP Wrapper - Resource Decryption Tool" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Check if path exists
if (-not (Test-Path $ExternalsPath)) {
    Write-Host "Error: Externals path not found: $ExternalsPath" -ForegroundColor Red
    exit 1
}

# Encryption settings (must match Wrapper.cs GetAes() method)
$ApplicationName = "rdpWrapper"  # From Updater.ApplicationName
$ApplicationTitle = "RDP Wrapper" # From Updater.ApplicationTitle

Write-Host "Using encryption key derived from:" -ForegroundColor Yellow
Write-Host "  Application Name: $ApplicationName" -ForegroundColor Gray
Write-Host "  Application Title: $ApplicationTitle`n" -ForegroundColor Gray

# Create AES decryptor
Add-Type -AssemblyName System.Security
$aes = [System.Security.Cryptography.Aes]::Create()
$salt = [System.Text.Encoding]::UTF8.GetBytes($ApplicationTitle)
$keyDerivation = New-Object System.Security.Cryptography.Rfc2898DeriveBytes($ApplicationName, $salt, 100000, [System.Security.Cryptography.HashAlgorithmName]::SHA256)
$aes.Key = $keyDerivation.GetBytes(32)
$aes.IV = $keyDerivation.GetBytes(16)
$aes.Padding = [System.Security.Cryptography.PaddingMode]::PKCS7
$aes.Mode = [System.Security.Cryptography.CipherMode]::CBC

Write-Host "AES-256 decryptor initialized`n" -ForegroundColor Green

# Find all .cr files
$crFiles = Get-ChildItem -Path $ExternalsPath -Filter "*.cr" -Recurse
$totalFiles = $crFiles.Count

if ($totalFiles -eq 0) {
    Write-Host "No .cr files found in $ExternalsPath" -ForegroundColor Yellow
    Write-Host "Either files are already decrypted or path is incorrect.`n" -ForegroundColor Yellow
    exit 0
}

Write-Host "Found $totalFiles encrypted files to decrypt`n" -ForegroundColor White

$decrypted = 0
$failed = 0

foreach ($crFile in $crFiles) {
    $outputFile = $crFile.FullName -replace '\.cr$', ''
    $fileName = $crFile.Name

    Write-Host "Decrypting: " -NoNewline
    Write-Host $fileName -ForegroundColor Cyan -NoNewline
    Write-Host " -> " -NoNewline
    Write-Host (Split-Path $outputFile -Leaf) -ForegroundColor Green

    try {
        # Read encrypted file
        $encryptedBytes = [System.IO.File]::ReadAllBytes($crFile.FullName)
        $encryptedStream = New-Object System.IO.MemoryStream(,$encryptedBytes)

        # Create decryptor
        $decryptor = $aes.CreateDecryptor()
        $cryptoStream = New-Object System.Security.Cryptography.CryptoStream(
            $encryptedStream,
            $decryptor,
            [System.Security.Cryptography.CryptoStreamMode]::Read
        )

        # Decrypt to output file
        $outputStream = [System.IO.File]::Create($outputFile)
        $cryptoStream.CopyTo($outputStream)

        # Cleanup
        $outputStream.Close()
        $cryptoStream.Close()
        $encryptedStream.Close()

        # Verify file was created
        if (Test-Path $outputFile) {
            $size = (Get-Item $outputFile).Length
            Write-Host "  ✓ Success " -ForegroundColor Green -NoNewline
            Write-Host "($size bytes)" -ForegroundColor Gray
            $decrypted++
        }
        else {
            Write-Host "  ✗ Failed - output file not created" -ForegroundColor Red
            $failed++
        }
    }
    catch {
        Write-Host "  ✗ Failed - $($_.Exception.Message)" -ForegroundColor Red
        $failed++
    }
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Decryption Complete!" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

Write-Host "Results:" -ForegroundColor White
Write-Host "  Total files: $totalFiles" -ForegroundColor Gray
Write-Host "  Decrypted:   " -NoNewline
Write-Host $decrypted -ForegroundColor Green
Write-Host "  Failed:      " -NoNewline
Write-Host $failed -ForegroundColor $(if ($failed -gt 0) { "Red" } else { "Gray" })

if ($decrypted -gt 0) {
    Write-Host "`nDecrypted files location:" -ForegroundColor Yellow
    Write-Host "  $ExternalsPath" -ForegroundColor Gray
    Write-Host "`nYou can now:" -ForegroundColor Yellow
    Write-Host "  1. Verify the decrypted files are correct" -ForegroundColor White
    Write-Host "  2. Delete the .cr files: " -NoNewline -ForegroundColor White
    Write-Host "Remove-Item $ExternalsPath\**\*.cr" -ForegroundColor Cyan
    Write-Host "  3. Proceed with code changes to remove encryption" -ForegroundColor White
}

if ($failed -gt 0) {
    Write-Host "`nWarning: Some files failed to decrypt!" -ForegroundColor Red
    Write-Host "Please review errors above before proceeding.`n" -ForegroundColor Red
    exit 1
}

Write-Host "`n"
exit 0
