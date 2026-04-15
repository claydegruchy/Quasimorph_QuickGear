This mod lets you automatically add selected items to mercs easily, for example to restock medkits or smokes.

[h2]How it works[/h2]

You define a list of items you want  a merc to get, then press G (by default) and the mod attempts to automatically add those items to the currently selected merc’s inventory. If no merc is selected, it will attempt to add the items to EVERY merc.

You can change what is added in the config, by default it attempts to give everyone: 
2 military medkits 
A bottle of water.


[h2]Setting the items / Config[/h2]

The config should be at 
[code]%appdata%\..\LocalLow\Magnum Scriptum LTD\Quasimorph_ModConfigs\QuickGear\config.json[/code]

For me thats 
[code]C:\Users\ME\AppData\LocalLow\Magnum Scriptum LTD\Quasimorph_ModConfigs\QuickGear\config.json[/code]

You can set the items you want in there as well as the hotkey for the mod (Default G)



[h3]Per Save Config[/h3]
Each save slot also has its own config, you can find it in the same directory.

[h3]Premade settings[/h3]

Not everyone is big on playing around in files (who tf is jason) so heres some presets/examples that can be pasted directly into config.json:


2 military medkits and a bottle of water
  [code]{"Items":[{"ItemId":"medical_kit_2","Count":2},{"ItemId":"water_bottle_1","Count":1}],"HotkeyCode":"G"}[/code]
2 normal medkits and 2 packets of smokes
  [code]{"Items":[{"ItemId":"medical_kit_1","Count":2},{"ItemId":"cigarettes_1","Count":2}],"HotkeyCode":"G"}[/code]
1 set of antibiotics, 3 bandages, and 3 splits
  [code]{"Items":[{"ItemId":"pills_antibiotics","Count":1},{"ItemId":"bandage","Count":3},{"ItemId":"splint","Count":3}],"HotkeyCode":"G"}[/code]


The config uses the games internal names for items, you can find them on the wiki.

[h2]Issues[/h2]

Why can’t I just make it work for my currently selected merc? Why does it have to give items to everyone?
Limit of the game and my own ability. I couldn’t find a way to see what the currently selected merc is so it just gives them to everyone.


[h2]Why did I make this[/h2]

I made this as I suck and keep dying, and I’m sick of requipping the same setup: Bantages, Splints, Medpacks, etc


If you want to change something, the repo is here:
[url=github.com/claydegruchy/Quasimorph_QuickGear] Github [/url]
