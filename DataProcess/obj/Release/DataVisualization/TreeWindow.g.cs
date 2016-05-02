﻿#pragma checksum "..\..\..\DataVisualization\TreeWindow.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "BE17D461A40FC055CDE69F31DA04A18F"
//------------------------------------------------------------------------------
// <auto-generated>
//     此代码由工具生成。
//     运行时版本:4.0.30319.42000
//
//     对此文件的更改可能会导致不正确的行为，并且如果
//     重新生成代码，这些更改将会丢失。
// </auto-generated>
//------------------------------------------------------------------------------

using Lava.Visual;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace DataProcess.DataVisualization {
    
    
    /// <summary>
    /// TreeWindow
    /// </summary>
    public partial class TreeWindow : System.Windows.Window, System.Windows.Markup.IComponentConnector {
        
        
        #line 16 "..\..\..\DataVisualization\TreeWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button TestCurrentButton;
        
        #line default
        #line hidden
        
        
        #line 17 "..\..\..\DataVisualization\TreeWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock TestCurrentTextBlock;
        
        #line default
        #line hidden
        
        
        #line 18 "..\..\..\DataVisualization\TreeWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button SaveUserLabelsButton;
        
        #line default
        #line hidden
        
        
        #line 19 "..\..\..\DataVisualization\TreeWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button LoadUserLabelsButton;
        
        #line default
        #line hidden
        
        
        #line 23 "..\..\..\DataVisualization\TreeWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button RemoveNoiseButton;
        
        #line default
        #line hidden
        
        
        #line 25 "..\..\..\DataVisualization\TreeWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox SampleNumberTextBox;
        
        #line default
        #line hidden
        
        
        #line 27 "..\..\..\DataVisualization\TreeWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox IndexPathTextBox;
        
        #line default
        #line hidden
        
        
        #line 28 "..\..\..\DataVisualization\TreeWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock RemoveNoiseTextBlock;
        
        #line default
        #line hidden
        
        
        #line 32 "..\..\..\DataVisualization\TreeWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal Lava.Visual.Display TreeDisplay;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/DataProcess;component/datavisualization/treewindow.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\DataVisualization\TreeWindow.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            this.TestCurrentButton = ((System.Windows.Controls.Button)(target));
            return;
            case 2:
            this.TestCurrentTextBlock = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 3:
            this.SaveUserLabelsButton = ((System.Windows.Controls.Button)(target));
            return;
            case 4:
            this.LoadUserLabelsButton = ((System.Windows.Controls.Button)(target));
            return;
            case 5:
            this.RemoveNoiseButton = ((System.Windows.Controls.Button)(target));
            return;
            case 6:
            this.SampleNumberTextBox = ((System.Windows.Controls.TextBox)(target));
            return;
            case 7:
            this.IndexPathTextBox = ((System.Windows.Controls.TextBox)(target));
            return;
            case 8:
            this.RemoveNoiseTextBlock = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 9:
            this.TreeDisplay = ((Lava.Visual.Display)(target));
            return;
            }
            this._contentLoaded = true;
        }
    }
}

