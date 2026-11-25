$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$validationRoot = Join-Path $projectRoot 'Temp/CodexValidationProject'
$editorVersion = (Get-Content (Join-Path $projectRoot 'ProjectSettings/ProjectVersion.txt') | Select-String '^m_EditorVersion:').ToString().Split(':')[1].Trim()
$unityEditor = Join-Path ${env:ProgramFiles} "Unity/Hub/Editor/$editorVersion/Editor/Unity.exe"
if (!(Test-Path -LiteralPath $unityEditor)) { throw "Unity $editorVersion was not found." }
python (Join-Path $PSScriptRoot 'prepare-unity-checks.py')
if ($LASTEXITCODE -ne 0) { throw 'Validation preparation failed.' }
$logPath = Join-Path $validationRoot 'validation.log'
$unityArguments = '-batchmode -nographics -projectPath "' + $validationRoot + '" -executeMethod RunChecks.Run -logFile "' + $logPath + '"'
$validationProcess = Start-Process -FilePath $unityEditor -ArgumentList $unityArguments -WindowStyle Hidden -PassThru
if (!$validationProcess.WaitForExit(180000)) {
    Stop-Process -Id $validationProcess.Id
    throw "Unity validation timed out. Log: $logPath"
}
if ($validationProcess.ExitCode -ne 0) { throw "Unity validation failed. Log: $logPath" }
Get-Content -LiteralPath (Join-Path $validationRoot 'results.txt')
