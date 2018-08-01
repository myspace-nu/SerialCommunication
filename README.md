# MySpace.SerialCommunication

Making serial communication a bit easier.

## Installation

Add a reference to the class .dll in your project. For convenience, also add the namespace.

	using MySpace.SerialCommunication;
	
## Usage

	SerialCommunication mySerial;
	// ...
	mySerial = new SerialCommunication(serialPort1); // Create a new instance and bind it to serialPort1
	mySerial.port.BaudRate = 57600; // Set port properties
	mySerial.onDataReceived = () =>
	{
		// Optional event for continuously monitoring incoming data.
		// string mySerial.buffer
		// byte[] mySerial.bufferBytes
	};
	// ...
	mySerial.port.PortName = "COM1";
	try { mySerial.port.Open();  }
	catch
	{
		// Unable to open port. It might be in use by another application.
	}
	//
	sCmd = new SerialCommunication.serialCommand(mySerial);
	sCmd.command = "ATI\r\n";
	sCmd.waitAfter = "OK";
	sCmd.onSuccess = () =>
	{
		MessageBox.Show(mySerial.response.text);
	};
	sCmd.onFail = () =>
	{
		MessageBox.Show("Serial device did not respond in a timely manner.");
	};
	sCmd.onComplete = () =>
	{
		MessageBox.Show("All done.");
	};
	mySerial.commands.Add(sCmd);

## SerialCommunication class

**commands** - List of serial commands (*List<SerialCommunication.serialCommand>*)

**response** - Serial response (*SerialCommunication.serialResponse*)

**responseTimeout** - Default response timeout for commands (*long*)

*Default: 2000*

**sendDelay** - Wait this many milliseconds between bytes when sending data on the serial port (*long*)

*Default: 5*

**buffer** - Serial data buffer (*string*)

**bufferBytes** - Serial data buffer (*byte[]*)

**port** - Serial port object (*System.IO.Ports.SeriaPort*)

**onDataReceived** - Event for monitoring incoming serial data (*event*)

## SerialCommunication.serialCommand class

**command** - Command to send (*string*)

**commandBytes** - Command to send (*byte[]*)

**waitBefore** - Wait for this string before sending command (*string*)

**waitAfter** - Wait for this string after the command has been sent (*string*)

**waitAfterMS** - Wait this many milliseconds after the command has been sent (*long*)

**waitAfterBytes** - Wait this many bytes to be received after the command has been sent (*long*)

**responseTimeout** - The command will timeout and fail after this many milliseconds (*long*)

*Default: SerialCommunication.responseTimeout*

**onSuccess** - Event to execute on success (*event*)

**onFail** - Event to execute on fail (*event*)

**onComplete** - Event to execute on success or fail (*event*)

**instance** - Instance of SerialCommunication

## SerialCommunication.serialResponse class

**text** - The response as text (*string*)

**bytes** - The response as byte array (*byte[]*)

**responseTime** - The time in milliseconds it took to for the serial command to complete. (*long*)