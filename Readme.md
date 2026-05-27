# Douyin Danmu Unity3D Battle

Unity3D danmu battle prototype for a human-vs-monster battlefield. The project currently includes runtime model loading, two tank variants, soldier and giant enemy assets, battle effects/audio foundations, and a local HTTP danmu command gateway.

## Current Features

- 16 tanks total: 6 `tank_t-55a.glb` units and 10 `t-55ak.glb` units.
- Soldiers use `soldier.glb`.
- Giant enemies use `sevens_sin_helldog.glb`, with 10 units in the scene.
- Runtime GLB loading through UnityGLTF.
- Danmu command parsing, command queue, and HTTP gateway.
- Battle effect and audio manager foundations.
- Windows launch scripts for local play and testing.

## Project Layout

- `Assets/` Unity scenes, scripts, materials, and runtime assets.
- `Packages/` Unity package manifest and vendored UnityGLTF package.
- `ProjectSettings/` Unity project settings.
- `doc/` development design documents, asset sources, VFX setup, and implementation notes.
- `tools/` helper scripts for local danmu command testing.
- `start-game.ps1` and `start-game.bat` local startup scripts.

## Run Locally

Build or open the Unity project with the Unity version listed in `ProjectSettings/ProjectVersion.txt`.

After a Windows build exists under `Builds/`, start the game with:

```powershell
.\start-game.ps1
```

To enable the local danmu HTTP gateway on a specific port:

```powershell
.\start-game.ps1 -DanmuHttpPort 8788
```

Send test danmu commands:

```powershell
.\tools\send-danmu-test.ps1 -HostUrl http://127.0.0.1:8788
```

## Documentation

- [Development plan](doc/danmu-battle-unity3d-development.md)
- [Free assets and VFX setup](doc/free-assets-and-vfx-setup.md)
- [Danmu HTTP gateway](doc/danmu-http-gateway.md)
- [Implementation status](doc/implementation-status.md)

## Notes

Unity-generated folders such as `Library/`, `Temp/`, `Builds/`, `Logs/`, and `UserSettings/` are ignored by Git. Runtime assets required by the scene are kept under `Assets/StreamingAssets/`.
