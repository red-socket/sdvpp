# SDV++

SDV++ is a mod for Stardew Valley 1.6 that adds quality-of-life changes. It works in single-player and in co-op, including split-screen.

## Requirements

- Stardew Valley 1.6.15
- [SMAPI](https://smapi.io) 4.0 or later
- [Content Patcher](https://www.nexusmods.com/stardewvalley/mods/1915) 2.0 or later
- [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098), optional, to change settings in game

## Installing

Install SMAPI and Content Patcher, then copy the `SDVPP` and `SDVPP.Content` folders into your `Mods` folder. You need both: `SDVPP` is the mod and `SDVPP.Content` is its content pack.

In co-op, every player needs the same version of SDV++.

## Features

Every feature can be turned off. Settings are in `Mods/SDVPP/config.json`, or in `Mods/SDVPP.Content/config.json` for features marked "content pack". Generic Mod Config Menu shows both. In co-op, keep content pack settings the same for every player.

### Farming and shops

- On the last day of spring, summer and fall, Pierre and JojaMart also sell next season's seeds. (`NextSeasonSeeds`, content pack)
- Craft a Quality Sprinkler from a Sprinkler (Farming 6), and an Iridium Sprinkler from a Quality Sprinkler (Farming 9). (`SprinklerUpgradeRecipes`, content pack)
- Robin can upgrade a Silo to a Big Silo that holds 500 hay, then to a Deluxe Silo that holds 750. (`SiloUpgrades`, content pack)
- A sprinkler that reaches the pet bowl's water fills it every morning. (`SprinklersFillPetBowl`, host's setting)

### Fewer accidents

- The game asks before a warp totem or the Return Scepter sends you away. (`ConfirmWarpTotems`, `ConfirmReturnScepter`)

### Decorating

- Time stops at home while you hold a piece of furniture, so you can decorate without losing the day. Set `DecoratingTimeFreeze` to `HoldingFurniture` (default), `AnywhereAtHome` or `Off`. In co-op the host's setting applies, and the clock only stops when every player is decorating at home.

## Known limitations

- Each sprinkler upgrade recipe you know counts toward Craft Master and Perfection, so you need to craft each one once.
- Big and Deluxe Silos look like a regular silo.
- Turning off `SiloUpgrades` or removing SDV++ leaves upgraded silos without building data.

## Building from source

You need the .NET 6 SDK or later and an installed copy of the game.

```bash
dotnet build             # builds the mod and copies it to the game's Mods folder
dotnet build -c Release  # writes a release zip to ./release
```

`tools/link-content.sh` links the content pack into the game's `Mods` folder, so content changes apply after `patch reload Luke.SDVPP.Content` in the SMAPI console. `tools/check-version.sh` checks that the project and both manifests have the same version.

## License

SDV++ is licensed under the GNU AGPL v3.0. See [LICENSING.md](LICENSING.md).
