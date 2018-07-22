# MHScript
JS-like self-documented serializable scripting engine in C#

#### Requirements
* .net framework 3.5+

#### Hello world
```C#
//Create scripting engine instance and add our custom global function to it
Engine engine = new Engine(new Engine.WarningFunction(warning));
engine.addGlobalFunction("test", new GlobalFunction() {
	function = new GlobalFunction.UniversalFunction(test),
	functionDocsName = "void test(string message)",
	functionDocsDescription = "Выводит MessageBox на экран"
});

//Parse, compile and run example script from a string
Script script = engine.parseScript("test('Hello, world!');");
script.run(engine);

private object test(Engine engine, params object[] args) {
	if (args.Length >= 1) {
		MessageBox.Show(args[0].ToString());
	} else {
		MessageBox.Show("Неверное количество аргументов в test(..)!");
	}
	return null;
}

//Function, used to show warnings (log to file, console, messagebox, ...)
private void warning(string message) {
	MessageBox.Show(message, "Warning!");
}
```

#### Serialization
```C#
using (FileStream stream = new FileStream("test.script", FileMode.Create, FileAccess.Write)) {
	script.serialize(stream);
}
```

#### DeSerialization
```C#
using (FileStream stream = new FileStream("test.script", FileMode.Open, FileAccess.Read)) {
	Script script = engine.loadScript(stream);
	script.execute(engine);
}
```

#### Documentation
All functions (include your own) in MHS contains their descriptions and full signatures.
In future this will be used in documentation generator and in autocompleting in code editor.

#### Tasks
- [x] Parse lexems in script
- [x] Combine them to complex objects (compile)
- [x] Make local script functions
- [x] Make local function variables and callstack
- [ ] Restructurize engine
- [ ] "Compile" script objects into pseudo-bytecode for future c++ client-side interpreter
- [ ] Implement objects properties