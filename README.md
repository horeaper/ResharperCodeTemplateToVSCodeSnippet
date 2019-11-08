# Resharper Code Template to Visual Studio Code Snippet

Convert Resharper's code template to Visual Studio Code's C# snippet (Not Visual Studio's Code Snippet!!)

Tested with Resharper 2019.2.3. Requires .NET Framework or Mono to run.

## How To Use

1. Click Resharper->Tools->Templates Explorer
2. In "Scope", select C#
3. Ctrl+A to select all code templates
4. Click "Export..." in the toolbar
5. Save the DotSettings file
6. Drag that file onto T2S.exe, or use command line: `.\T2S.exe YourFileName.DotSettings`
7. Copy the generated `csharp.json` file to `%APPDATA%\Code\User\snippets\csharp.json`
8. Done!
