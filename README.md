# MHScript
JS-like scripting engine in C#

#### Requirements
* .net framework 3.5+

#### Hello world
```C#
//Create scripting engine instance and add our custom global function to it
Engine engine = new Engine();
engine.addGlobalFunction("test", new GlobalFunction() {
	function = new GlobalFunction.UniversalFunction(test),
	functionDocsName = "void test(string message)",
	functionDocsDescription = "Выводит MessageBox на экран"
});

//Parse, compile and run example script from a string. "<?mh" will be removed in future commits
Script script = engine.parseScript("<?mh test('Hello, world!');");
script.run(engine);

private object test(StringWriter output, Engine engine, params object[] args) {
	if (args.Length >= 1) {
		MessageBox.Show(args[0].ToString());
	} else {
		MessageBox.Show("Неверное количество аргументов в test(..)!");
	}
	return null;
}
```

#### Tasks
- [x] Parse lexems in script
- [x] Combine them to complex objects (compile)
- [x] Make local script functions
- [x] Make local function variables and callstack
- [ ] Restructurize engine
- [ ] "Compile" script objects into pseudo-bytecode for future c++ client-side interpreter
- [ ] Implement objects properties