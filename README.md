# BnL Reloaded
This is the open source github repo for the bnl private server recreation project.

To run the private server, you'll need to do a couple things first.

First, you must add to the BNL launch options (which you can access by right clicking Block N Load in your steam library and selecting "Properties") something along the lines of this: ["C:\Program Files (x86)\Steam\steamapps\common\BlockNLoad\Win64\BlockNLoad.exe" %COMMAND%] (without the square brackets). The path you put in may or may not be different depending on where the location of the BlockNLoad.exe file is on your computer.

After, you need to use something like https://github.com/dnSpy/dnSpy to edit the Assembly-CSharp.dll file in ..\Win64\BlockNLoad_Data\Managed and edit this function in LoginLogic to just "return true": https://media.discordapp.net/attachments/1400962017816215595/1401331912059912322/image.png?ex=68967b0b&is=6895298b&hm=90f5d980be3f0e152f38c7600a9c2c462bfc952b0652c1bddf852a0611a4d1d4&=&format=webp&quality=lossless&width=1923&height=402 

The next thing you have to change is the server IP. It's hardcoded to the original server's IP address: "162.55.251.122". You can use dnSpy, but I had trouble getting it to compile correctly, and so I used https://hexed.it/, uploaded the dll's contents, searched for the server's IP address, and replaced the bytes corresponding to that string with "bnlreloaded.co" (they need to be the same length). After you download the edited dll and replace the original dll in ..\Win64\BlockNLoad_Data\Managed with the new edited one, you have to go into your hosts file at C:\Windows\System32\drivers\etc\hosts and add a new entry linking 127.0.0.1 to bnlreloaded.co

The dll changes will be permanent unless you verify the integrity of the game files (or do something to cause steam to do it automatically). You should save a copy of the edited dll somewhere just in case you need to replace it again.

To use the cdb serializer/deserializer, create a folder called Cache in the base directory (should be in the same directory as the BaseTypes, Database, etc. folders). Put the cdb file from the game assets into the new Cache folder. Then change the toJson and fromJson constants to serialize/deserialize the cdb to a json file/zipped cdb file respectively.

## Example configuration

Server settings live in `BNLReloadedServer/Configs/configs.json` and are deserialized with snake_case field names. A minimal single-node setup with the master HTTP status endpoint enabled looks like this:

```json
{
  "is_master": true,
  "run_server": true,
  "use_master_cdb": false,
  "cdb_name": "cdb",
  "master_host": "127.0.0.1",
  "master_public_host": "127.0.0.1",
  "region_host": "127.0.0.1",
  "region_public_host": "127.0.0.1",
  "region_name": "Local Region",
  "region_icon": "server_namericaeast",
  "to_json": false,
  "to_json_name": null,
  "from_json": false,
  "from_json_name": null,
  "debug_mode": true,
  "do_readline": true,
  "enable_master_status_http": true,
  "master_status_http_port": 28104,
  "master_status_http_host": "*"
}
```

For multi-region deployments, set `is_master` to `false` on the region servers and point `master_host`/`master_public_host` at the master instance. Regions still register with the master and are exposed through the master status HTTP endpoint when it is enabled.

## Master status HTTP endpoint

If `EnableMasterStatusHttp` is enabled in the configuration, the master process hosts a lightweight HTTP endpoint at `MasterStatusHttpHost:MasterStatusHttpPort` that returns JSON describing every connected region. Each region registers with the master when it connects, and the master status endpoint fetches live status from every registered region using the existing master↔region service RPCs. That means you can still retrieve up-to-date data for all regions (including the master region itself) directly from the master endpoint without running any per-region HTTP status servers.

Example payload returned from `http://127.0.0.1:28104/` (whitespace added for clarity):

```json
{
  "regions": [
    {
      "RegionId": "master",
      "RegionName": "Local Region",
      "Host": "127.0.0.1",
      "Port": 28102,
      "OnlinePlayers": 3,
      "Queues": {
        "quick_match": 2,
        "custom": 1
      },
      "QueuePlayers": {
        "quick_match": [
          {
            "Id": 12345,
            "Name": "Alice",
            "SquadId": null,
            "JoinedAt": 1715200000
          }
        ]
      },
      "CustomGames": [],
      "ActiveGames": [
        {
          "Id": "game-1",
          "Mode": "quick_match",
          "StartedAt": 1715200100,
          "MatchDurationSeconds": null,
          "Players": []
        }
      ],
      "Error": null
    }
  ]
}
```
