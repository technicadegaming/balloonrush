#!/usr/bin/env python3
"""Static source validation for the Balloon Rush Unity project.

This is deliberately compiler-independent so it can run on a cabinet/build PC
before Unity is installed. It checks repository completeness, JSON syntax,
common placeholder markers, balanced C# delimiters/comments/strings,
preprocessor blocks, and duplicate declared type names.
"""
from __future__ import annotations

import json
import re
import sys
from collections import defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CS_ROOT = ROOT / "Assets" / "BalloonRush"

REQUIRED = [
    "Assets/BalloonRush/Editor/BalloonRushProjectBuilder.cs",
    "Assets/BalloonRush/Editor/BalloonRushPreflightValidator.cs",
    "Assets/BalloonRush/Editor/PayoutSimulatorWindow.cs",
    "Assets/BalloonRush/Scripts/Core/GameBootstrap.cs",
    "Assets/BalloonRush/Scripts/Core/CabinetRuntimeManager.cs",
    "Assets/BalloonRush/Scripts/Core/GameManager.cs",
    "Assets/BalloonRush/Scripts/Core/RoundManager.cs",
    "Assets/BalloonRush/Scripts/Gameplay/Balloon.cs",
    "Assets/BalloonRush/Scripts/Gameplay/BalloonPool.cs",
    "Assets/BalloonRush/Scripts/Gameplay/BalloonSpawner.cs",
    "Assets/BalloonRush/Scripts/Gameplay/TimingEvaluator.cs",
    "Assets/BalloonRush/Scripts/Gameplay/ComboManager.cs",
    "Assets/BalloonRush/Scripts/Gameplay/ScoreManager.cs",
    "Assets/BalloonRush/Scripts/Gameplay/EconomyMath.cs",
    "Assets/BalloonRush/Scripts/Gameplay/GoldenRoundManager.cs",
    "Assets/BalloonRush/Scripts/Gameplay/JackpotManager.cs",
    "Assets/BalloonRush/Scripts/Input/IArcadeIO.cs",
    "Assets/BalloonRush/Scripts/Input/ITicketFeedbackSource.cs",
    "Assets/BalloonRush/Scripts/Input/KeyboardArcadeIO.cs",
    "Assets/BalloonRush/Scripts/Input/SerialArcadeIO.cs",
    "Assets/BalloonRush/Scripts/Redemption/TicketManager.cs",
    "Assets/BalloonRush/Scripts/SaveSystem/SessionAuditLogger.cs",
    "Assets/BalloonRush/Scripts/UI/AttractModeManager.cs",
    "Assets/BalloonRush/Scripts/UI/ResultsManager.cs",
    "Assets/BalloonRush/Scripts/UI/OperatorMenuManager.cs",
    "Assets/BalloonRush/Tests/Editor/TimingEvaluatorTests.cs",
    "Hardware/Arduino/BalloonRushCabinetIO/BalloonRushCabinetIO.ino",
    "Packages/manifest.json",
    "ProjectSettings/ProjectVersion.txt",
    "Documentation/IMPROVED_PRODUCTION_SPEC.md",
    "Documentation/DEFAULT_BALANCE_PROFILE.md",
    "Documentation/BUILD_VALIDATION_REPORT.md",
    "Documentation/DELIVERY_MANIFEST.md",
    "CHANGELOG.md",
    "README.md",
]

TYPE_RE = re.compile(r"\b(?:class|struct|interface|enum)\s+([A-Za-z_][A-Za-z0-9_]*)")
PLACEHOLDER_RE = re.compile(r"\b(?:TODO|FIXME|HACK|NotImplementedException)\b", re.IGNORECASE)
PP_OPEN = re.compile(r"^\s*#(?:if|region)\b")
PP_CLOSE = re.compile(r"^\s*#(?:endif|endregion)\b")


def scan_csharp(path: Path) -> list[str]:
    text = path.read_text(encoding="utf-8")
    errors: list[str] = []
    stack: list[tuple[str, int]] = []
    pairs = {"}": "{", "]": "[", ")": "("}
    opens = set(pairs.values())
    state = "code"
    escaped = False
    line = 1
    i = 0

    while i < len(text):
        ch = text[i]
        nxt = text[i + 1] if i + 1 < len(text) else ""

        if ch == "\n":
            line += 1
            if state == "line_comment":
                state = "code"
            i += 1
            continue

        if state == "line_comment":
            i += 1
            continue
        if state == "block_comment":
            if ch == "*" and nxt == "/":
                state = "code"
                i += 2
            else:
                i += 1
            continue
        if state == "string":
            if escaped:
                escaped = False
            elif ch == "\\":
                escaped = True
            elif ch == '"':
                state = "code"
            i += 1
            continue
        if state == "verbatim_string":
            if ch == '"' and nxt == '"':
                i += 2
                continue
            if ch == '"':
                state = "code"
            i += 1
            continue
        if state == "char":
            if escaped:
                escaped = False
            elif ch == "\\":
                escaped = True
            elif ch == "'":
                state = "code"
            i += 1
            continue

        if ch == "/" and nxt == "/":
            state = "line_comment"
            i += 2
            continue
        if ch == "/" and nxt == "*":
            state = "block_comment"
            i += 2
            continue
        if ch == "@" and nxt == '"':
            state = "verbatim_string"
            i += 2
            continue
        if ch == '"':
            state = "string"
            i += 1
            continue
        if ch == "'":
            state = "char"
            i += 1
            continue

        if ch in opens:
            stack.append((ch, line))
        elif ch in pairs:
            if not stack or stack[-1][0] != pairs[ch]:
                errors.append(f"{path.relative_to(ROOT)}:{line}: unmatched {ch}")
            else:
                stack.pop()
        i += 1

    if state in {"block_comment", "string", "verbatim_string", "char"}:
        errors.append(f"{path.relative_to(ROOT)}:{line}: unterminated {state}")
    for opener, open_line in reversed(stack):
        errors.append(f"{path.relative_to(ROOT)}:{open_line}: unclosed {opener}")

    pp_stack: list[tuple[str, int]] = []
    for line_number, source_line in enumerate(text.splitlines(), 1):
        if PP_OPEN.match(source_line):
            pp_stack.append((source_line.strip(), line_number))
        elif PP_CLOSE.match(source_line):
            if not pp_stack:
                errors.append(f"{path.relative_to(ROOT)}:{line_number}: unmatched preprocessor close")
            else:
                pp_stack.pop()
    for directive, open_line in pp_stack:
        errors.append(f"{path.relative_to(ROOT)}:{open_line}: unclosed preprocessor block {directive}")
    return errors


def main() -> int:
    errors: list[str] = []
    warnings: list[str] = []

    for relative in REQUIRED:
        if not (ROOT / relative).is_file():
            errors.append(f"missing required file: {relative}")

    for json_path in sorted(ROOT.rglob("*.json")) + sorted(ROOT.rglob("*.asmdef")):
        try:
            json.loads(json_path.read_text(encoding="utf-8"))
        except Exception as exc:  # noqa: BLE001
            errors.append(f"invalid JSON: {json_path.relative_to(ROOT)}: {exc}")

    cs_files = sorted(CS_ROOT.rglob("*.cs"))
    types: dict[str, list[Path]] = defaultdict(list)
    line_count = 0
    for path in cs_files:
        text = path.read_text(encoding="utf-8")
        line_count += len(text.splitlines())
        errors.extend(scan_csharp(path))
        for match in TYPE_RE.finditer(text):
            types[match.group(1)].append(path)
        for match in PLACEHOLDER_RE.finditer(text):
            errors.append(f"placeholder marker in {path.relative_to(ROOT)}: {match.group(0)}")

    for name, paths in types.items():
        unique = sorted({str(path.relative_to(ROOT)) for path in paths})
        if len(unique) > 1:
            errors.append(f"duplicate declared type {name}: {', '.join(unique)}")

    operator_settings = ROOT / "Assets/BalloonRush/Scripts/SaveSystem/OperatorSettings.cs"
    score_manager = ROOT / "Assets/BalloonRush/Scripts/Gameplay/ScoreManager.cs"
    ticket_manager = ROOT / "Assets/BalloonRush/Scripts/Redemption/TicketManager.cs"
    cap_checks = [
        (operator_settings, "Mathf.Clamp(maxTicketPayout, minimumTotalCap, 1000)"),
        (operator_settings, "Mathf.Clamp(jackpotTickets, 1, 500)"),
        (score_manager, "Mathf.Clamp(cap, 1, 1000)"),
        (ticket_manager, "Mathf.Clamp(maximum, 1, 1000)"),
    ]
    for path, needle in cap_checks:
        if path.is_file() and needle not in path.read_text(encoding="utf-8"):
            errors.append(f"payout safety check not found in {path.relative_to(ROOT)}: {needle}")

    production_checks = [
        (operator_settings, "public int inputDebounceMilliseconds = 25;"),
        (operator_settings, "public float greenSpawnWeight = 1.0f;"),
        (operator_settings, "public float blueSpawnWeight = 0.08f;"),
        (operator_settings, "public float perfectTicketMultiplier = 1.10f;"),
        (ROOT / "Assets/BalloonRush/Scripts/SaveSystem/SaveManager.cs", "private const int CurrentSaveVersion = 3;"),
        (ROOT / "Assets/BalloonRush/Scripts/Core/GameConfig.cs", 'public string buildVersion = "1.4.0";'),
        (ROOT / "Assets/BalloonRush/Scripts/SaveSystem/SessionAuditLogger.cs", "tickets_regular,tickets_bonus,tickets_jackpot"),
        (ROOT / "Assets/BalloonRush/Scripts/Redemption/TicketManager.cs", "PAID:n acknowledgement"),
        (ROOT / "Assets/BalloonRush/Scripts/Input/SerialArcadeIO.cs", "READER_CREDIT"),
        (ROOT / "Assets/BalloonRush/Editor/BalloonRushProjectBuilder.cs", "BalloonRushPreflightValidator.ValidateOrThrow();"),
        (ROOT / "Assets/BalloonRush/Editor/BalloonRushPreflightValidator.cs", "IPreprocessBuildWithReport"),
        (ROOT / "Assets/BalloonRush/Scripts/Input/KeyboardArcadeIO.cs", "keyboard.pKey.wasPressedThisFrame"),
        (ROOT / "Assets/BalloonRush/Scripts/Input/KeyboardArcadeIO.cs", "keyboard.upArrowKey.wasPressedThisFrame"),
        (ROOT / "Assets/BalloonRush/Scripts/Input/KeyboardArcadeIO.cs", "keyboard.mKey.wasPressedThisFrame"),
        (ROOT / "Assets/BalloonRush/Scripts/UI/DebugPanelManager.cs", "ESC CLOSE"),
        (ROOT / "Assets/BalloonRush/Scripts/UI/OperatorMenuManager.cs", "HandleBackButton"),
    ]
    for path, needle in production_checks:
        if path.is_file() and needle not in path.read_text(encoding="utf-8"):
            errors.append(f"production safeguard not found in {path.relative_to(ROOT)}: {needle}")

    if not (ROOT / "Assets/BalloonRush/ReferenceArt/GameplayMockup.png").is_file():
        warnings.append("gameplay reference mockup is missing")

    print("Balloon Rush static validation")
    print(f"Project: {ROOT}")
    print(f"C# files: {len(cs_files)}")
    print(f"C# lines: {line_count}")
    print(f"Declared types: {len(types)}")
    print(f"Warnings: {len(warnings)}")
    for warning in warnings:
        print(f"WARNING: {warning}")

    if errors:
        print(f"Errors: {len(errors)}")
        for error in errors:
            print(f"ERROR: {error}")
        return 1

    print("Errors: 0")
    print("PASS: static project validation succeeded.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
