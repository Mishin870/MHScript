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
[Read this wiki page.](https://github.com/Mishin870/MHScript/wiki/How-to-start)

#### Serialization
You can serialize compiled script to increase loading speed (run it without syntax processing)
See the [wiki page for it.](https://github.com/Mishin870/MHScript/wiki/Serialization)

#### Documentation
All functions (include your own) in MHS contains their descriptions and full signatures.
Engine can generate documentation for export or autocompleting in some code editor.
[how to use it.](https://github.com/Mishin870/MHScript/wiki/Documentation)

#### Tasks
- [x] Parse lexems in script
- [x] Combine them to complex objects (compile)
- [x] Local script functions
- [x] Local function variables and callstack
- [x] Serialize script objects for future c++ client-side interpreter
- [x] Restructurize engine
- [x] Documentation generator
- [ ] Improve serialization (for example, replace string in function calls by numeric id of functions)
- [ ] Documentation for objects and object.functions()
- [ ] Code editor
- [ ] C++ client-side interpreter
- [ ] Implement objects properties