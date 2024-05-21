# Universal Save System
A persistant data storage system using JSON to store and retrieve data from a file easily and intuitively.

Supports individuals and arrays of types `string`, `int`, `float`, and `bool`.

Can store **global data**, which can be accessed by any mod using the correct key, or **private data**, which is further keyed to the name of the mod calling the functions (or, more specifically, the executing assembly).

## Usage
To use the Universal Save System, simply reference the .dll as a dependency and make sure to include it in BepInEx/plugins.

Use `ULTRAKIT.UniversalSaveSystem.SaveData.SetPersistent(string key, object value, bool global)` to save data.

Use `ULTRAKIT.UniversalSaveSystem.SaveData.TryGetPersistent<T>(string key, bool global, out object value)` to retrieve data. Make sure to cast the out value to the correct type!

Alternatively, use `ULTRAKIT.UniversalSaveSystem.SaveData.GetPersistent<T>(string key, bool global)` to retrieve and return data directly, but throws an exception if the data is missing.

### Example
```
using ULTRAKIT.UniversalSaveSystem;

string firstString = "This will be stored in a json file.";
SaveData.SetPersistent("example_data", firstString, true);

SaveData.TryGetPersistent<string>("example_data", true, out object value);
string secondString = (string)value; /* OR */ string secondString = value as string;

// Alternatively

string thirdString = SaveData.GetPersistent<string>("example_data", true);
```

### Closing Notes
SaveData's functions are equipped with intellisense documentation, so just hover over the function in your IDE to see all about it!
