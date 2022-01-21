using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using EAGetMail;

namespace Email_Project
{
    public partial class GelenKutusu : Page
    {
        #region Private alan tanımları
        private string[] kullaniciGirisBilgileri;
        private Frame AnaSayfa_Frame;
        private AES_Algoritmasi aes;
        private RSA_Algoritmasi rsa;
        private List<Mail> mails;
        private Sockets sockets;

        #endregion

        //Constructor
        public GelenKutusu(List<Mail> mails, Frame AnaSayfa_Frame, AES_Algoritmasi aes, RSA_Algoritmasi rsa, string[] kullaniciGirisBilgileri, Sockets sockets)
        {
            this.kullaniciGirisBilgileri = kullaniciGirisBilgileri;
            this.AnaSayfa_Frame = AnaSayfa_Frame;
            this.sockets = sockets;
            this.mails = mails;
            this.aes = aes;
            this.rsa = rsa;

            InitializeComponent();

            mailleriYerlestir();
        }

        //Listeye konuları teker teker gezerek yazdır. 
        private void mailleriYerlestir()
        {
            foreach (var item in mails)
            {
                ListBoxItem listBoxItem = new ListBoxItem();

                //ListBoxItem özelliklerini belirle.
                listBoxItem.FontSize = 14;

                //Item seçilince aşağıdaki eventi koştur.
                listBoxItem.Selected += listBoxItem_Selected;
                listBoxItem.MouseUp += listBoxItem_MouseUp;

                item.Subject = item.Subject.Replace("(Trial Version)", "");
                listBoxItem.DataContext = item;
                listBoxItem.Content = item.Subject;
                GelenKutusu_Liste.Items.Add(listBoxItem);
            }
        }

        #region Event Handlers

        //Seçilen maili aç.
        private void listBoxItem_Selected(object sender, RoutedEventArgs e)
        {
            var listBoxItem = e.OriginalSource as ListBoxItem;
            MailGoruntule mailGoruntule = new MailGoruntule((Mail)listBoxItem.DataContext, aes, rsa, kullaniciGirisBilgileri, sockets);
            AnaSayfa_Frame.Navigate(mailGoruntule);
        }

        private void listBoxItem_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var listBoxItem = sender as ListBoxItem;
            listBoxItem.IsSelected = false;
        }

        #endregion
    }
}
