using System.Windows;
using Abraham.HomenetFramework;
using Abraham.ProgramSettingsManager;
using CommandLine;
using NLog;

namespace HomenetFrameworkDemoWPF;

public partial class MainWindow : Window
{
    private const string VERSION = "2024-12-20";

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
    public MainWindow()
    {
       InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        F.ParseCommandLineArguments();
        F.ReadConfiguration(F.CommandLineArguments.ConfigurationFile);
        F.ValidateConfiguration();
        F.InitLogger(F.CommandLineArguments.NlogConfigurationFile);
        F.InitHomeAutomationServerConnection(F.Config.HomeAutomationServerConfig, F.Config.MqttBrokerConfig);
        HealthChecks();
        F.ReadStateFile(F.CommandLineArguments.StateFile);
        F.StartBackgroundWorker(MyBackgroundWorker, F.Config.IntervalInSeconds);
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        F.StopBackgroundJob();
        F.SaveStateFile(F.CommandLineArguments.StateFile);
    }
    #endregion



    #region ------------- Domain logic ------------------------------------------------------------
    private void ButtonOn_Click(object sender, RoutedEventArgs e)
    {
        F.SendDataobjectChangeToHomeAutomationServer("AZ_DECKENLAMPE", "1");
    }

    private void ButtonOff_Click(object sender, RoutedEventArgs e)
    {
        F.SendDataobjectChangeToHomeAutomationServer("AZ_DECKENLAMPE", "0");
    }
    #endregion



    #region ------------- Health checks -----------------------------------------------------------
    private static void HealthChecks()
    {
    }
    #endregion



    #region ------------- Background worker -------------------------------------------------------
    private void MyBackgroundWorker()
    {
        try
        {
            Dispatcher.Invoke( () =>
            {
                MyLabel.Content = (++F.State.MyProgramState).ToString();
            });
        }
        catch (Exception ex) 
        {
            F.Logger.Error(ex);
        }
    }
    #endregion
}