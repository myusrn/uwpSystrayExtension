using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace SystrayExtension
{
    public class MainPageViewModel : ViewModelBase
    {
        //readonly static IReadAndPersistSettings readAndPersistSettings = new ReadAndPersistSettings();  // singleton initialization pattern
        static IReadAndPersistSettings readAndPersistSettings = new ReadAndPersistSettings(); // non-singleton initialization pattern

        public MainPageViewModel(/* IReadAndPersistSettings readAndPersistSettings -- if IoC/DI framework were in place */ )
        {
            //this.readAndPersistSettings = readAndPersistSettings;
        }

        int altcWidth = readAndPersistSettings.GetAltcWidth();
        public int AltcWidth
        {
            get { return altcWidth; }
            set 
            { 
                SetProperty(ref altcWidth, value);
                readAndPersistSettings.SetAltcWidth(value);
            }
        }

        int altArrowWidth = readAndPersistSettings.GetAltArrowWidth();
        public int AltArrowWidth
        {
            get { return altArrowWidth; }
            set
            {
                SetProperty(ref altArrowWidth, value);
                readAndPersistSettings.SetAltArrowWidth(value);
            }
        }

        int ctrlArrowWidth = readAndPersistSettings.GetCtrlArrowWidth();
        public int CtrlArrowWidth
        {
            get { return ctrlArrowWidth; }
            set
            {
                SetProperty(ref ctrlArrowWidth, value);
                readAndPersistSettings.SetCtrlArrowWidth(value);
            }
        }

        //double theValue;
        //public double TheValue
        //{
        //    get { return theValue; }
        //    set
        //    {
        //        if (theValue == value) return;
        //        theValue = value;
        //        OnPropertyChanged("TheValue");
        //    }
        //}
    }
}
