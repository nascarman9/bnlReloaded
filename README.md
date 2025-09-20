# BnL Reloaded
This is the open source github repo for the bnl private server recreation project.

To run the private server, you'll need to do a couple things first.

First, you must add to the BNL launch options (which you can access by right clicking Block N Load in your steam library and selecting "Properties") something along the lines of this: ["C:\Program Files (x86)\Steam\steamapps\common\BlockNLoad\Win64\BlockNLoad.exe" %COMMAND%] (without the square brackets). The path you put in may or may not be different depending on where the location of the BlockNLoad.exe file is on your computer.

After, you need to use something like https://github.com/dnSpy/dnSpy to edit the Assembly-CSharp.dll file in ..\Win64\BlockNLoad_Data\Managed and edit this function in LoginLogic to just "return true": https://media.discordapp.net/attachments/1400962017816215595/1401331912059912322/image.png?ex=68967b0b&is=6895298b&hm=90f5d980be3f0e152f38c7600a9c2c462bfc952b0652c1bddf852a0611a4d1d4&=&format=webp&quality=lossless&width=1923&height=402 

The next thing you have to change is the server IP. It's hardcoded to the original server's IP address: "162.55.251.122". You can use dnSpy, but I had trouble getting it to compile correctly, and so I used https://hexed.it/, uploaded the dll's contents, searched for the server's IP address, and replaced the bytes corresponding to that string with "bnlreloaded.co" (they need to be the same length). After you download the edited dll and replace the original dll in ..\Win64\BlockNLoad_Data\Managed with the new edited one, you have to go into your hosts file at C:\Windows\System32\drivers\etc\hosts and add a new entry linking 127.0.0.1 to bnlreloaded.co

The dll changes will be permanent unless you verify the integrity of the game files (or do something to cause steam to do it automatically). You should save a copy of the edited dll somewhere just in case you need to replace it again.

To use the cdb serializer/deserializer, create a folder called Cache in the base directory (should be in the same directory as the BaseTypes, Database, etc. folders). Put the cdb file from the game assets into the new Cache folder. Then change the toJson and fromJson constants to serialize/deserialize the cdb to a json file/zipped cdb file respectively.
