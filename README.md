# MHScript
JS-like self-documented serializable scripting engine in C#

#### Requirements
* .net framework 3.5+

#### Hello world
```javascript
//If you define a global "alert" function, that shows MessageBox
alert("Hello, world!");
```

#### How to start
[Read this wiki page](https://github.com/Mishin870/MHScript/wiki/How-to-start)

#### Serialization
You can serialize compiled script to increase loading speed (run it without syntax processing)
See the [wiki page for it](https://github.com/Mishin870/MHScript/wiki/Serialization)

#### Documentation
All functions (include your own) in MHS contains their descriptions and full signatures.
In future this will be used in documentation generator and in autocompleting in code editor.

#### Tasks
- [x] Parse lexems in script
- [x] Combine them to complex objects (compile)
- [x] Make local script functions
- [x] Make local function variables and callstack
- [x] Serialize script objects for future c++ client-side interpreter
- [x] Restructurize engine
- [ ] Documentation generator
- [ ] Code editor
- [ ] C++ client-side interpreter
- [ ] Implement objects properties