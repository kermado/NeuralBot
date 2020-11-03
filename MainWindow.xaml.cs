using System;
using System.Windows;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Reflection;
using System.ComponentModel;

using Microsoft.Win32;
using Newtonsoft.Json;

using AimBot.Input;
using AimBot.Renderers;
using AimBot.Helpers;
using AimBot.Grabbers;
using AimBot.Detectors;
using AimBot.Trackers;
using AimBot.Selectors;
using AimBot.Injectors;
using AimBot.Aimers;

using Trigger = AimBot.Triggers.Trigger;

namespace AimBot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string processName;
        private Process process;

        private GlobalKey globalKey;
        private GlobalMouse globalMouse;
        private GlobalWindow globalWindow;

        private Esp esp;
        private Bot bot;

        private Thread botThread;
        private int botThreadTermination;

        private Stopwatch stopwatch;

        private bool running;

        private Rectangle region;
        private Rectangle client;

        private JsonSerializerSettings serializerSettings;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool Started
        {
            get { return running; }
            private set
            {
                if (running != value)
                {
                    running = value;
                    NotifyPropertyChanged(nameof(Started));
                    NotifyPropertyChanged(nameof(Stopped));
                }
            }
        }

        public bool Stopped
        {
            get { return Started == false; }
            private set { Started = !value; }
        }

        public IEnumerable<string> ProcessNames
        {
            get
            {
                foreach (var process in Process.GetProcesses())
                {
                    yield return process.ProcessName;
                }
            }
        }

        public string ProcessName
        {
            get { return processName; }
            set { UpdateProcess(value); }
        }

        public int RegionWidth
        {
            get { return region.Width; }
            set
            {
                region.Width = value;
                RecenterRegion();
            }
        }

        public int RegionHeight
        {
            get { return region.Height; }
            set
            {
                region.Height = value;
                RecenterRegion();
            }
        }

        public bool Esp
        {
            get { return esp != null; }
            set
            {
                if (value && esp == null)
                {
                    esp = new GameOverlayEsp();
                    StartEsp();
                }
                else if (value == false && esp != null)
                {
                    StopEsp();
                    esp = null;
                }
            }
        }

        public IEnumerable<string> ActivationKeys
        {
            get { return Enum.GetNames(typeof(Bot.ActivationKeys)); }
        }

        public IEnumerable<string> ActivationButtons
        {
            get { return Enum.GetNames(typeof(Bot.ActivationButtons)); }
        }

        public string ActivationKey
        {
            get { return bot.ActivationKey.ToString(); }
            set
            {
                if (Enum.TryParse(typeof(Bot.ActivationKeys), value, out var key))
                {
                    bot.ActivationKey = (Bot.ActivationKeys)key;
                }

                NotifyPropertyChanged(nameof(ActivationKey));
            }
        }

        public string ActivationButton
        {
            get { return bot.ActivationButton.ToString(); }
            set
            {
                if (Enum.TryParse(typeof(Bot.ActivationButtons), value, out var btn))
                {
                    bot.ActivationButton = (Bot.ActivationButtons)btn;
                }

                NotifyPropertyChanged(nameof(ActivationButton));
            }
        }

        public IEnumerable<string> GrabberNames
        {
            get { return TypeHelper.ConcreteTypeNames(typeof(Grabber)); }
        }

        public string GrabberName
        {
            get { return bot.Grabber?.GetType().Name ?? ""; }
            set
            {
                foreach (var option in TypeHelper.ConcreteTypes(typeof(Grabber)))
                {
                    if (option.Name == value)
                    {
                        bot.Grabber = (Grabber)Activator.CreateInstance(option);
                        UpdateSettings(bot.Grabber, GrabberSettings);
                        break;
                    }
                }

                NotifyPropertyChanged(nameof(GrabberName));
            }
        }

        public IEnumerable<string> DetectorNames
        {
            get { return TypeHelper.ConcreteTypeNames(typeof(Detector)); }
        }

        public string DetectorName
        {
            get { return bot.Detector?.GetType().Name ?? ""; }
            set
            {
                foreach (var option in TypeHelper.ConcreteTypes(typeof(Detector)))
                {
                    if (option.Name == value)
                    {
                        bot.Detector = (Detector)Activator.CreateInstance(option);
                        UpdateSettings(bot.Detector, DetectorSettings);
                        break;
                    }
                }

                NotifyPropertyChanged(nameof(DetectorName));
            }
        }

        public IEnumerable<string> TrackerNames
        {
            get { return TypeHelper.ConcreteTypeNames(typeof(Tracker)); }
        }

        public string TrackerName
        {
            get { return bot.Tracker?.GetType().Name ?? ""; }
            set
            {
                foreach (var option in TypeHelper.ConcreteTypes(typeof(Tracker)))
                {
                    if (option.Name == value)
                    {
                        bot.Tracker = (Tracker)Activator.CreateInstance(option);
                        UpdateSettings(bot.Tracker, TrackerSettings);
                        break;
                    }
                }

                NotifyPropertyChanged(nameof(TrackerName));
            }
        }

        public IEnumerable<string> SelectorNames
        {
            get { return TypeHelper.ConcreteTypeNames(typeof(Selector)); }
        }

        public string SelectorName
        {
            get { return bot.Selector?.GetType().Name ?? ""; }
            set
            {
                foreach (var option in TypeHelper.ConcreteTypes(typeof(Selector)))
                {
                    if (option.Name == value)
                    {
                        bot.Selector = (Selector)Activator.CreateInstance(option);
                        UpdateSettings(bot.Selector, SelectorSettings);
                        break;
                    }
                }

                NotifyPropertyChanged(nameof(SelectorName));
            }
        }

        public IEnumerable<string> AimerNames
        {
            get { return TypeHelper.ConcreteTypeNames(typeof(Aimer)); }
        }

        public string AimerName
        {
            get { return bot.Aimer?.GetType().Name ?? ""; }
            set
            {
                foreach (var option in TypeHelper.ConcreteTypes(typeof(Aimer)))
                {
                    if (option.Name == value)
                    {
                        bot.Aimer = (Aimer)Activator.CreateInstance(option);
                        UpdateSettings(bot.Aimer, AimerSettings);
                        break;
                    }
                }

                NotifyPropertyChanged(nameof(AimerName));
            }
        }

        public IEnumerable<string> InjectorNames
        {
            get { return TypeHelper.ConcreteTypeNames(typeof(MouseInjector)); }
        }

        public string InjectorName
        {
            get { return bot.Injector?.GetType().Name ?? ""; }
            set
            {
                foreach (var option in TypeHelper.ConcreteTypes(typeof(MouseInjector)))
                {
                    if (option.Name == value)
                    {
                        bot.Injector = (MouseInjector)Activator.CreateInstance(option);
                        UpdateSettings(bot.Injector, InjectorSettings);
                        break;
                    }
                }

                NotifyPropertyChanged(nameof(InjectorName));
            }
        }

        public IEnumerable<string> TriggerNames
        {
            get { return TypeHelper.ConcreteTypeNames(typeof(Trigger)); }
        }

        public string TriggerName
        {
            get { return bot.Trigger?.GetType().Name ?? ""; }
            set
            {
                foreach (var option in TypeHelper.ConcreteTypes(typeof(Trigger)))
                {
                    if (option.Name == value)
                    {
                        bot.Trigger = (Trigger)Activator.CreateInstance(option);
                        UpdateSettings(bot.Trigger, TriggerSettings);
                        break;
                    }
                }

                NotifyPropertyChanged(nameof(TriggerName));
            }
        }

        public string ConfigurationDirectory
        {
            get
            {
                var directory = Path.Combine(Directory.GetCurrentDirectory(), "Configurations");
                if (Directory.Exists(directory) == false)
                {
                    Directory.CreateDirectory(directory);
                }

                return directory;
            }
        }

        public IEnumerable<string> ConfigurationFiles
        {
            get
            {
                var paths = Directory.GetFiles(ConfigurationDirectory, "*.json");
                foreach (var path in paths)
                {
                    yield return path;
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            serializerSettings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Objects
            };

            stopwatch = new Stopwatch();
            esp = null;
            bot = new Bot();
        }

        private void StopEsp()
        {
            esp?.Stop();
        }

        private void StartEsp()
        {
            if (process != null)
            {
                esp?.Start(process.MainWindowHandle);
            }
        }

        private bool UpdateProcess(string processName)
        {
            StopBot();
            StopEsp();
            Unhook();

            var processes = Process.GetProcessesByName(processName);
            if (processes != null && processes.Length > 0)
            {
                process = processes[0];

                UpdateWindow();
                Hook();
                StartEsp();

                this.processName = processName;
                return true;
            }

            process = null;
            return false;
        }

        private void UpdateWindow()
        {
            if (process != null)
            {
                client = ScreenHelper.ClientRectangle(process.MainWindowHandle);
                esp?.Refit();
                RecenterRegion();
            }
        }

        private void RecenterRegion()
        {
            var cx = client.X + client.Width / 2;
            var cy = client.Y + client.Height / 2;
            var rx = cx - region.Width / 2;
            var ry = cy - region.Height / 2;
            region = new Rectangle(rx, ry, region.Width, region.Height);
        }

        private void StopBot()
        {
            if (botThread != null)
            {
                Interlocked.Increment(ref botThreadTermination);
                botThread.Join();
                botThread = null;
            }

            if (bot != null)
            {
                bot.Process = null;
            }

            Stopped = true;
        }

        private void StartBot()
        {
            if (process != null && bot != null)
            {
                bot.Process = process;

                botThreadTermination = 0;
                Started = true;
                botThread = new Thread(UpdateBot);
                botThread.Start();
            }
        }

        private void UpdateBot()
        {
            while (botThreadTermination <= 0)
            {
                var dt = (double)stopwatch.Elapsed.TotalMilliseconds / 1000.0;
                stopwatch.Restart();

                esp?.Clear();

                if (process != null && region.Width > 0 && region.Height > 0 && bot != null)
                {
                    bot.Tick(dt, region, esp);
                }

                esp?.SwapBuffers();
            }
        }

        private void Unhook()
        {
            if (globalKey != null)
            {
                globalKey.KeyEvent -= GlobalKey_KeyEvent;
                globalKey.Dispose();
                globalKey = null;
            }

            if (globalMouse != null)
            {
                globalMouse.ButtonEvent -= GlobalMouse_ButtonEvent;
                globalMouse.Dispose();
                globalMouse = null;
            }

            if (globalWindow != null)
            {
                globalWindow.MoveEvent -= GlobalWindow_MoveEvent;
                globalWindow.Dispose();
                globalWindow = null;
            }
        }

        private void Hook()
        {
            Unhook();

            if (process != null)
            {
                globalKey = new GlobalKey();
                globalKey.KeyEvent += GlobalKey_KeyEvent;

                globalMouse = new GlobalMouse();
                globalMouse.ButtonEvent += GlobalMouse_ButtonEvent;

                globalWindow = new GlobalWindow(process);
                globalWindow.MoveEvent += GlobalWindow_MoveEvent;
            }
        }

        private void UpdateSettings(object instance, StackPanel panel)
        {
            panel.Children.Clear();
            foreach (var prop in TypeHelper.PublicProperties(instance))
            {
                if (prop.PropertyType == typeof(string))
                {
                    var tl = new TextBlock();
                    tl.Text = prop.Name;
                    panel.Children.Add(tl);

                    var tb = new TextBox();
                    tb.Name = prop.Name;
                    tb.Text = (string)prop.GetValue(instance) ?? "";
                    tb.TextChanged += OnPropertyTextBoxChanged;
                    tb.DataContext = new PropertyContext(instance, prop);
                    panel.Children.Add(tb);
                }
                else if (prop.PropertyType == typeof(FileFilter))
                {
                    if (prop.GetValue(instance) is FileFilter ff)
                    {
                        var tl = new TextBlock();
                        tl.Text = prop.Name;
                        panel.Children.Add(tl);

                        var cb = new ComboBox();
                        cb.Name = prop.Name;
                        cb.ItemsSource = ff.FileNames;
                        cb.SelectedItem = ff.FileName;
                        cb.SelectionChanged += OnPropertyComboBoxChanged;
                        cb.DataContext = new PropertyContext(instance, prop);
                        panel.Children.Add(cb);
                    }
                }
                else if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(float) || prop.PropertyType == typeof(double))
                {
                    var tl = new TextBlock();
                    tl.Text = prop.Name;
                    panel.Children.Add(tl);

                    var tb = new TextBox();
                    tb.Name = prop.Name;
                    tb.Text = prop.GetValue(instance).ToString();
                    tb.TextChanged += OnPropertyTextBoxChanged;
                    tb.DataContext = new PropertyContext(instance, prop);
                    panel.Children.Add(tb);
                }
                else if (prop.PropertyType == typeof(bool))
                {
                    var tl = new TextBlock();
                    tl.Text = prop.Name;
                    panel.Children.Add(tl);

                    var cb = new CheckBox();
                    cb.Name = prop.Name;
                    cb.IsChecked = (bool)prop.GetValue(instance);
                    cb.Checked += OnPropertyCheckBoxChanged;
                    cb.Unchecked += OnPropertyCheckBoxChanged;
                    cb.DataContext = new PropertyContext(instance, prop);
                    panel.Children.Add(cb);
                }
                else if (prop.PropertyType.IsEnum)
                {
                    var tl = new TextBlock();
                    tl.Text = prop.Name;
                    panel.Children.Add(tl);

                    var cb = new ComboBox();
                    cb.Name = prop.Name;
                    cb.ItemsSource = Enum.GetNames(prop.PropertyType);
                    cb.SelectedItem = prop.GetValue(instance).ToString();
                    cb.SelectionChanged += OnPropertyComboBoxChanged;
                    cb.DataContext = new PropertyContext(instance, prop);
                    panel.Children.Add(cb);
                }
            }
        }

        private void OnPropertyComboBoxChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb)
            {
                if (cb.DataContext is PropertyContext context)
                {
                    if (context.Property.PropertyType == typeof(FileFilter))
                    {
                        var ff = context.GetValue<FileFilter>();
                        if (ff != null)
                        {
                            ff.FileName = (string)cb.SelectedItem;
                        }
                    }
                    else if (context.Property.PropertyType.IsEnum)
                    {
                        context.SetValue(Enum.Parse(context.Property.PropertyType, cb.SelectedItem.ToString()));
                    }
                }
            }
        }

        private void OnPropertyTextBoxChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                if (tb.DataContext is PropertyContext context)
                {
                    if (context.Property.PropertyType == typeof(string))
                    {
                        context.SetValue(tb.Text);
                    }
                    else if (context.Property.PropertyType == typeof(int))
                    {
                        if (int.TryParse(tb.Text, out int value))
                        {
                            context.SetValue(value);
                        }
                    }
                    else if (context.Property.PropertyType == typeof(float))
                    {
                        if (float.TryParse(tb.Text, out float value))
                        {
                            context.SetValue(value);
                        }
                    }
                    else if (context.Property.PropertyType == typeof(double))
                    {
                        if (double.TryParse(tb.Text, out double value))
                        {
                            context.SetValue(value);
                        }
                    }
                }
            }
        }

        private void OnPropertyCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb)
            {
                if (cb.DataContext is PropertyContext context)
                {
                    if (context.Property.PropertyType == typeof(bool))
                    {
                        context.SetValue(cb.IsChecked);
                    }
                }
            }
        }

        private void OnRefresh(object sender, RoutedEventArgs e)
        {
            NotifyPropertyChanged(nameof(ProcessNames));
        }

        private void OnSave(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog()
            {
                Filter = "Configuration Files(*.json)|*.json",
                InitialDirectory = ConfigurationDirectory
            };

            if (dialog.ShowDialog() == true)
            {
                var configuration = new Configuration() { Width = RegionWidth, Height = RegionHeight, Esp = Esp, Bot = bot };
                File.WriteAllText(dialog.FileName, JsonConvert.SerializeObject(configuration, Formatting.Indented, serializerSettings));
                NotifyPropertyChanged(nameof(ConfigurationFiles));
            }
        }

        private void OnChangedConfiguration(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb)
            {
                var configurationPath = (string)cb.SelectedItem;
                if (File.Exists(configurationPath))
                {
                    StopBot();
                    bot?.Dispose();

                    var configuration = (Configuration)JsonConvert.DeserializeObject(File.ReadAllText(configurationPath), typeof(Configuration), serializerSettings);
                    RegionWidth = configuration.Width;
                    RegionHeight = configuration.Height;
                    Esp = configuration.Esp;
                    bot = configuration.Bot;

                    UpdateWindow();

                    NotifyPropertyChanged(nameof(RegionWidth));
                    NotifyPropertyChanged(nameof(RegionHeight));
                    NotifyPropertyChanged(nameof(ActivationKey));
                    NotifyPropertyChanged(nameof(ActivationButton));
                    NotifyPropertyChanged(nameof(GrabberName));
                    NotifyPropertyChanged(nameof(DetectorName));
                    NotifyPropertyChanged(nameof(TrackerName));
                    NotifyPropertyChanged(nameof(SelectorName));
                    NotifyPropertyChanged(nameof(AimerName));
                    NotifyPropertyChanged(nameof(InjectorName));
                    NotifyPropertyChanged(nameof(TriggerName));

                    UpdateSettings(bot.Grabber, GrabberSettings);
                    UpdateSettings(bot.Detector, DetectorSettings);
                    UpdateSettings(bot.Tracker, TrackerSettings);
                    UpdateSettings(bot.Selector, SelectorSettings);
                    UpdateSettings(bot.Aimer, AimerSettings);
                    UpdateSettings(bot.Injector, InjectorSettings);
                    UpdateSettings(bot.Trigger, TriggerSettings);
                }
            }
        }

        private void OnStart(object sender, RoutedEventArgs e)
        {
            StartBot();
        }

        private void OnStop(object sender, RoutedEventArgs e)
        {
            StopBot();
        }

        private void NotifyPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void GlobalKey_KeyEvent(object sender, GlobalKey.KeyEventArgs e)
        {
            bot?.OnKey(e);
        }

        private void GlobalMouse_ButtonEvent(object sender, GlobalMouse.ButtonEventArgs e)
        {
            bot?.OnButton(e);
        }

        private void GlobalWindow_MoveEvent(object sender, GlobalWindow.MoveEventArgs e)
        {
            UpdateWindow();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            StopBot();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Refresh.Click += OnRefresh;
            Save.Click += OnSave;
            Start.Click += OnStart;
            Stop.Click += OnStop;
            Configuration.SelectionChanged += OnChangedConfiguration;
        }
    }

    public struct PropertyContext
    {
        public object Instance;
        public PropertyInfo Property;

        public PropertyContext(object instance, PropertyInfo property)
        {
            Instance = instance;
            Property = property;
        }

        public void SetValue<T>(T value)
        {
            Property.SetValue(Instance, value);
        }

        public T GetValue<T>() where T : class
        {
            return Property.GetValue(Instance) as T;
        }
    }

    public class Configuration
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public bool Esp { get; set; }
        public Bot Bot { get; set; }
    }
}
