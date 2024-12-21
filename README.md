# Abraham.HomenetFramework

![](https://img.shields.io/github/downloads/oliverabraham/Abraham.HomenetFramework/total) ![](https://img.shields.io/github/license/oliverabraham/Abraham.HomenetFramework) ![](https://img.shields.io/github/languages/count/oliverabraham/Abraham.HomenetFramework) ![GitHub Repo stars](https://img.shields.io/github/stars/oliverabraham/Abraham.HomenetFramework?label=repo%20stars) ![GitHub Repo stars](https://img.shields.io/github/stars/oliverabraham?label=user%20stars)



## OVERVIEW

Contains a collection of functions that I typically need for worker applications.
- Command line options parser
- Configuration file reader
- State file reader/writer
- Scheduler for background tasks
- NLog logger
- MQTT client
- Client for my personal home automation server


## CREDITS


## LICENSE

Licensed under Apache licence.
https://www.apache.org/licenses/LICENSE-2.0


## Compatibility

The nuget package was build with DotNET 6.



## INSTALLATION

Install the Nuget package "Abraham.HomenetFramework" into your application (from https://www.nuget.org).
Take my demo project as a template for your application.

The following code should only give an idea how to use it.
```C#
using Abraham.HomenetFramework;

public static void Main(string[] args)
{
    F.ParseCommandLineArguments();
    F.ReadConfiguration(F.CommandLineArguments.ConfigurationFile);
    F.ValidateConfiguration();
    F.InitLogger(F.CommandLineArguments.NlogConfigurationFile);
    PrintGreeting();
    HealthChecks();
    F.ReadStateFile(F.CommandLineArguments.StateFile);
    F.StartBackgroundWorker(MyBackgroundWorker, F.Config.IntervalInSeconds);


    DomainLogic();

        
    F.Logger.Debug($"Press any key to end the application.");
    Console.ReadKey();
    F.StopBackgroundJob();
    F.SaveStateFile(F.CommandLineArguments.StateFile);
}
```

## DEMO APPLICATION
My demo will
- read options from command line
- read a state file containing a counter fale and save the current value when the app ends
- log output to a log file which is rotated every monday at midnight. Watch the log file FrameworkDemo.log in your bin folder.
  Logging is widely configurable, see NLog documentation for details
- install a background job that increments the counter by one every second (just as a demo)
- watch the counter continue when you start the app the second time.

- 

## HOW TO INSTALL A NUGET PACKAGE
This is very simple:
- Start Visual Studio (with NuGet installed) 
- Right-click on your project's References and choose "Manage NuGet Packages..."
- Choose Online category from the left
- Enter the name of the nuget package to the top right search and hit enter
- Choose your package from search results and hit install
- Done!


or from NuGet Command-Line:

    Install-Package Abraham.HomenetFramework


## AUTHOR

Oliver Abraham, mail@oliver-abraham.de, https://www.oliver-abraham.de

Please feel free to comment and suggest improvements!



## SOURCE CODE

The source code is hosted at:

https://github.com/OliverAbraham/Abraham.HomenetFramework

The Nuget Package is hosted at: 

https://www.nuget.org/packages/Abraham.HomenetFramework



## SCREENSHOTS

# MAKE A DONATION !

If you find this application useful, buy me a coffee!
I would appreciate a small donation on https://www.buymeacoffee.com/oliverabraham

<a href="https://www.buymeacoffee.com/app/oliverabraham" target="_blank"><img src="https://cdn.buymeacoffee.com/buttons/v2/default-yellow.png" alt="Buy Me A Coffee" style="height: 60px !important;width: 217px !important;" ></a>
