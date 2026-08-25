param(
    [string]$ProjectRoot = (Get-Location).Path
)

$ErrorActionPreference = "Stop"

$target = Join-Path $ProjectRoot "Assets\BalloonRush\Scripts\UI\OperatorMenuManager.cs"

if (-not (Test-Path $target)) {
    throw "Could not find OperatorMenuManager.cs at: $target"
}

$text = [System.IO.File]::ReadAllText($target)
$original = $text

# Backup the exact pre-v1.9.5 file once.
$backup = "$target.v1.9.5-backup"
if (-not (Test-Path $backup)) {
    Copy-Item $target $backup
}

# 1) FIX THE ACTUAL RUNTIME CRASH:
# TMP_InputField.pointSize calls SetGlobalPointSize(), but at this point
# textComponent has not been assigned yet. That creates the NullReferenceException
# seen in the user's Unity Console and aborts OperatorMenuManager.Start().
$text = $text.Replace(
'            input.pointSize = 26f;' + [Environment]::NewLine,
''
)

# Also handle LF-only repositories.
$text = $text.Replace(
"            input.pointSize = 26f;`n",
""
)

# 2) CLEAN THE UNITY 6 / TMP OBSOLETE WORD-WRAP WARNINGS.
$text = $text.Replace(
'            valueText.enableWordWrapping = true;',
'            valueText.textWrappingMode = TextWrappingModes.Normal;'
)

$text = $text.Replace(
'            text.enableWordWrapping = false;',
'            text.textWrappingMode = TextWrappingModes.NoWrap;'
)

# 3) MAKE THE OPERATOR EXIT FAIL-SAFE.
# Subscribe to M / JoystickButton4 and Back BEFORE dynamic settings UI construction.
$oldCRLF = @"
            editable = GameServices.Settings.CreateEditableCopy();
            operatorFont = statusText != null && statusText.font != null ? statusText.font : TMP_Settings.defaultFontAsset;
            BuildSettingsUI();
            BindButtons();
            SubscribeInput();
            RefreshAllRows();
            RefreshStatistics();
            SetStatus("Operator settings loaded.", new Color(0.35f, 1f, 0.55f));
            GameServices.State?.ChangeState(GameState.OperatorMenu);
            GameServices.Audio?.PlayMusic(MusicCue.Attract, 0.3f);
"@

$newCRLF = @"
            editable = GameServices.Settings.CreateEditableCopy();
            operatorFont = statusText != null && statusText.font != null ? statusText.font : TMP_Settings.defaultFontAsset;

            // Cabinet fail-safe: make M / JoystickButton4 and BACK available before
            // any dynamic settings control is created. A broken field must never trap
            // an operator inside the service menu.
            BindButtons();
            SubscribeInput();
            GameServices.State?.ChangeState(GameState.OperatorMenu);

            try
            {
                BuildSettingsUI();
            }
            catch (Exception exception)
            {
                Debug.LogError("Operator Menu UI build failed, but service exit remains active: " + exception);
                SetStatus("MENU BUILD ERROR - use M / key switch to exit.", Color.red);
            }

            RefreshAllRows();
            RefreshStatistics();
            SetStatus("Operator settings loaded.", new Color(0.35f, 1f, 0.55f));
            GameServices.Audio?.PlayMusic(MusicCue.Attract, 0.3f);
"@

if ($text.Contains($oldCRLF)) {
    $text = $text.Replace($oldCRLF, $newCRLF)
}
else {
    $oldLF = $oldCRLF -replace "`r`n", "`n"
    $newLF = $newCRLF -replace "`r`n", "`n"
    if ($text.Contains($oldLF)) {
        $text = $text.Replace($oldLF, $newLF)
    }
    else {
        Write-Warning "Could not find the exact Start() block to reorder. The TMP crash/warning fixes will still be applied."
    }
}

if ($text -eq $original) {
    Write-Host "OperatorMenuManager.cs already appears to contain the v1.9.5 fixes." -ForegroundColor Yellow
}
else {
    [System.IO.File]::WriteAllText($target, $text, [System.Text.UTF8Encoding]::new($false))
    Write-Host "Patched OperatorMenuManager.cs" -ForegroundColor Green
}

# Validation checks.
$check = [System.IO.File]::ReadAllText($target)
$problems = @()

if ($check.Contains("input.pointSize = 26f;")) {
    $problems += "input.pointSize line still exists"
}
if ($check.Contains("enableWordWrapping")) {
    $problems += "obsolete enableWordWrapping still exists"
}
if (-not $check.Contains("textWrappingMode = TextWrappingModes.Normal")) {
    $problems += "Normal wrapping replacement not found"
}
if (-not $check.Contains("textWrappingMode = TextWrappingModes.NoWrap")) {
    $problems += "NoWrap replacement not found"
}
if (-not $check.Contains("Cabinet fail-safe")) {
    $problems += "fail-safe Start() reorder was not applied"
}

if ($problems.Count -gt 0) {
    Write-Host ""
    Write-Host "v1.9.5 finished with validation warnings:" -ForegroundColor Yellow
    foreach ($problem in $problems) {
        Write-Host " - $problem" -ForegroundColor Yellow
    }
    Write-Host "Send the PowerShell output to ChatGPT before testing." -ForegroundColor Yellow
}
else {
    Write-Host ""
    Write-Host "Balloon Rush v1.9.5 Operator Menu Runtime Fix: PASS" -ForegroundColor Green
    Write-Host " - TMP_InputField null-reference trigger removed" -ForegroundColor Green
    Write-Host " - obsolete TMP word-wrap calls removed" -ForegroundColor Green
    Write-Host " - M / JoystickButton4 exit is subscribed before UI building" -ForegroundColor Green
}

Write-Host ""
Write-Host "Return to Unity and wait for scripts to compile." -ForegroundColor Cyan
Write-Host "Then clear Console and test: Boot -> M -> Operator -> M -> Attract." -ForegroundColor Cyan
