properties {
  $config = "Debug"
}

$framework = '4.0'

include .\psake_ext.ps1
	
task default -depends ReleaseNET35, ReleaseNET40

task ReleaseNET40 {
	Invoke-Psake .\build.ps1 -properties @{
		"target_framework_version"="4.0";
		"config"="$config"
	}
} 

task ReleaseNET35 {
	Invoke-Psake .\build.ps1 -properties @{
		"run_tests"="true";
		"config"="$config"
	}
}
