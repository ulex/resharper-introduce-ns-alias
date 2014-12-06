#pushd $args[0]

$nuspec = ".\IntroduceNsAlias.nuspec"
$dll = (gi .\ReSharperIntroduceNsAlias8.0\bin\Release\IntroduceNsAlias.9.0.dll).FullName
$version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($dll).FileVersion
write-host "Version = $version"

$packages = @{
	"Ulex.IntroduceNsAlias" = @{
		'PackageId' = 'Ulex.IntroduceNsAlias';
        'PackageVersion' = $version;
        'Dll' = $dll;
    };
}
foreach ($p in $packages.Values){
    $properties = [String]::Join(";" ,($p.GetEnumerator() | % {("{0}={1}" -f @($_.Key, $_.Value))}))
    write-host $properties
    nuget.exe pack $nuspec -Properties $properties
}

Move-Item *.nupkg .\ReSharperIntroduceNsAlias8.0\bin\Release -Force

popd
