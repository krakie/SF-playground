#Connect-AzureRmAccount

$vaultname="SfPlaygroundVault"
$certname="SFPlayground"
$certpw="123hans"
$groupname="SFPlayground"
$clustername = "sfplayground"
$ExistingPfxFilePath="W:\SF-Playground\sfplaygroundssl.pfx"

$appcertpwd = ConvertTo-SecureString -String $certpw -AsPlainText -Force

Write-Host "Reading pfx file from $ExistingPfxFilePath"
$cert = new-object System.Security.Cryptography.X509Certificates.X509Certificate2 $ExistingPfxFilePath, $certpw

$bytes = [System.IO.File]::ReadAllBytes($ExistingPfxFilePath)
$base64 = [System.Convert]::ToBase64String($bytes)

$jsonBlob = @{
   data = $base64
   dataType = 'pfx'
   password = $certpw
   } | ConvertTo-Json

Write-Host $jsonBlob

$contentbytes = [System.Text.Encoding]::UTF8.GetBytes($jsonBlob)
$content = [System.Convert]::ToBase64String($contentbytes)

Write-Host $content

$secretValue = ConvertTo-SecureString -String $content -AsPlainText -Force

Write-Host $secretValue

# Upload the certificate to the key vault as a secret
Write-Host "Writing secret to $certname in vault $vaultname"
$secret = Set-AzureKeyVaultSecret -VaultName $vaultname -Name $certname -SecretValue $secretValue

# Add a certificate to all the VMs in the cluster.
Add-AzureRmServiceFabricApplicationCertificate -ResourceGroupName $groupname -Name $clustername -SecretIdentifier $secret.Id -Verbose