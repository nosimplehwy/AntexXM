using Crestron.RAD.Common.BasicDriver;
using Crestron.RAD.Common.Logging;
using Crestron.RAD.Common.Transports;
using System;
using System.Collections.Generic;
using System.Text;
using Crestron.RAD.Common.Enums;
using Crestron.RAD.Common.Events;
using Crestron.SimplSharpPro.Thermostats;
using Independentsoft.Exchange;

namespace AntexXMRadio
{
    public class AntexXmRadioProtocol : ABaseDriverProtocol
    {

        private StringBuilder _keypadNumber;
        private StringBuilder _data;
        private TextChangedEventArgs _currentChannelFeedback;
        private bool _powerState;
        private const char EndOfResponse = '\r';
        private const char StartOfResponse = '*';


        public event EventHandler<BoolAttributeChangedEventArgs> BoolAttributeChanged;
        public event EventHandler<StringAttributeChangedEventArgs> StringAttributeChanged;
        public event EventHandler<TextChangedEventArgs> CurrentTextChanged;
        public event EventHandler<string> KeypadTextChanged;
        public event EventHandler<ValueEventArgs<bool>> IsConnectedChanged;

        public List<Preset> Presets { get; private set; }
        public string CurrentChannel { get; private set; }
        public AntexXmRadioProtocol(ISerialTransport transport, byte id) : base(transport, id)
        {
            _keypadNumber = new StringBuilder(3);
            _data = new StringBuilder();
            _currentChannelFeedback = new TextChangedEventArgs();
            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "AntexXMRadioProtocol", "");
            InitializePresets();
        }

        protected override void ConnectionChanged(bool connection)
        {
            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Error, "ConnectionChanged", String.Format($"connection: {connection}, IsConnected: {IsConnected}"));
            base.ConnectionChanged(connection);
        }

        protected override void ConnectionChangedEvent(bool connection)
        {
            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Error, "ConnectionChangedEvent", String.Format($"connection: {connection}, IsConnected: {IsConnected}"));
            IsConnectedChanged?.Invoke(this, new ValueEventArgs<bool>(connection));
        }

        protected override void ChooseDeconstructMethod(ValidatedRxData validatedData)
        {
            //not used
        }


        public override void SetUserAttribute(string attributeId, string attributeValue)
        {
            if (string.IsNullOrEmpty(attributeValue))
            {
                AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Error, "SetUserAttribute",
                    "Attribute value is null or empty");
                return;
            }

            StringAttributeChanged?.Invoke(this,
                new StringAttributeChangedEventArgs {Id = attributeId, Value = attributeValue});
        }

        public override void SetUserAttribute(string attributeId, bool attributeValue)
        {
            BoolAttributeChanged?.Invoke(this,
                new BoolAttributeChangedEventArgs {Id = attributeId, Value = attributeValue});
        }

        public override void Dispose()
        {
            // Do nothing for now, this is due to a bug in the base class Dispose method
        }


        public class BoolAttributeChangedEventArgs : EventArgs
        {
            public string Id;
            public bool Value;
        }

        public class StringAttributeChangedEventArgs : EventArgs
        {
            public string Id;
            public string Value;
        }

        public class TextChangedEventArgs : EventArgs
        {
            public string ChannelNameNum;
            public string Category;
            public string Artist;
            public string Song;
        }

        public override void DataHandler(string rx)
        {
            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "DataHandler", rx);
            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "DataHandler", string.Format($"IsConnected = {IsConnected}"));
            if (IsConnected == false)
            {
                ConnectionChanged(true);
            }

            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "DataHandler",
                string.Format($"Append: {rx} to {_data}"));

            foreach (var chr in rx)
            {
                if (chr == StartOfResponse)
                {
                    _data.Clear();
                }

                if (chr == EndOfResponse)
                {
                    ValidateData(_data.ToString());
                    _data.Clear();

                }
                _data.Append(chr);

            }

        }

        private void ValidateData(string data)
        {

            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "ValidateData",
                string.Format($"{data}"));

            if(data == null)
                return;

            if (data.Contains("UN1") || data.Contains("CU1") || data.Contains("CD1") || data.Contains("CGU1") || data.Contains("CGD1") || data.Contains("CH1"))
            {
                AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "ValidateData",
                    string.Format($"Channel Info: {data}"));

                ProcessChannelFeedback(data);
                return;
            }

            if (data.Contains("PR1"))
            {
                AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "ValidateData",
                    string.Format($"Case Power On: {data}"));
                _powerState = true;
                PollZone1();

            }

            if (data.Contains("PR0"))
            {
                AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "ValidateData",
                    string.Format($"Case Power Off: {data}"));
                _powerState = false;
            }

        }



        protected override bool CanSendCommand(CommandSet commandSet)
        {
            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "CanSendCommand", commandSet.Command);
            return base.CanSendCommand(commandSet);
        }

        protected override bool PrepareStringThenSend(CommandSet commandSet)
        {
            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "PrepareStringThenSend", commandSet.Command);

            if (!commandSet.CommandPrepared)
            {

                commandSet.Command = string.Format($"{commandSet.Command}\r");
                commandSet.CommandPrepared = true;
            }

            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "PrepareStringThenSend", commandSet.Command);
            return base.PrepareStringThenSend(commandSet);

        }

        protected override bool Send(CommandSet commandSet)
        {
            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "Send", commandSet.Command);
            return base.Send(commandSet);
        }


        protected override bool CanQueueCommand(CommandSet commandSet, bool powerOnCommandInQueue)
        {
            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "CanQueueCommand", commandSet.Command);
            return base.CanQueueCommand(commandSet, powerOnCommandInQueue);
        }

        public void PowerOn()
        {
            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "PowerOn", "");
            var command = new CommandSet("PowerOn", "*PR1", CommonCommandGroupType.Power, null, false,
                CommandPriority.Normal, StandardCommandsEnum.PowerOn);
            SendCommand(command);
        }

        public void PowerOff()
        {
            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "PowerOff", "");
            var command = new CommandSet("PowerOff", "*PR0", CommonCommandGroupType.Power, null, false,
                CommandPriority.Normal, StandardCommandsEnum.PowerOn);
            SendCommand(command);
        }

        public void PollZone1()
        {
            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "PollZone1", "");
            var command = new CommandSet("Poll", "*QZ1", CommonCommandGroupType.Power, null, false,
                CommandPriority.Normal, StandardCommandsEnum.AvPoll);
            SendCommand(command);

        }

        public void ActivateUnsolicitedFeedback()
        {
            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "ActivateUnsolicitedFeedback", "");
            var command = new CommandSet("PowerOn", "*UN1", CommonCommandGroupType.Power, null, false,
                CommandPriority.Normal, StandardCommandsEnum.AvPoll);
            SendCommand(command);

        }
        public void ChanUp()
        {
            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "ChanUp", "");
            var command = new CommandSet("ChanUp", "*CU1", CommonCommandGroupType.Channel, null, false,
                CommandPriority.Normal, StandardCommandsEnum.ChannelUp);
            SendCommand(command);

        }

        public void ChanDn()
        {
            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "ChanDn", "");
            var command = new CommandSet("ChanDn", "*CD1", CommonCommandGroupType.Channel, null, false,
                CommandPriority.Normal, StandardCommandsEnum.ChannelDown);
            SendCommand(command);
        }

        public void CatUp()
        {
            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "CatUp", "");
            var command = new CommandSet("CatUp", "*CGU1", CommonCommandGroupType.Other, null, false,
                CommandPriority.Normal, StandardCommandsEnum.FSkip);
            SendCommand(command);
        }

        public void CatDn()
        {
            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "CatDn", "");
            var command = new CommandSet("CatDn", "*CGD1", CommonCommandGroupType.Other, null, false,
                CommandPriority.Normal, StandardCommandsEnum.RSkip);
            SendCommand(command);
        }

        public void KeypadButtonPressed(string key)
        {
            switch (key)
            {
                case "10":
                    AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "KeypadButtonPressed Switch", "Clear");
                    _keypadNumber.Clear();
                    break;
                case "11":
                    AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "KeypadButtonPressed Switch", string.Format($"Enter: {_keypadNumber}"));
                    ChannelSelect(_keypadNumber);
                    _keypadNumber.Clear();
                    break;
                default:
                    AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "KeypadButtonPressed Switch", key);
                    if (_keypadNumber.Length < 3)
                        _keypadNumber.Append(key);
                    break;
            }

            OnKeypadTextChanged(_keypadNumber.ToString());

        }

        public void ChannelSelect(StringBuilder num)
        {
            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "ChannelSelect", num.ToString());

            try
            {
                switch (num.Length)
                {
                    case 2:
                        num.Insert(0, '0');
                        break;
                    case 1:
                        num.Insert(0, "00");
                        break;
                    default:
                        break;
                }



            var command = new CommandSet("SetChannel", string.Format($"*CH1,{num}"), CommonCommandGroupType.Keypad, null, false,
                CommandPriority.Normal, StandardCommandsEnum.KeypadNumber);
            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "ChannelSelect", command.Command);
            SendCommand(command);
            }
            catch (Exception exception)
            {
                AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Error, "ChannelSelect", exception.Message);
            }


        }

        public void ProcessChannelFeedback(string fb)
        {
            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "ProcessChannelFeedback", fb);
            try
            {
                var info = fb.Split(',');

                if (CurrentChannel == info[1]) return;
                CurrentChannel = info[1];
                _currentChannelFeedback.ChannelNameNum = string.Format($"{info[1]}: {info[3]}");
                _currentChannelFeedback.Artist = info[4];
                _currentChannelFeedback.Category = info[2];
                _currentChannelFeedback.Song = info[5];


                CurrentTextChanged?.Invoke(this, _currentChannelFeedback);


            }
            catch (Exception exception)
            {
                AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Error, "ProcessChannelFeedback", exception.Message);
            }
        }

        public void OnKeypadTextChanged(string text)
        {
            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "KeypadButtonPressed", text);
            KeypadTextChanged?.Invoke(this, text);

        }

        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
        public class Preset
        {
            public string Name { get; set; }
            public string Channel { get; set; }

            public Preset(string name, string channel)
            {
                Name = name;
                Channel = channel;
            }
        }

        public void InitializePresets()
        {
            Presets = new List<Preset>
            {
                new Preset("Preset 1", "017"),
                new Preset("Preset 2", "016"),
                new Preset("Preset 3", "012"),
                new Preset("Preset 4", "025"),
                new Preset("Preset 5", "071"),
                new Preset("Preset 6", "067"),
                new Preset("Preset 7", "006"),
                new Preset("Preset 8", "026"),
                new Preset("Preset 9", "068"),
                new Preset("Preset 10", "066"),
                new Preset("Preset 11", "007"),
                new Preset("Preset 12", "027"),
                new Preset("Preset 13", "076"),
                new Preset("Preset 14", "075"),
                new Preset("Preset 15", "008"),
                new Preset("Preset 16", "018"),
                new Preset("Preset 17", "005"),
                new Preset("Preset 18", "941"),
                new Preset("Preset 19", "000"),
                new Preset("Preset 20", "000"),

            };

        }

    }
}