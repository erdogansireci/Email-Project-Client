using System.Security.Cryptography;
using System.Text;
using System.IO;
using System;

namespace Email_Project
{
    public class AES_Algoritmasi
    {
        //Private alan tanımları
        private Aes aes = Aes.Create();

        //Public alan tanımları
        //Giriş yapan kullanıcıya ait keyi tutar.
        public byte[] user_AES_Key;
        public byte[] user_AES_IV;

        #region App_Keys

        public byte[] App_Key =
        {
            65,52,104,5,249,73,250,90,108,129,251,74,93,225,
            50,77,188,34,144,57,174,12,75,10,239,18,104,253,113,4,95,216
        };
        public byte[] App_IV =
        {
            114,86,160,143,91,41,138,204,94,172,147,39,41,105,17,63
        };

        #endregion

        public string AES_Sifrele(string metin, byte[] Key, byte[] IV)
        {
            byte[] encrypted;

            aes.Key = Key;
            aes.IV = IV;

            // Şifreleme için şifreleyici oluştur.
            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            // Şifreleme için gerekli streamleri oluştur.
            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        //Verileri streame yaz.
                        swEncrypt.Write(metin);
                    }

                    //Şifreli veriyi streamden byte[] olarak oku.
                    encrypted = msEncrypt.ToArray();

                    // Şifreli metni yeniden stringe çevir ve geri döndür.
                    return Convert.ToBase64String(encrypted);
                }
            }
            
        }

        public string AES_Sifre_Coz(string cipherText, byte[] Key, byte[]IV)
        {
            aes.Key = Key;
            aes.IV = IV;

            // Streamda çalışmak üzere şifre çözücü oluştur.
            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            //String'i byte'a çevir.
            byte[] byteArray = Convert.FromBase64String(cipherText);

            //Şifre çözmek için gerekli streamleri oluştur.
            using (MemoryStream msDecrypt = new MemoryStream(byteArray))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        //Çözülmüş metni streamden oku ve dön.
                        return srDecrypt.ReadToEnd();
                    }
                }
            }
        }

        public string[] KeyGenerator()
        {
            //Key oluştur.
            Aes aes = Aes.Create();

            //Key'leri return işlemi için listeye koy.
            string[] keys = new string[2];
            keys[0] = Convert.ToBase64String(aes.Key);
            keys[1] = Convert.ToBase64String(aes.IV);

            return keys;
        }

        public void AES_Dosya_Sifrele(string path, byte[] Key, byte[] IV)
        {
            //create output file name
            FileStream fsCrypt = new FileStream(path + ".aes", FileMode.Create);

            aes.Key = Key;
            aes.IV = IV;

            // Şifreleme için şifreleyici oluştur.
            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            // Şifreleme için gerekli streamleri oluştur.
            CryptoStream csEncrypt = new CryptoStream(fsCrypt, encryptor, CryptoStreamMode.Write);

            FileStream fsIn = new FileStream(path, FileMode.Open);

            //create a buffer (1mb) so only this amount will allocate in the memory and not the whole file
            byte[] buffer = new byte[1048576];
            int read;

            try
            {
                while ((read = fsIn.Read(buffer, 0, buffer.Length)) > 0)
                {
                    csEncrypt.Write(buffer, 0, read);
                }

                // Close up
                fsIn.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            finally
            {
                csEncrypt.Close();
                fsCrypt.Close();
            }
        }

        public string AES_Dosya_Sifre_Coz(string Encrypted_File_Path, byte[] Key, byte[] IV)
        {
            string output_File_Path = Encrypted_File_Path.Replace(".aes", "");

            FileStream fsCrypt = new FileStream(Encrypted_File_Path, FileMode.Open);

            aes.Key = Key;
            aes.IV = IV;

            // Streamda çalışmak üzere şifre çözücü oluştur.
            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            CryptoStream cs = new CryptoStream(fsCrypt, decryptor, CryptoStreamMode.Read);

            FileStream fsOut = new FileStream(output_File_Path, FileMode.Create);

            int read;
            byte[] buffer = new byte[1048576];

            try
            {
                while ((read = cs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    fsOut.Write(buffer, 0, read);
                }
            }
            catch (CryptographicException ex_CryptographicException)
            {
                Console.WriteLine("CryptographicException error: " + ex_CryptographicException.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            try
            {
                cs.Close();
                return output_File_Path;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error by closing CryptoStream: " + ex.Message);
                return output_File_Path;
            }
            finally
            {
                fsOut.Close();
                fsCrypt.Close();
                
            }
        }
    }
}
