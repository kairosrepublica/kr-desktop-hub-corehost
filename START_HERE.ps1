[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$Message = Read-Host "Enter an accurate GitHub checkpoint message"

& "$PSScriptRoot\tools\AUTO_CHECKPOINT.ps1" -Message $Message
