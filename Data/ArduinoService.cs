using System;
using System.IO.Ports;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Eco_Matic.Data
{
    public class ArduinoService
    {
        private const string PortSettingKey = "ECOMATIC_ARDUINO_PORT";
        private const string BaudSettingKey = "ECOMATIC_ARDUINO_BAUD";
        private const string DefaultPortName = "COM5";
        private const int DefaultBaudRate = 9600;

        private readonly object _writeLock = new();
        private SerialPort _serialPort;
        private int _sessionCommandVersion;
        public event EventHandler<string>? OnCardScanned;
        public string PortName => _serialPort.PortName;
        public int BaudRate => _serialPort.BaudRate;
        public bool IsOpen => _serialPort?.IsOpen == true;

        public ArduinoService(string portName = DefaultPortName, int baudRate = DefaultBaudRate)
        {
            _serialPort = new SerialPort(portName, baudRate)
            {
                NewLine = "\n",
                Encoding = Encoding.ASCII,
                ReadTimeout = 1000,
                WriteTimeout = 1000
            };
            _serialPort.DataReceived += SerialPort_DataReceived;
        }

        public static ArduinoService FromEnvironment()
        {
            string portName = AppEnvironment.GetOptional(PortSettingKey) ?? DefaultPortName;
            int baudRate = DefaultBaudRate;

            string? configuredBaud = AppEnvironment.GetOptional(BaudSettingKey);
            if (!string.IsNullOrWhiteSpace(configuredBaud) &&
                (!int.TryParse(configuredBaud, out baudRate) || baudRate <= 0))
            {
                baudRate = DefaultBaudRate;
                Debug.WriteLine($"{BaudSettingKey} is invalid. Falling back to {DefaultBaudRate}.");
            }

            return new ArduinoService(portName, baudRate);
        }

        public bool Start()
        {
            try
            {
                if (!_serialPort.IsOpen)
                {
                    _serialPort.Open();
                }

                return _serialPort.IsOpen;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Arduino not connected or port busy: " + ex.Message);
                return false;
            }
        }

        public void Stop()
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.Close();
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string data = _serialPort.ReadLine().Trim();
                if (data.StartsWith("RFID:", StringComparison.Ordinal) &&
                    data.Length > 5 &&
                    IsLikelyRfidUid(data[5..]))
                {
                    string rfid = data.Substring(5);
                    Debug.WriteLine("RFID scanned: " + rfid);
                    OnCardScanned?.Invoke(this, rfid);
                }
            }
            catch (TimeoutException) { }
            catch (Exception ex)
            {
                Debug.WriteLine("Arduino serial read failed: " + ex.Message);
            }
        }

        private static bool IsLikelyRfidUid(string value)
        {
            if (value.Length < 4 || value.Length > 20)
            {
                return false;
            }

            foreach (char c in value)
            {
                bool isHex =
                    (c >= '0' && c <= '9') ||
                    (c >= 'A' && c <= 'F') ||
                    (c >= 'a' && c <= 'f');
                if (!isHex)
                {
                    return false;
                }
            }

            return true;
        }

        public void SendResponse(bool isValid)
        {
            WriteLine(isValid ? "VALID" : "INVALID");
        }

        public void SendStateCommand(string state)
        {
            WriteLine(state);
        }

        public void SendCustomerSessionActive()
        {
            int version = Interlocked.Increment(ref _sessionCommandVersion);
            WriteLine("STATE:ACTIVE");
            WriteLine("MSG:CUSTOMER MODE READY");

            // Arduino boards often reset when the serial port opens. These short
            // repeats make customer mode reliable without blocking the WPF UI.
            _ = Task.Run(() =>
            {
                Thread.Sleep(350);
                if (Volatile.Read(ref _sessionCommandVersion) != version)
                {
                    return;
                }

                WriteLine("STATE:ACTIVE");
                Thread.Sleep(350);
                if (Volatile.Read(ref _sessionCommandVersion) != version)
                {
                    return;
                }

                WriteLine("MSG:CUSTOMER MODE READY");
            });
        }

        public void SendCustomerSessionAfk()
        {
            Interlocked.Increment(ref _sessionCommandVersion);
            WriteLine("MSG:ECO-MATIC IDLE");
            WriteLine("STATE:AFK");
        }

        public void SendMessage(string message)
        {
            WriteLine("MSG:" + CleanMessage(message));
        }

        private void WriteLine(string message)
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                lock (_writeLock)
                {
                    _serialPort.WriteLine(message);
                }
            }
        }

        private static string CleanMessage(string message)
        {
            string compact = (message ?? string.Empty)
                .Replace("\r", " ", StringComparison.Ordinal)
                .Replace("\n", " ", StringComparison.Ordinal)
                .Trim();

            return compact.Length <= 32 ? compact : compact[..32];
        }
    }
}
