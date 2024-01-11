# Project Title

Fully managed injector that inject managed assemblies to managed applications.<br />

## Usage

 SharpAssemblyInjector.Console {ProcessName} {PathToAssembly} {PathToRuntimeConfig} {ClassPath} {EntryMethod}<br />
 If you need to inject more than 1 assembly, keep specifying the last 4 parameters for all assemblies to be injected.<br />

 Example from sample:<br />
 - SharpAssemblyInjector.Console "TestApp" "Patcher\TestAppPatcher.dll" "Patcher\TestAppPatcher.runtimeconfig.json" "TestAppPatcher.Main, TestAppPatcher" "Init"
   
 Injects TestAppPatcher.dll to TestApp.exe and run TestAppPatcher.Main.Init method.
## Sample
 Check sample in releases.<br />
![alt text](SampleImages/1.png)


## License

[MIT](https://choosealicense.com/licenses/mit/)
