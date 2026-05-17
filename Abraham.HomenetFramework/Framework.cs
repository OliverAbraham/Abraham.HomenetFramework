using Abraham.ProgramSettingsManager;
using CommandLine;
using MQTTnet.Client;
using Newtonsoft.Json;
using NLog.Web;
using System.Threading;

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
    public Abraham.MQTTClient.MQTTClient    MqttClient                   { get; set; }
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
        if (MqttBrokerIsConfigured())                        
            Logger.Debug($"MQTT broker target                : {MqttBrokerConfig.Url} / {MqttBrokerConfig.User} / ***************");
        else                                                 
            Logger.Debug($"MQTT broker target                : Not configured");
    }
    #endregion



    #region ------------- Periodic actions --------------------------------------------------------
    public void StartBackgroundWorker(Action periodicJob, int intervalInSeconds, int firstIntervalInSeconds = 1)
    {
        Scheduler = new Abraham.Scheduler.Scheduler()
            .UseAction(periodicJob)
            .UseFirstInterval(TimeSpan.FromSeconds(firstIntervalInSeconds))
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
    public void InitHomeAutomationServerConnection(MqttBrokerConfig mqttBrokerConfig)
    {
        MqttBrokerConfig = mqttBrokerConfig;
        
        if (MqttBrokerIsConfigured())
            ConnectToMqttBroker();
    }
    #endregion

    #region Sending results
    public void SendDataobjectChangeToHomeAutomationServer(string dataObjectName, string value, bool retain, bool wholeDto = false, DateTimeOffset? timestamp = null)
    {
        SendOutToMQTT(value, dataObjectName, retain);

        if (wholeDto && timestamp is not null)
        {
            // we're sending a json structure with the value together with the timestamp,
		    // so that we can easily detect in Home Assistant when the value was updated for the last time.
            var dto = new MqttEntity(value.ToString(), timestamp?.ToString("o"));
		    var json = JsonConvert.SerializeObject(dto, Formatting.None);
            SendOutToMQTT(json, dataObjectName, retain);
        }
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

            MqttClient.Connect();
            Logger.Debug($"Client is connected.");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error("Error connecting to MQTT broker:\n" + ex.ToString());
            return false;
        }
    }

    public void SendOutToMQTT(string value, string mqttTopic, bool retain)
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
                UpdateTopic(value, mqttTopic, retain);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"SendOutToMQTT: {ex}");
        }
    }

    public void UpdateTopic(string value, string topicName, bool retain)
    {
        if (MqttClient is null || value is null)
            return;

        var result = MqttClient.Publish(topicName, value, useOpenConnection: true, cancellationToken: default, retain: retain);
        if (result.IsSuccess)
            Logger.Debug($"MQTT topic {topicName} updated with value {value}");
        else
            Logger.Error($"MQTT topic update error! {result.ReasonString}");
    }
    #endregion
    #endregion
}
