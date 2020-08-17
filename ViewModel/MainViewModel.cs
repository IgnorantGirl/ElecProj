using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecProj.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        //接收搜索条件
        private string amvalue = "0.4";
        public string AMValue
        {

            get { return amvalue; }
            set
            {
                amvalue = value;
                RaisePropertyChanged();
            }
        }
    }
}
