using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using EAGetMail;

namespace Email_Project
{
    public partial class LoginScreen : Page
    {

        //Private alan tanımları
        string[] kullaniciGirisBilgileri = new string[2];//[0] E-Posta Adresi -- [1] Parola
        private Frame mainFrame;

        //Constructor
        public LoginScreen(Frame mainFrame) 
        {
            this.mainFrame = mainFrame;

            InitializeComponent();
        }

        #region Event Handlers

        //Giriş butonuna tıklandığında çalışır.
        private void Login_Button_Click(object sender, RoutedEventArgs e)
        {

            //Kullanıcı bilgilerini al ve sunucuya göndermek üzere hazırla.
            MailServer mailGetServer = new MailServer("imap.gmail.com",
                                E_Posta_TextBox.Text,
                                Parola_TextBox.Text,
                                ServerProtocol.Imap4);
            mailGetServer.SSLConnection = true;
            mailGetServer.Port = 993;

            //Sunucuya bağlan.
            MailClient mailGetClient = new MailClient("TryIt");
            mailGetClient.Connect(mailGetServer);

            //Bağlantı başarılı olursa giriş bilgilerini sakla.
            kullaniciGirisBilgileri[0] = E_Posta_TextBox.Text;
            kullaniciGirisBilgileri[1] = Parola_TextBox.Text;

            //Sunucudan mailleri al ve AnaSayfa'yı aç.
            if (mailGetClient.Connected == true)
            {
                
                MailInfo[] infos = mailGetClient.GetMailInfos();
                List<Mail> mails = new List<Mail>();

                for (int i = 0; i < infos.Length; i++)
                {
                    mails.Add(mailGetClient.GetMail(infos[i]));
                }

                mails.Reverse();
                AnaSayfa anaSayfa = new AnaSayfa(this, mainFrame, mails, kullaniciGirisBilgileri,
                    mailGetServer, mailGetClient);
                mainFrame.Navigate(anaSayfa);
            }
        }

        #endregion
    }
}
