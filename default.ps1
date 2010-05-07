properties { 
  $base_dir  = resolve-path .
  $lib_dir = "$base_dir\SharedLibs"
  $build_dir = "$base_dir\build" 
  $buildartifacts_dir = "$build_dir\" 
  $sln_file = "$base_dir\Rhino.ServiceBus.sln" 
  $version = "1.8.0.0"
  $tools_dir = "$base_dir\Tools"
  $release_dir = "$base_dir\Release"
} 

include .\psake_ext.ps1
	
task default -depends Release

task Clean { 
  remove-item -force -recurse $buildartifacts_dir -ErrorAction SilentlyContinue 
  remove-item -force -recurse $release_dir -ErrorAction SilentlyContinue 
} 

task Init -depends Clean { 
	Generate-Assembly-Info `
		-file "$base_dir\Rhino.ServiceBus\Properties\AssemblyInfo.cs" `
		-title "Rhino Service Bus $version" `
		-description "Developer friendly service bus for .NET" `
		-company "Hibernating Rhinos" `
		-product "Rhino Service Bus $version" `
		-version $version `
		-copyright "Hibernating Rhinos & Ayende Rahien 2004 - 2009"
		
	Generate-Assembly-Info `
		-file "$base_dir\Rhino.ServiceBus.Tests\Properties\AssemblyInfo.cs" `
		-title "Rhino Service Bus $version" `
		-description "Developer friendly service bus for .NET" `
		-company "Hibernating Rhinos" `
		-product "Rhino Service Bus $version" `
		-version $version `
		-copyright "Hibernating Rhinos & Ayende Rahien 2004 - 2009"
    
    Generate-Assembly-Info `
		-file "$base_dir\Rhino.ServiceBus.Host\Properties\AssemblyInfo.cs" `
		-title "Rhino DistributedHashTable $version" `
		-description "Distributed Hash Table for .NET" `
		-company "Hibernating Rhinos" `
		-product "Rhino DHT $version" `
		-version $version `
		-copyright "Hibernating Rhinos & Ayende Rahien 2004 - 2009"
	   
    Generate-Assembly-Info `
		-file "$base_dir\Rhino.ServiceBus.DistributedHashTableIntegration\Properties\AssemblyInfo.cs" `
		-title "Rhino Service Bus $version" `
		-description "Developer friendly service bus for .NET" `
		-company "Hibernating Rhinos" `
		-product "Rhino Service Bus $version" `
		-version $version `
		-copyright "Hibernating Rhinos & Ayende Rahien 2004 - 2009"
		
	new-item $release_dir -itemType directory 
	new-item $buildartifacts_dir -itemType directory 
} 

task Compile -depends Init { 
  exec msbuild "/p:OutDir=""$buildartifacts_dir "" $sln_file"
} 

task Test -depends Compile {
  $old = pwd
  cd $build_dir
  exec "$tools_dir\xUnit\xunit.console.exe" "$build_dir\Rhino.ServiceBus.Tests.dll"
  cd $old		
}


task Release  -depends Test{

	& $tools_dir\zip.exe -9 -A -j `
		$release_dir\Rhino.ServiceBus.zip `
		$build_dir\Castle.Core.dll `
    	$build_dir\Castle.Core.xml `
    	$build_dir\Castle.DynamicProxy2.dll `
    	$build_dir\Castle.DynamicProxy2.xml `
    	$build_dir\Castle.MicroKernel.dll `
    	$build_dir\Castle.MicroKernel.xml `
    	$build_dir\Castle.Windsor.dll `
    	$build_dir\Castle.Windsor.xml `
    	$build_dir\Esent.Interop.dll `
    	$build_dir\Esent.Interop.xml `
    	$build_dir\log4net.dll `
    	$build_dir\log4net.xml `
    	$build_dir\Rhino.DistributedHashTable.Client.dll `
    	$build_dir\Rhino.DistributedHashTable.dll `
    	$build_dir\Rhino.PersistentHashTable.dll `
    	$build_dir\Rhino.Queues.dll `
    	$build_dir\Rhino.ServiceBus.dll `
    	$build_dir\Rhino.ServiceBus.xml `
    	$build_dir\Wintellect.Threading.dll `
    	$build_dir\Wintellect.Threading.xml `
    	license.txt `
		acknowledgements.txt
	if ($lastExitCode -ne 0) {
        throw "Error: Failed to execute ZIP command"
    }
}