﻿#pragma checksum "C:\Users\Lukas\Documents\Programmieren\WP7\BingSlideshow\BingSlideshow\BingSlideshow\MainPage.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "C002797D8AEC03D885EDDD1FA598E819"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.239
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using Microsoft.Phone.Controls;
using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using System.Windows.Shapes;
using System.Windows.Threading;


namespace BingSlideshow {
    
    
    public partial class MainPage : Microsoft.Phone.Controls.PhoneApplicationPage {
        
        internal System.Windows.Controls.Grid LayoutRoot;
        
        internal Microsoft.Phone.Controls.Panorama Pan;
        
        internal System.Windows.Controls.Grid ContentPanel;
        
        internal System.Windows.Controls.TextBlock TextBlock1;
        
        internal System.Windows.Controls.TextBox TextBox1;
        
        internal System.Windows.Controls.Button Button1;
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Windows.Application.LoadComponent(this, new System.Uri("/BingSlideshow;component/MainPage.xaml", System.UriKind.Relative));
            this.LayoutRoot = ((System.Windows.Controls.Grid)(this.FindName("LayoutRoot")));
            this.Pan = ((Microsoft.Phone.Controls.Panorama)(this.FindName("Pan")));
            this.ContentPanel = ((System.Windows.Controls.Grid)(this.FindName("ContentPanel")));
            this.TextBlock1 = ((System.Windows.Controls.TextBlock)(this.FindName("TextBlock1")));
            this.TextBox1 = ((System.Windows.Controls.TextBox)(this.FindName("TextBox1")));
            this.Button1 = ((System.Windows.Controls.Button)(this.FindName("Button1")));
        }
    }
}

