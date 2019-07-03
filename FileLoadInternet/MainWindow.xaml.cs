using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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

namespace FileLoadInternet
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int MaxThread = 5; // максимальное кол-во потоков
        private static Queue<string> proxyes; //глобальная очередь проксей
        private static readonly Object sync = new object(); //объект для синхронизации потоков

        private static readonly AutoResetEvent reset = new AutoResetEvent(false); //сигнализатор для потоков
        private static readonly List<Thread> threads = new List<Thread>(); //список потоков

        public MainWindow()
        {
            InitializeComponent();
        }

        public void LoadFile()
        {
            List<Record> accounts = new List<Record>(); //получили все аккаунты
            accounts.Add(new Record
            {
                Url = tbContent1.Text,
                FileName = tbFileName1.Text
            });
            accounts.Add(new Record
            {
                Url = tbContent2.Text,
                FileName = tbFileName2.Text
            });
            accounts.Add(new Record
            {
                Url = tbContent3.Text,
                FileName = tbFileName3.Text
            });
            proxyes = new Queue<string>(File.ReadAllLines("proxy.txt")); // и все прокси

            foreach (Record account in accounts) //перебираем все пути
            {
                if (account.Url != "")
                {
                    Thread worker = new Thread(Brute); //для каждого создаем новый поток
                    worker.Start(account); //запускаем его с параметром
                    threads.Add(worker); //и добавляем в список запущеных потоков
                    if (threads.Count >= MaxThread) reset.WaitOne(); //если потоков больше чем должно быть то ждем окончания
                }
            }
           
        }

        private static void Brute(object param)
        {
            string proxy;
            lock (sync)
            {
                proxy = proxyes.Dequeue(); //вытащили прокси из очереди
                proxyes.Enqueue(proxy); //и запихнули в конец очереди, то есть зациклили
            }

            string ResultFileName;
            Record rc = (Record)param;
            string href = rc.Url;
            //Uri uri = new Uri(href);
            //ResultFileName = System.IO.Path.GetFileName(uri.LocalPath);
            ResultFileName = rc.FileName;
            if (ResultFileName =="")
            {
                ResultFileName = "FileLoad.ext";
            }

            HttpWebRequest wr = (HttpWebRequest)HttpWebRequest.Create(href);
            //wr.Proxy = new WebProxy(proxy); // с этой командой не работает
            HttpWebResponse ws = (HttpWebResponse)wr.GetResponse();  
            Stream str = ws.GetResponseStream();

            byte[] inBuf = new byte[100000];
            int bytesReadTotal = 0;
            FileStream fstr = new FileStream(ResultFileName, FileMode.Create, FileAccess.Write);

            while (true)
            {
                int n = str.Read(inBuf, 0, 100000);
                if ((n == 0) || (n == -1))
                {
                    break;
                }

                fstr.Write(inBuf, 0, n);

                bytesReadTotal += n;
            }

            str.Close();
            fstr.Close();

            lock (sync) threads.Remove(Thread.CurrentThread); //обезопалили удаление потока из списка
            reset.Set(); //намекнули циклу что можно пускать новый поток
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            LoadFile();
        }

        private void btClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();

        }
    }
}
