using System.Security.Cryptography;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System;

namespace Email_Project
{
    public class RSA_Algoritmasi
    {
        //Public alan tanımları.
        public byte[] User_RSA_Public_Key;
        public byte[] User_RSA_Private_Key;

        //Private alan tanımları.
        private AES_Algoritmasi aes;
        private static readonly int bitUzunlugu = 2048;
        private RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(bitUzunlugu);

        //Byte dönüşümü için, dönüştürücü sınıfı
        UnicodeEncoding ByteConverter = new UnicodeEncoding();

        //Constructor
        public RSA_Algoritmasi(AES_Algoritmasi aes)
        {
            this.aes = aes;
        }

        public string RSA_Sifrele(string metin, byte[] PublicKey)
        {

            rsa.ImportRSAPublicKey(PublicKey, out int a);

            //String metni byte'a çevir, şifrele ve string olarak döndür.
            byte[] Byte_Metin = ByteConverter.GetBytes(metin);
            byte[] Byte_Sifreli = rsa.Encrypt(Byte_Metin, RSAEncryptionPadding.Pkcs1);
            return Convert.ToBase64String(Byte_Sifreli);
        }

        public string RSA_Sifre_Coz(string SifreliMetin, byte[] PrivateKey)
        {

            rsa.ImportRSAPrivateKey(PrivateKey, out int b);

            //Stringi byte'a çevir, metni çöz ve string olarak döndür.
            byte[] Byte_Sifreli = Convert.FromBase64String(SifreliMetin);
            byte[] Byte_Cozulmus = rsa.Decrypt(Byte_Sifreli, RSAEncryptionPadding.Pkcs1);
            return ByteConverter.GetString(Byte_Cozulmus);
        }

        public string Imzala(string metin, byte[] PrivateKey)
        {
            //İmzalama işleminde kendimize ait Private key kullanılır.
            rsa.ImportRSAPrivateKey(PrivateKey, out int b);

            //Stringi byte'a çevir, imzala ve imzayı string olarak döndür.
            byte[] Byte_Metin = ByteConverter.GetBytes(metin);
            byte[] Byte_Imza = rsa.SignData(Byte_Metin, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return Convert.ToBase64String(Byte_Imza);
        }

        public bool ImzaKontrol(string metin, string imza, byte[] PublicKey)
        {
            //İmza kontrolünde karşı tarafın Public anahtarı kullanılır.
            rsa.ImportRSAPublicKey(PublicKey, out int a);

            //imzayı ve metni byte'a çevir, imzayı kontrol et.
            byte[] Byte_Metin = ByteConverter.GetBytes(metin);
            byte[] Byte_Imza = Convert.FromBase64String(imza);

            return rsa.VerifyData(Byte_Metin, Byte_Imza,
                HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }

        public List<byte[]> KeyGenerator(string mailAdress)
        {
            //RSA oluştur.
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(bitUzunlugu);

            //Key'leri çıkart.
            byte[] publicKey = rsa.ExportRSAPublicKey();
            byte[] privateKey = rsa.ExportRSAPrivateKey();

            //Key'leri return işlemi için listeye at.
            List<byte[]> keys = new List<byte[]>();
            keys.Add(publicKey);
            keys.Add(privateKey);

            return keys;
        }

        //Dosyadan Public key oku.
        public byte[] PublicKeyOku(string mailAdress)
        {
            string ayirac = "!";
            int ayiracIndex = 0;
            string satir;

            string path = "RSA_Public_Keys.txt";

            if (!File.Exists(path))
                return null;

            System.IO.StreamReader file = new System.IO.StreamReader(path);

            while ((satir = file.ReadLine()) != null)
            {
                //Ayıracın indexi ve satırdan alınan mail adresi tutulur.
                ayiracIndex = satir.IndexOf(ayirac);
                string temp_adress = satir.Substring(0, ayiracIndex);

                //mail adresleri eşleşirse string olarak ilgili key çekilir.
                if (mailAdress == temp_adress)
                {
                    //Key'i oku.
                    string str_Key_Encrypted = satir.Substring(ayiracIndex + 1);

                    //Byte[] haline geri getir ve döndür.
                    file.Close();
                    return Convert.FromBase64String(str_Key_Encrypted);
                }
            }
            file.Close();
            return null;
        }

        //Dosyadan Private key oku.
        public byte[] PrivateKeyOku(string mailAdress)
        {
            string ayirac = "!";
            int ayiracIndex = 0;
            string satir;

            string path = "RSA_Private_Keys.txt";

            if (!File.Exists(path))
                return null;

            System.IO.StreamReader file = new System.IO.StreamReader(path);

            while ((satir = file.ReadLine()) != null)
            {
                //Ayıracın indexi ve satırdan alınan mail adresi tutulur.
                ayiracIndex = satir.IndexOf(ayirac);
                string temp_adress = satir.Substring(0, ayiracIndex);

                //mail adresleri eşleşirse string olarak ilgili key çekilir.
                if (mailAdress == temp_adress)
                {
                    //Key'i oku.
                    string str_Key_Encrypted = satir.Substring(ayiracIndex + 1);

                    //Byte[] haline geri getir ve döndür.
                    file.Close();
                    return Convert.FromBase64String(str_Key_Encrypted);
                }
            }
            file.Close();
            return null;
        }

        //Public keyi dosyaya yaz.
        public async void PublicKeyYaz(string mailAdress, byte[] publicKey)
        {
            //"!" mail adresi ile keyi ayırır.
            //Dosya yapısı her satır "mail!Key" şeklindedir.

            //Key'i String'e çevir.
            string str_Key = Convert.ToBase64String(publicKey);

            //Dosyaya satır olarak yaz.
            //txt yoksa bir tane oluştur ve dosyaya yaz.
            string path = "RSA_Public_Keys.txt";
            if (!File.Exists(path))
            {
                using StreamWriter file = new StreamWriter(path, true);
                await file.WriteLineAsync(mailAdress + "!" + str_Key);
                file.Close();
            }
            else if (File.Exists(path))
            {
                using StreamWriter file = new StreamWriter(path, true);
                await file.WriteLineAsync(mailAdress + "!" + str_Key);
                file.Close();
            }
        }

        //Private keyi dosyaya yaz.
        public async void PrivateKeyYaz(string mailAdress, byte[] privateKey)
        {
            //"!" mail adresi ile keyi ayırır.
            //Dosya yapısı her satır "mail!Key" şeklindedir.

            //Key'i stringe çevir.
            string str_Key = Convert.ToBase64String(privateKey);

            //Dosyaya satır olarak yaz.
            //txt yoksa bir tane oluştur ve dosyaya yaz.
            string path = "RSA_Private_Keys.txt";
            if (!File.Exists(path))
            {
                using StreamWriter file = new StreamWriter(path, true);
                await file.WriteLineAsync(mailAdress + "!" + str_Key);
                file.Close();
            }
            else if (File.Exists(path))
            {
                using StreamWriter file = new StreamWriter(path, true);
                await file.WriteLineAsync(mailAdress + "!" + str_Key);
                file.Close();
            }
        }
    }
}
