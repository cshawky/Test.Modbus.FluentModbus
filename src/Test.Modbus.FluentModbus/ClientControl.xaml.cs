using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using FluentModbus;

namespace ModbusFluentTest
{
    /// <summary>
    /// Hosts a Modbus TCP client. Reads the sample holding register at 40001
    /// (protocol address 0) and writes to the sample holding register at 41001
    /// (protocol address 1000) on the connected server.
    /// </summary>
    public partial class ClientControl : UserControl
    {
        private static readonly ushort ReadRegisterAddress = ModbusAddress.ToProtocolAddress(40001);
        private static readonly ushort WriteRegisterAddress = ModbusAddress.ToProtocolAddress(41001);

        private ModbusTcpClient _client;
        private byte _unitId = 0;

        public ClientControl()
        {
            InitializeComponent();
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (_client != null)
            {
                LogWarning("Client already connected.");
                return;
            }

            if (!int.TryParse(PortTextBox.Text, out var port))
            {
                LogError("Invalid port.");
                return;
            }

            if (!byte.TryParse(UnitIdTextBox.Text, out _unitId))
            {
                LogError("Invalid unit ID.");
                return;
            }

            try
            {
                var address = IPAddress.Parse(HostTextBox.Text);
                _client = new ModbusTcpClient
                {
                    // Defaults are Infinite — an unanswered request (e.g. a unit ID
                    // mismatch with the server) would otherwise hang this call forever
                    // and freeze the UI thread that invoked it.
                    ReadTimeout = 3000,
                    WriteTimeout = 3000
                };
                _client.Connect(new IPEndPoint(address, port));
                LogMessage($"Connected to {HostTextBox.Text}:{port} (unit ID {_unitId}).");
            }
            catch (Exception ex)
            {
                LogError($"Connection failed: {ex.Message}");
                _client = null;
            }
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (_client == null)
            {
                LogWarning("Client is not connected.");
                return;
            }

            try
            {
                _client.Disconnect();
                LogMessage("Disconnected.");
            }
            catch (Exception ex)
            {
                LogError($"Error disconnecting: {ex.Message}");
            }
            finally
            {
                _client = null;
            }
        }

        private void ReadButton_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireConnected()) return;

            try
            {
                var registers = _client.ReadHoldingRegisters<short>(_unitId, ReadRegisterAddress, 1);
                var value = registers[0];
                ReadValueTextBox.Text = value.ToString();
                LogMessage($"Read holding register 40001 (addr {ReadRegisterAddress}) = {value}.");
            }
            catch (Exception ex)
            {
                LogError($"Read failed: {ex.Message}");
            }
        }

        private void WriteButton_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireConnected()) return;

            if (!short.TryParse(WriteValueTextBox.Text, out var value))
            {
                LogError("Invalid write value.");
                return;
            }

            try
            {
                _client.WriteSingleRegister(_unitId, WriteRegisterAddress, value);
                LogMessage($"Wrote holding register 41001 (addr {WriteRegisterAddress}) = {value}.");
            }
            catch (Exception ex)
            {
                LogError($"Write failed: {ex.Message}");
            }
        }

        private bool RequireConnected()
        {
            if (_client == null)
            {
                LogError("Not connected. Click Connect first.");
                return false;
            }
            return true;
        }

        private void LogMessage(string message) => AppendLog("INFO", message);
        private void LogWarning(string message) => AppendLog("WARN", message);
        private void LogError(string message) => AppendLog("ERROR", message);

        private void AppendLog(string level, string message)
        {
            var line = $"[{DateTime.Now:HH:mm:ss}] [{level}] {message}{Environment.NewLine}";
            LogTextBox.AppendText(line);
            LogTextBox.ScrollToEnd();
        }

        public void Shutdown()
        {
            if (_client != null)
            {
                _client.Disconnect();
                _client = null;
            }
        }
    }
}
