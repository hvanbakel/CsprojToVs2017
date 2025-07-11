[![Build status](https://ci.appveyor.com/api/projects/status/bpo5n2yehpqrxbc4?svg=true)](https://ci.appveyor.com/project/hvanbakel/csprojtovs2017)
[![NuGet Version](https://img.shields.io/nuget/v/Project2015To2017.svg?label=Nupkg%20Version)](https://www.nuget.org/packages/Project2015To2017)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Project2015To2017.svg?label=Nupkg%20Downloads)](https://www.nuget.org/packages/Project2015To2017)
[![VS15 Global Tool NuGet Version](https://img.shields.io/nuget/v/Project2015To2017.Migrate2017.Tool.svg?label=Global%20Tool%20Version)](https://www.nuget.org/packages/Project2015To2017.Migrate2017.Tool)
[![VS15 Global Tool NuGet Downloads](https://img.shields.io/nuget/dt/Project2015To2017.Migrate2017.Tool.svg?label=Global%20Tool%20Downloads)](https://www.nuget.org/packages/Project2015To2017.Migrate2017.Tool)
[![VS16 Global Tool NuGet Version](https://img.shields.io/nuget/v/Project2015To2017.Migrate2019.Tool.svg?label=Global%20Tool%20Version)](https://www.nuget.org/packages/Project2015To2017.Migrate2019.Tool)
[![VS16 Global Tool NuGet Downloads](https://img.shields.io/nuget/dt/Project2015To2017.Migrate2019.Tool.svg?label=Global%20Tool%20Downloads)](https://www.nuget.org/packages/Project2015To2017.Migrate2019.Tool)

# Convert your old project files to the new 2017/2019 format
With the introduction of Visual Studio 2017, Microsoft added some optimizations to how a project file can be set up. However, no tooling was made available that performed this conversion as it was not necessary to do since Visual Studio 2017 would work with the old format too.

This project converts an existing csproj to the new format, shortening the project file and using all the nice new features that are part of modern Visual Studio versions.

## What does it fix?
There are a number of things [that VS2017+ handles differently](http://www.natemcmaster.com/blog/2017/03/09/vs2015-to-vs2017-upgrade/) that are performed by this tool:
1. Include files using a wildcard as opposed to specifying every single file
2. A more succinct way of defining project references
3. A more succinct way of handling NuGet package references
4. Moving some of the attributes that used to be defined in AssemblyInfo.cs into the project file
5. Defining the NuGet package definition as part of the project file

## Quick Start
Assuming you have .NET Core 2.1+ installed you can run this on the command line:
```
dotnet tool install --global Project2015To2017.Migrate2019.Tool
```

This will install the tool for you to use it anywhere you would like. You can then call the tool as shown in the examples below.

```
dotnet migrate-2019 wizard "D:\Path\To\My\TestProject.csproj"
```

Or

```
dotnet migrate-2019 wizard "D:\Path\To\My\TestProject.sln"
```

Or

```
dotnet migrate-2019 wizard .\MyProjectDirectory
```

Or even

```
dotnet migrate-2019 wizard **\*
```

This will start the interactive wizard, which will guide you through the conversion process.
You will have an option to create backups before all critical conversion stages.

**Note:** There is no need to specify paths if there is only one convertible object (project or solution) in your current working directory.
The tool will discover them automatically, or inform you in case it can't make definite (and safest) decision.

**Note:** in case you need to migrate to VS2017, not VS2019, install `Project2015To2017.Migrate2017.Tool` instead.
It will provide `dotnet migrate-2017` command with a few tiny behavioral differences to support older VS versions.

## Commands
* `wizard` will run interactive conversion wizard as shown above
* `migrate` will run non-interactive migration (useful for scripts or advanced users)
* `evaluate` will run evaluation of projects found given the path specified
* `analyze` will run analyzers to signal issues in the project files without performing actual conversion

Most likely the only command you would use is the `wizard`, since it will execute all others in a way to achieve best user experience.

## Flags
* `target-frameworks` will override the target framework on the outputted project file
* `force-transformations` allows specifying individual transforms to be run on the projects
* `force` ignores checks like project type being supported and will attempt a conversion regardless
* `keep-assembly-info` instructs the migrate logic to keep the assembly info file
* `old-output-path` will set `AppendTargetFrameworkToOutputPath` in converted project file
* `no-backup` do not create a backup folder (e.g. when your solution is under source control)

Not all flags are supported by all commands, verify help output of the command to learn which options apply to the particular command.

In case you need to specify multiple values for option, specify it multiple times:

```
dotnet migrate-2019 migrate -t net40 -t net45
```

## Use as a NuGet library from your own code

For additional control of the project migration process, you can use the NuGet packages directly from your code.

Add the `Project2015To2017.Migrate2019.Library` package to your project e.g.
```
dotnet add package Project2015To2017.Migrate2019.Library
```

Then, to apply the default project migration:

```c#
using Project2015To2017;
using Project2015To2017.Analysis;
using Project2015To2017.Migrate2017;
using Project2015To2017.Migrate2019.Library;
using Project2015To2017.Writing;

// We use Serilog, but you can use any logging provider
using Serilog;
using Serilog.Extensions.Logging;

namespace Acme.ProjectMigration
{
    class Program
    {
        static void Main(string[] args)
        {
            var logger = new LoggerConfiguration()
                        .Enrich.FromLogContext()
                        .MinimumLevel.Debug()
                        .WriteTo.Console()
                        .CreateLogger();

            var genericLogger = new SerilogLoggerProvider(logger)
                                .CreateLogger(nameof(Serilog));

            var facility = new MigrationFacility(genericLogger);

            facility.ExecuteMigrate(
                new[] { @"C:\full-path-to-solution-or-project-file.sln" },
                Vs16TransformationSet.Instance, // the default set of project file transformations

                // The rest are optional, will use sane defaults if not specified

                new ConversionOptions(), // control over things like target framework and AssemblyInfo treatment
                new ProjectWriteOptions(), // control over backup creation and custom source control logic
                new AnalysisOptions() // control over diagnostics which will be run after migration
            );
        }
    }
}
```

To provide a custom set of project transforms, provide these to the `ExecuteMigrate` function call:

```c#
var customTransforms = new BasicTransformationSet(
    // Note that these should implement ITransformationWithTargetMoment
    // in order to make sure that they run before or after
    // the majority of standard transforms.

    // You can also implement ITransformationWithDependencies to ensure
    // that your transformation always runs after some other
    // standard or user-specified transformations.

    new MyCustomPreTransform1(),
    new MyCustomPreTransform2(),
    new MyCustomPostTransform1(),
    new MyCustomPostTransform2()
);

// Mix transformations from Vs16TransformationSet and from customTransforms.
// The correct order will be resolved by the library based on
// dependency graph topological ordering within each execution moment
// (early, normal, late).
var resultTransforms = new ChainTransformationSet(
    Vs16TransformationSet.Instance,
    customTransforms
);

facility.ExecuteMigrate(
    new[] { @"C:\full-path-to-solution-or-project-file.sln" },
    resultTransforms
);
```
