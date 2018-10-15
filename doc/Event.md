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
