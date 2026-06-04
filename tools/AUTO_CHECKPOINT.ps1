[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$Message
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$ExpectedRemote = "https://github.com/kairosrepublica/kr-desktop-hub-corehost.git"
$PrivatePatternFile = Join-Path $RepoRoot "owner_private_docs\PUBLIC_SAFETY_PATTERNS.txt"

function Assert-LastExitCode {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    if ($LASTEXITCODE -ne 0) {
        throw $Message
    }
}

Set-Location $RepoRoot

if (-not (Test-Path -LiteralPath ".git")) {
    throw "Git repository not found: $RepoRoot"
}

$Branch = (git branch --show-current).Trim()
Assert-LastExitCode "Unable to read current branch."

if ($Branch -ne "main") {
    throw "Expected main branch. Current branch: $Branch"
}

$Remote = (git remote get-url origin).Trim()
Assert-LastExitCode "Unable to read origin remote."

if ($Remote -ne $ExpectedRemote) {
    throw "Unexpected GitHub remote: $Remote"
}

Write-Host "`n=== Automated GitHub checkpoint ===" -ForegroundColor Cyan
Write-Host "Branch:  $Branch"
Write-Host "Remote:  $Remote"
Write-Host "Message: $Message"

git add -A
Assert-LastExitCode "git add failed."

$AllStaged = @(
    git diff --cached --name-only
)

Assert-LastExitCode "Unable to inspect staged files."

if ($AllStaged.Count -eq 0) {
    Write-Host "No public changes to commit." -ForegroundColor Yellow
    git status -sb
    return
}

$PrivateLeaks = @(
    $AllStaged |
        Where-Object {
            $_ -like "owner_private_docs/*"
        }
)

if ($PrivateLeaks.Count -gt 0) {
    $PrivateLeaks |
        ForEach-Object {
            Write-Host $_ -ForegroundColor Red
        }

    throw "Private Owner files were staged unexpectedly."
}

$TextScanFiles = @(
    git diff --cached --name-only --diff-filter=ACMR
)

Assert-LastExitCode "Unable to inspect staged text files."

$TextFileNames = @(
    ".gitignore",
    ".editorconfig"
)

$TextExtensions = @(
    ".md",
    ".txt",
    ".json",
    ".yml",
    ".yaml",
    ".ps1",
    ".cs",
    ".csproj",
    ".sln",
    ".props",
    ".targets",
    ".xml",
    ".config"
)

$Patterns = @()

if (Test-Path -LiteralPath $PrivatePatternFile) {
    $Patterns = @(
        Get-Content -LiteralPath $PrivatePatternFile |
            ForEach-Object {
                $_.Trim()
            } |
            Where-Object {
                ($_ -ne "") -and (-not $_.StartsWith("#"))
            }
    )
}

$Leaks = @()

if ($Patterns.Count -gt 0) {
    foreach ($File in $TextScanFiles) {
        if (-not (Test-Path -LiteralPath $File -PathType Leaf)) {
            continue
        }

        $LeafName = Split-Path $File -Leaf
        $Extension = [System.IO.Path]::GetExtension($File).ToLowerInvariant()

        if (($TextFileNames -notcontains $LeafName) -and ($TextExtensions -notcontains $Extension)) {
            continue
        }

        $FileContent = Get-Content -LiteralPath $File -Raw -ErrorAction Stop

        foreach ($Pattern in $Patterns) {
            if ($FileContent -match $Pattern) {
                $Leaks += [PSCustomObject]@{
                    File = $File
                    Pattern = $Pattern
                }
            }
        }
    }
}

if ($Leaks.Count -gt 0) {
    $Leaks |
        Format-Table -AutoSize

    throw "Public-content safety check failed."
}

git diff --cached --check
Assert-LastExitCode "git diff --cached --check failed."

git commit -m $Message
Assert-LastExitCode "git commit failed."

git push origin main
Assert-LastExitCode "git push failed."

Write-Host "`n=== Checkpoint complete ===" -ForegroundColor Green

git log `
    -n 1 `
    --format="%h | %ad | %an <%ae> | %s" `
    --date=short

git status -sb