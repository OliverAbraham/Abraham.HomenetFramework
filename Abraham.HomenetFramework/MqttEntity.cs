namespace Abraham.HomenetFramework
{
    public class MqttEntity
    {
		public string value;
		public string timestamp;

		public MqttEntity(string value, string timestamp)
		{
			this.value = value;
			this.timestamp = timestamp;
		}
    }
}
