param([Parameter(ValueFromRemainingArguments=$true)][string[]]$FlutterArguments)
$ErrorActionPreference = 'Stop'
$launcher = Get-Command flutter.bat -ErrorAction SilentlyContinue
if ($launcher) { & $launcher.Source @FlutterArguments; exit $LASTEXITCODE }
$flutterCommand = Get-Command flutter -ErrorAction Stop
$bin = Split-Path $flutterCommand.Source
$dart = Join-Path $bin 'cache/dart-sdk/bin/dart.exe'
$snapshot = Join-Path $bin 'cache/flutter_tools.snapshot'
if (!(Test-Path -LiteralPath $dart) -or !(Test-Path -LiteralPath $snapshot)) { throw 'No se encontró una instalación Flutter válida.' }
& $dart $snapshot @FlutterArguments
exit $LASTEXITCODE
