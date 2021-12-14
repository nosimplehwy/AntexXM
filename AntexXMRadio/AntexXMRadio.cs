using Crestron.RAD.Common.Enums;
using Crestron.RAD.Common.Interfaces;
using Crestron.RAD.Common.Interfaces.ExtensionDevice;
using Crestron.RAD.Common.Logging;
using Crestron.RAD.DeviceTypes.ExtensionDevice;
using System;
using System.Collections.Generic;
using System.Text;
using Crestron.RAD.Common.Attributes.Programming;
using Crestron.RAD.Common.Events;
using Crestron.RAD.Common.Transports;
using Crestron.RAD.ProTransports;



namespace AntexXMRadio
{
    public class AntexXmRadio : AExtensionDevice, ISerialComport, ISimpl
    {

        #region Fields

        private PropertyValue<string> _tileStatusText;
        private PropertyValue<string> _tileStatusIcon;
        private PropertyValue<string> _currentChannelNameNumText;

        private PropertyValue<string> _currentCategoryText;

        private PropertyValue<string> _currentArtistText;

        private PropertyValue<string> _currentSongText;
        private PropertyValue<string> _keypadText;
        private ClassDefinition _presetObject;
        private ObjectList _presetList;


        private AntexXmRadioProtocol _protocol;

        private SimplTransport _transport;

        public event EventHandler<ValueEventArgs<string>> CommandFired;


        #endregion Fields



        #region Constructor
        public AntexXmRadio()
        {

            EnableLogging = true;
            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "Constructor", "AntexXMRadio");


        }

        #endregion Constructor

        #region AExtensionDevice Members

        protected override IOperationResult DoCommand(string command, string[] parameters)
        {

            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "DoCommand", command);

            if (string.IsNullOrEmpty(command))
                return new OperationResult(OperationResultCode.Error, "command string is empty");

            switch (command)
            {
                case "ChanUpCommand":
                        AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "Switch", command);
                        ChanUp();
                        break;
                case "ChanDnCommand":
                        AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "Switch", command);
                        ChanDn();
                        break;
                case "CatUpCommand":
                    AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "Switch", command);
                    _protocol.CatUp();
                    break;
                case "CatDnCommand":
                    AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "Switch", command);
                    _protocol.CatDn();
                    break;
                case "KeypadButtonPressed":
                    AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "Switch", string.Format($"{command}: {parameters[0]}"));
                    if (string.IsNullOrEmpty(parameters[0]))
                        return new OperationResult(OperationResultCode.Error, "Number string is empty");

                    _protocol.KeypadButtonPressed(parameters[0]);
                    break;
                case "SelectPreset":
                    AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "Switch",
                        string.Format($"{command}: {parameters[0]}"));
                    _protocol.ChannelSelect(new StringBuilder(parameters[0]));
                    break;
                default:
                        AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "Switch", "Unhandled command!");
                        break;
            }

            return new OperationResult(OperationResultCode.Success);
        }

        protected override IOperationResult SetDriverPropertyValue<T>(string propertyKey, T value)
        {

            if (string.IsNullOrEmpty(propertyKey) || value == null)
                return new OperationResult(OperationResultCode.Error, "Property or value is null.");


            return new OperationResult(OperationResultCode.Success);
        }

        protected override IOperationResult SetDriverPropertyValue<T>(string objectId, string propertyKey, T value)
        {
            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "SetDriverPropertyValuewithObject", propertyKey);
            return new OperationResult(OperationResultCode.Error, "The property with object does not exist.");
        }

        #endregion AExtensionDevice Members

        #region  Transport


        public void Initialize(IComPort comPort)
        {
            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "ISerialComport Initialize", "AntexXMRadio");

            ConnectionTransport = new CommonSerialComport(comPort)
            {
                EnableLogging = InternalEnableLogging,
                CustomLogger = InternalCustomLogger,
                EnableRxDebug = InternalEnableRxDebug,
                EnableTxDebug = InternalEnableTxDebug
            };


            _protocol = new AntexXmRadioProtocol(ConnectionTransport, Id)
            {
                EnableLogging = InternalEnableLogging,
                CustomLogger = InternalCustomLogger
            };
            _protocol.RxOut += SendRxOut;
            _protocol.KeypadTextChanged += OnKeypadTextChanged;
            _protocol.CurrentTextChanged += OnCurrentTextChanged;
            _protocol.IsConnectedChanged += Protocol_IsConnectedChanged;
            _protocol.Initialize(DriverData);
            DeviceProtocol = _protocol;

            CreateDeviceDefinition();


        }



        public SimplTransport Initialize(Action<string, object[]> send)
        {
            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "Simpl Transport Initialize", "AntexXMRadio");

            _transport = new SimplTransport { Send = send };
            ConnectionTransport = _transport;
            ConnectionTransport.LogTxAndRxAsBytes = false;

            _protocol = new AntexXmRadioProtocol(ConnectionTransport, Id)
            {
                EnableLogging = InternalEnableLogging,
                CustomLogger = InternalCustomLogger
            };
            _protocol.RxOut += SendRxOut;
            _protocol.KeypadTextChanged += OnKeypadTextChanged;
            _protocol.CurrentTextChanged += OnCurrentTextChanged;
            _protocol.IsConnectedChanged += Protocol_IsConnectedChanged;
            _protocol.Initialize(DriverData);
            DeviceProtocol = _protocol;

            CreateDeviceDefinition();


            return _transport;


        }

        private void Protocol_IsConnectedChanged(object sender, ValueEventArgs<bool> e)
        {
            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "_protocol_IsConnectedChanged", e.Value.ToString());

            Connected = e.Value;
        }

        public override void Connect()
        {
            base.Connect();

           // Connected = _protocol.IsConnected;
            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "Connect", Connected.ToString());
            _protocol.PowerOn();

        }

        #endregion  Transport



        #region Programmable Operations

        [ProgrammableOperation]
        public void PowerOn()
        {
            _protocol.PowerOn();
        }

        [ProgrammableOperation]
        public void PowerOff()
        {
            _protocol.PowerOff();
        }

        [ProgrammableOperation]
        public void ChanUp()
        {
            _protocol.ChanUp();
        }

        [ProgrammableOperation]
        public void ChanDn()
        {
            _protocol.ChanDn();
        }

        [ProgrammableOperation]
        public void ActivateUnsolicitedFeedback()
        {
            _protocol.ActivateUnsolicitedFeedback();
        }

        [ProgrammableOperation]
        public void PollZone1()
        {
            _protocol.PollZone1();
        }
        #endregion Programmable Operations

        #region Programmable Events


        #endregion Programmable Events

        #region Private Methods
        private void CreateDeviceDefinition()
        {
            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "CreateDeviceDefinition", "");

            //Tile
            _tileStatusText = CreateProperty<string>(new PropertyDefinition("TileStatus", String.Empty, DevicePropertyType.String));
            _tileStatusIcon = CreateProperty<string>(new PropertyDefinition("MainIcon", String.Empty, DevicePropertyType.String));
            _currentChannelNameNumText =
                CreateProperty<string>(
                    new PropertyDefinition("ChannelNameNum", String.Empty, DevicePropertyType.String));
            _currentCategoryText =
                CreateProperty<string>(
                    new PropertyDefinition("Category", String.Empty, DevicePropertyType.String));
            _currentArtistText =
                CreateProperty<string>(
                    new PropertyDefinition("Artist", String.Empty, DevicePropertyType.String));
            _currentSongText =
                CreateProperty<string>(
                    new PropertyDefinition("Song", String.Empty, DevicePropertyType.String));
            _keypadText =
                CreateProperty<string>(
                    new PropertyDefinition("KeypadText", String.Empty, DevicePropertyType.String));

            // Define example object 
            _presetObject = CreateClassDefinition("PresetObject");
            _presetObject.AddProperty(new PropertyDefinition("Name", string.Empty, DevicePropertyType.String));
            _presetObject.AddProperty(new PropertyDefinition("Channel", string.Empty, DevicePropertyType.String));

            // Define example list 
            _presetList = CreateList(new PropertyDefinition("PresetsList", string.Empty, DevicePropertyType.ObjectList, _presetObject));
            foreach (var preset in _protocol.Presets)
            {
                var tempObject = CreateObject(_presetObject);
                tempObject.GetValue<string>("Name").Value = preset.Name;
                tempObject.GetValue<string>("Channel").Value = preset.Channel;
                _presetList.AddObject(tempObject);

            }


            //Initialize property values
            _tileStatusText.Value = "Status Text";
            _tileStatusIcon.Value = "icPlaceholder";
            _currentChannelNameNumText.Value = "Channel Name : 000";
            _currentCategoryText.Value = "Category";
            _currentArtistText.Value = "Artist Name";
            _currentSongText.Value = "This is the name of the Song.";
            _keypadText.Value = "000";

            Commit();
        }




        #endregion Private Methods

        #region Helper Methods
        private void OnKeypadTextChanged(object sender, string e)
        {
            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "OnKeypadTextChanged", e);
            _keypadText.Value = e;
            Commit();
        }

        private void OnCurrentTextChanged(object sender, AntexXmRadioProtocol.TextChangedEventArgs e)
        {
            _currentChannelNameNumText.Value = e.ChannelNameNum;
            _currentCategoryText.Value = e.Category;
            _currentArtistText.Value = e.Artist;
            _currentSongText.Value = e.Song;

            Commit();


        }

        #endregion

        #region Events
        protected void OnCommandFired(string command)
        {
            AntexXmRadioLog.Log(EnableLogging, Log, LoggingLevel.Debug, "OnCommandFired", "");
            CommandFired?.Invoke(this, new ValueEventArgs<string>(command));
        }


        #endregion Events


    }

}
