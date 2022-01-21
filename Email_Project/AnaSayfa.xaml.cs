using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows;
using EAGetMail;
using System;

namespace Email_Project
{
    public partial class AnaSayfa : Page
    {

        # region Private alan tanımları
        private string[] kullaniciGirisBilgileri = new string[2];//[0] E-Posta Adresi -- [1] Parola
        private GelenKutusu GelenKutusu_Tut;
        private MailClient mailGetClient;
        private MailServer mailGetServer;
        private LoginScreen LoginScreen;
        private AES_Algoritmasi aes;
        private RSA_Algoritmasi rsa;
        private List<Mail> mails;
        private Sockets sockets;
        private Frame mainFrame;

        #endregion

        //Constructor
        public AnaSayfa(LoginScreen loginScreen, Frame mainFrame, List<Mail> mails, string[] kullaniciGirisBilgileri, 
            MailServer mailGetServer, MailClient mailGetClient)
        {
            this.kullaniciGirisBilgileri = kullaniciGirisBilgileri;
            this.mailGetServer = mailGetServer;
            this.mailGetClient = mailGetClient;
            this.LoginScreen = loginScreen;
            this.mainFrame = mainFrame;
            this.mails = mails;

            //Sistem başlatma fonksiyonları
            aes = new AES_Algoritmasi();
            rsa = new RSA_Algoritmasi(aes);
            sockets = new Sockets();

            KeyKontrol();

            InitializeComponent();
        }

        //Giriş yapan kullanıcının RSA keylerini kontrol et.
        private void KeyKontrol()
        {
            //Key yerelde yoksa sunucuda da yoktur. Şeklinde düşünülerek aşağısı yazıldı.
            //Normalde hem dosya kontrol edilip hemde sunucuda keyler kontrol edilmeli.
            //şimdilik normal dışı durumlar yok sayıldı.

            byte[] publicKey = rsa.PublicKeyOku(kullaniciGirisBilgileri[0]);
            byte[] privateKey = rsa.PrivateKeyOku(kullaniciGirisBilgileri[0]);

            if (publicKey == null || privateKey == null)
            {
                //Yeni RSA key oluştur.
                List<byte[]> RSA_Keys = rsa.KeyGenerator(kullaniciGirisBilgileri[0]);

                //Programda kullanılmak üzere sakla.
                rsa.User_RSA_Public_Key = RSA_Keys[0];
                rsa.User_RSA_Private_Key = RSA_Keys[1];

                //Oluşturulan Key'leri şifreleyip dosyaya ve sunucuya yaz.
                rsa.PublicKeyYaz(kullaniciGirisBilgileri[0], RSA_Keys[0]);
                rsa.PrivateKeyYaz(kullaniciGirisBilgileri[0], RSA_Keys[1]);

                //Public key'i sunucuya göndermek için şifrele.
                string str_Key = Convert.ToBase64String(RSA_Keys[0]);
                //string RSA_Public_Encrypted = aes.AES_Sifrele(str_Key, aes.App_Key, aes.App_IV);

                //Veri tabanına kayıt ekleme komutu gönder.
                sockets.KomutGonder("Insert into PublicKeys (MailAdress, PublicKey) values('"
                    + kullaniciGirisBilgileri[0] + "', '" + str_Key + "')");
            }
            else
            {
                //Programda kullanılmak üzere sakla.
                rsa.User_RSA_Public_Key = publicKey;
                rsa.User_RSA_Private_Key = privateKey;
            }
        }

        #region Arayüz yönlendirme fonksiyonları (Events)

        //Yeni Posta'ya tıklandığında çalışır.
        //Sağ tarafta yeni eposta ekranını aç.
        private void Yeni_Posta_Button_Selected(object sender, RoutedEventArgs e)
        {
            //Yazı rengini açık maviye döndür.
            Yeni_Posta_Button.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#69afe5"));

            //Yönlendir.
            YeniPosta yeniPosta = new YeniPosta(aes, rsa, kullaniciGirisBilgileri, sockets);
            AnaSayfa_Frame.Navigate(yeniPosta);
        }

        //Gelen Kutusu'na tıklandığında çalışır.
        //Sağ tarafta gelen kutusunu göster.
        private void Gelen_Kutusu_Button_Selected(object sender, RoutedEventArgs e)
        {
                
            //Yazı rengini açık maviye döndür.
            Gelen_Kutusu_Button.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#69afe5"));

            //Gelen kutusunun önceden yratılıp yaratılmadığını kontrol et ve yönlendir.
            if (GelenKutusu_Tut == null)
            {
                GelenKutusu gelenKutusu = new GelenKutusu(mails, AnaSayfa_Frame, aes, rsa, kullaniciGirisBilgileri, sockets);
                GelenKutusu_Tut = gelenKutusu;
                AnaSayfa_Frame.Navigate(gelenKutusu);
            }
            else
            {
                AnaSayfa_Frame.Navigate(GelenKutusu_Tut);
            }
        }

        //Kullanıcı değiştire tıklandığında çalışır.
        private void Kullanici_Degistir_Button_Selected(object sender, RoutedEventArgs e)
        {
            //Yazı rengini açık maviye döndür.
            Kullanici_Degistir_Button.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#69afe5"));

            //Yönlendir.
            mainFrame.Navigate(LoginScreen);
        }

        #endregion

        #region Arayüz animasyon fonksiyonları ve arayüz bugları için fonksyonlar

        //Aşağıdaki fonksiyonlar arayüzün güzel gözükmesi için tasarlandı.
        //Basılan objeye göre Foreground renk değişimi gerçekleştirirler.
        private void Yeni_Posta_Button_Unselected(object sender, RoutedEventArgs e)
        {
            //Yazı rengini beyaza geri döndür.
            Yeni_Posta_Button.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#b2b6bf"));
        }

        private void Gelen_Kutusu_Button_Unselected(object sender, RoutedEventArgs e)
        {
            //Yazı rengini beyaza geri döndür.
            Gelen_Kutusu_Button.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#b2b6bf"));
        }

        private void Kullanici_Degistir_Button_Unselected(object sender, RoutedEventArgs e)
        {
            //Yazı rengini beyaza geri döndür.
            Kullanici_Degistir_Button.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#b2b6bf"));
        }

        //Gelen kutusu Butonuna mouse tıklandıktan sonra çalışır.
        private void Gelen_Kutusu_Button_MouseUp(object sender, MouseButtonEventArgs e)
        {
            //Gelen kutusu yeniden açılmama bugu için yazıldı.
            Gelen_Kutusu_Button.IsSelected = false;
        }

        #endregion

    }
}
