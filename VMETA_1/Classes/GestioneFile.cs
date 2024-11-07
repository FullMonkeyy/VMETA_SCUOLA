using System.Xml.Serialization;
using System;
using VMETA_1.Entities;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.Serialization;
using System.Net;

namespace VMETA_1.Classes
{
    public static class GestioneFile
    {
        static public string PATH = "RubricaPersona.xml";
        static public string path1 = "C:\\Users\\irond\\source\\repos\\Progetto_Vanessa_Gemini_GUI\\Progetto_Vanessa_Gemini_GUI\\bin\\Debug\\net8.0-windows\\InizializeXMLConversation.xml";
        static public string path2 = @"C:\Users\irond\source\repos\Progetto_Vanessa_Gemini_GUI\Progetto_Vanessa_Gemini_GUI\bin\Debug\net8.0-windows\CronologiaMessaggiVanessa.xml";
        static public string path3 = @"bin\Debug\net8.0\CronologiaMessaggiVanessa.xml";
        static public string _requestPath = @"RequestRegister.xml";
        static string _chatsPath = "TelegramChats.xml";

        static string ftpAddress = "ftp://ftp.scapellatodavide.altervista.org/TELEGRAMCHAT.xml";  // URL FTP di destinazione
        static string ftpUsername = "scapellatodavide";  // Nome utente FTP
        static string ftpPassword = "ft9pAyc9B5Zd";  // Password FTP

        static Mutex _mtxRequest = new Mutex();

        static public string Read(string path)
        {
            string text = "NIENTE";
            try
            {
                StreamReader sr = new StreamReader(path);
                text = sr.ReadToEnd();
                sr.Close();
            }
            catch (Exception ex)
            {

            }
            return text;

        }
        static public bool Write(string path, string text)
        {

            try
            {
                StreamWriter sw = new StreamWriter(@path, false);
                sw.Write(text);
                sw.Close();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }


        }
        static public void WriteXMLConversation(string path, List<Messaggio> lm)
        {

            try
            {

                StreamWriter sw = new StreamWriter(@path, false);
                XmlSerializer xmls = new XmlSerializer(typeof(List<Messaggio>));
                xmls.Serialize(sw, lm);
                sw.Close();

            }
            catch (IOException e)
            {

                Console.WriteLine("Non sono riuscito a salvare la cronologia della conversazione");

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }


        }
        static public List<Messaggio> ReadXMLConversation(string path)
        {

            List<Messaggio> tmp = new List<Messaggio>();

            try
            {

                StreamReader sw = new StreamReader(@path);
                XmlSerializer xmls = new XmlSerializer(typeof(List<Messaggio>));
                tmp.AddRange((List<Messaggio>)xmls.Deserialize(sw));
                sw.Close();
                for (int i = 0; i < tmp.Count; i++)
                {
                    tmp[i].Message = tmp[i].Message.Trim(' ', '\n');
                }

            }
            catch (IOException e)
            {

                Console.WriteLine("Non sono riuscito a caricare la cronologia della conversazione");

            }
            catch (Exception e)
            {

                Console.WriteLine(e.Message);
            }
            return tmp;

        }
        static public void WriteXMLRubrica(string path, List<Person> rb)
        {

            try
            {

                StreamWriter sw = new StreamWriter(@path, false);
                XmlSerializer xmls = new XmlSerializer(typeof(List<Person>));
                xmls.Serialize(sw, rb);
                sw.Close();

            }
            catch (IOException e)
            {

                Console.WriteLine("Non sono riuscito a salvare la rubrica");

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }


        }
        static public List<Person> ReadXMLRubrica(string path)
        {
            List<Person> tmp = new List<Person>();

            try
            {

                StreamReader sw = new StreamReader(@path);
                XmlSerializer xmls = new XmlSerializer(typeof(List<Person>));
                tmp = (List<Person>)xmls.Deserialize(sw);
                sw.Close();

            }
            catch (IOException e)
            {

                Console.WriteLine("Non sono riuscito a caricare la rubrica");

            }
            catch (Exception e)
            {

                Console.WriteLine(e.Message);

            }


            return tmp;

        }
        static public void WriteXMLTelegramChats(Dictionary<long, Dictionary<DateTime, int>> chats)
        {

            try
            {
                DataContractSerializer serializer = new DataContractSerializer(typeof(Dictionary<long, Dictionary<DateTime, int>>));
                var writer = XmlWriter.Create(@_chatsPath);
                serializer.WriteObject(writer, chats);
                writer.Close();

            }
            catch (IOException e)
            {

                Console.WriteLine("Non sono riuscito a salvare le chat telegram");

            }
            catch (Exception e)
            {

                Console.WriteLine(e.Message);
            }
        }
        static public Dictionary<long, Dictionary<DateTime, int>> ReadXMLTelegramChats()
        {
            Dictionary<long, Dictionary<DateTime, int>> dct = new Dictionary<long, Dictionary<DateTime, int>>();

            if (File.Exists(@_chatsPath))
            {
                var reader = XmlReader.Create(@_chatsPath);
                try
                {

                    DataContractSerializer serializer = new DataContractSerializer(typeof(Dictionary<long, Dictionary<DateTime, int>>));
                    dct = (Dictionary<long, Dictionary<DateTime, int>>)serializer.ReadObject(reader);
                    reader.Close();

                }
                catch (IOException e)
                {

                    Console.WriteLine("Non sono riuscito a leggere le chat telegram");
                    reader.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    reader.Close();
                }
            }


            return dct;

        }
        static public void WriteXMLRequestRegister(List<RegisterRequest> listaRichieste)
        {
            StreamWriter sw = null;
            try
            {

                _mtxRequest.WaitOne();
                sw = new StreamWriter(_requestPath, false);
                XmlSerializer xmls = new XmlSerializer(typeof(List<RegisterRequest>));
                xmls.Serialize(sw, listaRichieste);
                _mtxRequest.ReleaseMutex();
                sw.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                if (sw != null)
                    sw.Close();
            }


        }
        static public List<RegisterRequest> ReadXMLRequestRegister()
        {
            List<RegisterRequest> tmp = new List<RegisterRequest>();
            StreamReader sw = null;
            try
            {

                _mtxRequest.WaitOne();
                if (File.Exists(_requestPath))
                {
                    sw = new StreamReader(_requestPath);
                    XmlSerializer xmls = new XmlSerializer(typeof(List<RegisterRequest>));
                    tmp.AddRange((List<RegisterRequest>)xmls.Deserialize(sw));
                    _mtxRequest.ReleaseMutex();
                    sw.Close();
                }
                else
                {
                    WriteXMLRequestRegister(tmp);
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                if (sw != null)
                    sw.Close();

            }

            return tmp;
        }


        static public List<string> GetCSVLines(string filePath)
        {

            List<string> lines = new List<string>();
            try
            {
                // Leggi tutte le righe del file
                lines = File.ReadAllLines(filePath).ToList();


            }
            catch (IOException ex)
            {
                Console.WriteLine($"Errore nella lettura del file: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore: {ex.Message}");
            }

            return lines;

        }


        static public void WriteFTP(string filePath)
        {
            try
            {
                // Creazione richiesta FTP
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpAddress);
                request.Method = WebRequestMethods.Ftp.UploadFile;

                // Impostazione credenziali FTP
                request.Credentials = new NetworkCredential(ftpUsername, ftpPassword);
                request.UsePassive = true;
                request.UseBinary = true;
                request.KeepAlive = false;

                // Lettura del file da caricare
                byte[] fileContents;
                using (StreamReader sourceStream = new StreamReader(filePath))
                {
                    fileContents = System.Text.Encoding.UTF8.GetBytes(sourceStream.ReadToEnd());
                }

                // Imposta la lunghezza del contenuto
                request.ContentLength = fileContents.Length;

                // Scrittura dei dati sul server FTP
                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(fileContents, 0, fileContents.Length);
                }

                // Ricezione della risposta dal server FTP
                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    Console.WriteLine($"Caricamento completato, stato: {response.StatusDescription}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore: {ex.Message}");
            }
        }

    }
}
