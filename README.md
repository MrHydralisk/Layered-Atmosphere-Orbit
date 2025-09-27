# Layered Atmosphere and Orbit

![Text](/Mod%20Page/Images/Layered%20Atmosphere%20and%20Orbit.png)

If you ever wanted to have more planet layers to explore with Gravship. Or you wanted to easily create new planet layers or moons as a modder.

## Mod Contents

Layered Atmosphere and Orbit (LAO) mod adds new planet layers with their own points of interest to fly to on your Gravship. It also aims to unify planet layer logic to align modded layers, especially when there are many of them. It consists of many functions and QoL as a **Framework** for creating custom Planet Layers, Plants, Moons and adding content to existing.

### Some of features:

* A compact button on the world map to view any planet layer.
* Support for creating and adding custom Planets and Moons.
* Quest and incident generation is optimized for modded planet layers, preventing, for example, surface-specific objects from appearing on inappropriate layers.
* Can automatically integrate new planet layers into game scenarios, even in existing saves, without patches.
* Connections between planet layers are optimized and no longer require manual patching, with order determined by elevation above the planet's surface.
* More logical pathing of Gravship between planet layer and planets, instead of vanilla way of just teleporting to end point.
* Gravship using Grav Jump to move between Planets and Moons after leaving their gravity well.
* Introduces groups (Surface, Atmosphere, Orbit) to combine layers with similar functionality.
* Added planet like Rimworld consisting current vanilla layers.
* Introduces Luna moon as proof of concept.
* Adds numerous natural and quest-generated world objects to the new planet layers.
* A dedicated tab now describes the currently selected world object, its associated planet layer, biome and planet/moon.

![Text](/Mod%20Page/Images/Content/LAODesc1.png)
![Text](/Mod%20Page/Images/Content/LAODesc2.png)
![Text](/Mod%20Page/Images/Content/LAODesc3.png)
![Text](/Mod%20Page/Images/Content/LAODesc4.png)

### Features of Mod Settings:

* Option to hide "view ***" buttons for individual planet layers, which is especially useful when many modded layers clutter the interface. Leaving only LAO's compact button.
* Option to group "view ***" in LAO's compact button by planet layer groups and planets, which is usefull when more than one planet.
* Option to hide Terrain, Planet and Orbit world interface tabs. Leaving only LAO's Layer tab combining all that info.
* Option for additional fuel cost to fly from a lower layer to a higher layer. It makes it more logical that you need more fuel to counter the force of gravity. Have an adjustable fuel per km ratio.
* Option to adjust fuel cost for Grav Jump between planets and moons. Have an adjustable km per fuel ratio.
* Option to enable Gravship moving visually between planet layers and planets/moons, instead of teleportation to target planet layer from vanilla.
* Option for showing world objects within same planet layer group. For example, allowing to see all objects within the atmosphere group without needing to swap between layers.
* Option for showing world objects within related planet layer groups. Allow to see all objects within the atmosphere group when Surface layer selected.
* Option to auto swap to the correct planet layer, when clicked on a world object, even if it's not on the currently viewed layer.
* Option to select what layers to add to the scenario without needing to patch every single one. Allowing to add new layers even to existing saves.

## Links

Was intended to be part of [Complementary Odyssey](https://steamcommunity.com/sharedfiles/filedetails/?id=3546612303) mod, but end up separating it into this mod.

[Steam Workshop Page](https://steamcommunity.com/sharedfiles/filedetails/?id=3546612303)

[Discord](https://discord.gg/tKsBgzzTsG)

## Mod Compatibility

Should have no compatibility issues with other mods itself. Will work without issues even with mods that adds unrelated new planet layers, but for better integration request additional compatibility patch for such mod. This would insert new layer within LAO's planet layer groups. Process is very simple, so refer to **For Modders**.

Supported languages:
* English,
* Russian,
* Japanese (available on JP mod database site),
* Chinese (available on workshop by 天山螣老)

## Add/Remove

Layered Atmosphere and Orbit mod should be safe to add even to existing save. However, due to the intricate way planet layers are integrated within the save data, removing the mod afterward is not feasible. Adding orbiting type planet layers to existing save is possible, but they will spawn empty and will be filled after some in-game time pass. But adding surface type planet layers like new Planets and Moons require new save, since terrain generation happens only during game creation.

## To Do

- More content to new planet layers and moon.
- Create a new scenario for a tribal start on a floating island.
- Possible support for underwater/underground planet layers.
