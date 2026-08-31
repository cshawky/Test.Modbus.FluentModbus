namespace ModbusFluentTest
{
    /// <summary>
    /// Helper for converting traditional Modicon-style 4xxxx holding register
    /// addresses (as printed on device documentation) to the zero-based protocol
    /// addresses that FluentModbus (and the Modbus wire protocol) actually uses.
    ///
    /// Holding register 40001 = protocol address 0
    /// Holding register 41001 = protocol address 1000
    /// Holding register 4xxxx = protocol address (xxxx - 1)
    /// </summary>
    public static class ModbusAddress
    {
        private const int HoldingRegisterBase = 40001;

        /// <summary>Converts a 4xxxx holding register number to a zero-based protocol address.</summary>
        public static ushort ToProtocolAddress(int holdingRegisterNumber)
        {
            return (ushort)(holdingRegisterNumber - HoldingRegisterBase);
        }

        /// <summary>Converts a zero-based protocol address back to its 4xxxx holding register number.</summary>
        public static int ToHoldingRegisterNumber(ushort protocolAddress)
        {
            return HoldingRegisterBase + protocolAddress;
        }
    }
}
