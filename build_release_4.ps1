import-module .\psake.psm1
invoke-psake -framework '4.0' .\default.ps1 -properties @{config='Release';target_framework_version='4.0'}
remove-module psake