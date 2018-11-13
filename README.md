[![Build status](https://ci.appveyor.com/api/projects/status/bpo5n2yehpqrxbc4?svg=true)](https://ci.appveyor.com/project/hvanbakel/csprojtovs2017)
[![NuGet Version](https://img.shields.io/nuget/v/Project2015To2017.svg?label=Nupkg%20Version)](https://www.nuget.org/packages/Project2015To2017)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Project2015To2017.svg?label=Nupkg%20Downloads)](https://www.nuget.org/packages/Project2015To2017)
[![Global Tool NuGet Version](https://img.shields.io/nuget/v/Project2015To2017.Migrate2017.Tool.svg?label=Global%20Tool%20Version)](https://www.nuget.org/packages/Project2015To2017.Migrate2017.Tool)
[![Global Tool NuGet Downloads](https://img.shields.io/nuget/dt/Project2015To2017.Migrate2017.Tool.svg?label=Global%20Tool%20Downloads)](https://www.nuget.org/packages/Project2015To2017.Migrate2017.Tool)

# Convert your old project files to the new 2017 format
With the introduction of Visual Studio 2017, Microsoft added some optimizations to how a project file can be set up. However, no tooling was made available that performed this conversion as it was not necessary to do since Visual Studio 2017 would work with the old format too.

This project converts an existing csproj to the new format, shortening the project file and using all the nice new features that are part of Visual Studio 2017.

## What does it fix?
There are a number of things [that VS2017 handles differently](http://www.natemcmaster.com/blog/2017/03/09/vs2015-to-vs2017-upgrade/) that are performed by this tool: 
1. Include files using a wildcard as opposed to specifying every single file 
2. A more succinct way of defining project references 
3. A more succinct way of handling NuGet package references
4. Moving some of the attributes that used to be defined in AssemblyInfo.cs into the project file
5. Defining the NuGet package definition as part of the project file

## How it works
### As a Net Core Global Tool
Assuming you have net core 2.1 installed you can run this on the command line:
`dotnet tool install Project2015To2017.Migrate2017.Tool --global`
This will install the tool for you to use it anywhere you would like. You can then call the tool as shown in the examples below.

### As a normal file download
Using the tool is simple, it is a simple command line utility that has a single argument being the project file, solution file or folder you would like to convert.
When you give it a directory path, the tool will discover all csproj files nested in it.

### Examples
Below examples are for the global tool, for the normal file just replace `dotnet migrate-2017 migrate` with your executable.

`dotnet migrate-2017 migrate "D:\Path\To\My\TestProject.csproj"`

Or

`dotnet migrate-2017 migrate "D:\Path\To\My\TestProject.sln"`

Or

`dotnet migrate-2017 migrate "D:\Path\To\My\Directory"`

After confirming this is an old style project file, it will start performing the conversion. When it has gathered all the data it needs it first creates a backup of the old files and puts them into a backup folder and then generates a new project file in the new format.

## Commands
As a sub command `migrate` being shown above there are 2 more options:
* `evaluate` will run evaluation of projects found given the path specified
* `analyze` will run analyzers to signal issues in the project files without converting

## Flags
* `target-frameworks` will set the target framework on the outputted project file
* `force-transformations` allows specifying individual transforms to be run on the projects
* `force` ignores checks like project type being supported and will attempt a conversion regardless
* `keep-assembly-info` instructs the migrate logic to keep the assembly info file 
* `old-output-path` will set `AppendTargetFrameworkToOutputPath` in converted project file
* `no-backup` do not create a backup folder (e.g. when your solution is under source control)
