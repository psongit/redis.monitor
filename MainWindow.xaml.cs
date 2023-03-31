using StackExchange.Redis;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
namespace redis.monitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly string _stopMonitoringvalue = "Stop Monitoring";
        ConnectionMultiplexer _connectionMultiplexer;
        readonly Timer _triggerTimer;

        public MainWindow()
        {
            InitializeComponent();
            _triggerTimer = new Timer(5000) { Enabled = true };
        }
        private async void btnRedisClear_Click(object sender, RoutedEventArgs e)
        {
            txtConnectionLog.Clear();
        }

        private async void btnRedisConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (btnRedisConnect.Content.Equals(_stopMonitoringvalue))
                {
                    await _connectionMultiplexer.CloseAsync();
                    _connectionMultiplexer.Dispose();
                    _triggerTimer.Stop();
                    _triggerTimer.Elapsed -= TriggerTimer_Elapsed;
                    await UpdateUI(() => btnRedisConnect.Content = "Start Monitoring");
                    return;
                }

                await UpdateUI(() => btnRedisConnect.Content = "Connection.......");
                _connectionMultiplexer = ConnectionMultiplexer.Connect(txtRedisConnectionString.Text);


                _connectionMultiplexer.ConnectionFailed += ConnectionMultiplexer_ConnectionFailed;
                _connectionMultiplexer.ConnectionRestored += ConnectionMultiplexer_ConnectionRestored;
                _connectionMultiplexer.ConfigurationChanged += ConnectionMultiplexer_ConfigurationChanged;
                _connectionMultiplexer.ConfigurationChangedBroadcast += ConnectionMultiplexer_ConfigurationChangedBroadcast;
                _connectionMultiplexer.ErrorMessage += ConnectionMultiplexer_ErrorMessage;
                _connectionMultiplexer.HashSlotMoved += ConnectionMultiplexer_HashSlotMoved;
                _connectionMultiplexer.InternalError += ConnectionMultiplexer_InternalError;
                _connectionMultiplexer.ServerMaintenanceEvent += ConnectionMultiplexer_ServerMaintenanceEvent;

                Log("Connected");
                _triggerTimer.Elapsed += TriggerTimer_Elapsed;
                _triggerTimer.Start();
                await UpdateUI(() => btnRedisConnect.Content = _stopMonitoringvalue);
            }
            catch (Exception ex)
            {
                Log($"Connection Error : {ex.Message} occurred  while connecting with the connection string '{txtRedisConnectionString.Text}'");
            }
        }

        private async void TriggerTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            try
            {
                var database = _connectionMultiplexer.GetDatabase();
                var redisKey = new RedisKey("Ascertra-Key");
                database.StringSet(redisKey, new RedisValue("Ascertra-Value"));
            }
            catch(Exception ex)
            {
                Log(ex.ToString());
            }
        }

        private void ConnectionMultiplexer_HashSlotMoved(object? sender, HashSlotMovedEventArgs e)
        {
            Log($"Hash Slot {e.HashSlot} has moved from {e.OldEndPoint} to {e.NewEndPoint}");
        }

        private void ConnectionMultiplexer_ErrorMessage(object? sender, RedisErrorEventArgs e)
        {
            Log($"Error Occurred :  {e.Message} for the endpoint {e.EndPoint}");
        }

        private void ConnectionMultiplexer_ConfigurationChangedBroadcast(object? sender, EndPointEventArgs e)
        {
            Log($"Configuration changed Broadcast for the endpoint {e.EndPoint}");
        }

        private void ConnectionMultiplexer_ConfigurationChanged(object? sender, EndPointEventArgs e)
        {
            Log($"Configuration changed for the endpoint {e.EndPoint}");
        }

        private void ConnectionMultiplexer_ConnectionRestored(object? sender, ConnectionFailedEventArgs e)
        {
            Log($"Connection Restored for the endpoint {e.EndPoint} and Connection Type : {e.ConnectionType} with Failure Type : {e.FailureType} and exception: {e.Exception}");
        }

        private void ConnectionMultiplexer_ConnectionFailed(object? sender, ConnectionFailedEventArgs e)
        {
            Log($"Connection Failed for the endpoint {e.EndPoint} and Connection Type : {e.ConnectionType} with Failure Type : {e.FailureType} and exception: {e.Exception}");
        }

        private void ConnectionMultiplexer_ServerMaintenanceEvent(object? sender, StackExchange.Redis.Maintenance.ServerMaintenanceEvent e)
        {
            Log($"Server Maintenance Event received {e.ReceivedTimeUtc} with Start Time: {e.StartTimeUtc} and message is : {e.RawMessage}");
        }

        private void ConnectionMultiplexer_InternalError(object? sender, InternalErrorEventArgs e)
        {
            Log($"Internal Error occurred in endpoint {e.EndPoint} connection type : {e.ConnectionType} origin : {e.Origin} with exception {e.Exception}");
        }

        private void Log(string value)
        {
            UpdateUI(() =>
            {
                txtConnectionLog.Text = txtConnectionLog.Text.Insert(0, $"Time : {DateTime.Now} Message : {value} {Environment.NewLine}");
            });
        }

        private async Task<Task> UpdateUI(Action action)
        {
            Task t = Task.Run(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    action();
                });
            });

            return t;
        }
    }
}
