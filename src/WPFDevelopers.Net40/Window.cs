﻿using Microsoft.Windows.Shell;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using WPFDevelopers.Controls;
using WPFDevelopers.Helpers;

namespace WPFDevelopers.Net40
{
    [TemplatePart(Name = TitleBarIcon, Type = typeof(Button))]
    public class Window : System.Windows.Window
    {
        private const string TitleBarIcon = "PART_TitleBarIcon";
        private WindowStyle _windowStyle;
        private Button _titleBarIcon;

        public static readonly DependencyProperty TitleHeightProperty =
            DependencyProperty.Register("TitleHeight", typeof(double), typeof(Window), new PropertyMetadata(50d));

        public static readonly DependencyProperty NoChromeProperty =
            DependencyProperty.Register("NoChrome", typeof(bool), typeof(Window), new PropertyMetadata(false));

        public static readonly DependencyProperty TitleBarProperty =
            DependencyProperty.Register("TitleBar", typeof(object), typeof(Window), new PropertyMetadata(null));

        public static readonly DependencyProperty TitleBackgroundProperty =
           DependencyProperty.Register("TitleBackground", typeof(Brush), typeof(Window), new PropertyMetadata(null));

        public static readonly DependencyProperty TitleBarModeProperty =
            DependencyProperty.Register("TitleBarMode", typeof(TitleBarMode), typeof(Window), new PropertyMetadata(TitleBarMode.Normal));

        static Window()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Window), new FrameworkPropertyMetadata(typeof(Window)));
        }

        public Window()
        {
            Loaded += Window_Loaded;
            CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, CloseWindow));
            CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand, MaximizeWindow,
                CanResizeWindow));
            CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand, MinimizeWindow,
                CanMinimizeWindow));
            CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand, RestoreWindow,
                CanResizeWindow));
           
        }
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _windowStyle = WindowStyle;
            _titleBarIcon = GetTemplateChild(TitleBarIcon) as Button;
            if (_titleBarIcon != null)
            {
                _titleBarIcon.MouseDoubleClick -= Icon_MouseDoubleClick;
                _titleBarIcon.MouseDoubleClick += Icon_MouseDoubleClick;
            }
        }

        private void Icon_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                Close();
        }

        public double TitleHeight
        {
            get => (double)GetValue(TitleHeightProperty);
            set => SetValue(TitleHeightProperty, value);
        }

        public bool NoChrome
        {
            get => (bool)GetValue(NoChromeProperty);
            set => SetValue(NoChromeProperty, value);
        }

        public object TitleBar
        {
            get => (object)GetValue(TitleBarProperty);
            set => SetValue(TitleBarProperty, value);
        }

        public Brush TitleBackground
        {
            get => (Brush)GetValue(TitleBackgroundProperty);
            set => SetValue(TitleBackgroundProperty, value);
        }

        public TitleBarMode TitleBarMode
        {
            get => (TitleBarMode)GetValue(TitleBarModeProperty);
            set => SetValue(TitleBarModeProperty, value);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            hWnd = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(hWnd).AddHook(WindowProc);
            if(TitleBarMode == TitleBarMode.Normal)
                TitleHeight = SystemParameters2.Current.WindowNonClientFrameThickness.Top;
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            if (SizeToContent == SizeToContent.WidthAndHeight)
                InvalidateMeasure();
        }

        #region Window Commands

        private void CanResizeWindow(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ResizeMode == ResizeMode.CanResize || ResizeMode == ResizeMode.CanResizeWithGrip;
        }

        private void CanMinimizeWindow(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ResizeMode != ResizeMode.NoResize;
        }

        private void CloseWindow(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        private void MaximizeWindow(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MaximizeWindow(this);
        }

        private void MinimizeWindow(object sender, ExecutedRoutedEventArgs e)
        {
            SendMessage(hWnd, ApiCodes.WM_SYSCOMMAND, new IntPtr(ApiCodes.SC_MINIMIZE), IntPtr.Zero);
        }

        private void RestoreWindow(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.RestoreWindow(this);
        }


        internal class ApiCodes
        {
            public const int SC_RESTORE = 0xF120;
            public const int SC_MINIMIZE = 0xF020;
            public const int WM_SYSCOMMAND = 0x0112;
        }

        private IntPtr hWnd;

        [DllImport(Win32.User32)]
        public static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == ApiCodes.WM_SYSCOMMAND)
            {
                if (wParam.ToInt32() == ApiCodes.SC_MINIMIZE)
                {
                    _windowStyle = WindowStyle;
                    if (WindowStyle != WindowStyle.SingleBorderWindow)
                        WindowStyle = WindowStyle.SingleBorderWindow;
                    WindowState = WindowState.Minimized;
                    handled = true;
                }
                else if (wParam.ToInt32() == ApiCodes.SC_RESTORE)
                {
                    WindowState = WindowState.Normal;
                    WindowStyle = WindowStyle.None;
                    if(WindowStyle.None != _windowStyle)
                        WindowStyle = _windowStyle;
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        private void ShowSystemMenu(object sender, ExecutedRoutedEventArgs e)
        {
            var element = e.OriginalSource as FrameworkElement;
            if (element == null)
                return;

            var point = WindowState == WindowState.Maximized
                ? new Point(0, element.ActualHeight)
                : new Point(Left + BorderThickness.Left, element.ActualHeight + Top + BorderThickness.Top);
            point = element.TransformToAncestor(this).Transform(point);
            SystemCommands.ShowSystemMenu(this, point);
        }

        #endregion
    }
}