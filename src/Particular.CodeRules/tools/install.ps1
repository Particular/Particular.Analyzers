param($installPath, $toolsPath, $package, $project)

$analyzerPath = join-path $toolsPath "..\analyzers\dotnet\cs"
$analyzerFilePath = join-path $analyzerPath "Particular.CodeRules.dll"

$project.Object.AnalyzerReferences.Add("$analyzerFilePath")