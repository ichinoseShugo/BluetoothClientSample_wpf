using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Windows.Devices.Enumeration;
using Windows.Foundation;

namespace BluetoothClientSample_wpf
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 通信可能なデバイス一覧
        /// </summary>
        public ObservableCollection<RfcommDeviceDisplay> ResultCollection { get; private set; }
        private DeviceWatcher deviceWatcher;
        private BluetoothClient bluetoothClient = new BluetoothClient();

        public MainWindow()
        {
            InitializeComponent();
            ResultCollection = new ObservableCollection<RfcommDeviceDisplay>();
        }

        //Windowが開くときに呼び出されるイベント
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            //viewのリストと変数のリストを紐づける
            resultsListView.DataContext = ResultCollection;
        }

        //Windowが閉じる時に呼び出されるイベント
        private void WindowClosing(object sender, CancelEventArgs e)
        {

        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            bluetoothClient.Send();
        }
        
        private void ReadButton_Click(object sender, RoutedEventArgs e)
        {
            //Console.WriteLine(1);
            bluetoothClient.Receive();
            Console.WriteLine(1);
            bluetoothClient.Send();
            Console.WriteLine(2);
            bluetoothClient.Start();
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            bluetoothClient.Disconnect();
            ResetMainUI();
        }

        //Bluetooth接続機能に関する部分
        #region Connect   
        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            //デバイス一覧からデバイスが選択されているか確認
            if (resultsListView.SelectedItem == null)
            {
                MessageBox.Show("接続先デバイスが選択されてないよ");
                return;
            }

            bluetoothClient.Connect(resultsListView.SelectedItem as RfcommDeviceDisplay);

            StopWatcher();

            ReadButton.IsEnabled = true;
            SendButton.IsEnabled = true;
            DisconnectButton.IsEnabled = true;
        }

        //接続候補のリストのアイテム選択時に発生するイベントハンドラ
        private void ResultsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePairingButtons();
        }

        private void UpdatePairingButtons()
        {
            RfcommDeviceDisplay deviceDisp = (RfcommDeviceDisplay)resultsListView.SelectedItem;

            if (null != deviceDisp)
            {
                ConnectButton.IsEnabled = true;
            }
            else
            {
                ConnectButton.IsEnabled = false;
            }
        }
        #endregion

        //接続可能なデバイス一覧の取得と表示UI操作に関する部分
        #region DeviceWatcher  
        private void EnumerateButton_Click(object sender, RoutedEventArgs e)
        {
            if (deviceWatcher == null)
            {
                //リスト表示のUIの初期化処理
                SetDeviceWatcherUI();
                //デバイス一覧を取得してリストに表示する命令
                StartUnpairedDeviceWatcher();
            }
            else
            {
                //リストの一覧表示をリセットする
                ResetMainUI();
            }
        }

        private void SetDeviceWatcherUI()
        {
            // Disable the button while we do async operations so the user can't Run twice.
            EnumerateButton.Content = "Stop";
            //StatusMessage.Text = "Enumerate Start";
            resultsListView.Visibility = Visibility.Visible;
            resultsListView.IsEnabled = true;
        }

        private void StartUnpairedDeviceWatcher()
        {
            // Request additional properties
            string[] requestedProperties = new string[] { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };

            deviceWatcher = DeviceInformation.CreateWatcher("(System.Devices.Aep.ProtocolId:=\"{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}\")",
                                                            requestedProperties,
                                                            DeviceInformationKind.AssociationEndpoint);

            //接続可能なデバイス候補が出現した際に呼び出されるイベントハンドラ
            deviceWatcher.Added += new TypedEventHandler<DeviceWatcher, DeviceInformation>(async (watcher, deviceInfo) =>
            {
                // Since we have the collection databound to a UI element, we need to update the collection on the UI thread.
                await Dispatcher.BeginInvoke(
                 new Action(() =>
                 {
                     // Make sure device name isn't blank
                     if (deviceInfo.Name != "")
                     {
                         Console.WriteLine(deviceInfo.Name);
                         ResultCollection.Add(new RfcommDeviceDisplay(deviceInfo));
                     }
                 }
                ));
            });

            //デバイス候補が更新されるたびに呼び出されるイベントハンドラ
            deviceWatcher.Updated += new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>(async (watcher, deviceInfoUpdate) =>
            {
                await Dispatcher.BeginInvoke(
                new Action(() => {
                    foreach (RfcommDeviceDisplay rfcommInfoDisp in ResultCollection)
                    {
                        if (rfcommInfoDisp.Id == deviceInfoUpdate.Id)
                        {
                            rfcommInfoDisp.Update(deviceInfoUpdate);
                            break;
                        }
                    }
                }
                ));
            });

             //デバイス一覧の表示が終了した際に呼び出されるイベントハンドラ（現段階では使用していない）
            deviceWatcher.EnumerationCompleted += new TypedEventHandler<DeviceWatcher, Object>(async (watcher, obj) =>
            {
                await Dispatcher.BeginInvoke(
                new Action(() => {
           
                }
                ));
            });
            
            //一覧から削除された際に呼び出されるイベントハンドラ
            deviceWatcher.Removed += new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>(async (watcher, deviceInfoUpdate) =>
            {
                // Since we have the collection databound to a UI element, we need to update the collection on the UI thread.
                await Dispatcher.BeginInvoke(
                new Action(() => {
                    // Find the corresponding DeviceInformation in the collection and remove it
                    foreach (RfcommDeviceDisplay rfcommInfoDisp in ResultCollection)
                    {
                        if (rfcommInfoDisp.Id == deviceInfoUpdate.Id)
                        {
                            ResultCollection.Remove(rfcommInfoDisp);
                            break;
                        }
                    }
                }));
            });

            //デバイス一覧の列挙が停止した際に呼び出されるイベントハンドラ
            deviceWatcher.Stopped += new TypedEventHandler<DeviceWatcher, Object>(async (watcher, obj) =>
            {
                await Dispatcher.BeginInvoke(
                new Action(() => {
                    ResultCollection.Clear();
                }));
            });

            deviceWatcher.Start();
        }

        private void ResetMainUI()
        {
            EnumerateButton.Content = "Start";
            ConnectButton.Visibility = Visibility.Visible;
            resultsListView.Visibility = Visibility.Visible;
            resultsListView.IsEnabled = true;

            // Re-set device specific UX
            //RequestAccessButton.Visibility = Visibility.Collapsed;
            StopWatcher();
        }

        private void StopWatcher()
        {
            if (null != deviceWatcher)
            {
                if ((DeviceWatcherStatus.Started == deviceWatcher.Status ||
                     DeviceWatcherStatus.EnumerationCompleted == deviceWatcher.Status))
                {
                    deviceWatcher.Stop();
                }
                deviceWatcher = null;
            }
        }
        #endregion
    }

    //接続先候補のリストを表すクラス
    #region DeviceDisplay
    public class RfcommDeviceDisplay : INotifyPropertyChanged
    {
        private DeviceInformation deviceInfo;

        public RfcommDeviceDisplay(DeviceInformation deviceInfoIn)
        {
            deviceInfo = deviceInfoIn;
        }

        public DeviceInformation DeviceInformation
        {
            get
            {
                return deviceInfo;
            }

            private set
            {
                deviceInfo = value;
            }
        }

        public string Id
        {
            get
            {
                return deviceInfo.Id;
            }
        }

        public string Name
        {
            get
            {
                return deviceInfo.Name;
            }
        }

        public void Update(DeviceInformationUpdate deviceInfoUpdate)
        {
            deviceInfo.Update(deviceInfoUpdate);
            OnPropertyChanged("Name");
            //デバイスサムネ画像はいらないのでコメントアウト
            //UpdateGlyphBitmapImage();
        }

        /*
        private async void UpdateGlyphBitmapImage()
        {
            DeviceThumbnail deviceThumbnail = await deviceInfo.GetGlyphThumbnailAsync();
            BitmapImage glyphBitmapImage = new BitmapImage();
            glyphBitmapImage.(deviceThumbnail);
            GlyphBitmapImage = glyphBitmapImage;
            OnPropertyChanged("GlyphBitmapImage");
         }
        */

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
    #endregion
}
