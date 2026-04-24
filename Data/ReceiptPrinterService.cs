using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Text;
using System.Printing;

namespace Eco_Matic.Data;

public sealed class ReceiptPrintResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? PortName { get; init; }
}

public sealed class ReceiptPrinterService
{
    private const string PrinterEnabledKey = "ECOMATIC_RECEIPT_PRINTER_ENABLED";
    private const string PrinterPortKey = "ECOMATIC_RECEIPT_PRINTER_PORT";
    private const string PrinterBaudRateKey = "ECOMATIC_RECEIPT_PRINTER_BAUD_RATE";
    private const string PrinterDataBitsKey = "ECOMATIC_RECEIPT_PRINTER_DATA_BITS";
    private const string PrinterStopBitsKey = "ECOMATIC_RECEIPT_PRINTER_STOP_BITS";
    private const string PrinterParityKey = "ECOMATIC_RECEIPT_PRINTER_PARITY";
    private const string PrinterDtrKey = "ECOMATIC_RECEIPT_PRINTER_DTR_ENABLE";
    private const string PrinterRtsKey = "ECOMATIC_RECEIPT_PRINTER_RTS_ENABLE";
    private const string PrinterModeKey = "ECOMATIC_RECEIPT_PRINTER_MODE";
    private const string PrinterNameKey = "ECOMATIC_RECEIPT_PRINTER_NAME";
    private const string DefaultArduinoPort = "COM5";

    public static ReceiptPrinterService Instance { get; } = new();

    private ReceiptPrinterService()
    {
    }

    public ReceiptPrintResult TryPrintReceipt(Transaction transaction)
    {
        if (transaction == null)
        {
            return new ReceiptPrintResult
            {
                Success = false,
                Message = "No transaction data is available to print."
            };
        }

        PrinterSettings settings = LoadSettings();
        if (!settings.Enabled)
        {
            return new ReceiptPrintResult
            {
                Success = false,
                Message = "Receipt printer is disabled in configuration."
            };
        }

        if (settings.Mode is PrinterConnectionMode.Windows or PrinterConnectionMode.Auto)
        {
            ReceiptPrintResult windowsResult = TryPrintUsingWindowsQueue(transaction, settings);
            if (windowsResult.Success || settings.Mode == PrinterConnectionMode.Windows)
            {
                return windowsResult;
            }
        }

        if (settings.Mode is PrinterConnectionMode.Serial or PrinterConnectionMode.Auto)
        {
            ReceiptPrintResult serialResult = TryPrintUsingSerial(transaction, settings);
            if (serialResult.Success || settings.Mode == PrinterConnectionMode.Serial)
            {
                return serialResult;
            }

            return new ReceiptPrintResult
            {
                Success = false,
                Message = $"Windows printer queue and serial printing both failed. {serialResult.Message}"
            };
        }

        return new ReceiptPrintResult
        {
            Success = false,
            Message = "Receipt printer mode is invalid. Use Auto, Windows, or Serial."
        };
    }

    private static ReceiptPrintResult TryPrintUsingSerial(Transaction transaction, PrinterSettings settings)
    {
        string? portName = ResolveSerialPortName(settings.ConfiguredPort);
        if (string.IsNullOrWhiteSpace(portName))
        {
            return new ReceiptPrintResult
            {
                Success = false,
                Message = "No serial receipt printer port was found."
            };
        }

        byte[] payload = Utilities.EscPosReceiptFormatter.BuildReceipt(transaction);

        try
        {
            using var serialPort = new SerialPort(portName, settings.BaudRate, settings.Parity, settings.DataBits, settings.StopBits)
            {
                Encoding = Encoding.ASCII,
                DtrEnable = settings.DtrEnable,
                RtsEnable = settings.RtsEnable,
                ReadTimeout = 1500,
                WriteTimeout = 4000
            };

            serialPort.Open();
            serialPort.Write(payload, 0, payload.Length);
            serialPort.BaseStream.Flush();
            Thread.Sleep(200);

            return new ReceiptPrintResult
            {
                Success = true,
                PortName = portName,
                Message = $"Receipt printed successfully on serial port {portName}."
            };
        }
        catch (Exception ex)
        {
            return new ReceiptPrintResult
            {
                Success = false,
                PortName = portName,
                Message = $"Could not print to serial port {portName}: {ex.Message}"
            };
        }
    }

    private static ReceiptPrintResult TryPrintUsingWindowsQueue(Transaction transaction, PrinterSettings settings)
    {
        try
        {
            PrintQueue? printQueue = ResolvePrintQueue(settings.ConfiguredPrinterName, settings.ConfiguredPort);
            if (printQueue == null)
            {
                return new ReceiptPrintResult
                {
                    Success = false,
                    Message = "No Windows receipt printer queue was found. Set ECOMATIC_RECEIPT_PRINTER_NAME to your POS58 printer name."
                };
            }

            byte[] payload = Utilities.EscPosReceiptFormatter.BuildReceipt(transaction);
            RawPrinterHelper.SendBytesToPrinter(printQueue.Name, payload, "Eco-Matic Receipt");

            return new ReceiptPrintResult
            {
                Success = true,
                Message = $"Receipt printed successfully on printer '{printQueue.Name}' in raw mode.",
                PortName = printQueue.Name
            };
        }
        catch (Exception ex)
        {
            return new ReceiptPrintResult
            {
                Success = false,
                Message = $"Could not print through the Windows printer driver: {ex.Message}"
            };
        }
    }

    private static PrinterSettings LoadSettings()
    {
        AppEnvironment.Initialize();

        return new PrinterSettings
        {
            Enabled = ReadBool(PrinterEnabledKey, true),
            ConfiguredPort = ReadString(PrinterPortKey),
            BaudRate = ReadInt(PrinterBaudRateKey, 9600),
            DataBits = ReadInt(PrinterDataBitsKey, 8),
            StopBits = ReadStopBits(PrinterStopBitsKey, StopBits.One),
            Parity = ReadParity(PrinterParityKey, Parity.None),
            DtrEnable = ReadBool(PrinterDtrKey, true),
            RtsEnable = ReadBool(PrinterRtsKey, true),
            Mode = ReadPrinterMode(PrinterModeKey, PrinterConnectionMode.Auto),
            ConfiguredPrinterName = ReadString(PrinterNameKey)
        };
    }

    private static string? ResolveSerialPortName(string? configuredPort)
    {
        string[] ports = SerialPort.GetPortNames()
            .OrderByDescending(GetPortOrder)
            .ToArray();

        if (!string.IsNullOrWhiteSpace(configuredPort))
        {
            return ports.FirstOrDefault(port => string.Equals(port, configuredPort, StringComparison.OrdinalIgnoreCase))
                ?? configuredPort;
        }

        string[] printerCandidates = ports
            .Where(port => !string.Equals(port, DefaultArduinoPort, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (printerCandidates.Length == 1)
        {
            return printerCandidates[0];
        }

        if (printerCandidates.Length > 1)
        {
            return printerCandidates[0];
        }

        if (ports.Length == 1)
        {
            return ports[0];
        }

        return null;
    }

    private static PrintQueue? ResolvePrintQueue(string? configuredPrinterName, string? configuredPort)
    {
        using var printServer = new LocalPrintServer();
        List<PrintQueue> queues = printServer.GetPrintQueues().ToList();

        if (!string.IsNullOrWhiteSpace(configuredPrinterName))
        {
            PrintQueue? exactByName = queues.FirstOrDefault(queue =>
                string.Equals(queue.Name, configuredPrinterName, StringComparison.OrdinalIgnoreCase));
            if (exactByName != null)
            {
                return exactByName;
            }
        }

        if (!string.IsNullOrWhiteSpace(configuredPort))
        {
            string normalizedPort = NormalizePortName(configuredPort);
            PrintQueue? byPort = queues.FirstOrDefault(queue =>
                string.Equals(NormalizePortName(queue.QueuePort?.Name), normalizedPort, StringComparison.OrdinalIgnoreCase));
            if (byPort != null)
            {
                return byPort;
            }
        }

        if (!string.IsNullOrWhiteSpace(configuredPrinterName))
        {
            PrintQueue? fuzzyByName = queues.FirstOrDefault(queue =>
                queue.Name.Contains(configuredPrinterName, StringComparison.OrdinalIgnoreCase));
            if (fuzzyByName != null)
            {
                return fuzzyByName;
            }
        }

        return queues.FirstOrDefault(queue =>
            queue.Name.Contains("POS-58", StringComparison.OrdinalIgnoreCase) ||
            queue.Name.Contains("POS58", StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizePortName(string? portName)
    {
        return string.IsNullOrWhiteSpace(portName)
            ? string.Empty
            : portName.Trim().TrimEnd(':');
    }

    private static int GetPortOrder(string portName)
    {
        string digits = new string(portName.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out int number) ? number : 0;
    }

    private static string? ReadString(string key)
    {
        string? value = Environment.GetEnvironmentVariable(key)?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static int ReadInt(string key, int defaultValue)
    {
        string? value = Environment.GetEnvironmentVariable(key)?.Trim();
        return int.TryParse(value, out int parsedValue) && parsedValue > 0 ? parsedValue : defaultValue;
    }

    private static bool ReadBool(string key, bool defaultValue)
    {
        string? value = Environment.GetEnvironmentVariable(key)?.Trim();
        return bool.TryParse(value, out bool parsedValue) ? parsedValue : defaultValue;
    }

    private static StopBits ReadStopBits(string key, StopBits defaultValue)
    {
        string? value = Environment.GetEnvironmentVariable(key)?.Trim();
        return Enum.TryParse(value, ignoreCase: true, out StopBits parsedValue) ? parsedValue : defaultValue;
    }

    private static Parity ReadParity(string key, Parity defaultValue)
    {
        string? value = Environment.GetEnvironmentVariable(key)?.Trim();
        return Enum.TryParse(value, ignoreCase: true, out Parity parsedValue) ? parsedValue : defaultValue;
    }

    private static PrinterConnectionMode ReadPrinterMode(string key, PrinterConnectionMode defaultValue)
    {
        string? value = Environment.GetEnvironmentVariable(key)?.Trim();
        return Enum.TryParse(value, ignoreCase: true, out PrinterConnectionMode parsedValue) ? parsedValue : defaultValue;
    }

    private sealed class PrinterSettings
    {
        public bool Enabled { get; init; }
        public string? ConfiguredPort { get; init; }
        public int BaudRate { get; init; }
        public int DataBits { get; init; }
        public StopBits StopBits { get; init; }
        public Parity Parity { get; init; }
        public bool DtrEnable { get; init; }
        public bool RtsEnable { get; init; }
        public PrinterConnectionMode Mode { get; init; }
        public string? ConfiguredPrinterName { get; init; }
    }

    private enum PrinterConnectionMode
    {
        Auto,
        Windows,
        Serial
    }

    private static class RawPrinterHelper
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private sealed class DocInfo1
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string? pDocName;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string? pOutputFile;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string? pDataType;
        }

        [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In] DocInfo1 docInfo);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        public static void SendBytesToPrinter(string printerName, byte[] bytes, string documentName)
        {
            if (!OpenPrinter(printerName, out IntPtr printerHandle, IntPtr.Zero))
            {
                throw new InvalidOperationException($"OpenPrinter failed with Win32 error {Marshal.GetLastWin32Error()}.");
            }

            try
            {
                var docInfo = new DocInfo1
                {
                    pDocName = documentName,
                    pDataType = "RAW"
                };

                if (!StartDocPrinter(printerHandle, 1, docInfo))
                {
                    throw new InvalidOperationException($"StartDocPrinter failed with Win32 error {Marshal.GetLastWin32Error()}.");
                }

                try
                {
                    if (!StartPagePrinter(printerHandle))
                    {
                        throw new InvalidOperationException($"StartPagePrinter failed with Win32 error {Marshal.GetLastWin32Error()}.");
                    }

                    IntPtr unmanagedBytes = Marshal.AllocCoTaskMem(bytes.Length);
                    try
                    {
                        Marshal.Copy(bytes, 0, unmanagedBytes, bytes.Length);
                        if (!WritePrinter(printerHandle, unmanagedBytes, bytes.Length, out int written) || written != bytes.Length)
                        {
                            throw new InvalidOperationException($"WritePrinter failed with Win32 error {Marshal.GetLastWin32Error()}.");
                        }
                    }
                    finally
                    {
                        Marshal.FreeCoTaskMem(unmanagedBytes);
                    }

                    EndPagePrinter(printerHandle);
                }
                finally
                {
                    EndDocPrinter(printerHandle);
                }
            }
            finally
            {
                ClosePrinter(printerHandle);
            }
        }
    }
}
