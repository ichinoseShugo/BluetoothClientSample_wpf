using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Windows;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace BluetoothClientSample_wpf
{
    //BluetoothClient(子機)側のサンプルコード
    class BluetoothClient
    {
        private RfcommDeviceService ConnectService = null;
        private StreamSocket ConnectSocket = null;
        private BluetoothDevice bluetoothDevice;
        
        #region Connect   
        public async void Connect(RfcommDeviceDisplay deviceInfoDisp)
        {

            try
            {
                bluetoothDevice = await BluetoothDevice.FromIdAsync(deviceInfoDisp.Id);
            }
            catch
            {
                //rootPage.NotifyUser(ex.Message, NotifyType.ErrorMessage);
                return;
            }
            // If we were unable to get a valid Bluetooth device object,
            // it's most likely because the user has specified that all unpaired devices
            // should not be interacted with.

            // This should return a list of uncached Bluetooth services (so if the server was not active when paired, it will still be detected by this call
            Guid RfcommChatServiceUuid = Guid.Parse("17fcf242-f86d-4e35-805e-546ee3040b84");
            var rfcommServices = await bluetoothDevice.GetRfcommServicesForIdAsync(
                RfcommServiceId.FromUuid(RfcommChatServiceUuid), BluetoothCacheMode.Uncached);

            if (rfcommServices.Services.Count > 0)
            {
                ConnectService = rfcommServices.Services[0];
            }
            else
            {
                return;
            }

            // Do various checks of the SDP record to make sure you are talking to a device that actually supports the Bluetooth Rfcomm Chat Service
            UInt16 SdpServiceNameAttributeId = 0x100;
            var attributes = await ConnectService.GetSdpRawAttributesAsync();
            if (!attributes.ContainsKey(SdpServiceNameAttributeId))
            {
                MessageBox.Show("sdpAttributeがおかしい");
                return;
            }

            byte SdpServiceNameAttributeType = (4 << 3) | 5;
            var attributeReader = DataReader.FromBuffer(attributes[SdpServiceNameAttributeId]);
            var attributeType = attributeReader.ReadByte();
            if (attributeType != SdpServiceNameAttributeType)
            {
                MessageBox.Show("sdpNameAttributeがおかしい");
                return;
            }
            var serviceNameLength = attributeReader.ReadByte();

            lock (this) //lock構文、排他制御
            {
                ConnectSocket = new StreamSocket();
            }

            try
            {
                await ConnectSocket.ConnectAsync(ConnectService.ConnectionHostName, ConnectService.ConnectionServiceName);
            }
            catch (Exception ex) when ((uint)ex.HResult == 0x80072740) // WSAEADDRINUSE
            {
                MessageBox.Show("socket接続がおかしい");
            }
        }
        #endregion

        //接続切断命令
        public void Disconnect()
        {
            if (ConnectService != null)
            {
                ConnectService.Dispose();
                ConnectService = null;
            }

            if (ConnectSocket.InputStream != null)
            {
                ConnectSocket.InputStream.Dispose();
            }

            if (ConnectSocket.OutputStream != null)
            {
                ConnectSocket.OutputStream.Dispose();
            }

            if (ConnectSocket != null)
            {
                ConnectSocket.Dispose();
                ConnectSocket = null;
            }

        }

        public async void Send()
        {
            // There's no need to send a zero length message
            // Make sure that the connection is still up and there is a message to send
            if (ConnectSocket != null)
            {
                //7文字(7バイト)のデータ
                string data = "ABCDEFG";
                //バイトデータの文字コードを変更(androidを想定してUTF8に変更しているが変更の必要があるかどうかは未実験、必要ないかも)
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(data);
                //OutputStreamに文字列を送信
                await ConnectSocket.OutputStream.WriteAsync(bytes.AsBuffer());
            }
        }

        public async void Receive()
        {
            try
            {
                if (ConnectSocket != null)
                {
                    byte[] buffer = new byte[120];
                    //InputStreamのデータを変数bufferに格納
                    await ConnectSocket.InputStream.ReadAsync(buffer.AsBuffer(), 120, InputStreamOptions.Partial);
                    //受信したbyteデータを文字列に変換
                    string str = Encoding.GetEncoding("ASCII").GetString(buffer);
                    MessageBox.Show("" + str);
                }
            }
            catch
            {
                lock (this)
                {
                    if (ConnectSocket == null)
                    {
                        // Do not print anything here -  the user closed the sock
                    }
                    else
                    {
                        Disconnect();
                    }
                }
            }
        }
    }//class
}//namespace
