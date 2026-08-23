# GitHub Setup

The project is already Git-ready and includes a Unity `.gitignore`.

## Create the repository

From the project root:

```bash
git init
git add .
git commit -m "Initial Balloon Rush Unity 6 project"
git branch -M main
git remote add origin <YOUR_GITHUB_REPOSITORY_URL>
git push -u origin main
```

## What should be committed

Commit:

- `Assets`
- `Packages`
- `ProjectSettings`
- documentation, hardware sketches, and build scripts
- all Unity `.meta` files after Unity generates them

Do not commit:

- `Library`
- `Temp`
- `Logs`
- `Obj`
- `Builds`
- IDE-generated files

## Git LFS

The current placeholder project does not require Git LFS. Add it before importing large production audio, video, layered art, or source animation files.

Example:

```bash
git lfs install
git lfs track "*.wav" "*.psd" "*.fbx" "*.mp4"
git add .gitattributes
git commit -m "Configure Git LFS for production assets"
```

## Recommended workflow

- `main` — cabinet-tested release branch
- `develop` — integration branch
- `feature/...` — gameplay or art changes
- release tags such as `v0.1-playtest`, `v0.9-location-test`, and `v1.0-cabinet`

Before merging into `main`:

1. Run Edit Mode tests.
2. Run a full game from Boot to Attract.
3. Test credit consumption.
4. Test a normal payout and capped payout.
5. Test with serial hardware enabled and disabled.
6. Build and run the Windows cabinet executable.
