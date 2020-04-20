using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SystrayExtension
{
    // diy alternative to Caliburn.Micro [ https://caliburnmicro.com ], MvvmCross [ https://mvvmcross.com ], MvvmLight [ http://mvvmlight.net ], 
    // ReactiveUI [ https://reactiveui.net ], Prism.Core [ https://github.com/PrismLibrary/Prism ] packages
    // best mvvm framework for uwp -> https://www.reddit.com/r/csharp/comments/aawjpz/recommended_mvvm_libraryframework/
    // caliburn.micro vs mvvm light -> https://stackoverflow.com/questions/33342285/caliburn-micro-vs-mvvm-light-are-these-frameworks-the-same
    // mvvmcross vs mvvm light -> 
    // reactiveui vs mvvm light -> https://www.quora.com/What-are-the-advantages-or-disadvantages-of-ReactiveUI-vs-MVVM-Light
    // mvvm light vs prism.core -> https://stackoverflow.com/questions/20487243/prism-vs-mvvm-light-for-wpf
    // xaml two way binding slider and textbox -> https://stackoverflow.com/questions/14075255/two-way-binding-of-a-textbox-to-a-slider-in-wpf
    // uwp ViewModelBase -> https://stackoverflow.com/questions/36149863/how-to-write-a-viewmodelbase-in-mvvm
    // xaml attaching viewmodel -> https://stackoverflow.com/questions/4590446/how-do-i-set-a-viewmodel-on-a-window-in-xaml-using-datacontext-property
    // xaml margin.bottom binding -> https://stackoverflow.com/questions/1316405/how-to-set-a-top-margin-only-in-xaml
    // xaml binding vs x:bind -> https://stackoverflow.com/questions/37398038/difference-between-binding-and-xbind -> use x:bind
    // xaml binding calculation -> https://www.tutorialspoint.com/xaml/xaml_data_binding.htm
    // xaml binding computations -> https://docs.microsoft.com/en-us/xamarin/xamarin-forms/xaml/xaml-basics/data-binding-basics
    // xaml textbox updatesourcetrigger -> https://docs.microsoft.com/en-us/dotnet/framework/wpf/data/how-to-control-when-the-textbox-text-updates-the-source
    // xaml slider updatesourcetrigger -> 
    // uwp app data storage location -> https://docs.microsoft.com/en-us/windows/uwp/design/app-settings/store-and-retrieve-app-data
    public abstract class ViewModelBase : INotifyPropertyChanged //, BindableBase /* requires Prism.Core package ??? */
    {        
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(storage, value)) return false;
            storage = value;
            this.OnPropertyChanged(propertyName);
            return true;
        }

        //project specific logic for all viewmodels, e.g. in this project I want to use EventAggregator heavily
        //public virtual IEventAggregator() => ServiceLocator.GetInstance<IEventAggregator>()
    }
}
