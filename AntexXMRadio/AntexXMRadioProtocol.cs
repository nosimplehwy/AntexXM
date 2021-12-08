using Crestron.RAD.Common.BasicDriver;
using Crestron.RAD.Common.Logging;
using Crestron.RAD.Common.Transports;
using System;
using System.Text;
using Crestron.RAD.Common.Enums;

namespace AntexXMRadio
{
    public class AntexXmRadioProtocol : ABaseDriverProtocol
    {

        private StringBuilder _keypadNumber;
        private TextChangedEventArgs _currentChannelFeedback;
        private bool _powerState;

        public event EventHandler<BoolAttributeChangedEventArgs> BoolAttributeChanged;
        public event EventHandler<StringAttributeChangedEventArgs> StringAttributeChanged;
        public event EventHandler<TextChangedEventArgs> CurrentTextChanged;
        public event EventHandler<string> KeypadTextChanged;

        public AntexXmRadioProtocol(ISerialTransport transport, byte id) : base(transport, id)
        {
            EnableLogging = true;
            _keypadNumber = new StringBuilder(3);
            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "AntexXMRadioProtocol", "");

            PowerOn();
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
            switch (rx)
            {
                case "*PR1":
                    _powerState = true;
                    break;
                case "*PR0":
                    _powerState = false;
                    break;

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

        public void Poll()
        {
        }

        public void ActivateUnsolicitedFeedback()
        {
        }
        public void ChanUp()
        {
            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "ChanUp", "");
            //var command = new CommandSet("ChanUp", "*CU1", CommonCommandGroupType.Channel, null, false,
            //    CommandPriority.Normal, StandardCommandsEnum.ChannelUp);
            //SendCommand(command);

            SendCustomCommandValue("*CU1");
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
            _currentChannelFeedback.Artist = "Artist";
            _currentChannelFeedback.Category = "Category";
            _currentChannelFeedback.ChannelNameNum = "000 Channel Name";
            _currentChannelFeedback.Song = "Song Title";


            CurrentTextChanged?.Invoke(this, _currentChannelFeedback);
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
        }


    }
}