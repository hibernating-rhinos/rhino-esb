properties { 
  $base_dir  = resolve-path .
  $build_dir = "$base_dir\build"
  $40_build_dir = "$build_dir\4.0\"
  $35_build_dir = "$build_dir\3.5\"
  $lib_dir = "$base_dir\SharedLibs"
  $35_lib_dir = "$base_dir\SharedLibs\3.5\"
  $release_dir = "$base_dir\Release"
  $sln_file = "$base_dir\Rhino.ServiceBus.sln"
  $version = Get-Version-From-Git-Tag
  $tools_dir = "$base_dir\Tools"
  $config = "Release"
  $run_tests = "true"
}

$framework = '4.0'

include .\psake_ext.ps1
	
task default -depends Release

task Clean {
  remove-item -force -recurse $build_dir -ErrorAction SilentlyContinue
  remove-item -force -recurse $release_dir -ErrorAction SilentlyContinue
}

task Init -depends Clean {
	$infos = (
		"$base_dir\Rhino.ServiceBus\Properties\AssemblyInfo.cs",
		"$base_dir\Rhino.ServiceBus.Tests\Properties\AssemblyInfo.cs",
		"$base_dir\Rhino.ServiceBus.Host\Properties\AssemblyInfo.cs",
		"$base_dir\Rhino.ServiceBus.Castle\Properties\AssemblyInfo.cs",
		"$base_dir\Rhino.ServiceBus.StructureMap\Properties\AssemblyInfo.cs",
		"$base_dir\Rhino.ServiceBus.Autofac\Properties\AssemblyInfo.cs",
		"$base_dir\Rhino.ServiceBus.Unity\Properties\AssemblyInfo.cs",
		"$base_dir\Rhino.ServiceBus.Spring\Properties\AssemblyInfo.cs"
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
	new-item $build_dir -itemType directory 
}

task Compile40 -depends Init {
  msbuild $sln_file /p:"OutDir=$40_build_dir;Configuration=$config;TargetFrameworkVersion=4.0"
}

task Compile35 -depends Init {
  msbuild $sln_file /p:"OutDir=$35_build_dir;Configuration=$config;TargetFrameworkVersion=V3.5;LibDir=$35_lib_dir"
}

task Test -depends Compile35, Compile40 {
  if($run_tests -eq "false")
  {
    return
  }
  $old = pwd
  cd $build_dir
  & $tools_dir\xUnit\xunit.console.clr4.exe "$build_dir\3.5\Rhino.ServiceBus.Tests.dll" /noshadow
  cd $old		
}

task Release  -depends Test {

  cd $build_dir
	& $tools_dir\7za.exe a $release_dir\Rhino.ServiceBus.zip `
    	*\Castle.Core.dll `
    	*\Castle.Core.pdb `
    	*\Castle.Core.xml `
    	*\Castle.Windsor.dll `
    	*\Castle.Windsor.pdb `
    	*\Castle.Windsor.xml `
    	*\Esent.Interop.dll `
    	*\Esent.Interop.pdb `
    	*\Esent.Interop.xml `
    	*\log4net.dll `
    	*\log4net.xml `
    	*\Rhino.PersistentHashTable.dll `
    	*\Rhino.PersistentHashTable.pdb `
    	*\Rhino.Queues.dll `
    	*\Rhino.Queues.pdb `
    	*\Rhino.ServiceBus.dll `
    	*\Rhino.ServiceBus.pdb `
    	*\Rhino.ServiceBus.Castle.dll `
    	*\Rhino.ServiceBus.Castle.pdb `
    	*\StructureMap.dll `
    	*\Rhino.ServiceBus.StructureMap.dll `
    	*\Rhino.ServiceBus.StructureMap.pdb `
    	*\Autofac.dll `
    	*\Rhino.ServiceBus.Autofac.dll `
    	*\Rhino.ServiceBus.Autofac.pdb `
    	*\Microsoft.Practices.Unity.dll `
    	*\Microsoft.Practices.Unity.Interception.dll `
    	*\Rhino.ServiceBus.Unity.dll `
    	*\Rhino.ServiceBus.Unity.pdb `
    	*\Common.Logging.dll `
    	*\Spring.Core.dll `
    	*\Spring.Core.pdb `
    	*\Spring.Core.xml `
		*\Common.Logging.dll `
    	*\Rhino.ServiceBus.Spring.dll `
    	*\Rhino.ServiceBus.Spring.pdb `
    	*\Rhino.ServiceBus.Host.exe `
    	*\Rhino.ServiceBus.Host.pdb `
    	*\Wintellect.Threading.dll `
    	*\Wintellect.Threading.xml `
    	..\license.txt `
		..\acknowledgements.txt
	if ($lastExitCode -ne 0) {
        throw "Error: Failed to execute ZIP command"
    }
}

