﻿using Crestron.RAD.Common.BasicDriver;
using Crestron.RAD.Common.Logging;
using Crestron.RAD.Common.Transports;
using System;
using System.Collections.Generic;
using System.Text;
using Crestron.RAD.Common.Enums;
using Crestron.SimplSharpPro.Thermostats;

namespace AntexXMRadio
{
    public class AntexXmRadioProtocol : ABaseDriverProtocol
    {

        private StringBuilder _keypadNumber;
        private StringBuilder _header;
        private StringBuilder _data;
        private TextChangedEventArgs _currentChannelFeedback;
        private bool _powerState;
        private const char EndOfResponse = '\r';
        private const char StartOfResponse = '*';

        private bool _findingHeader;
        private bool _findingChannelFeedback;
        private List<Preset> _presets;


        public event EventHandler<BoolAttributeChangedEventArgs> BoolAttributeChanged;
        public event EventHandler<StringAttributeChangedEventArgs> StringAttributeChanged;
        public event EventHandler<TextChangedEventArgs> CurrentTextChanged;
        public event EventHandler<string> KeypadTextChanged;

        public List<Preset> Presets
        {
            get => _presets;
            private set => _presets = value;
        }

        public AntexXmRadioProtocol(ISerialTransport transport, byte id) : base(transport, id)
        {
            _keypadNumber = new StringBuilder(3);
            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "AntexXMRadioProtocol", "");
            InitializePresets();
        }

        protected override void ConnectionChangedEvent(bool connection)
        {
            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Error, "ConnectionChangedEvent",
                connection.ToString());
        }

        protected override void ConnectionChanged(bool connection)
        {
            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Error, "ConnectionChanged", connection.ToString());
            base.ConnectionChanged(connection);

            if (!IsConnected)
                return;

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
            foreach (var chr in rx)
            {

                AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "DataHandler",
                    string.Format($"Chr: {chr}"));
                if(chr == StartOfResponse)
                {
                        AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "DataHandler",
                            string.Format($"Found Header"));
                        _findingHeader = true;
                        _data.Clear();
                        continue;
                }
                if (_findingHeader == true)
                {
                    switch (_data.ToString())
                    {
                        case "PR1":
                            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "DataHandler",
                                string.Format($"Case Power On: {_header}"));
                            _powerState = true;
                            _findingHeader = false;
                            continue;
                        case "PR0":
                            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "DataHandler",
                                string.Format($"Case Power Off: {_header}"));
                            _powerState = false;
                            _findingHeader = false;
                            continue;
                        case "UN1":
                            _findingChannelFeedback = true;
                            _findingHeader = false;
                            continue;
                        default:
                            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "DataHandler",
                                string.Format($"No header match found: {_header}"));
                            continue;

                    }
                }
                
                
                if (_findingChannelFeedback && chr == EndOfResponse)
                {
                    ProcessChannelFeedback(_data.ToString());
                    AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "DataHandler",
                        string.Format($"Ready to process channel feedback: {_data}"));
                    _findingChannelFeedback = false;
                    continue;
                }

                _data.Append(chr);
                AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "DataHandler",
                    string.Format($"Found Channel Info: {_data}"));

            }
        }


        protected override void ChooseDeconstructMethod(ValidatedRxData validatedData)
        {
            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Error, "ChooseDeconstructMethod", validatedData.Data);
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
            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "ProcessChannelFeedback", "");
            try
            {
                var info = fb.Split(',');
                _currentChannelFeedback.ChannelNameNum = string.Format($"{info[0]}: {info[2]}");
                _currentChannelFeedback.Artist = info[3];
                _currentChannelFeedback.Category = info[1];
                _currentChannelFeedback.Song = info[4];


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
            _presets = new List<Preset>
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