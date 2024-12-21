using Abraham.HomenetBase.Connectors;
using Abraham.HomenetBase.Models;
using Abraham.ProgramSettingsManager;
using CommandLine;
using Newtonsoft.Json;
using NLog.Web;

namespace Abraham.HomenetFramework;

public class Framework<CMDLINEARGS,SETTINGS,STATE> 
    where CMDLINEARGS:class,new() 
    where SETTINGS:class,new()
    where STATE:class,new()
{
    #region ------------- Properties --------------------------------------------------------------
    public CMDLINEARGS                      CommandLineArguments         { get; set; }
    public ProgramSettingsManager<SETTINGS> ProgramSettingsManager       { get; set; }
    public SETTINGS                         Config                       { get; set; }
    public ProgramSettingsManager<STATE>    StateFileManager             { get; set; }
    public STATE                            State                        { get; set; }
    public NLog.Logger                      Logger                       { get; set; }
    public Abraham.Scheduler.Scheduler?     Scheduler                    { get; set; }
                                                                         
    public DataObjectsConnector             HomenetClient                { get; set; }
    public Abraham.MQTTClient.MQTTClient    MqttClient                   { get; set; }
    public HomeAutomationServerConfig       HomenetConfig                { get; set; }
    public MqttBrokerConfig                 MqttBrokerConfig             { get; set; }
    #endregion



    #region ------------- Init --------------------------------------------------------------------
    public Framework()
    {
        CommandLineArguments = new CMDLINEARGS();
        Config = new SETTINGS();
        State = new STATE();
    }
    #endregion



    #region ------------- Methods -----------------------------------------------------------------
    #region ------------- Command line arguments --------------------------------------------------
    public void Init()
    {
	    ParseCommandLineArguments();
    }

    public void ParseCommandLineArguments()
	{
        CommandLineArguments = new CMDLINEARGS();

	    string[] args = Environment.GetCommandLineArgs();
	    CommandLine.Parser.Default.ParseArguments<CMDLINEARGS>(args)
	        .WithParsed   <CMDLINEARGS>(options => { CommandLineArguments = options; })
	        .WithNotParsed<CMDLINEARGS>(errors  => { Console.WriteLine(errors.ToString()); });
    
        if (CommandLineArguments is null)
            throw new Exception();
	}
    #endregion

    #region ------------- Configuration -----------------------------------------------------------
    public void ReadConfiguration(string configurationFile)
    {
        // ATTENTION: When loading fails, you probably forgot to set the properties of appsettings.hjson to "copy if newer"!
        // ATTENTION: or you have an error in your json file

	    ProgramSettingsManager = new ProgramSettingsManager<SETTINGS>()
            .UseFullPathAndFilename(configurationFile)
            .Load();

        Console.WriteLine($"Loaded configuration file '{ProgramSettingsManager.ConfigPathAndFilename}'");
        Config = ProgramSettingsManager.Data;
    }

    public void ValidateConfiguration()
    {
        // ATTENTION: When validating fails, you missed to enter a value for a property in your json file
        ProgramSettingsManager.Validate();
    }

    public void SaveConfiguration()
    {
        ProgramSettingsManager.Save(Config);
    }
    #endregion

    #region ------------- State file --------------------------------------------------------------
	public void ReadStateFile(string filename)
    {
        try
        {
	        StateFileManager = new ProgramSettingsManager<STATE>();

            if (!File.Exists(filename))
            {
                Logger.Debug($"Info: No state file exists. (filename '{filename}')");
                return;
            }

            StateFileManager
                .UseFilename(Path.GetFileName(filename))
                .UseFullPathAndFilename(filename)
                .Load();
            State = StateFileManager.Data;
            Logger.Debug($"Loaded saved state from file '{filename}'");
        }
        catch (Exception ex)
        {
            Logger.Debug($"Failed loading saved state from file '{filename}'.");
            Logger.Debug($"Reason: {ex}");
        }
    }

    public void SaveStateFile(string filename)
    {
        StateFileManager.Data = State;
        var json = JsonConvert.SerializeObject(StateFileManager.Data);
        File.WriteAllText(filename, json);
    }

    public void ReadStateFile_ForUnitTestsOnly(string savedStates)
    {
        State = JsonConvert.DeserializeObject<STATE>(savedStates);
    }

    public string SaveStateFile_ForUnitTestsOnly()
    {
        StateFileManager.Data = State;
        return JsonConvert.SerializeObject(StateFileManager.Data);
    }
    #endregion

    #region ------------- Logging -----------------------------------------------------------------
    public void InitLogger(string nlogConfigurationFile)
    {
        try
        {
            Logger = NLogBuilder.ConfigureNLog(nlogConfigurationFile).GetCurrentClassLogger();
            if (Logger is null)
                throw new Exception();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing our logger with the configuration file {nlogConfigurationFile}. More info: {ex}");
            throw;  // ATTENTION: When you come here, you probably forgot to set the properties of nlog.config to "copy if newer"!
        }
    }

    public void LogHomeAutomationConfig()
    {
        if (HomenetServerIsConfigured())
            Logger.Debug($"Home Automation target            : {HomenetConfig.Url} / {HomenetConfig.User} / ***************");
        else                                                 
            Logger.Debug($"Home Automation target            : Not configured");
                                                             
        if (MqttBrokerIsConfigured())                        
            Logger.Debug($"MQTT broker target                : {MqttBrokerConfig.Url} / {MqttBrokerConfig.User} / ***************");
        else                                                 
            Logger.Debug($"MQTT broker target                : Not configured");
    }
    #endregion

    #region ------------- Periodic actions --------------------------------------------------------
    public void StartBackgroundJob(Action periodicJob, int intervalInSeconds)
    {
        Scheduler = new Abraham.Scheduler.Scheduler()
            .UseAction(periodicJob)
            .UseFirstInterval(TimeSpan.FromSeconds(intervalInSeconds))
            .UseIntervalSeconds(intervalInSeconds)
            .Start();
    }

    public void StopBackgroundJob()
    {
        Scheduler?.Stop();
    }
    #endregion

    #region ------------- Home Automation Server communication ------------------------------------
    #region Init
    public void InitHomeAutomationServerConnection(HomeAutomationServerConfig homeAutomationServerConfig, MqttBrokerConfig mqttBrokerConfig)
    {
        HomenetConfig = homeAutomationServerConfig;

        if (HomenetServerIsConfigured())
            ConnectToHomenetServer();

        MqttBrokerConfig = mqttBrokerConfig;
        
        if (MqttBrokerIsConfigured())
            ConnectToMqttBroker();
    }
    #endregion

    #region Sending results
    public void SendDataobjectChangeToHomeAutomationServer(string dataObjectName, string value)
    {
        SendOutToHomenet(value, dataObjectName);
        SendOutToMQTT(value, dataObjectName);
    }
    #endregion

    #region Home automation server target
    public bool HomenetServerIsConfigured()
    {
        return HomenetConfig is not null && 
                !string.IsNullOrWhiteSpace(HomenetConfig.Url) && 
                !string.IsNullOrWhiteSpace(HomenetConfig.User) && 
                !string.IsNullOrWhiteSpace(HomenetConfig.Password) &&
                HomenetConfig.Timeout > 0;
    }

    public bool ConnectedToHomenetServer()
    {
        return HomenetConfig is not null;
    }

    public bool ConnectToHomenetServer()
    {
        Logger.Debug("Connecting to homenet server...");
        try
        {
            HomenetClient = new DataObjectsConnector(HomenetConfig.Url, HomenetConfig.User, HomenetConfig.Password, HomenetConfig.Timeout);
            Logger.Debug("Connect successful");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error("Error connecting to homenet server:\n" + ex.ToString());
            return false;
        }
    }

    public void SendOutToHomenet(string value, string dataObjectName)
    {
        try
        {
            if (HomenetServerIsConfigured())
            {
                Logger.Debug($"Sending out result to Home automation target");
                if (!ConnectedToHomenetServer())
                {
                    if (!ConnectToHomenetServer())
                        Logger.Error("Error connecting to homenet server.");
                }
                UpdateDataObject(value, dataObjectName);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"SendOutToHomenet: {ex}");
        }
    }

    public void UpdateDataObject(string value, string dataObjectName)
    {
        if (HomenetClient is null)
            return;

        bool success = HomenetClient.UpdateValueOnly(new DataObject() { Name = dataObjectName, Value = value});
        if (success)
            Logger.Info($"Homeset server topic {dataObjectName} updated with value {value}");
        else
            Logger.Error($"server update error! {HomenetClient.LastError}");
    }
    #endregion

    #region MQTT target
    public bool MqttBrokerIsConfigured()
    {
        return MqttBrokerConfig is not null && 
                !string.IsNullOrWhiteSpace(MqttBrokerConfig.Url) && 
                !string.IsNullOrWhiteSpace(MqttBrokerConfig.User) && 
                !string.IsNullOrWhiteSpace(MqttBrokerConfig.Password) &&
                MqttBrokerConfig.Timeout > 0;
    }

    public bool ConnectedToMqttBroker()
    {
        return MqttClient is not null;
    }

    public bool ConnectToMqttBroker()
    {
        Logger.Debug("Connecting to MQTT broker...");
        try
        {
            MqttClient = new Abraham.MQTTClient.MQTTClient()
                .UseUrl(MqttBrokerConfig.Url)
                .UseUsername(MqttBrokerConfig.User)
                .UsePassword(MqttBrokerConfig.Password)
                .UseTimeout(MqttBrokerConfig.Timeout)
                .UseLogger(delegate(string message) { Logger.Debug(message); })
                .Build();

            Logger.Debug("Created MQTT client");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error("Error connecting to MQTT broker:\n" + ex.ToString());
            return false;
        }
    }

    public void SendOutToMQTT(string value, string mqttTopic)
    {
        try
        {
            if (MqttBrokerIsConfigured())
            {
                Logger.Debug($"Sending out group result to MQTT target");
                if (!ConnectedToMqttBroker())
                {
                    Logger.Debug("Connecting to MQTT broker...");
                    if (!ConnectToMqttBroker())
                        Logger.Error("Error connecting to MQTT broker.");
                }
                UpdateTopic(value, mqttTopic);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"SendOutToMQTT: {ex}");
        }
    }

    public void UpdateTopic(string value, string topicName)
    {
        if (MqttClient is null || value is null)
            return;

        var result = MqttClient.Publish(topicName, value);
        if (result.IsSuccess)
            Logger.Info($"MQTT topic {topicName} updated with value {value}");
        else
            Logger.Error($"MQTT topic update error! {result.ReasonString}");
    }
    #endregion
    #endregion
    #endregion
}
