pushd $args[0]

$nuspec = ".\IntroduceNsAlias.nuspec"
$version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo((gi .\bin\Release\IntroduceNsAlias.8.0.dll).FullName)
$xml = [xml] (gc $nuspec)
$node = $xml.SelectSingleNode("package/metadata/version").InnerText = $version.ProductVersion


$xml.OuterXml > out.nuspec

nuget.exe pack out.nuspec

Move-Item *.nupkg .\bin\Release
ri .\out.nuspec

popd