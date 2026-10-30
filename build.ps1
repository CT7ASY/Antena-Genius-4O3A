param([string]$InnoCompiler)
$ErrorActionPreference='Stop'
$build=Join-Path $PSScriptRoot 'build'
New-Item -ItemType Directory -Path $build -Force | Out-Null
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if(!(Test-Path -LiteralPath $compiler)){$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'}
$sources=@(Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.cs' | ForEach-Object {$_.FullName})
& $compiler /nologo /target:winexe /optimize+ "/out:$build\AG4O3A.exe" "/win32icon:$PSScriptRoot\AG4O3A.ico" "/win32manifest:$PSScriptRoot\AG4O3A.manifest" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll /reference:System.Security.dll $sources
if($LASTEXITCODE -ne 0){throw 'Falha na compilação C#.'}
foreach($name in @('AG4O3A.exe.config','languages.json','LEIA-ME.txt','LICENSE')){Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination $build -Force}
if($InnoCompiler){& $InnoCompiler /Q (Join-Path $PSScriptRoot 'Instalar-AG4O3A.iss');if($LASTEXITCODE -ne 0){throw 'Falha na compilação do instalador.'}}
Write-Output 'Compilação concluída.'
