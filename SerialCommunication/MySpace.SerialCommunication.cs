using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Text.RegularExpressions;

namespace MySpace.SerialCommunication
{
    public class SerialCommunication
    {
        public SerialCommunication(SerialPort p)
        {
            port = p;
            commands = new List<serialCommand>();
            response = new serialResponse(this);
            sendDelay = 5;
            responseTimeout = 2000;
            serialTimer.Elapsed += new System.Timers.ElapsedEventHandler(onTimedEvent);
            serialTimer.Interval = 100;
            serialTimer.Enabled = true;
            onDataReceived = () => {
                // Do something
            };
        }
        private List<serialCommand> _commands;
        public List<serialCommand> commands
        {
            get { return _commands; }
            set { _commands = value; response = new serialResponse(this); }
        }
        public System.Timers.Timer serialTimer = new System.Timers.Timer();
        public serialResponse response;
        public DateTime lastCommandSent;
        public int sendDelay;
        public int responseTimeout;
        public byte[] bufferBytes;
        public string buffer;
        private SerialPort _port;
        public SerialPort port
        {
            get { return _port; }
            set
            {
                _port = value;
                _port.BaudRate = 57600;
                _port.DataBits = 8;
                _port.Parity = Parity.None;
                _port.StopBits = StopBits.One;
                _port.WriteTimeout = 500;
                _port.ReadTimeout = 500;
                _port.Handshake = Handshake.None;
                _port.RtsEnable = false;
                _port.DtrEnable = false;
                _port.NewLine = "\r\n";
                _port.DataReceived += new SerialDataReceivedEventHandler(onSerialDataReceived);
            }
        }
        public Action onDataReceived { get; set; }
        // Events
        private void onSerialDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            bufferBytes = new byte[port.BytesToRead];
            port.Read(bufferBytes, 0, bufferBytes.Length);
            buffer = System.Text.Encoding.Default.GetString(bufferBytes);
            response.bytes = ByteArrayCombine(response.bytes, bufferBytes);
            onDataReceived();
        }
        private void onTimedEvent(object source, System.Timers.ElapsedEventArgs e)
        {
            serialTimer.Stop();
            serialTimer.Enabled = false;
            if (port.IsOpen && commands.Count > 0)
            {
                process();
            }
            serialTimer.Enabled = true;
            serialTimer.Start();
        }
        // Classes
        public class serialCommand
        {
            public SerialCommunication instance;
            public serialCommand(SerialCommunication instance)
            {
                this.instance = instance;
                isCommandSent = false;
                responseTimeout = instance.responseTimeout;
                waitBeforeCopy = null;
                waitAfterCopy = null;
                waitAfterMSCopy = null;
                waitAfterBytesCopy = null;
                onSuccess = () => {
                    // System.Diagnostics.Debugger.Log(0, null, "Success:" + instance.response.text);
                };
                onFail = () => {
                    // System.Diagnostics.Debugger.Log(0, null, "Fail:" + instance.response.text);
                };
                onComplete = () => {
                    // System.Diagnostics.Debugger.Log(0, null, "Complete:" + instance.response.text);
                };

            }
            public int responseTimeout;
            public string command { get; set; }
            public byte[] commandBytes { get; set; }
            public bool isCommandSent { get; set; }
            string _waitBefore;
            public string waitBefore
            {
                get { return _waitBefore; }
                set { _waitBefore = value; if (waitBeforeCopy == null) { waitBeforeCopy = _waitBefore; } }
            }
            string _waitAfter;
            public string waitAfter
            {
                get { return _waitAfter; }
                set { _waitAfter = value; if (waitAfterCopy == null) { waitAfterCopy = _waitAfter; } }
            }
            long _waitAfterMS;
            public long waitAfterMS
            {
                get { return _waitAfterMS; }
                set { _waitAfterMS = value; if (waitAfterMSCopy == null) { waitAfterMSCopy = _waitAfterMS; } }
            }
            long _waitAfterBytes;
            public long waitAfterBytes
            {
                get { return _waitAfterBytes; }
                set { _waitAfterBytes = value; if (waitAfterBytesCopy == null) { waitAfterBytesCopy = _waitAfterBytes; } }
            }
            public Action onSuccess { get; set; }
            public Action onFail { get; set; }
            public Action onComplete { get; set; }

            // Copy of properties
            private string waitBeforeCopy { get; set; }
            private string waitAfterCopy { get; set; }
            private long? waitAfterMSCopy { get; set; }
            private long? waitAfterBytesCopy { get; set; }
            public void reset()
            {
                waitBefore = waitBeforeCopy;
                waitAfter = waitAfterCopy;
                waitAfterMS = waitAfterMSCopy ?? default(long);
                waitAfterBytes = waitAfterBytesCopy ?? default(long);
                isCommandSent = false;
            }
        }
        public class serialResponse
        {
            private SerialCommunication instance;
            public serialResponse(SerialCommunication instance)
            {
                this.instance = instance;
                clear();
            }
            public void clear()
            {
                text = string.Empty;
                bytes = new byte[] { };
            }
            // public string command { get; set; }
            public string text { get; set; }
            private byte[] _bytes;
            public byte[] bytes
            {
                get { return _bytes; }
                set
                {
                    _bytes = value;
                    text = System.Text.Encoding.Default.GetString(bytes);

                }
            }

            private long _responseTime;
            public long responseTime
            {
                get { return (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - (instance.lastCommandSent.Ticks / TimeSpan.TicksPerMillisecond); }
                set
                {
                    _responseTime = value;
                }
            }
        }
        // Functions
        public void process()
        {
            // Wait before command
            if (!this.commands[0].isCommandSent && !string.IsNullOrEmpty(this.commands[0].waitBefore))
            {
                Regex waitRegExp = new Regex(this.commands[0].waitBefore, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                if (waitRegExp.IsMatch(this.response.text))
                {
                    this.commands[0].waitBefore = null;
                }

            }
            // Send command
            if (!this.commands[0].isCommandSent && string.IsNullOrEmpty(this.commands[0].waitBefore))
            {
                bool portSendFailure = false;
                this.response.clear();
                if (this.commands[0].commandBytes != null && this.commands[0].commandBytes.Length > 0)
                {
                    for (int i = 0; i < this.commands[0].commandBytes.Length && !portSendFailure; i++)
                    {
                        try { this.port.Write(this.commands[0].commandBytes, i, 1); }
                        catch { portSendFailure = true; }
                        WinApi.TimeBeginPeriod(1);
                        System.Threading.Thread.Sleep(this.sendDelay);
                        WinApi.TimeEndPeriod(1);
                    }
                }
                else
                {
                    foreach (char c in this.commands[0].command)
                    {
                        try { this.port.Write(c.ToString()); }
                        catch { portSendFailure = true; }
                        WinApi.TimeBeginPeriod(1);
                        System.Threading.Thread.Sleep(this.sendDelay);
                        WinApi.TimeEndPeriod(1);
                    }
                }
                this.commands[0].isCommandSent = true;
                this.lastCommandSent = DateTime.Now;
            }
            // Wait after command - Regex
            if (this.commands[0].isCommandSent && !string.IsNullOrEmpty(this.commands[0].waitAfter))
            {
                Regex waitRegExp = new Regex(this.commands[0].waitAfter, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                if (waitRegExp.IsMatch(this.response.text))
                {
                    this.commands[0].waitAfter = null;
                }
            }
            // Wait after command - Time
            if (this.commands[0].isCommandSent && (this.commands[0].waitAfterMS > 0))
            {
                if (this.response.responseTime > this.commands[0].waitAfterMS)
                {
                    this.commands[0].waitAfterMS = 0;
                }
            }
            // Wait after command - Number of bytes
            if (this.commands[0].isCommandSent && (this.commands[0].waitAfterBytes > 0))
            {
                if (this.response.bytes.Length >= this.commands[0].waitAfterBytes)
                {
                    this.commands[0].waitAfterBytes = 0;
                }
            }
            // Command completed successfully
            if (this.commands[0].isCommandSent &&
                string.IsNullOrEmpty(this.commands[0].waitBefore) &&
                string.IsNullOrEmpty(this.commands[0].waitAfter) &&
                !(this.commands[0].waitAfterMS > 0) &&
                !(this.commands[0].waitAfterBytes > 0))
            {
                this.commands[0].onSuccess();
                this.commands[0].onComplete();
                this.commands.RemoveAt(0);

            }
            // Command failed - Timeout
            else if (this.commands[0].isCommandSent && this.response.responseTime > this.commands[0].responseTimeout)
            {
                this.commands[0].onFail();
                this.commands[0].onComplete();
                this.commands.RemoveAt(0);
            }

        }
        // Generic functions
        public static byte[] ByteArrayCombine(params byte[][] arrays)
        {
            int len = 0;
            foreach (byte[] array in arrays) { if (array != null) len += array.Length; }
            // byte[] rv = new byte[arrays.Sum(a => a.Length)];
            byte[] rv = new byte[len];
            int offset = 0;
            foreach (byte[] array in arrays)
            {
                if (array != null)
                {
                    System.Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                    offset += array.Length;
                }
            }
            return rv;
        }
        public static byte[] ByteArrayLast(byte[] ByteArray, int Length)
        {
            byte[] rv = new byte[Math.Min(ByteArray.Length, Length)];
            System.Buffer.BlockCopy(ByteArray, Math.Max(ByteArray.Length - Length, 0), rv, 0, rv.Length);
            return rv;
        }
        public static byte[] ByteArrayFirst(byte[] ByteArray, int Length)
        {
            byte[] rv = new byte[Math.Min(ByteArray.Length, Length)];
            System.Buffer.BlockCopy(ByteArray, 0, rv, 0, rv.Length);
            return rv;
        }
    }
    public static class WinApi
    {
        /// <summary>TimeBeginPeriod(). See the Windows API documentation for details.</summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Interoperability", "CA1401:PInvokesShouldNotBeVisible"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage"), System.Security.SuppressUnmanagedCodeSecurity]
        [System.Runtime.InteropServices.DllImport("winmm.dll", EntryPoint = "timeBeginPeriod", SetLastError = true)]
        public static extern uint TimeBeginPeriod(uint uMilliseconds);
        /// <summary>TimeEndPeriod(). See the Windows API documentation for details.</summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Interoperability", "CA1401:PInvokesShouldNotBeVisible"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage"), System.Security.SuppressUnmanagedCodeSecurity]
        [System.Runtime.InteropServices.DllImport("winmm.dll", EntryPoint = "timeEndPeriod", SetLastError = true)]
        public static extern uint TimeEndPeriod(uint uMilliseconds);
    }
}
