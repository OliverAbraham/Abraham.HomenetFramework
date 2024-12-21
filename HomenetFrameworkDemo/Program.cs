using Abraham.HomenetFramework;
using Abraham.ProgramSettingsManager;
using CommandLine;
using NLog;

namespace HomenetFrameworkDemo;

/// <summary>
/// PROGRAM TITLE
/// 
///
/// EXAMPLES
/// 
/// 
/// FUNCTION
///     
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
/// This project is licensed under 
/// 
/// 
/// SOURCE CODE
/// 
/// 
/// </summary>
internal class Program
{
    private const string VERSION = "2024-12-20";

    #region ------------- Fields ------------------------------------------------------------------
    private static Framework<CommandLineArguments,Configuration,StateFile> F = new();
    #endregion



    #region ------------- Command line arguments --------------------------------------------------
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
        F.StartBackgroundJob(PeriodicJob, F.Config.IntervalInSeconds);


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
        F.SendDataobjectChangeToHomeAutomationServer("MY_DATAOBJECT", "MY_VALUE");
    }
    #endregion



    #region ------------- Health checks -----------------------------------------------------------
    private static void HealthChecks()
    {
    }
    #endregion



    #region ------------- Background worker -------------------------------------------------------
    private static void PeriodicJob()
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
