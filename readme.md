


This mod adds Save Loadout and Load Loadout buttons to the equipment screen that let you quickly equip sets of items to a merc or restock selected items easily, as well as share gear sets between mercs.

With this mod you can:
- Restock medkits and smokes quickly.
- Put your melee merc back togeher after their 25th death.
- Requip implants to your cyborg superhuman are a gory meeting with Shedu's Thousand.



[h2]Whats new[/h2]
Made it possible to equip loadouts between mercs
Improved reliability of implant/aug/backpack loading (no more minigun arms carrying a minigun)
Added multi language support for 
- Russian
- Spanish
- German
- French
- Portuguese (Brazil)
- Chinese (Simplified)
- Chinese (Traditional)
- Japanese
- Korean
- Polish
See the Changing Language section lower down for more info


[h2]Setting the items[/h2]
[h3]Saving a mercenary loadout[/h3]
Select a merc and hit Save Loadout to save whatever that merc is wearing (including limbs and implants). This loadout is not shared between mercs.
[h3]Updating the Quick Restock items[/h3]
Select a merc and hit Update Quick Restock to save the current inventory items into the quick restock configuration. This config is shared between all mercs.

Not that everything in the invetory will be saved (including belt slots). 

[h2]Loading items[/h2]
[h3]Equipping a mercenary loadout[/h3]
After you've saved a loadout, just hit the Load equipment button to have your merc automatically equip those items from the ship inventory. 

You can also hit the dropdown next to the load button to pick a loadout from another merc.

[h3]Equipping Quick Restock items[/h3]
Hit Quick Restock to add the items to the inventory, if you find some are not equipping it's likely that there aren't any left on board the ship.

By default it'll equip 
- 2 military medkits 
- A bottle of water.


[h2]Per Save Config[/h2]
Each save slot also has its own config but you can copy them between each save in the mod config (requires editing files, see Editing the config manually)

[h2]Editing the config manually[/h2]
If you want you can edit the config yourself. The config should be at 
[code]%appdata%\..\LocalLow\Magnum Scriptum LTD\Quasimorph_ModConfigs\QuickGear\slot_0_config.json[/code]

For me thats 
[code]C:\Users\ME\AppData\LocalLow\Magnum Scriptum LTD\Quasimorph_ModConfigs\QuickGear\slot_0_config.json[/code]

make sure to change the slot number for your game slot. You can set the items you want in there as well as the shared quick restock list for the mod.

The item names can be found on the wiki.

[h2]Changing language[/h2]
Check the save location (as mentioned in "Editing the config manually") and edit global_config.json, then change the "en" next to Language to your selected language option.


[h2]Why did I make this[/h2]
I made this as I suck and keep dying, and I’m sick of requipping the same setup: Bantages, Splints, Medpacks, etc

If you want to change something, or add a language the repo is here:
[url=github.com/claydegruchy/Quasimorph_QuickGear] Github [/url]
