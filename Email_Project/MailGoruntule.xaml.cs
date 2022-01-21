using System.Windows.Controls;
using System.Windows;
using EAGetMail;
using System;
using System.Windows.Media;
using System.IO;
using Microsoft.Win32;

namespace Email_Project
{
    public partial class MailGoruntule : Page
    {
        #region Private alan tanımları
        string[] kullaniciGirisBilgileri;
        private AES_Algoritmasi aes;
        private RSA_Algoritmasi rsa;
        private Sockets sockets;
        private Mail mail;
        private byte[]  Key = null;
        private byte[] IV = null;

        #endregion

        //Constructor
        public MailGoruntule(Mail mail, AES_Algoritmasi aes, RSA_Algoritmasi rsa, string[] kullaniciGirisBilgileri, Sockets sockets)
        {
            this.kullaniciGirisBilgileri = kullaniciGirisBilgileri;
            this.sockets = sockets;
            this.aes = aes;
            this.rsa = rsa;
            this.mail = mail;

            InitializeComponent();

            //İşlemek üzere mailden texti al.
            string tempMetin = mail.TextBody;
            //Mail sonundaki boş kısmı sil.
            tempMetin = tempMetin.Remove(tempMetin.Length - 4);

            # region AES Key'i metinden ayır.

            string AES_Key_Encrypted = null;
            string KeyIsareti = "!AESKey";
            if (tempMetin.Contains(KeyIsareti))
            {
                //KeyIsareti indexi bul.
                int index = tempMetin.IndexOf(KeyIsareti);

                //AES Key'i al.
                AES_Key_Encrypted = tempMetin.Substring(index + 7);

                //Key'i ve işareti metinden sil.
                tempMetin = tempMetin.Replace(AES_Key_Encrypted, "");
                tempMetin = tempMetin.Replace(KeyIsareti, "");
            }

            #endregion

            #region İmzayı ayır.
            string dijitalImza = null;
            string imzaIsareti = "!Signature";
            if (tempMetin.Contains(imzaIsareti))
            {

                //Ekranda imzaya dair bilgi göster.
                DijitalImza_Bilgi.Visibility = Visibility.Visible;

                //String indexi al.
                int sonIndex = tempMetin.IndexOf(imzaIsareti);

                //İmzayı al.
                dijitalImza = tempMetin.Substring(sonIndex + 10);

                //İmzayı ve işareti metinden sil.
                tempMetin = tempMetin.Replace(dijitalImza, "");
                tempMetin = tempMetin.Replace(imzaIsareti, "");
                
            }

            #endregion

            #region AES Key'i RSA ile çöz.

            if (AES_Key_Encrypted != null)
            {

                //Şifreyi çöz
                string str_AES_Key = rsa.RSA_Sifre_Coz(AES_Key_Encrypted, rsa.User_RSA_Private_Key);

                //Key ve IV'yi ayır. (Format = KEY#IV)
                int index = str_AES_Key.IndexOf("#");
                string str_Key = str_AES_Key.Substring(0, index);
                string str_IV = str_AES_Key.Substring(index + 1);

                Key = Convert.FromBase64String(str_Key);
                IV = Convert.FromBase64String(str_IV);

            }

            #endregion

            #region Şifreli metni çöz.

            string sifreliMetinIsareti = "!SifreliMetin";
            if (tempMetin.Contains(sifreliMetinIsareti))
            {
                //Ekranda şifreli metin olduğuna dair bilgi mesajı göster.
                Sifre_Bilgi.Visibility = Visibility.Visible;

                //İşareti sil
                tempMetin = tempMetin.Replace(sifreliMetinIsareti, "");

                //Deşifre et.
                tempMetin = aes.AES_Sifre_Coz(tempMetin, Key, IV);
            }

            #endregion

            #region Konu'yu çöz.

            //Konuyu çöz ve maile geri yaz.
            if(AES_Key_Encrypted != null)
            {
                if (mail.Subject.Contains(" (Trial Version)"))
                    mail.Subject = mail.Subject.Replace(" (Trial Version)", "");

                mail.Subject = aes.AES_Sifre_Coz(mail.Subject, Key, IV);
            }
            

            #endregion

            #region Tarih ve saati yeniden kontrol için ayır.

            string str_tarih = null;
            string yenidengonderimKontrolIsareti = "!YenidenGonderimKontrol";
            if (tempMetin.Contains(yenidengonderimKontrolIsareti))
            {
                //İşareti metinden kaldır.
                tempMetin = tempMetin.Replace(yenidengonderimKontrolIsareti, "");

                //Tarihi ve saati al.
                str_tarih = tempMetin.Substring(tempMetin.Length - 17, 17);

                //Saat bilgisini metinden çıkart.
                tempMetin = tempMetin.Replace(str_tarih, "");

            }

            #endregion

            #region İmza kontrolü yap.

            if(dijitalImza != null)
            {
                //Dosyadan Public keyi oku (maili gönderen kullanıcının)
                byte[] publicKey = rsa.PublicKeyOku(mail.From.Address);

                //Eğer lokalde yoksa, sunucudan gönderenin public keyini iste.
                if(publicKey == null)
                {
                    string str_RSA_PublicKey = sockets.KomutGonder("Select PublicKey from PublicKeys where convert" +
                    " (VARCHAR, MailAdress) = '" + mail.From.Address + "'");

                    if (str_RSA_PublicKey.Equals("-1"))
                    {
                        //Hatayı yazdır ve mail gönderimini iptal et.
                        MailGoruntule_Metin.Text = mail.From.Address + "adresi sunucuda bulunamadı.\n" +
                            "Metni görüntüleyemezsiniz.!";
                        return;
                    }

                    //Buraya gelirse key okundu demektir. Byte[] yap ve lokale kaydet.
                    publicKey = Convert.FromBase64String(str_RSA_PublicKey);
                    rsa.PublicKeyYaz(mail.From.Address, publicKey);
                }

                //İmza'yı kontrol et.
                bool ImzaSonuc = rsa.ImzaKontrol(tempMetin, dijitalImza, publicKey);
                if (!ImzaSonuc)
                {
                    tempMetin = "\n\n\nDİKKAT!! \n\nMESAJ İÇERİĞİ KİMLİĞİ BELİRSİZ KİŞİLER TARAFINDAN " +
                        "DEĞİŞTİRİLDİĞİNDEN DOLAYI METNİ ENGELLEMİŞ BULUNMAKTAYIZ!";

                    mail.Subject = "Engellendi";

                    //Mesaj içeriği farklı olduğundan oluşturulan mesajı kapat. 
                    DijitalImza_Bilgi.Visibility = Visibility.Collapsed;
                    //Aşağıda saat parse edilemeyeceğinden aşağıdaki if'e girmeyi engelle.
                    //Ayrıca içerik değişimi olayı yeniden gönderimden daha kritik.
                    str_tarih = null;
                }
            }

            #endregion

            #region Yeniden gönderim kontrolü yap.

            if(str_tarih != null)
            {
                //Tarihi Parse et.
                DateTime gonderimTarihi = DateTime.ParseExact(str_tarih, "HH:mm:ss dd/MM/yy", null);

                //Mailin gönderim tarihi ile metin içindeki tarihin farkını hesapla.
                TimeSpan fark = mail.SentDate.Subtract(gonderimTarihi);

                //Gönderim farkı 10dk'dan fazla ise mesaj engellenir.
                if (fark.TotalSeconds > 600)
                {
                    tempMetin = "DİKKAT YENİDEN GÖNDERİM ATAĞI TESPİT EDİLDİ. " +
                        "MAIL İÇERİĞİ ENGELLENMİŞTİR.";

                    mail.Subject = "Engellendi";

                }
            }

            #endregion

            #region Attachment'leri yerleştir.
            //Attachment şifreleri kaydedilirken çözülür.
            //Bkz. Attachment_Indir fonksiyonu.

            Attachment[] attachments = mail.Attachments;
            if (attachments.Length != 0)
            {
                double marginX = 31;
                double marginOffset = 55;
                foreach (var item in attachments)
                {

                    //Buton tanımla.
                    Button button = ButonTanimla(marginX, item);

                    //Attachment'i ekrana ekle.
                    MailGoruntule_Grid.Children.Add(button);

                    //Yeni attahment için konumu kaydır.
                    marginX += marginOffset;
                }
            }

                #endregion

            #region Ekrana yaz.

                if (mail.Subject.Contains(" (Trial Version)"))
                    mail.Subject = mail.Subject.Replace(" (Trial Version)", "");

            //Mail bileşenlerini uygun yerlere yazdır.
            MailGoruntule_Konu.Text = mail.Subject;
            MailGoruntule_From.Text = mail.From.Address;
            MailGoruntule_To.Text = mail.To[0].Address;
            MailGoruntule_Metin.Text = tempMetin;

            #endregion

        }

        //Ekranda bulunan attachmentlerden birine tıklanınca çalışır.
        //Attachmentlerin şifresini çözüp kaydeder.
        private void Attachment_Indir(object sender, RoutedEventArgs e)
        {
            //Attachment'i al.
            var button = e.OriginalSource as Button;
            Attachment attachment = (Attachment)button.DataContext;

            //Şifreli dosyayı bilgisayara kaydet.
            attachment.SaveAs(attachment.Name, true);

            //Şifresini çöz ve çözülmüş dosyanın yolunu al.
            string path = aes.AES_Dosya_Sifre_Coz(attachment.Name, Key, IV);

            //Kaydedilen şifreli dosyayı sil.
            File.Delete(attachment.Name);

            //Diyalog üzerinden çözülmüş metnin nereye kaydedileceğini seç.
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            //Dialog başlatıcı ayarları.
            saveFileDialog.FileName = attachment.Name.Substring(0, attachment.Name.IndexOf(".aes"));
            saveFileDialog.OverwritePrompt = true;
            saveFileDialog.Title = "Ek kaydet";

            //Seçilen yere dosyayı kaydet.
            if (saveFileDialog.ShowDialog() == true)
                File.Move(path, saveFileDialog.FileName, true);


        }

        private Button ButonTanimla(double marginX, Attachment item)
        {

            //Attachment'i görüntüleyecek butonu oluştur ve özelliklerini belirle.
            Button button = new Button();

            button.Background = (SolidColorBrush)(new BrushConverter().ConvertFromString("#00000000"));
            button.BorderBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#69afe5"));
            button.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#b2b6bf"));
            button.HorizontalAlignment = HorizontalAlignment.Left;
            button.VerticalAlignment = VerticalAlignment.Top;
            button.HorizontalContentAlignment = HorizontalAlignment.Left;
            button.VerticalContentAlignment = VerticalAlignment.Top;
            button.Margin = new Thickness(marginX, 454, 0, 0);
            button.BorderThickness = new Thickness(0.4);
            button.FontStyle = FontStyles.Italic;
            button.FontSize = 12;
            button.Height = 20;
            button.Width = 50;
            button.Click += Attachment_Indir;
            button.Content = item.Name;
            button.DataContext = item;
            button.ToolTip = ToolTipTanimla(item);

            return button;
        }

        private ToolTip ToolTipTanimla(Attachment item)
        {
            //Tooltip özelliklerini belirle.
            ToolTip toolTip = new ToolTip();
            toolTip.Content = item.Name;
            toolTip.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#262626"));
            toolTip.BorderThickness = new Thickness(0.8);
            toolTip.BorderBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#69afe5"));
            toolTip.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#b2b6bf"));
            toolTip.Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse;
            toolTip.HasDropShadow = true;

            return toolTip;
        }
    }
}
