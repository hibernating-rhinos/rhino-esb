properties { 
  $base_dir = resolve-path .
  $build_dir = "$base_dir\build"
  $packageinfo_dir = "$base_dir\packaging"
  $40_build_dir = "$build_dir\4.0\"
  $35_build_dir = "$build_dir\3.5\"
  $lib_dir = "$base_dir\SharedLibs"
  $35_lib_dir = "$base_dir\SharedLibs\3.5\"
  $release_dir = "$base_dir\Release"
  $sln_file = "$base_dir\Rhino.ServiceBus.sln"
  $version = Get-Version-From-Git-Tag
  $tools_dir = "$base_dir\Tools"
  $config = "Release"
  $run_tests = $true
}

Framework "4.0"

include .\psake_ext.ps1
	
task default -depends Package

task Clean {
  remove-item -force -recurse $build_dir -ErrorAction SilentlyContinue
  remove-item -force -recurse $release_dir -ErrorAction SilentlyContinue
}

task Init -depends Clean {
	$infos = (
		"$base_dir\Rhino.ServiceBus\Properties\AssemblyInfo.cs",
		"$base_dir\Rhino.ServiceBus.Tests\Properties\AssemblyInfo.cs",
		"$base_dir\Rhino.ServiceBus.Host\Properties\AssemblyInfo.cs",
		"$base_dir\Rhino.ServiceBus.RhinoQueues\Properties\AssemblyInfo.cs",
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

task Compile -depends Init {
  msbuild $sln_file /p:"OutDir=$35_build_dir;Configuration=$config;TargetFrameworkVersion=V3.5;LibDir=$35_lib_dir"
  msbuild $sln_file /target:Rebuild /p:"OutDir=$35_build_dir\UAC\;Configuration=$config;TargetFrameworkVersion=V3.5;LibDir=$35_lib_dir;ApplicationManifest=$base_dir\Rhino.ServiceBus.manifest"
  msbuild $sln_file /target:Rebuild /p:"OutDir=$35_build_dir\x86\;Configuration=$config;Platform=x86;TargetFrameworkVersion=V3.5;LibDir=$35_lib_dir"
  msbuild $sln_file /target:Rebuild /p:"OutDir=$35_build_dir\x86\UAC\;Configuration=$config;Platform=x86;TargetFrameworkVersion=V3.5;LibDir=$35_lib_dir;ApplicationManifest=$base_dir\Rhino.ServiceBus.manifest"
  msbuild $sln_file /target:Rebuild /p:"OutDir=$35_build_dir\x64\;Configuration=$config;Platform=x64;TargetFrameworkVersion=V3.5;LibDir=$35_lib_dir"
  msbuild $sln_file /target:Rebuild /p:"OutDir=$35_build_dir\x64\UAC\;Configuration=$config;Platform=x64;TargetFrameworkVersion=V3.5;LibDir=$35_lib_dir;ApplicationManifest=$base_dir\Rhino.ServiceBus.manifest"
  msbuild $sln_file /target:Rebuild /p:"OutDir=$40_build_dir;Configuration=$config;TargetFrameworkVersion=V4.0"
  msbuild $sln_file /target:Rebuild /p:"OutDir=$40_build_dir\UAC\;Configuration=$config;TargetFrameworkVersion=V4.0;ApplicationManifest=$base_dir\Rhino.ServiceBus.manifest"
  msbuild $sln_file /target:Rebuild /p:"OutDir=$40_build_dir\x86\;Configuration=$config;Platform=x86;TargetFrameworkVersion=V4.0"
  msbuild $sln_file /target:Rebuild /p:"OutDir=$40_build_dir\x86\UAC\;Configuration=$config;Platform=x86;TargetFrameworkVersion=V4.0;ApplicationManifest=$base_dir\Rhino.ServiceBus.manifest"
  msbuild $sln_file /target:Rebuild /p:"OutDir=$40_build_dir\x64\;Configuration=$config;Platform=x64;TargetFrameworkVersion=V4.0"
  msbuild $sln_file /target:Rebuild /p:"OutDir=$40_build_dir\x64\UAC\;Configuration=$config;Platform=x64;TargetFrameworkVersion=V4.0;ApplicationManifest=$base_dir\Rhino.ServiceBus.manifest"
}

task Test -depends Compile -precondition { return $run_tests }{
  $old = pwd
  cd $build_dir
  & $tools_dir\xUnit\xunit.console.clr4.exe "$build_dir\3.5\Rhino.ServiceBus.Tests.dll" /noshadow
  cd $old		
}

task Release -depends Compile, Test {

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
    	*\Rhino.PersistentHashTable.dll `
    	*\Rhino.PersistentHashTable.pdb `
    	*\Rhino.Queues.dll `
    	*\Rhino.Queues.pdb `
    	*\Rhino.ServiceBus.dll `
    	*\Rhino.ServiceBus.pdb `
    	*\Rhino.ServiceBus.RhinoQueues.dll `
    	*\Rhino.ServiceBus.RhinoQueues.pdb `
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
    	*\Spring.Aop.dll `
    	*\Spring.Aop.pdb `
    	*\Spring.Aop.xml `
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
    	*\UAC\Rhino.ServiceBus.Host.exe `
    	*\UAC\Rhino.ServiceBus.Host.pdb `
		*\x86\Rhino.ServiceBus.dll `
    	*\x86\Rhino.ServiceBus.pdb `
    	*\x86\Rhino.ServiceBus.Host.exe `
    	*\x86\Rhino.ServiceBus.Host.pdb `
		*\x86\UAC\Rhino.ServiceBus.Host.exe `
    	*\x86\UAC\Rhino.ServiceBus.Host.pdb `
		*\x64\Rhino.ServiceBus.dll `
    	*\x64\Rhino.ServiceBus.pdb `
		*\x64\Rhino.ServiceBus.Host.exe `
    	*\x64\Rhino.ServiceBus.Host.pdb `
		*\x64\UAC\Rhino.ServiceBus.Host.exe `
    	*\x64\UAC\Rhino.ServiceBus.Host.pdb `
    	..\license.txt `
		..\acknowledgements.txt
	if ($lastExitCode -ne 0) {
        throw "Error: Failed to execute ZIP command"
    }
}

task Package -depends Release {
  $spec_files = @(Get-ChildItem $packageinfo_dir)
  foreach ($spec in $spec_files)
  {
    & $tools_dir\NuGet.exe pack $spec.FullName -o $release_dir -Version $version -Symbols -BasePath $base_dir
  }
}
