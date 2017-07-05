using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;

namespace SimpleDoor2
{
    public class Connection : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public MqttClient client;

        public CancellationTokenSource ReadCancellationTokenSource;
        private SerialDevice serialPort = null;
        DataWriter dataWriteObject = null;
        DataReader dataReaderObject = null;

        public Connection()
        {
            MqttConnect();
            SerialConnection();
        }

        static private string _mqttStatus = "Not Connected";
        public string mqttStatus
        {
            get { return _mqttStatus; }
            set
            {
                _mqttStatus = value;
                RaisePropertyChanged("mqttStatus");
            }
        }

        static private string _mqttData = "No Data";
        public string mqttData
        {
            get { return _mqttData; }
            set
            {
                _mqttData = value;
                RaisePropertyChanged("mqttData");
            }
        }

        static private string _serialStatus = "Not Connected";
        public string serialStatus
        {
            get { return _serialStatus; }
            set
            {
                _serialStatus = value;
                RaisePropertyChanged("serialStatus");
            }
        }

        static private string _serialData = "No Data";
        public string serialData
        {
            get { return _serialData; }
            set
            {
                _serialData = value;
                RaisePropertyChanged("serialData");
            }
        }

        protected void RaisePropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));

            }
        }

        /// <summary>
        /// MQTT block
        /// </summary>
        //string serverAddr = "test.mosquitto.org";
        string serverAddr = "192.168.1.2";
        const string topic1 = "AS/DoorCoworkingOut/server_response"; //"AS/DoorCoworkingOut/server_response"  "TestOut/server_response"
        const string topic2 = "AS/F";   //"AS/DoorCoworkingOut/F"
        const string topic3 = "AS/I";   //"AS/DoorCoworkingOut/I"
        const string topic4 = "AS/rez"; //"AS/DoorCoworkingOut/rez"
        const string topic5 = "AS/ind";   //"AS/DoorCoworkingOut/ind"

        const string topic6 = "AS/DoorCoworkingIn/server_response";    //"AS/DoorCoworkingIn/ind"  "TestIn/server_response"

        public void MqttConnect()
        {

            client = new MqttClient(serverAddr);
            client.Connect(Guid.NewGuid().ToString());

            if (client.IsConnected)
            {
                mqttStatus = "I`m Connected to " + serverAddr;

            }

            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;


            // subscribe to the topic

            client.Subscribe(new string[] { topic1 }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
            client.Subscribe(new string[] { topic2 }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
            client.Subscribe(new string[] { topic3 }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
            client.Subscribe(new string[] { topic4 }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
            client.Subscribe(new string[] { topic5 }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
            client.Subscribe(new string[] { topic6 }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });

        }

        byte[] mqttMessage;
        string mqttTopic;
        string _FirstName;
        string _secondName;
        string _rez;
        int _ind;

        async void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            mqttMessage = e.Message;
            mqttTopic = e.Topic;
            switch (mqttTopic)
            {
                case topic1:
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        //Do some UI-code that must be run on the UI thread.
                        mqttData = Encoding.UTF8.GetString(mqttMessage);
                        if (mqttData == "yes")
                            Add();
                    });

                    break;
                case topic2:
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        //Do some UI-code that must be run on the UI thread.
                        _secondName = Encoding.UTF8.GetString(mqttMessage);
                    });
                    break;

                case topic3:
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        //Do some UI-code that must be run on the UI thread.
                        _FirstName = Encoding.UTF8.GetString(mqttMessage);
                    });
                    break;
                case topic4:
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        //Do some UI-code that must be run on the UI thread.
                        switch (Encoding.UTF8.GetString(mqttMessage))
                        {
                            case "rez":
                                _rez = "Резидент";
                                break;
                            case "staj":
                                _rez = "Стажер";
                                break;
                        }
                    });
                    break;
                case topic5:
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        //Do some UI-code that must be run on the UI thread.
                        int.TryParse(Encoding.UTF8.GetString(mqttMessage), out _ind);
                    });
                    break;
                case topic6:
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        //Do some UI-code that must be run on the UI thread.
                        if (Encoding.UTF8.GetString(mqttMessage) == "yes")
                        {
                            Delete();
                        }

                    });
                    break;
            }

        }

        /// <summary>
        /// SerialPort block
        /// </summary>
        public async void SerialConnection()
        {
            string qFilter = SerialDevice.GetDeviceSelector("COM3");
            DeviceInformationCollection devices = await DeviceInformation.FindAllAsync(qFilter);

            if (devices.Any())
            {
                string deviceId = devices.First().Id;

                await OpenPort(deviceId);
            }

            ReadCancellationTokenSource = new CancellationTokenSource();

            while (true)
            {
                await Listen();
            }
        }

        public void CancelReadTask()
        {
            if (ReadCancellationTokenSource != null)
            {
                if (!ReadCancellationTokenSource.IsCancellationRequested)
                {
                    ReadCancellationTokenSource.Cancel();
                }
            }

            if (serialPort != null)
            {
                serialPort.Dispose();
            }
            serialPort = null;
        }

        private async Task OpenPort(string deviceId)
        {
            serialPort = await SerialDevice.FromIdAsync(deviceId);

            if (serialPort != null)
            {
                serialPort.WriteTimeout = TimeSpan.FromMilliseconds(100);
                serialPort.ReadTimeout = TimeSpan.FromMilliseconds(100);
                serialPort.BaudRate = 9600;
                serialPort.Parity = SerialParity.None;
                serialPort.StopBits = SerialStopBitCount.One;
                serialPort.DataBits = 8;
                serialPort.Handshake = SerialHandshake.None;

                //SerialPortStatus.Text = "I`m Connected via COM3";
            }
        }

        private async Task Listen()
        {
            try
            {
                if (serialPort != null)
                {
                    dataReaderObject = new DataReader(serialPort.InputStream);
                    await ReadAsync(ReadCancellationTokenSource.Token);
                }
            }
            catch (Exception ex)
            {
                //txtStatus.Text = ex.Message;
            }
            finally
            {
                if (dataReaderObject != null)    // Cleanup once complete
                {
                    dataReaderObject.DetachStream();
                    dataReaderObject = null;
                }
            }
        }

        private async Task ReadAsync(CancellationToken cancellationToken)
        {
            Task<UInt32> loadAsyncTask;

            uint ReadBufferLength = 256;  // only when this buffer would be full next code would be executed

            dataReaderObject.InputStreamOptions = InputStreamOptions.Partial;

            loadAsyncTask = dataReaderObject.LoadAsync(ReadBufferLength).AsTask(cancellationToken);   // Create a task object

            UInt32 bytesRead = await loadAsyncTask;    // Launch the task and wait until buffer would be full

            if (bytesRead > 0)
            {
                string strFromPort = dataReaderObject.ReadString(bytesRead);
                int fstLetter = strFromPort.IndexOf("Info");
                int lstLetter = strFromPort.IndexOf("Info", fstLetter + 1);
                if ((fstLetter >= 0) && (lstLetter > 0)) strFromPort = strFromPort.Substring(fstLetter, lstLetter - fstLetter);

                this.client.Publish("AS/DoorCoworkingOut/cardID", Encoding.UTF8.GetBytes(strFromPort));
                
                serialData = Regex.Replace(strFromPort, @"\t|\n|\r", "");
                
            }
        }

        private async Task WriteAsync(string text2write)
        {
            Task<UInt32> storeAsyncTask;

            if (text2write.Length != 0)
            {
                dataWriteObject.WriteString(text2write);

                storeAsyncTask = dataWriteObject.StoreAsync().AsTask();  // Create a task object

                UInt32 bytesWritten = await storeAsyncTask;   // Launch the task and wait
                if (bytesWritten > 0)
                {
                    //txtStatus.Text = bytesWritten + " bytes written at " + DateTime.Now.ToString(System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat.LongTimePattern);
                }
            }
            else { }
        }


        private async Task sendToPort(string sometext)
        {
            try
            {
                if (serialPort != null)
                {
                    dataWriteObject = new DataWriter(serialPort.OutputStream);

                    await WriteAsync(sometext);
                }
                else { }
            }
            catch (Exception ex)
            {
                // txtStatus.Text = ex.Message;
            }
            finally
            {
                if (dataWriteObject != null)   // Cleanup once complete
                {
                    dataWriteObject.DetachStream();
                    dataWriteObject = null;
                }
            }
        }

        /// <summary>
        /// Inside person list
        /// </summary>

        ObservableCollection<Person> _People = new ObservableCollection<Person>();
        public ObservableCollection<Person> People
        {
            get { return _People; }
            set
            {
                _People = value;
                RaisePropertyChanged("People");
            }
        }

        public void Add()
        {
            if (!_People.Any(p => p.ind == _ind))
            {
                var person = new Person { FirstName = _FirstName, SecondName = _secondName, rez = _rez, ind = _ind };
                People.Add(person);
            }

        }

        public void Delete()
        {
            People.Remove(People.SingleOrDefault(i => i.ind == _ind));
        }
    }
}
