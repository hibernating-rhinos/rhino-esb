import-module .\psake.psm1
invoke-psake -framework '3.5' .\default.ps1 -properties @{config='Release';target_framework_version='3.5'}
remove-module psake