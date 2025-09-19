# Layered Atmosphere and Orbit

![Text](/Mod%20Page/Images/Layered%20Atmosphere%20and%20Orbit.png)

If you ever wanted to have more planet layers to explore with Gravship.

## Mod Contents

Layered Atmosphere and Orbit (LAO) mod adds new planet layers with their own points of interest to fly to on your Gravship. It also aims to unify planet layer logic to align modded layers, especially when there are many of them.

### Some of features:

* A compact button on the world map to view any planet layer.
* Quest and incident generation is optimized for modded planet layers, preventing, for example, surface-specific objects from appearing on inappropriate layers.
* Can automatically integrate new planet layers into game scenarios, even in existing saves, without patches.
* Connections between planet layers are optimized and no longer require manual patching, with order determined by elevation above the planet's surface.
* Introduces groups (Surface, Atmosphere, Orbit) to combine layers with similar functionality.
* Adds numerous natural and quest-generated world objects to the new planet layers.
* A dedicated tab now describes the currently selected world object and its associated planet layer.

![Text](/Mod%20Page/Images/Content/LAODesc1.png)
![Text](/Mod%20Page/Images/Content/LAODesc2.png)
![Text](/Mod%20Page/Images/Content/LAODesc3.png)

### Features of Mod Settings:

* Option to hide "view ***" buttons for individual planet layers, which is especially useful when many modded layers clutter the interface. Leaving only LAO's compact button.
* Option for additional fuel cost to fly from a lower layer to a higher layer. It makes it more logical that you need more fuel to counter the force of gravity. Has an adjustable fuel per km ratio.
* Option for showing world objects within same planet layer group. For example, allowing to see all objects within the atmosphere group without needing to swap between layers.
* Option for showing world objects within related planet layer groups. Allow to see all objects within the atmosphere group when Surface layer selected.
* Option to auto swap to the correct planet layer, when clicked on a world object, even if it's not on the currently viewed layer.
* Option to select what layers to add to the scenario without needing to patch every single one. Allowing to add new layers even to existing saves.

## Links

Was intended to be part of [Complementary Odyssey](https://steamcommunity.com/sharedfiles/filedetails/?id=3546612303) mod, but end up separating it into this mod.

[Steam Workshop Page](https://steamcommunity.com/sharedfiles/filedetails/?id=3546612303)

[Discord](https://discord.gg/tKsBgzzTsG)

## Mod Compatibility

Should have no compatibility issues with other mods itself. Will work without issues even with mods that adds unrelated new planet layers, but for better integration request additional compatability patch for such mod. This would insert new layer within LAO's planet layer groups. To utilize LAO mod developer only need to add LayeredAtmosphereOrbit.LayeredAtmosphereOrbitDefModExtension to relatable defs. Refer to [b]For Modders[/b] on steam workshop page.

Deeper integration with:
[Mod Collection](https://steamcommunity.com/workshop/filedetails/?id=3569410532)
- Sky Islands (waiting for update release)

Supported languages:
* English,
* Russian (TBA)

## Add/Remove

Layered Atmosphere and Orbit mod should be safe to add even to existing save. However, due to the intricate way planet layers are integrated within the save data, removing the mod afterward is currently not feasible. While a future solution to enable removal might be implemented, this functionality is still under development.

## To Do

- Support for moons planet layer.
- RU TL.
- Investigate methods to safely remove planet layers from existing save files.
- Manual selection of what layer to add.
- More content to new planet layers.
- Create a new scenario for a tribal start on a floating island.
- Possible support for underwater planet layer.