using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TelnetPlugin.ModAPI;

namespace TelnetTestServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ITelnetServer server;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            server = Telnet.CreateServer(57888);
            server.Accept(() =>
            {
                server.Write("Type in your name: ");
                server.ReadLine(name =>
                {
                    Debug.WriteLine($"Got {name}");
                    server.WriteLine($"Hello {name}");
                    server.Write("What's you favourite colour? ");
                    server.ReadLine(color =>
                    {
                        Debug.WriteLine($"Got {color}");
                        server.WriteLine($"I don't like {color}");
                        server.Close();
                    });
                });
            });

            infoLabel.Content = $"{server.Address}:{server.Port}";
        }
    }
}
