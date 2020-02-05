Import-Module "$PSScriptRoot\SymbolicLinkHelpers.psm1"
Import-Module "$PSScriptRoot\ExternalDependencyHelpers.psm1"

function SetupRepository
{
    param(
        [switch] $iOS,
        [switch] $NoDownloads,
        [switch] $NoSymbolicLinks
    )
    
    $origLoc = Get-Location
    Set-Location $PSScriptRoot
    Write-Host "`n"
    
    ConfigureRepo

    If (!$NoDownloads)
    {
        DownloadQRCodePlugin
    
        If ($iOS)
        {
            DownloadARKitPlugin
        }
    }
        
    # Ensure that submodules are initialized and cloned.
    Write-Output "Updating spectator view related submodules."
    git submodule update --init

    If (!$NoSymbolicLinks)
    {
        FixSymbolicLinks
    }
    Set-Location $origLoc
}