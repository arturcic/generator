﻿//HintName: ExampleViewModel.generated.cs
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
#nullable enable
using System.ComponentModel;
namespace app
{
    public partial class ExampleViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        
        public string Text
        {
            get
            {
                return this.text;
            }
            set
            {
                if (this.text != value)
                {
                    this.text = value;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
                }
            }
        }
    }
}
