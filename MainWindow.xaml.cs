using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using FluentFTP;

namespace UploadSchedule
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        BackgroundWorker worker = new BackgroundWorker();
        string ProgramPath = "C:\\Выгрузка расписания";
        DateTime date = new DateTime(0, 0);
        System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
        public MainWindow()
        {
            InitializeComponent();
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += worker_DoWork;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            timer.Tick += timer_tick;
            if (!File.Exists($"{ProgramPath}\\FTP.txt"))
            {
                Directory.CreateDirectory(ProgramPath);
                File.Create($"{ProgramPath}\\FTP.txt");
            }
        }
        private void OnTabItemChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                using (StreamReader sr = new StreamReader($"{ProgramPath}\\FTP.txt"))
                {
                    ScheduleBox.Text = sr.ReadLine();
                    IPBox.Text = sr.ReadLine();
                    LoginBox.Text = sr.ReadLine();
                    PasswordBox.Password = sr.ReadLine();
                }
            }
            catch { }
        }
        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                date = DateTime.Now;
                worker.RunWorkerAsync();
                timer.Start();
                UploadButton.IsEnabled = false;
            }
            catch { System.Windows.MessageBox.Show("Выгрузка уже началась! Для прекращения выгрузки закройте программу."); }
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter($"{ProgramPath}\\FTP.txt"))
                {
                    string Schedule = ScheduleBox.Text;
                    Schedule = Schedule.Replace(@"""", "");
                    ScheduleBox.Text = Schedule;
                    sw.WriteLine(Schedule);
                    sw.WriteLine(IPBox.Text);
                    sw.WriteLine(LoginBox.Text);
                    sw.WriteLine(PasswordBox.Password);
                    System.Windows.MessageBox.Show("Конфигурация успешно сохранена!");
                }
            }
            catch { System.Windows.MessageBox.Show("Ошибка сохранения! Для сохранения конфигурации необходимо дождаться окончания выгрузки."); }
        }
        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            using (StreamReader sr = new StreamReader($"{ProgramPath}\\FTP.txt"))
            {
                try
                {
                    string localSchedule = sr.ReadLine();
                    string connection = sr.ReadLine();
                    string login = sr.ReadLine();
                    string password = sr.ReadLine();
                    string[] filePaths = Directory.GetFiles($"{localSchedule}", "*.htm", SearchOption.TopDirectoryOnly);
                    using (var ftp = new FtpClient($"{connection}", $"{login}", $"{password}"))
                    {
                        if (!ftp.IsConnected)
                        {
                            try
                            {
                                ftp.AutoConnect();
                            }
                            catch { System.Windows.MessageBox.Show("Повторите попытку позже!"); }
                        }

                        foreach (string file in filePaths)
                        {
                            ftp.UploadFile($"{file}", $"/{Path.GetFileName(file)}", FtpRemoteExists.Overwrite, false, FtpVerify.None);
                            Delegate deli = new Delegate(displaytext);
                            string FileMessage = Path.GetFileName(file);
                            FileName.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, deli, FileMessage);
                        }
                        timer.Stop();
                        ftp.Disconnect();
                    }
                }
                catch { System.Windows.MessageBox.Show("Ошибка подключения! Удостоверьтесь в правильности исходных данных!"); }
            }
        }
        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            System.Windows.Forms.MessageBox.Show(new Form() { TopMost = true }, "Выгрузка завершилась!");
            LatestTime.Text = "Прошло времени:";
            FileName.Text = "Текущий загружаемый файл:";
            UploadButton.IsEnabled = true;
        }
        private delegate void Delegate(string FileMessage);
        private void displaytext(string FileMessage)
        {
            FileName.Text = $"Текущий загружаемый файл: {FileMessage}";
        }

        private void timer_tick(object sender, EventArgs e)
        {
            LatestTime.Text = "Прошло времени: " + (DateTime.Now - date).ToString(@"mm\:ss");
        }
    }

}