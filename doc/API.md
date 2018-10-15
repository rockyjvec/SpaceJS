# API Globals

## Global API Objects

[Console](Console.md)

[Event](Event.md)

[Blocks](Blocks.md)

## Objects

[Block](Block.md)

## Blocks

Manages blocks.

#### Blocks.get(string)

Returns a Block object by name.

Example:
```javascript
var soundBlock = Blocks.Get("Sound Block 1");
```

# Block

An object represending an ingame terminal block

#### ApplyAction(string)

Applies/executes the given action.

Example:
```javascript
var soundBlock = Blocks.Get("Sound Block 1");
soundBlock.applyAction("PlaySound");
```


