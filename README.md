# LethalQuestsDiscordBot
Discord Bot for Lethal Quests ArkAPI Plugin https://discord.gg/u5v44xC

_.net core 3.1 runtime needed_

Fill in the all the settings inside the `appsettings.json`
Valid connection strings for MySQL will look like this:
> "Server=myServerAddress;Database=myDataBase;Uid=myUsername;Pwd=myPassword;"
> "Server=myServerAddress;Port=1234;Database=myDataBase;Uid=myUsername;Pwd=myPassword;"
I have created 2 sample commands `kills` and `rares` as examples for you.
You will need to create additional commands to use.

Usage Example `!LQ top kills` will output the following type of information.
```d
# | Player Name | Tribe          | Player Kills | Your Deaths | K/D   | 
-----------------------------------------------------------------------
1 | Bob         | Does it freeze | 20           | 1           | 20.00 | 
2 | Jimmmy      | Nope           | 2            | 5           | 0.40  |
```
If you want to run multiple bots for whatever reason make sure each bot uses a unique port number in the `Url` under the `Kestrel` section

Need help figuring out how to get a bot token? Check out the link below:
