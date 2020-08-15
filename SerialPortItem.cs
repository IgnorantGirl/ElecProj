using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace ElecProj
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public SerialPort _serialPort = new SerialPort();

        /// <summary>
        /// 初始化
        /// </summary>
        public void initialize()
        {
            //关闭串口时回抛异常
            try
            {
                _serialPort.PortName = cb_SerialPortNumber.SelectedItem.ToString();//串口号
                ComboBoxItem seletedItem = (ComboBoxItem)this.cb_BaudRate.SelectedItem;
                _serialPort.BaudRate = Convert.ToInt32(seletedItem.Content.ToString());//波特率
                _serialPort.DataBits = 8;//数据位
                _serialPort.StopBits = StopBits.One;//停止位
                _serialPort.Parity = Parity.None;//校验位
            }
            catch
            {

            }
        }

        /// <summary>
        /// /串口开关
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bt_SerialSwitch_Click(object sender, RoutedEventArgs e)
        {
            initialize();
            string strContent = this.bt_SerialSwitch.Content.ToString();
            if (strContent == "打开串口")
            {
                try
                {
                    _serialPort.Open();
                    _serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);//添加数据接收事件
                    //_serialPort.DataReceived += DataReceivedHandler;
                    bt_SerialSwitch.Content = "关闭串口";
                    tb_switchStatus.Text = "串口状态 ";
                    bt_send.IsEnabled = true;
                    // bt_stopSend.IsEnabled = true;
                    e_status.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF0000"));
                }
                catch { }
            }
            else
            {
                try
                {
                    _serialPort.DataReceived -= DataReceivedHandler;
                    _serialPort.Close();
                    bt_SerialSwitch.Content = "打开串口";
                    tb_switchStatus.Text = "串口状态 ";
                    e_status.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#000000"));
                    bt_send.IsEnabled = false;
                    //bt_stopSend.IsEnabled = false;
                }
                catch
                {

                }
            }
        }

        /// <summary>
        /// 接收数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            int len = this._serialPort.BytesToRead;
            byte[] buffer = new byte[len];
            this._serialPort.Read(buffer, 0, len);
            string strData = BitConverter.ToString(buffer, 0, len);
            Dispatcher.Invoke(() =>
            {
                this.tb_receiveData.Text += strData;
                this.tb_receiveData.Text += "-";
            });
        }


        private void bt_send_Click(object sender, RoutedEventArgs e)//发送按钮
        {
            string SendData = tb_SendData.Text;
            byte[] Data = new byte[20];
            for (int i = 0; i < SendData.Length / 2; i++)
            {
                //每次取两位字符组成一个16进制
                Data[i] = Convert.ToByte(tb_SendData.Text.Substring(i * 2, 2), 16);
            }
            this._serialPort.Write(Data, 0, Data.Length);
        }
       
        /// <summary>
        /// 清空接受数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearReceiveData_Click(object sender, RoutedEventArgs e)
        {
            tb_receiveData.Text = "";
        }
    }
}
