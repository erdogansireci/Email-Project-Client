using System.Windows.Controls;
using Microsoft.Win32;
using System.Windows;
using EASendMail;
using System.IO;
using System;

namespace Email_Project
{
    public partial class YeniPosta : Page
    {
        #region Private alan tanımları
        private Sockets sockets;
        private AES_Algoritmasi aes;
        private RSA_Algoritmasi rsa;
        private string[] attachments = null;
        private string[] kullaniciGirisBilgileri = new string[2];//[0] E-Posta Adresi -- [1] Parola

        #endregion

        //Constructor
        public YeniPosta(AES_Algoritmasi aes, RSA_Algoritmasi rsa, string[] kullaniciGirisBilgileri, Sockets sockets)
        {
            this.aes = aes;
            this.rsa = rsa;
            this.sockets = sockets;
            this.kullaniciGirisBilgileri = kullaniciGirisBilgileri;

            InitializeComponent();

        }

        //Ekrandan verileri al ve maili şifreli veya şifresiz olarak gönder.
        private void Gonder_Button_Click(object sender, RoutedEventArgs e)
        {
            //Yeni mail oluştur.
            SmtpMail newMail = new SmtpMail("TryIt");

            //Mesajı al.
            string tempMetin = Mesaj.Text;

            #region ALICININ PUBLIC KEY'İNİ AL

            //İlk önce lokalde var mı kontrol et.
            byte[] publicKey = rsa.PublicKeyOku(gonderilecekPostaAdresi.Text);

            //Yoksa sunucuyu kontrol et.
            //Sunucuda da yoksa mail gönderimi iptal olur.
            if(publicKey == null)
            {
                string str_RSA_PublicKey = sockets.KomutGonder("Select PublicKey from PublicKeys where convert" +
                                " (VARCHAR, MailAdress) = '" + gonderilecekPostaAdresi.Text + "'");

                if (str_RSA_PublicKey.Equals("-1"))
                {
                    //Hatayı yazdır ve mail gönderimini iptal et.
                    Mesaj.Text = gonderilecekPostaAdresi.Text + "adresine sunucuda kayıtlı olmadığından mail atamazsınız.\n" +
                        "Kişinin daha önce uygulamaya en az 1 kere giriş yaptığına emin olun.";
                    return;
                }

                //Buraya gelirse key okundu demektir. Byte[] yap ve lokale kaydet.
                publicKey = Convert.FromBase64String(str_RSA_PublicKey);
                rsa.PublicKeyYaz(gonderilecekPostaAdresi.Text, publicKey);
            }

            #endregion

            #region METNE SAAT BİLGİSİ EKLE (Yeniden gönderimi engellemek için)

            tempMetin += DateTime.Now.ToString("HH:mm:ss") + " " + DateTime.Now.ToString("dd/MM/yy") + "!YenidenGonderimKontrol";

            #endregion

            #region METNİ AES İLE ŞİFRELE

            string[] str_AES_Keys = null;
            //Metin şifrelenecek mi? Kontrol et.
            if ((bool)Sifrele_CheckBox.IsChecked)
            {
                //Bu maile özel AES anahtarı oluştur.
                //[0] = Key
                //[1] = IV
                str_AES_Keys = aes.KeyGenerator();

                byte[] AES_Key = Convert.FromBase64String(str_AES_Keys[0]);
                byte[] AES_IV = Convert.FromBase64String(str_AES_Keys[1]);
                //Metni şifrele.
                tempMetin = aes.AES_Sifrele(tempMetin, AES_Key, AES_IV);

                // Şifrelendiğini anlamak için işaretçi ekle.
                tempMetin += "!SifreliMetin";
            }

            #endregion

            #region KONU'YU ŞİFRELE.

            //if true ise keyler oluşturulmuştur. Yani metin şifrelenecektir.
            //Konuyu şifrele
            if(str_AES_Keys != null)
            {
                byte[] AES_Key = Convert.FromBase64String(str_AES_Keys[0]);
                byte[] AES_IV = Convert.FromBase64String(str_AES_Keys[1]);

                Konu.Text = aes.AES_Sifrele(Konu.Text, AES_Key, AES_IV);
            }
            


            #endregion

            #region ATTACHMENT'LERİ MAIL'E EKLE.

            if (attachments != null && str_AES_Keys != null)
            {
                foreach (var fileName in attachments)
                {
                    //AES Key'leri oluştur.
                    byte[] AES_Key = Convert.FromBase64String(str_AES_Keys[0]);
                    byte[] AES_IV = Convert.FromBase64String(str_AES_Keys[1]);

                    //Dosyayı şifrele.
                    aes.AES_Dosya_Sifrele(fileName, AES_Key, AES_IV);

                    //Attachment'i çek. (Şifreli dosya isim formatı= isim + .aes)
                    Attachment attachment = newMail.AddAttachment(fileName + ".aes");

                    //Şifreli dosyayı bilgisayardan sil.
                    File.Delete(fileName + ".aes");
                }
            }

            #endregion

            #region DİJİTAL İMZA

            if ((bool)Imzala_CheckBox.IsChecked)
            {
                string dijitalImza = Mesaj.Text;
                
                //Metnin hashini al.
                //Kendi private keyin ile şifrele.
                dijitalImza = rsa.Imzala(dijitalImza, rsa.User_RSA_Private_Key);

                //İşaret ekle. ( "!Signature" ).
                tempMetin += "!Signature";

                //Maile ekle.
                tempMetin += dijitalImza;
            }

            #endregion

            #region AES ANAHTARINI RSA İLE ŞİFRELE VE METNE EKLE

            //Format = KEY#IV
            if (str_AES_Keys != null)
            {
                string AES_Keys = str_AES_Keys[0] + "#" + str_AES_Keys[1];
                string AES_Key_Encrypted = rsa.RSA_Sifrele(AES_Keys, publicKey);

                //İşaret ekle.
                tempMetin += "!AESKey";

                //Maile ekle.
                tempMetin += AES_Key_Encrypted;
            }

            #endregion

            #region MAIL GONDER
            //Maili göndermeye başla.
            try
            {
                //Maili bileşenlerini tanımla.
                newMail.From = kullaniciGirisBilgileri[0];
                newMail.To = gonderilecekPostaAdresi.Text;
                newMail.Subject = Konu.Text;
                newMail.TextBody = tempMetin;

                //Server bağlantı özelliklerini tanımla.
                SmtpServer server = new SmtpServer("smtp.gmail.com");
                server.User = kullaniciGirisBilgileri[0];
                server.Password = kullaniciGirisBilgileri[1];
                server.Port = 465;
                server.ConnectType = SmtpConnectType.ConnectSSLAuto;

                SmtpClient client = new SmtpClient();
                client.SendMail(server, newMail);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Mail gonderilemedi. Hata nedeni: ");
                Console.WriteLine(exception.Message);
            }
            #endregion
        }

        //Eklenecek Attachmentlerin dosya yollarını çeker.
        private void Gozat_Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();

            fileDialog.Title = "Ek seçimi";
            fileDialog.Multiselect = true;

            if ((bool)fileDialog.ShowDialog())
            {
                //Dosya yollarını maili gönderirken
                //kullanmak üzere kaydet.
                attachments = fileDialog.FileNames;

            }
        }
    }
}
