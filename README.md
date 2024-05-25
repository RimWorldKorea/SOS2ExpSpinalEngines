# SOS2ExpSpinalEngines

Built versus SOS2 for 1.5 only. Previous versions absolutely will not work due to namechanges/shipcache changes. Thanks to Thain for providing some non-jank art for me to use! Anything cool looking is for sure Thains fault, any weird gfx glitches are my failures to implement it. Additional jank I added, also my fault.
Thanks to Erimathas for reviewing my hacky code! 

Like any mod from github, installation would require:
Click "code" button, and download as zip
Extract the resulting zip somewhere
Copy the resulting folder (SOS2ExpSpinalEngines-master, probably) into your rimworld mod directory
Where is the mod directory? For any non-trivial modding, using rimpy or rimsort would be preferred. Failing that, right click the game in steam, properties, installed files, browse, open "mods".

Balance thoughts: The addition of "EngineMass" in SOS2 complicated this. Currently in testing, I made an overcomplicated spreadsheet. Since a regular fuel support slice supports 3 engine accelerators, around 9 (nuclear) or 12 (chemfuel) amplifiers are needed to make more total thrust/mass, accounting for plating, engineMass, etc. This means a 24 length spinal chemfuel engine the same thrust as 8 chem engines, for double the spacercomps. For nuclear, length 20 (9 accelerators) is a bit less than 3 nuclear engines, for also about twice the spacercomps. 
