# The Javascript Programmable Block Mod

Steam Workshop: https://steamcommunity.com/sharedfiles/filedetails/?id=1537730714

A Javascript Programmable Block mod for Space Engineers.

This mod is experimental and should NOT be used on live servers or games.  There are likely memory leaks and other game crashing bugs.  You have been warned.

### [Documentation](doc/API.md)

# YouTube Demo

[![SpaceJS](http://img.youtube.com/vi/uGIF6IA48zc/0.jpg)](http://www.youtube.com/watch?v=uGIF6IA48zc)

# Features

* Still works with in-game scripts disabled.
* Since the javascript interpreter is a state machine, the programmable blocks can be throttled at the statement level.  This means that a javascript application can go into an infinite loop and/or run forever and never cause complexity errors or slow down the server.
* The "throttle" prevents more than a set amount of statements from getting executed across ALL programmable blocks.  So having 20 programmable blocks should not slow down the server.  Instead, each programmable block will run slower.  I tested this with 64 programamble blocks all running an infinite while loop with no noticable extra cpu usage.  https://youtu.be/qgLJvDc4Zq0
* Since the interpreted code executes in a sandbox, it should be much safer.
* Events can "interrupt" the execution of a script.  No more needing to write state machines to avoid complexity errors.

# Credits

This mod uses a heavily modified version of esprima-dotnet and jint by Sebastien Ros:

https://github.com/sebastienros/esprima-dotnet

https://github.com/sebastienros/jint


The thumbnail is from logo.js by voodootikigod:

https://github.com/voodootikigod/logo.js
