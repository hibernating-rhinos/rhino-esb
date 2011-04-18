properties { 
  $base_dir  = resolve-path .
  $lib_dir = "$base_dir\SharedLibs"
  $lib_for_build_dir = "$lib_dir\build\"
  $target_framework_version = "3.5"
  $sln_file = "$base_dir\Rhino.ServiceBus.sln" 
  $version = "1.9.0.0"
  $tools_dir = "$base_dir\Tools"
  $config = "Release"
  $run_tests = "false"
}

$framework = '4.0'

include .\psake_ext.ps1
	
task default -depends Release

task SetDerivedProperties {
  $script:build_dir = "$base_dir\build\$target_framework_version\"
  $script:buildartifacts_dir = $build_dir 
  $script:release_dir = "$base_dir\Release\$target_framework_version"
  $script:build_properties = "OutDir=$buildartifacts_dir;Configuration=$config;TargetFrameworkVersion=V$target_framework_version"
}

task Clean -depends SetDerivedProperties { 
  remove-item -force -recurse $buildartifacts_dir -ErrorAction SilentlyContinue 
  remove-item -force -recurse $release_dir -ErrorAction SilentlyContinue 
  remove-item -force -recurse $lib_for_build_dir -ErrorAction SilentlyContinue
} 

task Init -depends Clean {
	$infos = (
		"$base_dir\Rhino.ServiceBus\Properties\AssemblyInfo.cs",
		"$base_dir\Rhino.ServiceBus.Tests\Properties\AssemblyInfo.cs",
		"$base_dir\Rhino.ServiceBus.Host\Properties\AssemblyInfo.cs",
		"$base_dir\Rhino.ServiceBus.Castle\Properties\AssemblyInfo.cs",
		"$base_dir\Rhino.ServiceBus.StructureMap\Properties\AssemblyInfo.cs",
		"$base_dir\Rhino.ServiceBus.Autofac\Properties\AssemblyInfo.cs"
	); 
		
	$infos | foreach { Generate-Assembly-Info `
		-file $_ `
		-title "Rhino Service Bus $version" `
		-description "Developer friendly service bus for .NET" `
		-company "Hibernating Rhinos" `
		-product "Rhino Service Bus $version" `
		-version $version `
		-copyright "Hibernating Rhinos & Ayende Rahien 2004 - 2009" `
	}
		
	new-item $release_dir -itemType directory 
	new-item $buildartifacts_dir -itemType directory 
	new-item $lib_for_build_dir -itemType directory -force
	get-childitem "$lib_dir\*.*" -exclude "3.5;4.0" | copy-item -destination {join-path $lib_for_build_dir $_.Name}
	get-childitem "$lib_dir\$target_framework_version\*.*" | copy-item -destination {join-path $lib_for_build_dir $_.Name}
} 

task Compile -depends Init {
  msbuild $sln_file /p:$build_properties
} 

task Test -depends Compile {
  if($run_tests -eq "false")
  {
    return
  }
  $test_runner =  "$tools_dir\xUnit\"
  $old = pwd
  cd $build_dir
  & $tools_dir\xUnit\xunit.console.clr4.exe "$build_dir\Rhino.ServiceBus.Tests.dll"
  cd $old		
}

task Release  -depends Test{

	& $tools_dir\zip.exe -9 -A -j `
	$release_dir\Rhino.ServiceBus.zip `
	$build_dir\Castle.Core.dll `
    	$build_dir\Castle.Core.xml `
    	$build_dir\Castle.Windsor.dll `
    	$build_dir\Castle.Windsor.xml `
    	$build_dir\StructureMap.dll `
    	$build_dir\Esent.Interop.dll `
    	$build_dir\Esent.Interop.xml `
    	$build_dir\log4net.dll `
    	$build_dir\log4net.xml `
    	$build_dir\Rhino.PersistentHashTable.dll `
    	$build_dir\Rhino.Queues.dll `
    	$build_dir\Rhino.ServiceBus.dll `
    	$build_dir\Rhino.ServiceBus.xml `
    	$build_dir\Rhino.ServiceBus.Castle.xml `
    	$build_dir\Rhino.ServiceBus.Castle.dll `
    	$build_dir\Rhino.ServiceBus.StructureMap.xml `
    	$build_dir\Rhino.ServiceBus.StructureMap.dll `
    	$build_dir\Rhino.ServiceBus.Host.exe `
    	$build_dir\Wintellect.Threading.dll `
    	$build_dir\Wintellect.Threading.xml `
    	license.txt `
		acknowledgements.txt
	if ($lastExitCode -ne 0) {
        throw "Error: Failed to execute ZIP command"
    }
}

