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


        List<short> listVolt = new List<short>();
        List<double[]> listVoltPlot = new List<double[]>();
        List<short> listCurrent = new List<short>();
        List<double[]> listCurrentPlot = new List<double[]>();

        public MainWindow()
        {
            InitializeComponent();
            WebCurrent.Navigate(new Uri(System.IO.Directory.GetCurrentDirectory() + "/area.html"));
            WebVolt.Navigate(new Uri(System.IO.Directory.GetCurrentDirectory() + "/area.html"));

            initSerialPort();
           // bt_stopSend.IsEnabled = false;
        }

        private void initSerialPort() {
            //获取当前计算机的串行端口名的数组。
            string[] ports = SerialPort.GetPortNames();
            for (int index = 0; index < ports.Length; index++)
            {
                cb_SerialPortNumber.Items.Add(ports[index]);//添加item
                cb_SerialPortNumber.SelectedIndex = index; //设置显示的item索引
            }
            bt_send.IsEnabled = false;
        }

        /// <summary>
        /// 打开发射机文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenTransmitterClick(object sender, RoutedEventArgs e)
        {
            listCurrent.Clear();
            listCurrentPlot.Clear();
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "文本文件|*.dat;*.DAT";
            byte[] chrData = new byte[2];
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                using (FileStream fs = new FileStream(openFileDialog.FileName, FileMode.Open))
                {
                    while (fs.Read(chrData, 0, 2) > 0)
                    {
                        fs.Flush();
                        //把两个字节转换为无符号整数
                        listCurrent.Add(BitConverter.ToInt16(chrData, 0));
                        if (listCurrent[listCurrent.Count - 1] == 32767 || listCurrent[listCurrent.Count - 1] == -32768)
                        {
                            listCurrent.RemoveAt(listCurrent.Count - 1);
                        }
                    }
                    double offset = 0;
                    for (int i = 10000; i < 18000; i += 40)
                    {
                        offset += listCurrent[i];
                    }
                    offset /= 200.0;
                    for (int i = 0; i < listCurrent.Count; i += 4)
                    {
                        listCurrentPlot.Add(new double[] { i, (offset - listCurrent[i]) / 160.0 });
                    }
                    string yAxis = JsonConvert.SerializeObject(listCurrentPlot);
                    WebCurrent.InvokeScript("jsPushData", yAxis);
                }
            }
        }
    

        /// <summary>
        /// 打开接收机文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenReceiverClick(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// 计算装置系数K
        /// </summary>
        /// <param name="AM"></param>
        /// <param name="AN"></param>
        /// <param name="BM"></param>
        /// <param name="BN"></param>
        /// <returns></returns>
        private double CentralGradientK(double AM, double AN, double BM, double BN)
        {
            return 2 * Math.PI * AM * AN * BM * BN / (AN - AM) / (AM * AN - BM * BN);
        }

      
    }
}
