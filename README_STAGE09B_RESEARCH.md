# Forest Archery VR — Stage 09B Research Version

## Unity and target

- Unity: 6000.3.6f1
- Device: Meta Quest 3
- Main scene: Assets/Liu_Environment/Scenes/ForestArcheryScene.unity

## Implemented functionality

- Controller Mode
- Hand Tracking Mode
- bow/string modality separation
- rabbit, deer, and bird targets
- dynamic wildlife scoring
- arrow trajectory
- player profiles
- personal best and leaderboards
- Pause, Resume, and Quit Round
- Practice round: 1 minute
- Recorded research round: 5 minutes
- research CSV logging:
  - research_rounds.csv
  - research_shots.csv
  - research_events.csv

## Quest CSV files

The CSV files are written under:

Application.persistentDataPath/ForestArcheryResearch

Use:

BuildTools/Export_Quest_Research_CSVs.ps1

to export them from the headset.

## Important Quest build note

Do not commit the Library folder.

The repository includes BuildTools scripts for the local Meta XR manifest
workaround and Player build-cache repair used during development.

## Repository hygiene

Do not commit:

- Library
- Temp
- obj
- Logs
- UserSettings
- APK/AAB files
- local backups
- local baselines

This is a review branch. It does not overwrite main.
