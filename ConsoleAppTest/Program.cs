using Abraham.MQTTClient;

namespace ConsoleAppTest
{
    internal class Program
    {       
        private static string _serverUrl      = "<YOUR MQTT BROKER URL WITHOUT PROTOCOL WITHOUT PORT>";
        private static string _serverUser = "<YOUR MQTT BROKER USERNAME>";
        private static string _serverPassword = "<YOUR MQTT BROKER PASSWORD>";

        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
           Connect();
        }

        public static void Connect()
        {
            try
            {
                #if DEBUG
                _serverUrl      = File.ReadAllText(@"C:\Credentials\mosquitto_url.txt");
                _serverUser = File.ReadAllText(@"C:\Credentials\mosquitto_username.txt");
                _serverPassword = File.ReadAllText(@"C:\Credentials\mosquitto_password.txt");
                #endif

                var _mqttClient = new MQTTClient()
                    .UseUrl(_serverUrl)
                    .UseUsername(_serverUser)
                    .UsePassword(_serverPassword)
                    .UseTimeout(10)
                    .Build();

                //_mqttClient.SubscribeToAllTopics();

                var task = _mqttClient.SubscribeAsync("garden/temperature", delegate (string topic, string value)
                {
                });

                var result = task.GetAwaiter().GetResult();
                //_mqttClient.OnEvent = OnDataobjectChangeLocal;
            }
            catch (Exception ex)
            {
            }
        }
    }
}
