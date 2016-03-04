using System;
using System.Windows;
using System.Threading;

namespace TwitchSpammer
{
    public partial class MainWindow : Window
    {
        IrcClient irc;
        string Utilizador, PalavraPasse, Canal, Mensagem;
        double ValorSlider;
        bool Spamming = false;
        int MensagensEnviadas = 0;
        System.Timers.Timer TimerMensagem = new System.Timers.Timer();

        public MainWindow()
        {
            InitializeComponent();
            setStatus(0);
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.Utilizador != string.Empty)
                tbx_Utilizador.Text = Properties.Settings.Default.Utilizador;
            if (Properties.Settings.Default.PalavraPasse != string.Empty)
                pB_PalavraPasse.Password = Properties.Settings.Default.PalavraPasse;
            if (Properties.Settings.Default.Canal != string.Empty)
                tbx_Canal.Text = Properties.Settings.Default.Canal;
            if (Properties.Settings.Default.Rate > 0)
                sld_Velocidade.Value = Properties.Settings.Default.Rate;
            if (Properties.Settings.Default.Mensagem != string.Empty)
                tbx_Mensagem.Text = Properties.Settings.Default.Mensagem;
        }

        private void btn_Login_Click(object sender, RoutedEventArgs e)
        {
            Utilizador = tbx_Utilizador.Text;
            PalavraPasse = pB_PalavraPasse.Password;
            irc = new IrcClient("irc.twitch.tv", 6667, Utilizador, PalavraPasse);      

            ThreadStart TSTwitchConnect = delegate
            {
                string msgHelper = string.Empty;
                int flag = 0;

                while ((msgHelper = irc.readMessage()) != null)
                    if (msgHelper.Contains("Welcome, GLHF!"))
                    {
                        flag = 1;
                        break;
                    }

                if (flag == 1)
                {
                    Dispatcher.Invoke(new Action(() => {
                        btn_Start.IsEnabled = true;
                        btn_Login.IsEnabled = false;
                        btn_Login.Content = "Logged in";
                    }), System.Windows.Threading.DispatcherPriority.DataBind, null);
                    setStatus(1);
                    Properties.Settings.Default.Utilizador = Utilizador;
                    Properties.Settings.Default.PalavraPasse = PalavraPasse;
                }
                else
                    setStatus(2);

            };
            Thread TwitchConnect = new Thread(TSTwitchConnect);
            TwitchConnect.Start();
        }
        private void btn_Start_Click(object sender, RoutedEventArgs e)
        {
            if (!Spamming)
            {
                Canal = tbx_Canal.Text;
                ValorSlider = sld_Velocidade.Value;
                Mensagem = tbx_Mensagem.Text;

                if (Canal.Length < 3 || Canal == "Channel")
                    MessageBox.Show("Change the channel SwiftRage", "Error @ Channel", MessageBoxButton.OK, MessageBoxImage.Error);
                else if (ValorSlider == 0)
                    MessageBox.Show("You need to send at least ONE message per minute SwiftRage", "Error @ Rate of sending", MessageBoxButton.OK, MessageBoxImage.Error);
                else if (Mensagem.Length == 0 || Mensagem == "Message to send")
                    MessageBox.Show("Set your message to send SwiftRage", "Error @ Message", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                {
                    Properties.Settings.Default.Canal = Canal;
                    Properties.Settings.Default.Rate = ValorSlider;
                    Properties.Settings.Default.Mensagem = Mensagem;

                    MensagensEnviadas = 0;
                    irc.joinRoom(Canal);
                    setStatus(3);
                    
                    TimerMensagem.AutoReset = true;
                    TimerMensagem.Enabled = true;
                    TimerMensagem.Interval = Math.Round((60 / ValorSlider) * 1000, MidpointRounding.ToEven);
                    TimerMensagem.Elapsed += TimerMensagem_Elapsed;
                    TimerMensagem.Start();
                    btn_Start.Content = "Stop the tilt";
                    Spamming = true;
                }
            }
            else
            {
                TimerMensagem.Stop();
                TimerMensagem.Close();
                TimerMensagem.Dispose();
                btn_Start.Content = "Apply tilt";
                Spamming = false;
            }
        }

        private void TimerMensagem_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            MensagensEnviadas++;
            setStatus(3, MensagensEnviadas);
            irc.sendChatMessage(Mensagem);
        }
        private void sld_Velocidade_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Dispatcher.Invoke(new Action(() => {
                lb_SliderManagement.Content = sld_Velocidade.Value + " message(s)/minute";
            }), System.Windows.Threading.DispatcherPriority.DataBind, null);
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.Save();
        }
        private void setStatus(int _estado, int _nMensagens = 0)
        {
            string EstadoString = string.Empty;
            switch (_estado)
            {
                case 0:
                    EstadoString = "Waiting for login";
                    break;
                case 1:
                    EstadoString = "Logged in and waiting for an action";
                    break;
                case 2:
                    EstadoString = "Error on logging in";
                    break;
                case 3:
                    EstadoString = "SPAMMING 4Head (Mensagens enviadas: " + _nMensagens + ")";
                    break;
                default:
                    EstadoString = "Unspecified error!";
                    break;
            }

            Dispatcher.Invoke(new Action(() => {
                label_Status.Content = EstadoString;
            }), System.Windows.Threading.DispatcherPriority.DataBind, null);
        }
    }
}
