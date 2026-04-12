using System;
using System.IO.Ports;
using System.Diagnostics;

namespace Eco_Matic.Data
{
    public class ArduinoService
    {
        private SerialPort _serialPort;
        public event EventHandler<string>? OnCardScanned;

        public ArduinoService(string portName = "COM5", int baudRate = 9600)
        {
            _serialPort = new SerialPort(portName, baudRate);
            _serialPort.DataReceived += SerialPort_DataReceived;
        }

        public void Start()
        {
            try
            {
                if (!_serialPort.IsOpen)
                    _serialPort.Open();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Arduino not connected or port busy: " + ex.Message);
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
                if (data.StartsWith("RFID:"))
                {
                    string rfid = data.Substring(5);
                    OnCardScanned?.Invoke(this, rfid);
                }
            }
            catch { }
        }

        public void SendResponse(bool isValid)
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.WriteLine(isValid ? "VALID" : "INVALID");
            }
        }

        public void SendStateCommand(string state)
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.WriteLine(state);
            }
        }
    }
}
