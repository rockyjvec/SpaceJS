# The Javascript Programmable Block Mod

Steam Workshop: https://steamcommunity.com/sharedfiles/filedetails/?id=1537730714

A Javascript Programmable Block mod for Space Engineers.

This mod is experimental and should NOT be used on live servers or games.  There are likely memory leaks and other game crashing bugs.  You have been warned.

### [Documentation](doc/API.md)

# YouTube Demo

[![SpaceJS](http://img.youtube.com/vi/uGIF6IA48zc/0.jpg)](http://www.youtube.com/watch?v=uGIF6IA48zc)

# Features

* Still works with in-game scripts disabled.
* Interpreted in a state machine
* Scripts can go into an infinite loop and/or run forever and never cause any complexity errors or slow down the server.
* Smart throttle that prevents more then a set amount of statements from getting executed across ALL programmable blocks.  Tested by running an infinite while loop on 64 programmable blocks at the same time with no performance loss.  Each block just ran slower: https://youtu.be/qgLJvDc4Zq0
* Events can "interrupt" the execution of a script.  No more needing to write state machines to avoid complexity errors.

# Wish List

* I'm hoping to make it so other mods can add functionality to the API.

# Credits

This mod uses a heavily modified version of esprima-dotnet and jint by Sebastien Ros:

https://github.com/sebastienros/esprima-dotnet

https://github.com/sebastienros/jint


The thumbnail is from logo.js by voodootikigod:

https://github.com/voodootikigod/logo.js
