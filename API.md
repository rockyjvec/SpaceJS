# API Globals

## Console

Writes text to the terminal console.

Example:
```javascript
console.log("Hello World");
```

## Event

Manages events.


### Actions

When you create an action listener, it will automatically get added to the list of possible actions in button panels and cockpits when you setup actions.

Example:
```javascript
function doSomething(action)
{
  console.log("Action: " + action + " triggered.");
}

Event.onAction("ActionName", doSomething);
```

#### Event.onAction(string, function)

Create an action listener.

#### Event.offAction(string, function)

Remove an action listener.

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


