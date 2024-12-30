using Abraham.HomenetFramework;
using Abraham.ProgramSettingsManager;
using CommandLine;
using NLog;

namespace HomenetFrameworkDemo;

/// <summary>
/// PROGRAM TITLE
/// Demo for Abraham.HomenetFramework nuget package.
///
/// FUNCTION
/// The nuget package contains a collection of functions that I typically need for worker applications.
///   - Command line options parser
///   - Configuration file reader
///   - State file reader/writer
///   - Scheduler for background tasks
///   - NLog logger
///   - MQTT client
///   - Client for my personal home automation server
/// 
/// AUTHOR
/// Written by Oliver Abraham, mail@oliver-abraham.de
/// 
/// 
/// INSTALLATION AND CONFIGURATION
/// See README.md in the project root folder.
/// 
/// 
/// LICENSE
/// This project is licensed under Apache license.
/// 
/// 
/// SOURCE CODE
/// The source code is hosted at: https://github.com/OliverAbraham/Abraham.HomenetFramework
/// The Nuget Package is hosted at: https://www.nuget.org/packages/Abraham.HomenetFramework
/// 
/// </summary>
internal class Program
{
    private const string VERSION = "2024-12-30";

    #region ------------- Fields ------------------------------------------------------------------
    private static Framework<CommandLineArguments,Configuration,StateFile> F = new();
    #endregion



    #region ------------- Command line arguments --------------------------------------------------
    /// <summary>
    /// Definition of all command line arguments. For detailed info how to use this, refer to 
    /// https://github.com/commandlineparser/commandline
    /// </summary>
    class CommandLineArguments
	{
	    [Option('c', "config", Default = "appsettings.json", Required = false, HelpText = 
	        """
	        Configuration file (full path and filename).
	        If you don't specify this option, the program will expect your configuration file 
	        named 'appsettings.hjson' in your program folder.
	        You can specify a different location.
	        You can use Variables for special folders, like %APPDATA%.
	        Please refer to the documentation of my nuget package https://github.com/OliverAbraham/Abraham.ProgramSettingsManager
	        """)]
	    public string ConfigurationFile { get; set; } = "";


	    [Option('n', "nlogconfig", Default = "nlog.config", Required = false, HelpText = 
	        """
	        NLOG Configuration file (full path and filename).
	        If you don't specify this option, the program will expect your configuration file 
	        named 'nlog.config' in your program folder.
	        You can specify a different location.
	        """)]
        public string NlogConfigurationFile { get; set; } = "";



	    [Option('n', "statefile", Default = "state.json", Required = false, HelpText = 
	        """
	        File that contains the current program stare (full path and filename).
	        """)]
	    public string StateFile { get; set; } = "";
	}
	#endregion



    #region ------------- Configuration file (appsettings.json) -----------------------------------
    /// <summary>
    /// Contains all properties of the appsettings.json file where you store your configuration data.
    /// When starting the application, you can set the file location with the "--config" argument.
    /// </summary>
    public class Configuration
    {
        [Optional]
        public HomeAutomationServerConfig HomeAutomationServerConfig { get; set; }
        
        [Optional]
        public MqttBrokerConfig MqttBrokerConfig { get; set; }

        public int IntervalInSeconds { get; set; }

        public void LogOptions(ILogger logger)
        {
            logger.Debug($"Background worker interval        : {IntervalInSeconds}");
        }
    }
	#endregion



    #region ------------- State file --------------------------------------------------------------
    /// <summary>
    /// Stores a set of dynamic data. Contents is read a application start and saved when ending.
    /// Add your properties here.
    /// </summary>
    public class StateFile
    {
        public int MyProgramState { get; set; }
    }
	#endregion



    #region ------------- Init --------------------------------------------------------------------
    public static void Main(string[] args)
    {
        F.ParseCommandLineArguments();
        F.ReadConfiguration(F.CommandLineArguments.ConfigurationFile);
        F.ValidateConfiguration();
        F.InitLogger(F.CommandLineArguments.NlogConfigurationFile);
        F.InitHomeAutomationServerConnection(F.Config.HomeAutomationServerConfig, F.Config.MqttBrokerConfig);
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
    #endregion



    #region ------------- Domain logic ------------------------------------------------------------
    private static void DomainLogic()
    {
        F.Logger.Debug("DomainLogic Demo. Press any key to send a data object change to home automation server.");
        Console.ReadKey(true);
        F.SendDataobjectChangeToHomeAutomationServer("MY_DATAOBJECT", F.State.MyProgramState.ToString());
    }
    #endregion



    #region ------------- Health checks -----------------------------------------------------------
    private static void HealthChecks()
    {
    }
    #endregion



    #region ------------- Background worker -------------------------------------------------------
    private static void MyBackgroundWorker()
    {
        try
        {
            F.Logger.Debug($"PeriodicJob {++F.State.MyProgramState}");
        }
        catch (Exception ex) 
        {
            F.Logger.Error(ex);
        }
    }
    #endregion



    #region ------------- Logging -----------------------------------------------------------------
    private static void PrintGreeting()
    {
        // To generate text like this, use https://onlineasciitools.com/convert-text-to-ascii-art
        F.Logger.Debug("");
        F.Logger.Debug("");
        F.Logger.Debug("");
        F.Logger.Debug(@"-------------------------------------------------------------------------------------------");
        F.Logger.Debug(@"    ______                                           _       ______                        ");
        F.Logger.Debug(@"    |  ___|                                         | |      |  _  \                       ");
        F.Logger.Debug(@"    | |_ _ __ __ _ _ __ ___   _____      _____  _ __| | __   | | | |___ _ __ ___   ___     ");
        F.Logger.Debug(@"    |  _| '__/ _` | '_ ` _ \ / _ \ \ /\ / / _ \| '__| |/ /   | | | / _ \ '_ ` _ \ / _ \    ");
        F.Logger.Debug(@"    | | | | | (_| | | | | | |  __/\ V  V / (_) | |  |   <    | |/ /  __/ | | | | | (_) |   ");
        F.Logger.Debug(@"    \_| |_|  \__,_|_| |_| |_|\___| \_/\_/ \___/|_|  |_|\_\   |___/ \___|_| |_| |_|\___/    ");
        F.Logger.Debug(@"                                                                                           ");
        F.Logger.Info ($"                   Program started, Version {VERSION}                                      ");
        F.Logger.Debug(@"-------------------------------------------------------------------------------------------");
        F.Logger.Debug($"");
        F.Logger.Debug($"Configuration loaded from file    : {F.ProgramSettingsManager.ConfigPathAndFilename}");
        F.Config.LogOptions(F.Logger);
        F.LogHomeAutomationConfig();
        F.Logger.Debug($"");
    }
    #endregion
}
