# SDV++

SDV++ is a mod for Stardew Valley 1.6. It is at an early stage: this version sets up the mod and its content pack, and adds no features yet.

## Requirements

- Stardew Valley 1.6.15
- [SMAPI](https://smapi.io) 4.0 or later
- [Content Patcher](https://www.nexusmods.com/stardewvalley/mods/1915) 2.0 or later
- [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098), optional, to change settings in game

## Installing

Install SMAPI and Content Patcher, then copy the `SDVPP` and `SDVPP.Content` folders into your `Mods` folder. You need both: `SDVPP` is the mod and `SDVPP.Content` is its content pack.

In co-op, every player needs the same version of SDV++.

## Building from source

You need the .NET 6 SDK or later and an installed copy of the game.

```bash
dotnet build             # builds the mod and copies it to the game's Mods folder
dotnet build -c Release  # writes a release zip to ./release
```

`tools/link-content.sh` links the content pack into the game's `Mods` folder, so content changes apply after `patch reload Luke.SDVPP.Content` in the SMAPI console. `tools/check-version.sh` checks that the project and both manifests have the same version.

## License

SDV++ is licensed under the GNU AGPL v3.0. See [LICENSING.md](LICENSING.md).
