using Abraham.MQTTClient;
using System.Data;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static string _serverUrl      = "<YOUR MQTT BROKER URL WITHOUT PROTOCOL WITHOUT PORT>";
        private static string _serverUser = "<YOUR MQTT BROKER USERNAME>";
        private static string _serverPassword = "<YOUR MQTT BROKER PASSWORD>";


        public MainWindow()
        {
            InitializeComponent();
            Dispatcher.BeginInvoke( async () => Connect() );
        }

        public async Task Connect()
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

                var result = await _mqttClient.SubscribeAsync("garden/temperature", 
                    delegate (string topic, string value)
                {
                });
                //_mqttClient.OnEvent = OnDataobjectChangeLocal;
            }
            catch (Exception ex)
            {
            }
        }

    }
}