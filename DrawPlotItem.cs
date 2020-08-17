using ElecProj.Tools;
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
using Path = System.IO.Path;

namespace ElecProj
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        string str = System.Environment.CurrentDirectory;

        static double timeInterval = 1.0 / 1000;
        double[] i;
        double[] v1;
        double[] v2;
        double[] v3;
        int lengthChann = 0;
        String test11 = "";
        String days = "";
        string fileUrl = "";

        private void getFile(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = true;//该值确定是否可以选择多个文件
            dialog.Title = "请选择文件夹";
            dialog.Filter = "所有文件(*.*)|*.*";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                fileUrl = dialog.FileName;
                // MessageBox.Show("" + fileUrl);
            }
            lengthChann = getData(fileUrl);
            InvokeTestMethod(datatoString(i), dateToString(), "StringToJs");
            InvokeTestMethod(datatoString(v1), dateToString(), "getChannel2Data");
            InvokeTestMethod(datatoString(v2), dateToString(), "getChannel3Data");
            InvokeTestMethod(datatoString(v3), dateToString(), "getChannel4Data");


        }
        private void initWebBrowser()
        {

 

            ////（这个属性比较重要，可以通过这个属性，把WINFROM中的变量，传递到JS中，供内嵌的网页使用；但设置到的类型必须是COM可见的，所以要设置     [System.Runtime.InteropServices.ComVisibleAttribute(true)]，因为我的值设置为this,所以这个特性要加载窗体类上）
            //webBrowser1.ObjectForScripting = this;
            webBrowser1.Navigate(new Uri(System.IO.Directory.GetCurrentDirectory() + "/charts.html"));


        }
        private void InvokeTestMethod(String ydata, String xdate, String name)
        {

            Object[] objArray = new Object[2];
            objArray[0] = (Object)ydata;
            objArray[1] = (Object)xdate;

            this.webBrowser1.InvokeScript(name, objArray);
        }

        private String datatoString(double[] str)
        {
            test11 = "[";
            for (int i = 0; i < 20000; i++)
            {
                test11 = test11 + str[i] + ",";
            }
            test11 = test11 + "]";

            return test11;
        }

        private String dateToString()
        {


            days = "[";

            String name = Path.GetFileNameWithoutExtension(fileUrl);
            DateTime dt = Convert.ToDateTime(name.Substring(11, 2) + ":" + name.Substring(13, 2) + ":" + name.Substring(15, 2));

            String[] yData1 = new String[lengthChann];
            for (int j = 0; j < lengthChann; j++)
            {
                // yData[j] = fff[j];

                yData1[j] = dt.AddSeconds(timeInterval * (j)).ToString("HH:mm:ss");

            }
            for (int i = 0; i < 20000; i++)
            {
                days = days + "\'" + yData1[i] + "\'" + ",";
            }
            days = days + "]";

            return days;
        }



        private int getData(String file)
        {

            int head = 64;
            int channelLoc = 6144;
            ///   fileUrl = @"C:\Users\Administrator\Desktop\测井数据\电法数据\IP_20200422145808_300mA0mV.DAT";


            //将文件转换成byte[]数组
            byte[] byteArray = Tool.FileToByte(file);
            //存储各个通道数据的字节数组
            byte[] channel1 = new byte[(byteArray.Length - 64) / 4];
            byte[] channel2 = new byte[(byteArray.Length - 64) / 4];
            byte[] channel3 = new byte[(byteArray.Length - 64) / 4];
            byte[] channel4 = new byte[(byteArray.Length - 64) / 4];
            //存储转换后的数据
            int[] channellData = new Int32[(byteArray.Length - 64) / 12];
            int[] channel2Data = new Int32[(byteArray.Length - 64) / 12];
            int[] channel3Data = new Int32[(byteArray.Length - 64) / 12];
            int[] channel4Data = new Int32[(byteArray.Length - 64) / 12];
            //存储电压电流数据
            v1 = new double[(byteArray.Length - 64) / 12];
            v2 = new double[(byteArray.Length - 64) / 12];
            v3 = new double[(byteArray.Length - 64) / 12];
            i = new double[(byteArray.Length - 64) / 12];

            int count = (byteArray.Length - 64) / (6144 * 4);

            int flag = 1;
            if (byteArray[0] == 0x55 && byteArray[63] == 0xaa)
            {
                for (int i = 0; i < count; i++)
                {
                    for (int j = i * 6144; j < 6144 * flag; j++)
                    {

                        channel1[j] = byteArray[head + (channelLoc * (3 * (flag - 1))) + j];
                        channel2[j] = byteArray[head + channelLoc + (channelLoc * (3 * (flag - 1))) + j];
                        channel3[j] = byteArray[head + (channelLoc * 2) + (channelLoc * (3 * (flag - 1))) + j];
                        channel4[j] = byteArray[head + (channelLoc * 3) + (channelLoc * (3 * (flag - 1))) + j];

                    }
                    if (flag == count) break;
                    flag++;
                }
                for (int m = 0, n = 0; m < channellData.Length; m++, n += 3)
                {

                    channellData[m] = ((channel1[n] << 16) + (channel1[n + 1] << 8) + channel1[n + 2]);
                    channellData[m] = (channellData[m] << 8) >> 8;
                    channel2Data[m] = ((channel2[n] << 24) + (channel2[n + 1] << 16) + (channel2[n + 2] << 8)) / 256;
                    channel3Data[m] = ((channel3[n] << 24) + (channel3[n + 1] << 16) + (channel3[n + 2] << 8)) / 256;
                    channel4Data[m] = ((channel4[n] << 24) + (channel4[n + 1] << 16) + (channel4[n + 2] << 8)) / 256;

                }
                byte[] gain = { 0x00, 0x01, 0x02, 0x03 };
                //计算三个通道电压值
                for (int m = 0; m < channellData.Length; m++)
                {
                    i[m] = 512 - (channellData[m] * 1024L / 8388608);

                    v1[m] = channel2Data[m] * 4096.0 / 8388608;
                    v2[m] = channel2Data[m] * 4096.0 / 8388608;
                    v3[m] = channel2Data[m] * 4096.0 / 8388608;

                    //if (byteArray[17] == gain[0])
                    //{
                    //    v1[m] = channel2Data[m] * 4096.0 / 8388608;

                    //}
                    //else if (byteArray[17] == gain[1])
                    //{
                    //    v1[m] = channel2Data[m] * 4096.0 / 8388608 / 10;
                    //}
                    //else if (byteArray[17] == gain[2]) {

                    //    v1[m] = channel2Data[m] * 4096.0 / 8388608 / 100;

                    //}
                    //else if (byteArray[17] == gain[3])
                    //{

                    //    v1[m] = channel2Data[m] * 4096.0 / 8388608 / 1000;

                    //}

                    //if (byteArray[18] == gain[0])
                    //{
                    //    v2[m] = channel2Data[m] * 4096.0 / 8388608;

                    //}
                    //else if (byteArray[18] == gain[1])
                    //{
                    //    v2[m] = channel2Data[m] * 4096.0 / 8388608 / 10;
                    //}
                    //else if (byteArray[18] == gain[2])
                    //{

                    //    v2[m] = channel2Data[m] * 4096.0 / 8388608 / 100;

                    //}
                    //else if (byteArray[18] == gain[3])
                    //{

                    //    v2[m] = channel2Data[m] * 4096.0 / 8388608 / 1000;

                    //}
                    //if (byteArray[19] == gain[0])
                    //{
                    //    v3[m] = channel2Data[m] * 4096.0 / 8388608;

                    //}
                    //else if (byteArray[19] == gain[1])
                    //{
                    //    v3[m] = channel2Data[m] * 4096.0 / 8388608 / 10;
                    //}
                    //else if (byteArray[19] == gain[2])
                    //{

                    //    v3[m] = channel2Data[m] * 4096.0 / 8388608 / 100;

                    //}
                    //else if (byteArray[19] == gain[3])
                    //{

                    //    v3[m] = channel2Data[m] * 4096.0 / 8388608 / 1000;

                    //}

                    //v2[m] = channel3Data[m] * 4096.0 / 8388608 / 10;
                    // v3[m] = channel4Data[m] * 4096.0 / 8388608 / 10;

                }


            }

            return v1.Length;
        }


    }
}
