using System;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using FluentModbus;

namespace ModbusFluentTest
{
    /// <summary>
    /// Hosts a Modbus TCP server. Exposes one sample "read" holding register at
    /// 40001 (protocol address 0) that the operator can set here for clients to
    /// read, and one sample "write" holding register at 41001 (protocol address
    /// 1000) that clients can write to and this control displays.
    /// </summary>
    public partial class ServerControl : UserControl
    {
        private static readonly ushort ReadRegisterAddress = ModbusAddress.ToProtocolAddress(40001);
        private static readonly ushort WriteRegisterAddress = ModbusAddress.ToProtocolAddress(41001);

        private ModbusTcpServer _server;
        private byte _unitId;

        public ServerControl()
        {
            InitializeComponent();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (_server != null)
            {
                LogWarning("Server already running.");
                return;
            }

            if (!int.TryParse(PortTextBox.Text, out var port))
            {
                LogError("Invalid port.");
                return;
            }

            if (!byte.TryParse(UnitIdTextBox.Text, out var unitId))
            {
                LogError("Invalid unit ID.");
                return;
            }

            try
            {
                // Unit ID 0 = FluentModbus's default single-unit mode (matches a client
                // that also uses unit ID 0). For a non-zero ID, explicitly register that
                // unit with AddUnit so the server responds to it.
                _server = new ModbusTcpServer { EnableRaisingEvents = true };
                _unitId = unitId;

                if (unitId != 0)
                {
                    _server.AddUnit(unitId);
                }

                _server.RegistersChanged += Server_RegistersChanged;

                _server.Start(new IPEndPoint(IPAddress.Any, port));

                // Seed the sample read register with its current UI value.
                if (short.TryParse(ReadRegisterValueTextBox.Text, out var seed))
                {
                    var registers = _server.GetHoldingRegisters(_unitId);
                    registers[ReadRegisterAddress] = seed;
                }

                LogMessage($"Server started on port {port}.");
                LogMessage($"Sample read register 40001 -> protocol address {ReadRegisterAddress}.");
                LogMessage($"Sample write register 41001 -> protocol address {WriteRegisterAddress}.");
            }
            catch (Exception ex)
            {
                LogError($"Failed to start server: {ex.Message}");
                _server = null;
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (_server == null)
            {
                LogWarning("Server is not running.");
                return;
            }

            try
            {
                _server.RegistersChanged -= Server_RegistersChanged;
                _server.Stop();
                _server.Dispose();
                _server = null;
                LogMessage("Server stopped.");
            }
            catch (Exception ex)
            {
                LogError($"Error stopping server: {ex.Message}");
            }
        }

        private void SetReadRegisterButton_Click(object sender, RoutedEventArgs e)
        {
            if (_server == null)
            {
                LogWarning("Start the server first.");
                return;
            }

            if (!short.TryParse(ReadRegisterValueTextBox.Text, out var value))
            {
                LogError("Invalid value for read register.");
                return;
            }

            var registers = _server.GetHoldingRegisters(_unitId);
            registers[ReadRegisterAddress] = value;
            LogMessage($"Set holding register 40001 (addr {ReadRegisterAddress}) = {value}.");
        }

        private void RefreshWriteRegisterButton_Click(object sender, RoutedEventArgs e)
        {
            if (_server == null)
            {
                LogWarning("Start the server first.");
                return;
            }

            var registers = _server.GetHoldingRegisters(_unitId);
            var value = registers[WriteRegisterAddress];
            WriteRegisterValueTextBox.Text = value.ToString();
            LogMessage($"Refreshed holding register 41001 (addr {WriteRegisterAddress}) = {value}.");
        }

        private void Server_RegistersChanged(object sender, RegistersChangedEventArgs e)
        {
            // This event is raised on the server's own request-handling thread, possibly
            // while it still holds an internal lock. Calling back into the server
            // (GetHoldingRegisters) or blocking that thread (Dispatcher.Invoke) here can
            // deadlock the request that triggered the event — this is why "write 1" hung.
            // Copy what we need and marshal to the UI ASYNCHRONOUSLY (BeginInvoke), then
            // touch the server only from the UI thread afterwards.
            var addresses = e.Registers.ToArray();

            Dispatcher.BeginInvoke((Action)(() =>
            {
                foreach (var addr in addresses)
                {
                    if (addr == WriteRegisterAddress && _server != null)
                    {
                        var registers = _server.GetHoldingRegisters(_unitId);
                        var value = registers[WriteRegisterAddress];
                        WriteRegisterValueTextBox.Text = value.ToString();
                        LogMessage($"Client wrote holding register 41001 (addr {WriteRegisterAddress}) = {value}.");
                    }
                    else
                    {
                        LogMessage($"Register changed at protocol address {addr}.");
                    }
                }
            }));
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
            if (_server != null)
            {
                _server.RegistersChanged -= Server_RegistersChanged;
                _server.Stop();
                _server.Dispose();
                _server = null;
            }
        }
    }
}
