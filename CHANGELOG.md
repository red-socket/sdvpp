# Changelog

All notable changes to SDV++ are listed here. Versions apply to both the mod and its content pack.

## Unreleased

- Fixed: the birthday day question ran off the top of short screens, such as phones, the Steam Deck or a large UI scale, so you couldn't pick a day. Each week's days now come in two pages, and no question has more than five answers.
- The sprinkler upgrade recipes now sit right after the Quality and Iridium Sprinkler in the crafting menu, instead of at the end of the last page.

## 0.5.5 (2026-10-23)

- SMAPI can now tell you when a new version of SDV++ is out. Both manifests have update keys for Nexus and GitHub, so SMAPI no longer warns that they're missing.

## 0.5.4 (2026-10-20)

- SDV++ now loads on Android with the Android port of SMAPI. Android isn't tested, so some features might not work there.
- SMAPI now says clearly that SDV++ needs Stardew Valley 1.6.15.
- If the main mod can't load, SMAPI skips the content pack too, instead of showing errors for the Beach and Forest minecart patches.
- The README lists the supported platforms.

## 0.5.3 (2026-10-19)

- Sprinklers water the garden pots in their range every morning, the same way they water tilled soil. The Pressure Nozzle's extra range counts, and outdoor sprinklers don't run on rainy days. (`SprinklersWaterGardenPots`, host's setting)

## 0.5.2 (2026-10-18)

- The rails at the Beach minecart stop sit on the sand instead of a strip of dirt, and the driftwood log on the track is gone.
- At the Forest stop, the track comes out of a small cave in the cliff. In winter the ground in front of it is covered in snow, like the rest of the Forest.

## 0.5.1 (2026-10-18)

- Added French, German, Brazilian Portuguese and Spanish. SDV++ follows the game's language setting.
- Item, place and villager names follow each language's version of the game, and letters and greetings change their wording to match the farmer's gender.

## 0.5.0 (2026-10-18)

- Your birthday: Mayor Lewis asks for the date. On the day, villagers and your spouse wish you a happy birthday and friends send letters with small gifts. (`PlayerBirthday`)
- Robin can paint silos, including Big and Deluxe Silos. (`SiloPainting`, content pack)
- Robin's buildings and house upgrades can use Wood and Hardwood from the log pile. (`BuildWithShelterWood`)
- Clicking a crop or forage while holding food no longer asks whether to eat it. (`NoEatPromptOnCrops`)
- Opening a door while holding an item no longer gives it to a villager standing below the door. (`NoGiftsThroughDoors`)
- Holding, placing or pointing at a sprinkler, scarecrow, bee house or Junimo hut highlights the tiles it covers. (`RangePreview`)
- Settings and messages say "log pile" instead of "wood shelter".

## 0.4.0 (2026-10-17)

- The title screen skips the intro, and the slide to New, Load and Co-op is shorter. (`FastTitleScreen`, `TitleSlideMilliseconds`)
- The title, load, co-op and character creation screens use your in-game UI scale. (`TitleUiScale`)
- The options menu offers larger UI scales and zoom up to 250%. (`ExtendedScaleRanges`)
- Menus work with the keyboard. (`KeyboardMenus`)
- The fade between locations is faster. (`FadeSpeed`)
- You can refill your watering can at the Pelican Town fountain, except in winter. (`FountainWater`, content pack)
- Crafting on the farm or in a farmhouse can use Wood and Hardwood from the log pile. (`CraftWithShelterWood`)
- Fixed: the log pile stopped working after a night with nothing in it.

## 0.3.0 (2026-10-17)

- Mayor Lewis sends a letter the day before your wedding. (`WeddingReminder`)
- The log pile next to the farmhouse door stores Wood and Hardwood for the whole farm. (`WoodShelterStorage`)
- Minecart stops in Cindersap Forest and on the Beach. (`MinecartStops`, content pack)
- Unused fishing tackle stacks. (`StackUnusedTackle`)
- The release zip includes the content pack.

## 0.2.0 (2026-10-17)

- On the last day of spring, summer and fall, Pierre and JojaMart also sell next season's seeds. (`NextSeasonSeeds`, content pack)
- Craft a Quality Sprinkler from a Sprinkler, and an Iridium Sprinkler from a Quality Sprinkler. (`SprinklerUpgradeRecipes`, content pack)
- Robin can upgrade a Silo to a Big Silo (500 hay), then to a Deluxe Silo (750 hay). (`SiloUpgrades`, content pack)
- A sprinkler that reaches the pet bowl's water fills it every morning. (`SprinklersFillPetBowl`)
- The game asks before a warp totem or the Return Scepter sends you away. (`ConfirmWarpTotems`, `ConfirmReturnScepter`)
- Time stops at home while you decorate. (`DecoratingTimeFreeze`)
- Settings can be changed in Generic Mod Config Menu if it's installed.

## 0.1.0 (2026-10-17)

- First version: the mod and its content pack, with no features yet.
