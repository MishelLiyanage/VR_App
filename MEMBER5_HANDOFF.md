# Member 5 Handoff

## Workbench

Member 5 development lives in `Assets/_Project/Scenes/Workbenches/Shali.unity`. Use **CSI VR > Member 5 > Setup Shali Workbench** to add or refresh the case manager and spatial UI without replacing the existing environment or rigs. Test changes in Shali first; Member 1 remains responsible for final integration into `Main.unity`.

## Implemented

- `CaseManager`: Briefing -> Tutorial -> Investigating -> ReadyToSubmit -> Debrief.
- Action-driven tutorial checkpoints for teleport, grab/release, and held-tool activation.
- Four unique evidence records (`E01`-`E04`) with duplicate prevention and missing-evidence feedback.
- Three findings, including an evidence-based correct conclusion and a meaningful incorrect conclusion.
- World-space briefing/progress/finding/debrief UI, Help, Credits, and clean scene-reload restart.
- Desktop rig reports successful teleport, grab/release, and held-tool activation to the same tutorial logic.

## Integration contracts

- Member 3 evidence station calls `CaseManager.Instance.RecordEvidence("E01")`, `E02`, or `E03` only after an eligible item is released. Use the returned `bool` for accepted/duplicate feedback.
- Member 4 UV scanner calls `CaseManager.Instance.RecordEvidence("E04")` only after the full valid dwell succeeds.
- XR tutorial objects connect their real events to `TutorialActionReporter.ReportTeleport`, `ReportGrabRelease`, and `ReportToolActivation`.
- Do not keep progress counters inside evidence or scanner scripts; `CaseManager` owns progress.

## Required human tests before claiming completion

1. Another member completes the tutorial without coaching in XR Device Simulator.
2. Early submission reports the missing IDs; each clue records once; duplicates do not increase `4/4`.
3. Correct and incorrect findings both show a debrief; Restart returns to a clean briefing.
4. The Windows build is completed using keyboard/mouse with Unity closed.
5. Record tester, build/commit, expected/actual result, pass/fail, fix, and retest. Do not present these checks as passed until actually observed.
