using ElecProj.ViewModel;
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
        int Count = 0;
        public MainWindow()
        {
            InitializeComponent();

            //MainViewModel mainViewModel = new MainViewModel();
            //this.DataContext = mainViewModel;

            WebCurrent.Navigate(new Uri(System.IO.Directory.GetCurrentDirectory() + "/area.html"));
            WebVolt.Navigate(new Uri(System.IO.Directory.GetCurrentDirectory() + "/Volt.html"));
            this.initWebBrowser();
            initSerialPort();
        }

        /// <summary>
        /// 初始化串口
        /// </summary>
        private void initSerialPort()
        {
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
                    BinaryReader r = new BinaryReader(fs);
                    long length = r.BaseStream.Length;//获取数据长度
                    int count = Convert.ToInt32(length);
                    Count = count;
                    while (fs.Read(chrData, 0, 2) > 0)
                    {
                        fs.Flush();

                        //把两个字节转换为无符号整数
                        listCurrent.Add(BitConverter.ToInt16(chrData, 0));
                        //mark point 7FFF —— 32767
                        if (listCurrent[listCurrent.Count - 1] == 32767 || listCurrent[listCurrent.Count - 1] == -32768)
                        {
                            listCurrent.RemoveAt(listCurrent.Count - 1);
                        }
                    }
                    double offset = 0;
                    //取两个周期的数据
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

            listVolt.Clear();
            listVoltPlot.Clear();
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "文本文件|*.dat;*.DAT";
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                byte[] chrData = new byte[2];
                using (FileStream fs = new FileStream(openFileDialog.FileName, FileMode.Open))
                {
                    while (fs.Read(chrData, 0, 2) > 0)
                    {
                        fs.Flush();
                        listVolt.Add(BitConverter.ToInt16(chrData, 0));
                        if (listVolt[listVolt.Count - 1] == 32767 || listVolt[listVolt.Count - 1] == -32768)
                        {
                            listVolt.RemoveAt(listVolt.Count - 1);
                        }
                    }
                }
                double offset = 0;
                int period = Convert.ToInt32(TextBoxPeriod.Text);
                for (int i = 10000 * period; i < 30000; i += (40 * period))
                {
                    offset += listVolt[i];
                }
                offset /= 20000.0 / 40 / period;

                //for (int i = 4000 * period; i < listVolt.Count; i += 4000 * period)
                //{
                //    offset += listVolt[i - 1] + listVolt[i - 11] + listVolt[i - 21] + listVolt[i - 31];
                //    offset += listVolt[i - 2000 * period - 1] + listVolt[i - 2000 * period - 11] + listVolt[i - 2000 * period - 21] + listVolt[i - 2000 * period - 31];
                //}
                //offset /= listVolt.Count / 500 / period;

                for (int i = 0; i < listVolt.Count; i++)
                {
                    listVoltPlot.Add(new double[] { i, (listVolt[i] - offset) * 5000.0 / 32768 });
                }
                string yAxis = JsonConvert.SerializeObject(listVoltPlot);
                WebVolt.InvokeScript("jsPushData", yAxis);
            }
        }

        /// <summary>
        /// 装置系数K（采用中梯装置）
        /// </summary>
        /// <param name="AM"></param>
        /// <param name="AN"></param>
        /// <param name="BM"></param>
        /// <param name="BN"></param>
        /// <returns></returns>
        private double CentralGradientK(double AM, double AN, double BM, double BN)
        {
            return 2 * Math.PI * AM * AN * BM * BN / (AN - AM) / (AM * AN + BM * BN);
        }

        /// <summary>
        /// 计算视极化率
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GetApparentPolarizabilityClick(object sender, RoutedEventArgs e)
        {
            //断电延时
            int delay = Convert.ToInt32(TextBoxCutDelay.Text);
            //取样宽度
            int interval = Convert.ToInt32(TextBoxCutInterval.Text);
            //供电时长
            int period = Convert.ToInt32(TextBoxPeriod.Text);
            List<double> apparentPolarizability1 = new List<double>();
            List<double> apparentPolarizability2 = new List<double>();
            List<double> apparentPolarizability3 = new List<double>();
            List<double> apparentPolarizability4 = new List<double>();
            List<double> firstField = new List<double>();
            List<double> secondField = new List<double>();
            List<double> thirdField = new List<double>();
            List<double> fourthField = new List<double>();

            for (int i = 3000 * period; i < 23000 * period; i += 2000 * period)
            {

                double totalField = (listVoltPlot[i - 1][1] + listVoltPlot[i - 2][1] + listVoltPlot[i - 3][1] + listVoltPlot[i - 4][1]) / 4;
                for (int j = i; j < i + interval; j++)
                {
                    firstField.Add(listVoltPlot[j + delay - 1][1]);
                }
                for (int j = i + interval; j < i + interval * 3; j++)
                {
                    secondField.Add(listVoltPlot[j + delay - 1][1]);
                }
                for (int j = i + interval * 3; j < i + interval * 7; j++)
                {
                    thirdField.Add(listVoltPlot[j + delay - 1][1]);
                }
                for (int j = i + interval * 7; j < i + interval * 15; j++)
                {
                    fourthField.Add(listVoltPlot[j + delay - 1][1]);
                }
                apparentPolarizability1.Add(firstField[0] / totalField);
                apparentPolarizability2.Add(secondField[0] / totalField);
                apparentPolarizability3.Add(thirdField[0] / totalField);
                apparentPolarizability4.Add(fourthField[0] / totalField);

                firstField.Clear();
                secondField.Clear();
                thirdField.Clear();
                fourthField.Clear();


            }
            System.Globalization.NumberFormatInfo provider = new System.Globalization.NumberFormatInfo();
            provider.PercentDecimalDigits = 2;//小数点保留几位数.   
            provider.PercentPositivePattern = 1;//百分号出现在何处.   
            var text1 = apparentPolarizability1.Average().ToString("P", provider);
            var text2 = apparentPolarizability2.Average().ToString("P", provider);
            var text3 = apparentPolarizability3.Average().ToString("P", provider);
            var text4 = apparentPolarizability4.Average().ToString("P", provider);

            TextBlockApparentPolarizability.Text = text1 + "," + text2 + "," + text3 + "," + text4;
        }



        /// <summary>
        /// 计算视电阻率
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GetApparentResistivityClick(object sender, RoutedEventArgs e)
        {
            List<double> apprentResistivityMax = new List<double>();
            List<double> apprentResistivityMin = new List<double>();
            List<double> totalcurrent = new List<double>();
            List<double> firstField = new List<double>();
            List<double> secondField = new List<double>();
            List<double> thirdField = new List<double>();
            List<double> fourthField = new List<double>();
            double current = 0.0;
            int period = 2000;
            //for (int i = 1000 ; i < 5000; i += 4000 )
            //{
            for (int j = 1000; j < 5000; j += 4000)
            {
                double totalField = (listVoltPlot[j - 1][1] + listVoltPlot[j - 2][1] + listVoltPlot[j - 3][1] + listVoltPlot[j - 4][1]) / 4;
                double second = (listVoltPlot[j + 1][1] + listVoltPlot[j + 2][1] + listVoltPlot[j + 3][1] + listVoltPlot[j + 4][1]) / 4;
                apprentResistivityMax.Add(totalField - second);
                double totalField2 = (listVoltPlot[j + period - 1][1] + listVoltPlot[j + period - 2][1] + listVoltPlot[j + period - 3][1] + listVoltPlot[j + period - 4][1]) / 4;
                double second2 = (listVoltPlot[j + period + 1][1] + listVoltPlot[j + period + 2][1] + listVoltPlot[j + period + 3][1] + listVoltPlot[j + period + 4][1]) / 4;
                apprentResistivityMin.Add(totalField2 - second2);
                current = (listCurrentPlot[j - 1][1] + listCurrentPlot[j - 2][1] + listCurrentPlot[j - 3][1] + listCurrentPlot[j - 4][1]) / 4;

            }
            //}

             for (int j = 250; j < 2000; j += 1000)
            {
                var cur = listCurrentPlot[j][1];
                current = (listCurrentPlot[j - 1][1] + listCurrentPlot[j - 2][1] + listCurrentPlot[j - 3][1] + listCurrentPlot[j - 4][1]) / 4;
                totalcurrent.Add(current);

            }


            //一次场电压
            var result = (apprentResistivityMax.Max() + Math.Abs(apprentResistivityMin.Max())) / 2;
            var k = TextBlockK.Text  ;
            var res = Convert.ToDouble(k) * result / totalcurrent.Average();

            TextBlockP.Text = res.ToString("0.00");
        }
        
        /// <summary>
        /// 计算装置系数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GetDeviceFactorClick(object sender, RoutedEventArgs e)
        {

            double K = CentralGradientK(Convert.ToDouble(TextBoxCentralGradientAM.Text), Convert.ToDouble(TextBoxCentralGradientAN.Text), Convert.ToDouble(TextBoxCentralGradientBM.Text), Convert.ToDouble(TextBoxCentralGradientBN.Text));
            TextBlockK.Text = K.ToString("0.00");
        }
    
    }
}
