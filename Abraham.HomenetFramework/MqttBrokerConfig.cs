namespace Abraham.HomenetFramework;

public class MqttBrokerConfig
{
    public string Url                { get; set; }
    public string User               { get; set; }
    public string Password           { get; set; }
    public int    Timeout            { get; set; }
}