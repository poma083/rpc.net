using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;

namespace PDUDatas
{
    public class SCZI
    {
        public static byte[] Decrypt(byte[] encodedEnvelopedCms, X509Certificate2 certificate)
        {
            // Создаем объект для декодирования и расшифрования.
            EnvelopedCms envelopedCms = new EnvelopedCms();

            // Декодируем сообщение.
            envelopedCms.Decode(encodedEnvelopedCms);

            // Выводим количество получателей сообщения
            // (в данном примере должно быть равно 1) и
            // алгоритм зашифрования.
            StringBuilder sb = new StringBuilder();
            DisplayEnvelopedCms(envelopedCms, false, ref sb);
            Logger.Log.Debug(sb.ToString());

            // Расшифровываем сообщение для единственного 
            // получателя.
            Logger.Log.Debug("Расшифрование ... ");
            envelopedCms.Decrypt(envelopedCms.RecipientInfos[0], new X509Certificate2Collection(certificate));
            Logger.Log.Debug("Выполнено.");

            // После вызова метода Decrypt в свойстве ContentInfo 
            // содержится расшифрованное сообщение.
            return envelopedCms.ContentInfo.Content;
        }
        public static byte[] Encrypt(byte[] msg, X509Certificate2 certificate)
        {
            // Помещаем сообщение в объект ContentInfo 
            // Это требуется для создания объекта EnvelopedCms.
            ContentInfo contentInfo = new ContentInfo(msg);

            // Создаем объект EnvelopedCms, передавая ему
            // только что созданный объект ContentInfo.
            // Используем идентификацию получателя (SubjectIdentifierType)
            // по умолчанию (IssuerAndSerialNumber).
            // Не устанавливаем алгоритм зашифрования тела сообщения:
            // ContentEncryptionAlgorithm устанавливается в 
            // RSA_DES_EDE3_CBC, несмотря на это, при зашифровании
            // сообщения в адрес получателя с ГОСТ сертификатом,
            // будет использован алгоритм GOST 28147-89.
            EnvelopedCms envelopedCms = new EnvelopedCms(contentInfo);

            // Создаем объект CmsRecipient, который 
            // идентифицирует получателя зашифрованного сообщения.
            CmsRecipient recip1 = new CmsRecipient(SubjectIdentifierType.IssuerAndSerialNumber, certificate);

            Logger.Log.DebugFormat("Зашифровываем данные для одного получателя с именем \"{0}\" ...", recip1.Certificate.SubjectName.Name);
            // Зашифровываем сообщение.
            envelopedCms.Encrypt(recip1);
            Logger.Log.DebugFormat("Выполнено.");

            // Закодированное EnvelopedCms сообщение содержит
            // зашифрованный текст сообщения и информацию
            // о каждом получателе данного сообщения.
            return envelopedCms.Encode();
        }

        public static X509Certificate2 FindCertificate(StoreName storeName, StoreLocation storeLocation, string thumbprint)
        {
            X509Store store = null;
            try
            {
                store = new X509Store(storeName, storeLocation);
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                X509Certificate2Collection collection = (X509Certificate2Collection)store.Certificates;
                Logger.Log.DebugFormat("Number of certificates: {0}", collection.Count);
                foreach (X509Certificate2 x509 in collection)
                {
                    try
                    {
                        Logger.Log.DebugFormat("Friendly Name: {0}", x509.FriendlyName);
                        Logger.Log.DebugFormat("Signature Algorithm: {0}", x509.SignatureAlgorithm.FriendlyName);
                        Logger.Log.DebugFormat("Simple Name: {0}", x509.GetNameInfo(X509NameType.SimpleName, true));
                        Logger.Log.DebugFormat("Thumbprint: {0}", x509.Thumbprint);
                        if (thumbprint.ToLower().Equals(x509.Thumbprint.ToLower()))
                        {
                            Logger.Log.Debug("find");
                            return x509;
                        }
                    }
                    catch (CryptographicException)
                    {
                        Logger.Log.Info("Information could not be written out for this certificate.");
                    }
                }
                return null;
            }
            finally
            {
                if (store != null)
                {
                    store.Close();
                }
            }
        }

        public static void ValidateCertificate(X509Certificate2 certificate)
        {
            throw new NotImplementedException();
        }

        // Отображаем свойство ContentInfo объекта EnvelopedCms 
        static private void DisplayEnvelopedCmsContent(String desc, EnvelopedCms envelopedCms, ref StringBuilder sb)
        {
            sb.AppendFormat(desc + " (длина \"{0}\"):  ", envelopedCms.ContentInfo.Content.Length);
            foreach (byte b in envelopedCms.ContentInfo.Content)
            {
                sb.Append(b.ToString() + " ");
            }
            sb.AppendLine();
        }
        // Отображаем некоторые свойства объекта EnvelopedCms.
        static private void DisplayEnvelopedCms(EnvelopedCms e, Boolean displayContent, ref StringBuilder sb)
        {
            sb.AppendFormat("\"{0}\"Закодированное CMS/PKCS #7 Сообщение.\"{0}\" + Информация:", Environment.NewLine);
            sb.AppendFormat("\tАлгоритм шифрования сообщения:\"{0}\"", e.ContentEncryptionAlgorithm.Oid.FriendlyName);
            sb.AppendFormat("\tКоличество получателей закодированного CMS/PKCS #7 сообщения:\"{0}\"", e.RecipientInfos.Count);
            for (int i = 0; i < e.RecipientInfos.Count; i++)
            {
                sb.AppendFormat("\tПолучатель \"#{0}\" тип \"{1}\".", i + 1, e.RecipientInfos[i].RecipientIdentifier.Type);
            }
            if (displayContent)
            {
                DisplayEnvelopedCmsContent("Закодированное CMS/PKCS " + "#7 содержимое", e, ref sb);
            }
            sb.AppendLine();
        }
    }
}
