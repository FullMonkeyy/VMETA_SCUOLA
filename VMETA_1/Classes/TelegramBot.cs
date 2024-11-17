using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.AspNetCore.Identity.Data;
using OllamaSharp.Models.Chat;
using OllamaSharp;
using VMETA_1.Entities;
using System.Threading;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using static System.Net.Mime.MediaTypeNames;
using System.Collections.Generic;
using System;
using Message = Telegram.Bot.Types.Message;
using System.Linq;
using System.Security.Cryptography.Xml;
using Microsoft.Win32.SafeHandles;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.RegularExpressions;

namespace VMETA_1.Classes
{
    public class TelegramBot
    {
        public long DavideID{get;set;}
        string BotToken;
        SchoolContext schoolContext;
        public TelegramBotClient botClient { get; set; }
        ReceiverOptions receiverOptions;
        CancellationTokenSource cts;

        List<Person> topten;
        Thread Classifica;
        Dictionary<long,Problem> WritingProblems;
        Dictionary<long, Letter> WritingLetterss;
        Dictionary<long, Announcement> WritingAnnoucement;
        Dictionary<long, int> StateCounter;
        Mutex classificaMtx;
        Dictionary<long, Dictionary<DateTime,int>> MesssagesIdPerChat;
        bool active;
        SemaphoreSlim mtx;
        Queue<Problem> ProblemiQueue;
        Thread QualityChecker;
        int NumMaxEmails;

        public delegate void DelegatoEvento(object sender,Problem p);
        public event DelegatoEvento ProblemaPronto;

        public delegate void DelegatoEventoLettera(object sender, Letter p);
        public event DelegatoEventoLettera LetteraPronta;

        public delegate void DelegatoEventoAnnuncio(object sender, Announcement p);
        public event DelegatoEventoAnnuncio AnnuncioPronta;

        public delegate void DelegatoEventoReStart(object sender);
        public event DelegatoEventoReStart RiavvioNecessario;


        public delegate void DelegatoEventoRegisterRequest(object sender, RegisterRequest p, string classe, long tmptelegram);
        public event DelegatoEventoRegisterRequest RichiestaDaCompletare;


        public TelegramBot(string api,SchoolContext sc)
        {
            mtx = new SemaphoreSlim(1, 4);
            MesssagesIdPerChat =GestioneFile.ReadXMLTelegramChats();
            QualityChecker = new Thread(CheckChatQuality);
            QualityChecker.IsBackground = true;
            QualityChecker.Start();

            WritingProblems= new Dictionary<long,Problem>();
            WritingLetterss = new Dictionary<long, Letter>();
            WritingAnnoucement = new Dictionary<long, Announcement>();
            StateCounter = new Dictionary<long, int>();
            active = true;

            botClient = new TelegramBotClient(api);
            //botClient.Timeout = new TimeSpan(0, 5, 0);
                cts = new CancellationTokenSource();
            schoolContext = sc;
            DavideID = 1140272456;
            topten = new List<Person>();
            classificaMtx = new Mutex();
            CreateRank();
            Classifica = new Thread(TrustPointsRank);
            Classifica.IsBackground = true;
            Classifica.Start();
 
            receiverOptions = new()
            {
                AllowedUpdates = Array.Empty<UpdateType>() // receive all update types except ChatMember related updates
            };
           
            botClient.StartReceiving(
                     updateHandler: HandleUpdateAsync,
                     pollingErrorHandler: HandlePollingErrorAsync,                     
                     receiverOptions: receiverOptions,
                     cancellationToken: cts.Token
            );

            NumMaxEmails = 100;

        }
        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Telegram.Bot.Types.Update update, CancellationToken cancellationToken)
        {

            

            if (active)
            {
                if (update.Message != null )
                {
                    if (update.Message.Text != null)
                    {
                        string lettereAccentate = "1234567890ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzàèìòùáéíóúâêîôûäëïöüçñ ./:,;!?€$'-()=@";
                        if (update.Message.Text.All(c =>lettereAccentate.Contains(c)))
                        {
                            string text_message = update.Message.Text;
                            long id = update.Message.Chat.Id;

                            ADDTOCHAT(id, update.Message.MessageId);

                            bool convalidazione = false;
                            bool writing = false;
                            if (text_message.StartsWith("/code:"))
                            {

                                Person tmp33 = await schoolContext.Students.FirstOrDefaultAsync(x => x.TelegramId.Equals(id));

                                if (tmp33==null) {
                                    await CLEAR(id);
                                    string[] tmp = text_message.Split(':');
                                    if (tmp.Length == 2)
                                    {
                                        string tmpcodice = tmp[1];
                                        List<RegisterRequest> tmr = GestioneFile.ReadXMLRequestRegister();
                                        foreach (RegisterRequest r in tmr)
                                        {

                                            if (r.Code.Equals(tmpcodice))
                                            {

                                                Person p = schoolContext.Students.FirstOrDefault(x => x.Name.Equals(r.Name) && x.Surname.Equals(r.Surname));
                                                p.TelegramId = id;
                                              
                                                r.isRegistred = true;
                                                schoolContext.SaveChanges();
                                                convalidazione = true;

                                            }

                                        }




                                        if (convalidazione)
                                        {

                                            RegisterRequest p = tmr.Find(x => x.isRegistred && x.Code.Equals(tmpcodice));
                                            tmr.Remove(p);
                                            GestioneFile.WriteXMLRequestRegister(tmr);
                                            await SendMessage($"Convalidazione riuscita!\nBenvenuto {p.Name}!\nSpero che ti potrò tornare utile!", id);
                                            await Menu(id);


                                        }
                                        else
                                        {
                                            await SendMessage($"Codice errato... ", id);
                                        }


                                    }
                                    else
                                    {
                                        await SendMessage($"Sintassi del codice Errata... \nControllare che non sia stato messo alcuno spazio.", id);
                                    }
                                }
                                else
                                {
                                    await SendMessage($"Ciao {tmp33.Name} sei già registrato.", id);
                                }
                            }                            
                            else if (text_message.StartsWith("/start"))
                            {
                                await CLEAR(id);
                                await SendMessage("Vanessa Meta avviata", id);
                                await SendMessage("--- Versione 0.1 BetaRelease ---", id, false);

                                if (schoolContext.Students.ToList().Exists(x => x.TelegramId.Equals(id)))
                                {
                                    
                                    await Menu(id);
                                }
                                else
                                {
                                    await SendMessage("Salve, si prega di autenticarsi contattando l'amministratore di sistema.", id);
                                }
                            }
                            else if (text_message.StartsWith("/email:"))
                            {
                                int numtmp = schoolContext.Students.Count();
                                if (numtmp > NumMaxEmails)
                                {

                                    Person tmp33 = await schoolContext.Students.FirstOrDefaultAsync(x => x.TelegramId.Equals(id));

                                    if (tmp33 == null)
                                    {
                                        await CLEAR(id);
                                        string[] tmp = text_message.Split(':');
                                        if (tmp.Length == 2)
                                        {
                                            string emailRegex = @"^s-([a-zA-Z0-9]{2,})\.([a-zA-Z0-9]{2,})@isiskeynes\.it$";
                                            string tmpemail = tmp[1].Trim();
                                            if (Regex.IsMatch(tmpemail, emailRegex))
                                            {

                                                List<string> lines = GestioneFile.GetCSVLines("nomi_cognomi_classi.csv");
                                                List<string> linesemail = GestioneFile.GetCSVLines("Email.csv");
                                                string[] attributes, attributes2;
                                                string name = "";
                                                string surname = "";
                                                string classe = "";

                                                foreach (string line in linesemail)
                                                {

                                                    attributes = line.Split(",");



                                                    if (attributes.Count() == 3)
                                                    {
                                                        if (attributes[2].Equals(tmpemail))
                                                        {
                                                            name = attributes[0];
                                                            surname = attributes[1];

                                                            foreach (string lineclasse in lines)
                                                            {
                                                                attributes2 = lineclasse.Split(";");
                                                                if (attributes2.Count() == 3)
                                                                {



                                                                    if (attributes2[0].ToLower().Contains(surname.ToLower()) && attributes2[1].ToLower().Contains(name.ToLower()))
                                                                    {

                                                                        classe = attributes2[2];
                                                                        break;
                                                                    }

                                                                }
                                                            }
                                                            break;
                                                        }
                                                    }

                                                }

                                                if (name != "" && surname != "" && classe != "")
                                                {

                                                    name = char.ToUpper(name[0]) + name.Substring(1).ToLower();
                                                    surname = char.ToUpper(surname[0]) + surname.Substring(1).ToLower();
                                                    await SendMessage("Ok, ho preparato una richiesta di registrazione", id);

                                                    RichiestaDaCompletare(this, new RegisterRequest(name, surname, "NOT SETTED", tmpemail), classe, id);
                                                }
                                                else
                                                    await SendMessage("Non sono riuscita a recuperare le tue informazioni attraverso l'email...", id);






                                            }
                                            else
                                            {
                                                await SendMessage("Formattazione dell'email sbagliata.", id);
                                            }


                                        }
                                    }
                                    else
                                    {
                                        await SendMessage($"Ciao {tmp33.Name}, sei già registrato.", id);
                                    }
                                }
                                else
                                {
                                    await SendMessage("Numero massimo di registrazioni raggiunte.", id);

                                }
                            }
                            else if (schoolContext.Students.ToList().Exists(x => x.TelegramId.Equals(id)))
                            {

                                if (WritingProblems.ContainsKey(id))
                                {
                                    writing = true;
                                    if (WritingProblems[id].Category != null && WritingProblems[id].isStudente != null && !WritingProblems[id].AI_Analyzing)
                                    {
                                        Problem problem = WritingProblems[id];

                                        if (problem.Title == "-NOT SETTED5353453453435375698")
                                        {

                                            if (text_message.Contains("<") && text_message.Contains(">"))
                                            {

                                                await SendMessage("Ci hai provato ahahahahhaah.\nTogli subito le parentesi angolari (<, >).\n\nRiscrivi il titolo.", id);

                                            }
                                            else if (text_message.Length > 50)
                                            {
                                                await SendMessage("Il titolo non può superare la lunghezza di 50 caratteri\nNumero caratteri inseriti: " + text_message.Length + "\n\nRiscrivi il titolo", id);
                                            }
                                            else
                                            {

                                                problem.Title = text_message;
                                                if (problem.Description == "-NOT SETTED5353453453435375698")
                                                {
                                                    string text = "";
                                                    if (problem.isStudente.Equals("true"))
                                                    {
                                                        text = problem.Person.Name + " " + problem.Person.Surname + " " + problem.Person.Classroom.ToString() + " perciò si potrà visualizzare il nome e la classe del mittente.";
                                                    }
                                                    else
                                                    {
                                                        text = $"rappresentante di classe, ciò implica l'anonimato del mittente e lascia visualizzare solo la classe {problem.Person.Classroom.ToString()}";
                                                    }


                                                    await SendMessage($"Il titolo scelto è ''{problem.Title}''\n\nAdesso inserisci la descrizione del problema mantenendo un tono educato e non volgare senza essere in alcun modo offensivo. \n\nSi raccomanda quindi una descrizione obbiettiva e coincisa dei fatti.\nRicorda, segnali il problema in qualità di {text} ", id);

                                                }
                                            }

                                        }
                                        else if (problem.Description == "-NOT SETTED5353453453435375698")
                                        {

                                            if (text_message.Contains("<") && text_message.Contains(">"))
                                            {

                                                await SendMessage("Ci hai provato ahahahahhaah.\nTogli subito le parentesi angolari (<, >).\n\nRiscrivi il titolo.", id);

                                            }
                                            else if (text_message.Length > 600)
                                            {
                                                await SendMessage("La descrizione non può superare la lunghezza di 600 caratteri\nNumero caratteri inseriti: " + text_message.Length + "\n\nRiscrivi il titolo.", id);
                                            }
                                            else
                                            {
                                                problem.Description = text_message;
                                                if(problem.Solution == "-NOT SETTED5353453453435375698")
                                                await ProponiSoluzioni(id);
                                            }



                                        }
                                        else if (problem.Solution == "-NOT SETTED5353453453435375698")
                                        {

                                            problem.Solution = text_message;

                                        }

                                        if (problem.Description != "-NOT SETTED5353453453435375698" && problem.Title != "-NOT SETTED5353453453435375698" && problem.Solution != "-NOT SETTED5353453453435375698")
                                        {

                                            await Riepilogo(id, false);

                                        }
                                    }

                                }
                                if (WritingLetterss.ContainsKey(id))
                                {
                                    if (!(bool)WritingLetterss[id].AI_Analyzing)
                                    {
                                        writing = true;
                                        if (text_message.Contains("<") && text_message.Contains(">"))
                                        {

                                            await SendMessage("Ci hai provato ahahahahhaah.\nTogli subito le parentesi angolari (<, >)\nRiscrivi il titolo. ", id);

                                        }
                                        else if (text_message.Length > 250)
                                        {
                                            await SendMessage("Il messaggio non può superare la lunghezza di 250 caratteri\nNumero caratteri inseriti: " + text_message.Length, id);
                                        }
                                        else
                                        {
                                            WritingLetterss[id].Body = text_message;
                                            await RiepilogoLettera(id);
                                        }
                                    }
                                }
                                if (WritingAnnoucement.ContainsKey(id))
                                {

                                    if (!WritingAnnoucement[id].AI_Analyzing)
                                    {
                                        writing = true;
                                        if (text_message.Contains("<") && text_message.Contains(">"))
                                        {

                                            await SendMessage("Ci hai provato ahahahahhaah.\nTogli subito le parentesi angolari (<, >).\nRiscrivi il titolo.", id);

                                        }
                                        else if (text_message.Length > 500)
                                        {
                                            await SendMessage("Il messaggio non può superare la lunghezza di 500 caratteri\nNumero caratteri inseriti: " + text_message.Length, id);
                                        }
                                        else
                                        {
                                            if (WritingAnnoucement[id].Title == null)
                                            {
                                                WritingAnnoucement[id].Title = text_message;

                                                if (WritingAnnoucement[id].Description == null)
                                                    await SendMessage("Bene adesso scrivi la descrizione", id);
                                            }
                                            else if (WritingAnnoucement[id].Description == null)
                                            {

                                                if (text_message.Contains("<") && text_message.Contains(">"))
                                                {

                                                    await SendMessage("Ci hai provato ahahahahhaah.\nTogli subito le parentesi angolari (<, >).\nRiscrivi il titolo.", id);

                                                }
                                                else if (text_message.Length > 400)
                                                {
                                                    await SendMessage("Il messaggio non può superare la lunghezza di 400 caratteri\nNumero caratteri inseriti: " + text_message.Length, id);
                                                }
                                                else
                                                {
                                                    WritingAnnoucement[id].Description = text_message;



                                                }
                                            }

                                            if (WritingAnnoucement[id].Description != null && WritingAnnoucement[id].Title != null)
                                            {
                                                //Inviare il riepilogo
                                                await RiepilogoAnnuncio(id);
                                            }
                                        }
                                    }

                                }                               
                                if(!writing)
                                {
                                    await CLEAR(id);
                                    await Menu(id);

                                }


                            }
                            else
                            {
                                schoolContext.Students.ToList();
                                await SendMessage($"Utente sconosciuto. Contattare amministratore di sistema. ", id);
                            }
                        }
                        else
                        {

                            if (update.Message != null)
                            {
                                long id = update.Message.Chat.Id;
                                await SendMessage("E' stato utilizzato un set di caratteri non consentito (ma che lingua parli bro).\nUnica punteggiatura ritenuta accettabile: ./:,;!?€$'-()=\nNon si possono inserire emoji\nRiprova a scrivere il messaggio correttamente facendo attenzione ai caratteri usati", id);
                                ADDTOCHAT(id, update.Message.MessageId);
                            }

                        }

                    }
                   
                }
                else
                {
                    if (update.CallbackQuery != null)
                    {
                    
                        string callbackData = update.CallbackQuery.Data;
                        long FromId = update.CallbackQuery.From.Id;

                        if (schoolContext.Students.ToList().Exists(x => x.TelegramId.Equals(FromId))) {
                            string callback;

                            //await botClient.DeleteMessageAsync(FromId, update.CallbackQuery.Message.MessageId);
                            await CLEAR(FromId);
                            switch (callbackData)
                            {
                                case "callback_data_1":
                                    // Handle button 1 click


                                    await botClient.AnswerCallbackQueryAsync(update.CallbackQuery.Id, $"Problema di {schoolContext.Students.FirstOrDefault(x => x.TelegramId.Equals(FromId)).Name}");
                                    Problem newproblem = new Problem();
                                    newproblem.isStudente = "true";
                                    newproblem.Person = schoolContext.Students.Include(x => x.Classroom).FirstOrDefault(x => x.TelegramId.Equals(FromId));
                                    newproblem.Classroom = schoolContext.Students.Include(x => x.Classroom).FirstOrDefault(x => x.TelegramId.Equals(FromId)).Classroom;
                                    newproblem.TrustPoints = (double)newproblem.Person.TrustPoints;
                                    if (WritingLetterss.ContainsKey(FromId))
                                    {
                                        if (!(bool)WritingLetterss[FromId].AI_Analyzing)
                                            WritingLetterss.Remove(FromId);
                                    }
                                    if(WritingProblems.ContainsKey(FromId))
                                    {
                                        if (!(bool)WritingProblems[FromId].AI_Analyzing)
                                            WritingProblems.Remove(FromId);
                                        else
                                        {
                                            await SendMessage("E' già in corso l'analisi di un problema. Attendere la fine dell'analisi", FromId);
                                            await Menu(FromId);
                                            break;
                                        }
                                    }
                                    if(WritingAnnoucement.ContainsKey(FromId))
                                    {
                                        if (!(bool)WritingAnnoucement[FromId].AI_Analyzing)
                                            WritingAnnoucement.Remove(FromId);
                                    }
                                  

                                    WritingProblems.Add(FromId, newproblem);

                                    await MandaPulsantiCategorie(FromId);




                                    break;
                                case "callback_data_2":
                                    // Handle button 2 click
                                    await botClient.AnswerCallbackQueryAsync(update.CallbackQuery.Id, $"Problema della classe {schoolContext.Students.Include(x => x.Classroom).FirstOrDefault(x => x.TelegramId.Equals(FromId)).Classroom.ToString()}");
                                    newproblem = new Problem();
                                    newproblem.isStudente = "false";
                                    newproblem.Person = schoolContext.Students.FirstOrDefault(x => x.TelegramId.Equals(FromId));
                                    newproblem.Classroom = schoolContext.Students.Include(x => x.Classroom).FirstOrDefault(x => x.TelegramId.Equals(FromId)).Classroom;
                                    newproblem.TrustPoints = (double)newproblem.Person.TrustPoints;
                                    newproblem.TrustPoints += 2;
                                    if (WritingLetterss.ContainsKey(FromId))
                                    {
                                        if (!(bool)WritingLetterss[FromId].AI_Analyzing)
                                            WritingLetterss.Remove(FromId);
                                    }
                                    if (WritingProblems.ContainsKey(FromId))
                                    {
                                        if (!(bool)WritingProblems[FromId].AI_Analyzing)
                                            WritingProblems.Remove(FromId);
                                        else
                                        {
                                            await SendMessage("E' già in corso l'analisi di un problema. Attendere la fine dell'analisi", FromId);
                                            await Menu(FromId);
                                            break;
                                        }
                                    }
                                    if (WritingAnnoucement.ContainsKey(FromId))
                                    {
                                        if (!(bool)WritingAnnoucement[FromId].AI_Analyzing)
                                            WritingAnnoucement.Remove(FromId);
                                    }
                                    WritingProblems.Add(FromId, newproblem);

                                    await MandaPulsantiCategorie(FromId);
                                    break;
                                case "callback_data_3":
                                    if (WritingProblems.ContainsKey(FromId))
                                    {
                                        WritingProblems[FromId].Category = "Infrastrutture";
                                        await botClient.AnswerCallbackQueryAsync(update.CallbackQuery.Id, $"Problemi di {WritingProblems[FromId].Category}");
                                        await SendMessage("Scrivi adesso il titolo del problema", FromId);
                                    }
                                    else
                                    {
                                        await SendMessage("Procedura scaduta", FromId);
                                        await Riepilogo(FromId, false);

                                    }
                                    break;
                                case "callback_data_4":
                                    if (WritingProblems.ContainsKey(FromId))
                                    {
                                        WritingProblems[FromId].Category = "Bullismo";
                                        await botClient.AnswerCallbackQueryAsync(update.CallbackQuery.Id, $"Problemi di {WritingProblems[FromId].Category}");
                                        await SendMessage("Scrivi adesso il titolo del problema", FromId);
                                    }
                                    else
                                    {
                                        await SendMessage("Procedura scaduta", FromId);
                                        await Riepilogo(FromId, false);

                                    }
                                    break;
                                case "callback_data_5":
                                    if (WritingProblems.ContainsKey(FromId))
                                    {
                                        WritingProblems[FromId].Category = "Relazione professori-studenti";
                                        await botClient.AnswerCallbackQueryAsync(update.CallbackQuery.Id, $"Problemi di {WritingProblems[FromId].Category}");
                                        await SendMessage("Scrivi adesso il titolo del problema", FromId);
                                    }
                                    else
                                    {
                                        await SendMessage("Procedura scaduta", FromId);
                                        await Riepilogo(FromId, false);

                                    }
                                    break;
                                case "callback_data_6":
                                    if (WritingProblems.ContainsKey(FromId))
                                    {
                                        WritingProblems[FromId].Category = "Altro";
                                        await SendMessage("Scrivi adesso il titolo del problema", FromId);
                                    }
                                    else
                                    {
                                        await SendMessage("Procedura scaduta", FromId);
                                        await Riepilogo(FromId, false);

                                    }
                                    break;
                                case "callback_data_7":

                                    InlineKeyboardMarkup keyboard;
                                    Message m;

                                    foreach (Problem pr in schoolContext.Problems.Where(x => x.Person.TelegramId.Equals(FromId)))
                                    {

                                        callback = "ID_PROBLEM_" + pr.Id;

                                        keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("Visualizza dettagli", callback), } });

                                        m = await botClient.SendTextMessageAsync(FromId, pr.Title, replyMarkup: keyboard);
                                        ADDTOCHAT(FromId, m.MessageId);
                                    }

                                    keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("Torna indietro", "callback_data_23"), } });

                                    var mees = await botClient.SendTextMessageAsync(FromId, "Questo è tutto, premi il pulsante qui sotto per tornare al menu,", replyMarkup: keyboard);
                                    ADDTOCHAT(FromId, mees.MessageId);
                                    break;
                                case "callback_data_8":

                                    Person pet = await schoolContext.Students.Include(x=> x.Announcements).FirstOrDefaultAsync(x => x.TelegramId.Equals(FromId));
                                    bool procced = false;
                                    DateTime tmp = DateTime.Now;
                                    

                                    if (pet.Announcements.TrueForAll(x => (tmp - x.DataInserimento).Days > 7))
                                        procced = true;
                                    else
                                    {
                                        int giornoSettimana = (int)tmp.DayOfWeek;
                                        if (giornoSettimana == 0)
                                        {
                                            giornoSettimana = 7;
                                        }
                                        int differenza = giornoSettimana - 1;
                                        DateTime lunedi = tmp.AddDays(-differenza);
                                        DateTime Max = pet.Announcements.Max(x => x.DataInserimento);
                                        Announcement daverificare = pet.Announcements.Find(x => x.DataInserimento.Equals(Max));
                                        if (daverificare.DataInserimento < lunedi)
                                        {
                                            procced = true;
                                        }
                                 
                                    }

                                    if (procced)
                                    {

                                        await botClient.AnswerCallbackQueryAsync(update.CallbackQuery.Id, $"Annuncio di {pet.Name}");
                                        Announcement newannouncement = new Announcement();

                                        newannouncement.Announcer = schoolContext.Students.Include(x => x.Classroom).FirstOrDefault(x => x.TelegramId.Equals(FromId));
                                        int tmppp;
                                        if (int.TryParse(newannouncement.Announcer.Classroom.Year, out tmppp))
                                        {
                                            newannouncement.ClassroomYEAR = tmppp;
                                        }
                                        if (WritingLetterss.ContainsKey(FromId))
                                        {
                                            if (!(bool)WritingLetterss[FromId].AI_Analyzing)
                                                WritingLetterss.Remove(FromId);
                                        }
                                        if (WritingProblems.ContainsKey(FromId))
                                        {
                                            if (!(bool)WritingProblems[FromId].AI_Analyzing)
                                                WritingProblems.Remove(FromId);
                                        }
                                        if (WritingAnnoucement.ContainsKey(FromId))
                                        {
                                            if (!(bool)WritingAnnoucement[FromId].AI_Analyzing)
                                                WritingAnnoucement.Remove(FromId);
                                            else
                                            {
                                                await SendMessage("E' già in corso l'analisi di un annuncio. Attendere la fine dell'analisi", FromId);
                                                await Menu(FromId);
                                                break;
                                            }


                                        }

                                        WritingAnnoucement.Add(FromId, newannouncement);
                                        await SendMessage("L'annuncio è una funzionalità speciale che permetterà di mantenere l'anonimato tra gli studenti.\n\n-Il funzionamento della coda si basa sul principio TrustPoints assengato per studente.\n\n-Si può richiedere di inserire un solo annuncio per settimana (non accumulabile) \n\n-E' severamente vietato scrivere testi diffamatori o non conformi con le linee guide standard di una organizzazione sana.\n\nPerfetto, ora scrivi il titolo dell'annuncio.  ", FromId);
                                    }
                                    else
                                    {
                                        await SendMessage("Mi dispiace ma hai già inserito un annuncio questa settimana. Lunedì prossimo potrai nuovamente reinserire un annuncio", FromId);
                                        await Menu(FromId);
                                    }

                                    break;
                                case "callback_data_9":
                                    //write to another classroom

                                    pet = await schoolContext.Students.Include(x => x.Letters).FirstOrDefaultAsync(x => x.TelegramId.Equals(FromId));                                    
                                    procced = false;
                                    tmp = DateTime.Now;

                                   

                                    if (pet.Letters.TrueForAll(x => (tmp - x.InsertionDate).Days > 7))
                                        procced = true;
                                    else
                                    {
                                        int giornoSettimana = (int)tmp.DayOfWeek;
                                        if (giornoSettimana == 0)
                                        {
                                            giornoSettimana = 7;
                                        }
                                        int differenza = giornoSettimana - 1;
                                        DateTime lunedi = tmp.AddDays(-differenza);

                                        List<Letter> tmmp = pet.Letters.Where(x=> x.Author.Equals(pet.ToString())).ToList();
                                        if (tmmp.Count > 0)
                                        {
                                            DateTime Max = tmmp.Max(x => x.InsertionDate);

                                            Letter daverificare = pet.Letters.Find(x => x.InsertionDate.Equals(Max));
                                            if (daverificare.InsertionDate < lunedi)
                                            {
                                                procced = true;
                                            }
                                        }
                                        else
                                        {
                                            procced = true;
                                        }

                                    }
                                    int _c_counter;
                                    List<List<InlineKeyboardButton>> bottoni;
                                    List<InlineKeyboardButton> bottoniRiga;
                                    if (procced)
                                    {
                                         _c_counter = 0;

                                        bottoni = new List<List<InlineKeyboardButton>>();
                                        bottoniRiga = new List<InlineKeyboardButton>();
                                        foreach (Classroom cl in schoolContext.Classrooms)
                                        {

                                            callback = "ID_CLASSROOM_" + cl.Id;

                                            InlineKeyboardButton bottone = InlineKeyboardButton.WithCallbackData(cl.ToString(), callback);
                                            bottoniRiga.Add(bottone);
                                            if (_c_counter <= 2)
                                            {
                                                _c_counter++;

                                            }
                                            else
                                            {
                                                _c_counter = 0;
                                                bottoni.Add(bottoniRiga);
                                                bottoniRiga = new List<InlineKeyboardButton>();
                                            }

                                        }
                                        if (_c_counter >= 0)
                                        {
                                            bottoni.Add(bottoniRiga);
                                        }


                                        keyboard = new InlineKeyboardMarkup(bottoni);

                                        m = await botClient.SendTextMessageAsync(FromId, "Seleziona la classe", replyMarkup: keyboard);
                                        ADDTOCHAT(FromId, m.MessageId);

                                        keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("Torna indietro", "callback_data_23"), } });
                                        mees = await botClient.SendTextMessageAsync(FromId, "Questo è tutto, schiaccia il pulsante qui sotto per tornare al menu,", replyMarkup: keyboard);
                                        ADDTOCHAT(FromId, mees.MessageId);

                                    }
                                    else
                                    {

                                        await SendMessage("Mi dispiace ma hai già mandato un messaggio elaborato questa settimana. Lunedì prossimo potrai nuovamente mandare un altro messaggio", FromId);
                                        await Menu(FromId);

                                    }
                                    break;
                                case "callback_data_10":

                                 

                                    keyboard = new InlineKeyboardMarkup(new[]
                             {
                        new []
                        {
                            InlineKeyboardButton.WithCallbackData("Studente", "callback_data_1"),

                        },
                            new []
                        {

                            InlineKeyboardButton.WithCallbackData("Rappresentante di classe", "callback_data_2")
                        }
                            });

                                    var res = await botClient.SendTextMessageAsync(FromId, "Il problema viene segnalato in qualità di: ", replyMarkup: keyboard);
                                    ADDTOCHAT(FromId, res.MessageId);
                                    break;
                                case "callback_data_11":

                                    if (WritingProblems.ContainsKey(FromId))
                                    {
                                        WritingProblems[FromId].Title = "-NOT SETTED5353453453435375698";
                                        await SendMessage("Scrivi adesso il nuovo titolo del problema", FromId);
                                    }
                                    else
                                    {
                                        await SendMessage("Procedura scaduta", FromId);
                                        await Riepilogo(FromId, false);

                                    }
                                    break;
                                case "callback_data_12":

                                    if (WritingProblems.ContainsKey(FromId))
                                    {
                                        WritingProblems[FromId].Description = "-NOT SETTED5353453453435375698";
                                        await SendMessage("Scrivi adesso la nuova descrizione del problema", FromId);
                                    }
                                    else
                                    {
                                        await SendMessage("Procedura scaduta", FromId);
                                        await Riepilogo(FromId, false);

                                    }
                                    break;
                                case "callback_data_13":

                                    keyboard = new InlineKeyboardMarkup(new[]
                                {
                        new []
                                {
                                    InlineKeyboardButton.WithCallbackData("Accetta", "callback_data_15"),
                                    InlineKeyboardButton.WithCallbackData("Rifiuta", "callback_data_16")

                                }
                                });
                                    var mess = await botClient.SendTextMessageAsync(FromId, "Sei sicuro?", replyMarkup: keyboard);
                                    ADDTOCHAT(FromId, mess.MessageId);
                                    break;
                                case "callback_data_14":

                                    if (WritingProblems.ContainsKey(FromId))
                                    {
                                        ProblemaPronto(this, WritingProblems[FromId]);
                                        await SendMessage("Perfetto, non appena il mio centro AI sarà disponibile lo manderò in analisi.", FromId);
                                        await Menu(FromId);
                                    }
                                    else
                                    {
                                        await SendMessage("Procedura scaduta", FromId);
                                        await Riepilogo(FromId, false);

                                    }
                                    break;
                                case "callback_data_15":

                                    if (WritingProblems.ContainsKey(FromId))
                                    {
                                        WritingProblems.Remove(FromId);
                                        await SendMessage("Operazione annullata", FromId);
                                        await Menu(FromId);
                                    }
                                    else
                                    {
                                        await SendMessage("Procedura scaduta", FromId);
                                        await Riepilogo(FromId, false);

                                    }
                                    break;
                                case "callback_data_16":
                                    await Riepilogo(FromId, false);
                                    break;
                                case "callback_data_17":

                                    await SendMessage("Perfetto, scrivi la soluzione al tuo problema.", FromId);


                                    break;
                                case "callback_data_18":

                                    if (WritingProblems.ContainsKey(FromId))
                                    {
                                        WritingProblems[FromId].Solution = "Nessuna soluzione proposta.";
                                        await Riepilogo(FromId, false);
                                    }
                                    else
                                    {
                                        await SendMessage("Procedura scaduta", FromId);
                                        await Riepilogo(FromId, false);

                                    }

                                    break;
                                case "callback_data_19":
                                    if (WritingProblems.ContainsKey(FromId))
                                    {
                                        WritingProblems[FromId].Solution = "-NOT SETTED5353453453435375698";
                                        await SendMessage("Scrivi adesso la nuova soluzione del problema", FromId);
                                    }
                                    else
                                    {
                                        await SendMessage("Procedura scaduta", FromId);
                                        await Riepilogo(FromId, false);

                                    }

                                    break;
                                case "callback_data_20":


                                    keyboard = new InlineKeyboardMarkup(new[]
                                      {
                                            new []
                                                    {
                                                        InlineKeyboardButton.WithCallbackData("NO, MODIFICA SEGNALAZIONE", "callback_data_22"),
                                                        InlineKeyboardButton.WithCallbackData("Si procedo al bypass.", "callback_data_21"),

                                                    }
                                   });

                                    var mes = await botClient.SendTextMessageAsync(FromId, "Sei veramente sicuro?", replyMarkup: keyboard);

                                    ADDTOCHAT(FromId, mes.MessageId);

                                    break;
                                case "callback_data_21":
                                    if (WritingProblems.ContainsKey(FromId))
                                    {
                                        WritingProblems[FromId].AI_Forced = true;
                                        schoolContext.Problems.Add(WritingProblems[FromId]);
                                        WritingProblems[FromId].Person.TrustPoints += 0.5;
                                        schoolContext.SaveChanges();
                                        DeleteWritingProblem(FromId);
                                        await SendMessage("Bypass effettuato con successo. Segnalazione completata.\nIn caso di abuso, si procederà con il ban.\nHai compensato con +0.5 truspoints", FromId);
                                       
                                        await Menu(FromId);
                                    }
                                    else
                                    {
                                        await SendMessage("Procedura scaduta", FromId);
                                        await Riepilogo(FromId, false);

                                    }
                                    break;
                                case "callback_data_22":
                                    await Riepilogo(FromId, false);
                                    break;
                                case "callback_data_23":
                                    await Menu(FromId);
                                    break;
                                case "callback_data_24":

                                    List<Decision> dc = schoolContext.Decisions.Include(x => x.Person).ToList();
                                    foreach (Decision dec in schoolContext.Decisions.Include(x=> x.Person).Include(x=> x.Pool).Where(x=> x.Person.TelegramId.Equals(FromId) && !x.isChosen))
                                    {

                                        callback = "ID_DECISION_" + dec.Id;

                                        keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("VOTA IL SONDAGGIO", callback), } });

                                        m = await botClient.SendTextMessageAsync(FromId, dec.PoolTitle().ToUpper(), replyMarkup: keyboard);
                                        ADDTOCHAT(FromId, m.MessageId);
                                    }

                                    keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("Torna indietro", "callback_data_23"), } });

                                    mees = await botClient.SendTextMessageAsync(FromId, "Questo è tutto, premi il pulsante qui sotto per tornare al menu,", replyMarkup: keyboard);
                                    ADDTOCHAT(FromId, mees.MessageId);

                                    break;
                                case "callback_data_25":                              

                                    if (WritingLetterss.ContainsKey(FromId))
                                    {
                                        await SendMessage("Riscrivi il nuovo messaggio", FromId);
                                        WritingLetterss[FromId].Body = null;
                                    }
                                    else
                                    {
                                        await SendMessage("Procedura scaduta", FromId);
                                        await Riepilogo(FromId, false);

                                    }
                                    break;
                                case "callback_data_26":

                                        keyboard = new InlineKeyboardMarkup(new[]
                                    {
                            new []
                                    {
                                        InlineKeyboardButton.WithCallbackData("Accetta", "callback_data_27"),
                                        InlineKeyboardButton.WithCallbackData("Rifiuta", "callback_data_28")

                                    }
                                    });
                                        mess = await botClient.SendTextMessageAsync(FromId, "Sei sicuro?", replyMarkup: keyboard);
                                        ADDTOCHAT(FromId, mess.MessageId);
                                    break;
                                case "callback_data_27":
                                    if (WritingLetterss.ContainsKey(FromId))
                                    {
                                        WritingLetterss.Remove(FromId);
                                        await SendMessage("Operazione annullata", FromId);
                                        await Menu(FromId);
                                    }
                                    else
                                    {
                                        await SendMessage("Procedura scaduta", FromId);
                                        await Riepilogo(FromId, false);

                                    }
                                    break;
                                case "callback_data_28":

                                    ///COSA SUCCEDE SE RIFIUTI DI CANCELLARE IL MESSAGGIO?
                                    await RiepilogoLettera(FromId);

                                    break;
                                case "callback_data_29":

                                    if (WritingLetterss.ContainsKey(FromId))
                                    {
                                        await SendMessage("Messaggio inviato al centro AI, non appena sarà elaborato riceverai una notifica.", FromId);

                                        LetteraPronta(this, WritingLetterss[FromId]);
                                        await Menu(FromId);
                                    }
                                    else
                                    {
                                        await SendMessage("Procedura scaduta", FromId);
                                        await Riepilogo(FromId, false);

                                    }

                                    break;
                                case "callback_data_30":

                                    keyboard = new InlineKeyboardMarkup(new[]
                                {
                               new[]{
                                    InlineKeyboardButton.WithCallbackData("Quelli che ho scritto io", "callback_data_31"),


                                },
                                new[]
                                {     InlineKeyboardButton.WithCallbackData("Quelli che ho ricevuto ", "callback_data_32")

                                }
                                ,
                                    });
                               
                          
                                    mess = await botClient.SendTextMessageAsync(FromId, "Quali messaggi vuoi visualizzare?", replyMarkup: keyboard);
                                    ADDTOCHAT(FromId, mess.MessageId);
                                    break;
                                case "callback_data_31":
                                    Person p = await schoolContext.Students.FirstOrDefaultAsync(X=> X.TelegramId.Equals(FromId));
                                    string c;
                                    Person tmp1,tmp2;
                                    foreach(Letter lett in schoolContext.Letters.Include(x=> x.People).Where(y=> y.Author.Equals(p.ToString())))
                                    {
                                        c = "ID_LETTER_" + lett.Id;

                                        keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("Visualizza dettagli", c), } });
                                       
                                        if (lett.Title.Length>0)
                                        {
                                            mees = await botClient.SendTextMessageAsync(FromId, lett.Title+"\n\nMessaggio per "+lett.Destination, replyMarkup: keyboard);
                                            ADDTOCHAT(FromId, mees.MessageId);
                                        }
                                   


                                    }

                                    keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("Torna indietro", "callback_data_23"), } });

                                    mees = await botClient.SendTextMessageAsync(FromId, "Questo è tutto, premi il pulsante qui sotto per tornare al menu,", replyMarkup: keyboard);
                                    ADDTOCHAT(FromId, mees.MessageId);
                                    break;
                                case "callback_data_32":

                                    p = await schoolContext.Students.FirstOrDefaultAsync(X => X.TelegramId.Equals(FromId));
                                  
                                   
                                    foreach (Letter lett in schoolContext.Letters.Include(x => x.People).Where(y => y.Destination.Equals(p.ToString())))
                                    {
                                        c = "ID_LETTER_" + lett.Id;

                                        keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("Visualizza dettagli", c), } });

                                        if (lett.Title.Length > 0)
                                        {
                                            mees = await botClient.SendTextMessageAsync(FromId, lett.Title + "\n\nMessaggio da " + lett.Author, replyMarkup: keyboard);
                                            ADDTOCHAT(FromId, mees.MessageId);
                                        }



                                    }

                                    keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("Torna indietro", "callback_data_23"), } });

                                    mees = await botClient.SendTextMessageAsync(FromId, "Questo è tutto, premi il pulsante qui sotto per tornare al menu,", replyMarkup: keyboard);
                                    ADDTOCHAT(FromId, mees.MessageId);

                                    break;
                                case "callback_data_33":
                                    if (WritingAnnoucement.ContainsKey(FromId))
                                    {
                                        await SendMessage("Riscrivi il nuovo titolo dell'annuncio.", FromId);
                                        WritingAnnoucement[FromId].Title = null;
                                    }
                                    else
                                    {
                                        await SendMessage("Procedura scaduta", FromId);
                                        await Riepilogo(FromId, false);

                                    }
                                    break;
                                case "callback_data_34":
                                    if (WritingAnnoucement.ContainsKey(FromId))
                                    {
                                        await SendMessage("Riscrivi la nuova descrizione dell'annuncio.", FromId);
                                        WritingAnnoucement[FromId].Description = null;
                                    }
                                    else
                                    {
                                        await SendMessage("Procedura scaduta", FromId);
                                        await Riepilogo(FromId, false);

                                    }
                                    break;
                                case "callback_data_35":
                                    keyboard = new InlineKeyboardMarkup(new[]
                            {
                            new []
                                    {
                                        InlineKeyboardButton.WithCallbackData("Accetta", "callback_data_37"),
                                        InlineKeyboardButton.WithCallbackData("Rifiuta", "callback_data_38")

                                    }
                                    });
                                    mess = await botClient.SendTextMessageAsync(FromId, "Sei sicuro?", replyMarkup: keyboard);
                                    ADDTOCHAT(FromId, mess.MessageId);
                                    break;
                                case "callback_data_36":
                                    if (WritingAnnoucement.ContainsKey(FromId))
                                    {
                                        AnnuncioPronta(this, WritingAnnoucement[FromId]);
                                        await SendMessage("Perfetto, non appena il mio centro AI sarà disponibile, manderò in analisi l'annuncio.", FromId);
                                        await Menu(FromId);
                                    }
                                    else
                                    {
                                        await SendMessage("Procedura scaduta", FromId);
                                        await Riepilogo(FromId, false);

                                    }
                                    break;
                                case "callback_data_37":
                                    if (WritingAnnoucement.ContainsKey(FromId))
                                    {
                                        WritingAnnoucement.Remove(FromId);
                                        await SendMessage("Operazione annullata", FromId);
                                        await Menu(FromId);
                                    }
                                    else
                                    {
                                        await SendMessage("Procedura scaduta", FromId);
                                        await Riepilogo(FromId, false);

                                    }
                                    break;
                                case "callback_data_38":
                                    await RiepilogoAnnuncio(FromId);
                                    break;
                                case "callback_data_39":

                                    Person per;
                                    
                                   foreach (Announcement lett in schoolContext.Announcements.Include(x=> x.Announcer).ToList())
                                   {
                                       c = "ID_ANNOUNCEMENT_" + lett.id;

                                       per = await schoolContext.Students.Include(x=>x.Classroom).FirstOrDefaultAsync(x => x.TelegramId.Equals(lett.Announcer.TelegramId));

                                       keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("Visualizza dettagli", c), } });

                                       if (lett.Title.Length > 0)
                                       {
                                           mees = await botClient.SendTextMessageAsync(FromId, lett.Title + "\n\nL'announcer è del " + lett.Announcer.Classroom.Year+"° anno", replyMarkup: keyboard);
                                           ADDTOCHAT(FromId, mees.MessageId);
                                       }



                                   }

                                   keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("Torna indietro", "callback_data_23"), } });

                                   mees = await botClient.SendTextMessageAsync(FromId, "Questo è tutto, premi il pulsante qui sotto per tornare al menu,", replyMarkup: keyboard);
                                   ADDTOCHAT(FromId, mees.MessageId);
                                    break;
                                case "callback_data_40":

                                    string totaltopstr = "CLASSIFICA TOP 10";
                                    classificaMtx.WaitOne();
                                    int counterposition = 1;
                                    foreach (Person persona in topten)
                                    {

                                        totaltopstr += $"\n\n- {counterposition}# {persona.ToString()} [{persona.TrustPoints} TrP.]";
                                        counterposition++;
                                    }
                                    classificaMtx.ReleaseMutex();
                                    await SendMessage(totaltopstr, FromId);

                                    keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("Torna indietro", "callback_data_23"), } });

                                    mees = await botClient.SendTextMessageAsync(FromId, "Premi questo pulsante per tornare al menu", replyMarkup: keyboard);
                                    ADDTOCHAT(FromId, mees.MessageId);


                                    break;
                                default:
                                   
                                    if (callbackData.StartsWith("ID_PROBLEM_"))
                                    {

                                        List<string> temp = callbackData.Split('_').ToList();
                                        int id;
                                        if (int.TryParse(temp[2], out id))
                                        {
                                            keyboard = new InlineKeyboardMarkup(new[]
                                                   {
                                                new []
                                                        {
                                                            InlineKeyboardButton.WithCallbackData("Torna al menù", "callback_data_23"),

                                                        }
                                       });

                                            mes = await botClient.SendTextMessageAsync(FromId, schoolContext.Problems.Include(x => x.Classroom).Include(x => x.Person).FirstOrDefault(x => x.Id.Equals(id)).ToString(), replyMarkup: keyboard);
                                            ADDTOCHAT(FromId, mes.MessageId);

                                        }



                                    }
                                    else if (callbackData.StartsWith("ID_CLASSROOM_"))
                                    {

                                        string classIdStr = callbackData.Split('_').ToList()[2];
                                        int classId;
                                        int.TryParse(classIdStr, out classId);

                                        Classroom cs = await schoolContext.Classrooms.Include(x => x.People).FirstOrDefaultAsync(x => x.Id.Equals(classId));
                                        List<Person> studenti = new List<Person>();
                                        studenti.AddRange(cs.People);
                                        bottoni = new List<List<InlineKeyboardButton>>();
                                        bottoniRiga = new List<InlineKeyboardButton>();
                                        _c_counter = 0;

                                        foreach (Person stu in studenti)
                                        {
                                            if (stu.TelegramId.Equals(FromId) || stu.TelegramId.Equals(-1))
                                                continue;
                                               
                                            callback = "ID_STUDENTWRITING_" + stu.Id;
                                            InlineKeyboardButton bottone = InlineKeyboardButton.WithCallbackData(stu.ToString(), callback);
                                            if (_c_counter > 1)
                                            {
                                                bottoni.Add(bottoniRiga);
                                                bottoniRiga = new List<InlineKeyboardButton>();
                                            }
                                            bottoniRiga.Add(bottone);
                                            if (_c_counter <= 1)
                                            {
                                                _c_counter++;

                                            }
                                            else
                                            {
                                                _c_counter = 0;                                               
                                            }
                                        }
                                        if (_c_counter >= 0)
                                        {
                                            bottoni.Add(bottoniRiga);
                                        }

                                        keyboard = new InlineKeyboardMarkup(bottoni);

                                        m = await botClient.SendTextMessageAsync(FromId, "Seleziona lo studente con cui parlare", replyMarkup: keyboard);
                                        ADDTOCHAT(FromId, m.MessageId);

                                        keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("Torna indietro", "callback_data_23"), } });
                                        mees = await botClient.SendTextMessageAsync(FromId, "Questo è tutto, schiaccia il pulsante qui sotto per tornare al menu,", replyMarkup: keyboard);
                                        ADDTOCHAT(FromId, mees.MessageId);


                                    }
                                    else if (callbackData.StartsWith("ID_DECISION_"))
                                    {
                                        List<string> temp = callbackData.Split('_').ToList();
                                        int id;
                                        if (int.TryParse(temp[2], out id))
                                        {
                                            Decision tmp4 = await schoolContext.Decisions.Include(x => x.Pool).FirstOrDefaultAsync(x => x.Id.Equals(id));

                                            string descp = tmp4.PoolTitle() + "\n\n" + tmp4.Pool.Descrizione;
                                            bottoni = new List<List<InlineKeyboardButton>>();
                                            bottoniRiga = new List<InlineKeyboardButton>();
                                            _c_counter = 0;
                                            int count = 0;
                                            foreach (string opt in tmp4.Pool.Options)
                                            {

                                                callback = "ID_VOTE_" + tmp4.Id + "_" + count;
                                                InlineKeyboardButton bottone = InlineKeyboardButton.WithCallbackData(opt, callback);
                                                bottoniRiga.Add(bottone);
                                                if (_c_counter < 1)
                                                {
                                                    _c_counter++;

                                                }
                                                else
                                                {
                                                    _c_counter = 0;
                                                    bottoni.Add(bottoniRiga);
                                                    bottoniRiga = new List<InlineKeyboardButton>();
                                                }
                                                count++;
                                            }
                                            if (_c_counter >= 0)
                                            {
                                                bottoni.Add(bottoniRiga);
                                            }

                                            keyboard = new InlineKeyboardMarkup(bottoni);

                                            m = await botClient.SendTextMessageAsync(FromId, descp, replyMarkup: keyboard);
                                            ADDTOCHAT(FromId, m.MessageId);

                                            keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("Torna indietro", "callback_data_23"), } });
                                            mees = await botClient.SendTextMessageAsync(FromId, "Premi questo pulsante per tornare al menù.", replyMarkup: keyboard);
                                            ADDTOCHAT(FromId, mees.MessageId);
                                        }

                                    }
                                    else if (callbackData.StartsWith("ID_VOTE_"))
                                    {
                                        List<string> temp = callbackData.Split('_').ToList();
                                        int decisionid,optionvalue;
                                        if (int.TryParse(temp[2], out decisionid) && int.TryParse(temp[3], out optionvalue))
                                        {
                                            Decision tmp5 = await schoolContext.Decisions.Include(x => x.Pool).FirstOrDefaultAsync(x => x.Id.Equals(decisionid));

                                            string selected = tmp5.Pool.Options[optionvalue];

                                            tmp5.MakeYourDecision(selected);
                                            
                                            schoolContext.SaveChanges();
                                            await SendMessage("Hai votato \"" + selected + "\"\n\nTi verranno assegnati 0.75 TrustPoints", FromId);
                                            p = await schoolContext.Students.FirstOrDefaultAsync(x => x.TelegramId.Equals(FromId));
                                            p.TrustPoints += 0.75;
                                            schoolContext.SaveChanges();
                                            await Menu(FromId);
                                        }
                                    }
                                    else if (callbackData.StartsWith("ID_STUDENTWRITING_"))
                                    {

                                        string classIdStr = callbackData.Split('_').ToList()[2];
                                        int studentId;
                                        int.TryParse(classIdStr, out studentId);

                                        per=await schoolContext.Students.Include(x=> x.Classroom).FirstOrDefaultAsync(x=> x.Id.Equals(studentId));
                                        Letter letter = new Letter();
                                        letter.Destination = per.ToString();
                                        letter.People.Add(per);
                                        letter.AI_Analyzing = false;
                                        per = await schoolContext.Students.FirstOrDefaultAsync(x => x.TelegramId.Equals(FromId));
                                        letter.Author = per.ToString();
                                        letter.TrustPoints = (double)per.TrustPoints;
                                        letter.People.Add(per);
                                        if (WritingLetterss.ContainsKey(FromId))
                                        {
                                            
                                            if (!(bool)WritingLetterss[FromId].AI_Analyzing)
                                                WritingLetterss.Remove(FromId);
                                            else
                                            {
                                                await SendMessage("E' già in corso l'analisi di un messaggio.\nAttendere la fine dell'analisi", FromId);
                                                await Menu(FromId);
                                                break;
                                            }
                                        }
                                        if (WritingProblems.ContainsKey(FromId))
                                        {
                                            if (!(bool)WritingProblems[FromId].AI_Analyzing)
                                                WritingProblems.Remove(FromId);
                                        }
                                        if (WritingAnnoucement.ContainsKey(FromId))
                                        {
                                            if (!(bool)WritingAnnoucement[FromId].AI_Analyzing)
                                                WritingAnnoucement.Remove(FromId);
                                        }
                                        WritingLetterss.Add(FromId, letter);
                                        Person pe = await schoolContext.Students.FirstOrDefaultAsync(x => x.Id.Equals(studentId));
                                        if (per != null)
                                        {
                                            await SendMessage($"Cosa vuoi che scriva a {pe.ToString()}?\n\nDigita pure qui sotto di questo messaggio ciò che vuole comunicare. Farò da intermediaria.\nSi ricordano le normali norme di rispetto reciproco.", per.TelegramId);

                                        }
                                        else
                                        {
                                            await SendMessage("Studente non trovato, probabilmente è appena stato rimosso dal sistema...", FromId);
                                        }


                                    }    
                                    else if (callbackData.StartsWith("ID_LETTER_"))
                                    {
                                        string classIdStr = callbackData.Split('_').ToList()[2];
                                        int classId;
                                        int.TryParse(classIdStr, out classId);
                                        Letter letter = await schoolContext.Letters.Include(x => x.People).FirstOrDefaultAsync(y => y.Id.Equals(classId));
                                        await SendMessage(letter.ToString(),FromId);

                                        keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("Torna indietro", "callback_data_23"), } });
                                        mees = await botClient.SendTextMessageAsync(FromId, "Premi questo pulsante per tornare al menù.", replyMarkup: keyboard);
                                        ADDTOCHAT(FromId, mees.MessageId);

                                    }
                                    else if (callbackData.StartsWith("ID_ANNOUNCEMENT_"))
                                    {
                                        string classIdStr = callbackData.Split('_').ToList()[2];
                                        int classId;
                                        int.TryParse(classIdStr, out classId);
                                        Announcement announcement = await schoolContext.Announcements.Include(x => x.Announcer).FirstOrDefaultAsync(y => y.id.Equals(classId));

                                        //await SendMessage(announcement.ToString(), FromId);

                                        try
                                        {
                                            var chat = await botClient.GetChatAsync(announcement.Announcer.TelegramId);

                                            string username = chat.Username;
                                            string link= $"https://t.me/{username}";

                                            keyboard = new InlineKeyboardMarkup(new[]
                                                       {
                                                              new[]
                                                              {
                                                                 new InlineKeyboardButton("Rispondi in privato") { Url = link }
                                                              }
                                                       });

                                            mees=await botClient.SendTextMessageAsync(FromId, announcement.ToString(), replyMarkup: keyboard);
                                            ADDTOCHAT(FromId, mees.MessageId);

                                            keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("Torna indietro", "callback_data_23"), } });

                                            mees = await botClient.SendTextMessageAsync(FromId, "Questo è tutto, premi il pulsante al per tornare al menu,", replyMarkup: keyboard);
                                            ADDTOCHAT(FromId, mees.MessageId);

                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine($"Errore: {ex.Message}");
                                            
                                        }


                                    }
                                    else
                                    {
                                        await botClient.AnswerCallbackQueryAsync(update.CallbackQuery.Id, "Invalid callback data");
                                        await botClient.AnswerCallbackQueryAsync(update.CallbackQuery.Id, $"Problemi di {WritingProblems[FromId].Category}");
                                        await Menu(FromId);


                                    }
                                    break;

                            }
                        }
                     
                    }
                }
                GestioneFile.WriteXMLTelegramChats(MesssagesIdPerChat);
            }
            else
            {
                EliminateALLmessages();
                if (update.Message != null)
                {
                    long id = update.Message.Chat.Id;
                    await SendMessage("Attualmente non sono disponibile. ", id);
                }
             
            }
        }
        public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            botClient.SendTextMessageAsync(
                                               chatId: DavideID,
                                               text: ErrorMessage,
                                               cancellationToken: cancellationToken);

            //RiavvioNecessario(this);

            return Task.CompletedTask;
        }
        public async Task Riepilogo(long id,bool force)
        {
            if (WritingProblems.ContainsKey(id))
            {
                Problem problem = WritingProblems[id];
                string text = "";
                if (problem.isStudente.Equals("true"))
                {
                    text = problem.Person.Name + " " + problem.Person.Surname + " " + problem.Person.Classroom.ToString();
                }
                else
                {
                    text = $"rappresentante di classe  {problem.Person.Classroom.ToString()}.\nAnonimato garantito.";
                }


                await SendMessage($"Ecco qua sotto il riepilogo:\n\nTitolo:\n{problem.Title}\n\nMittente:\n{text}\n\nCategoria:\n{problem.Category}\n\nDescrizione del problema:\n{problem.Description}\n\nSoluzione proposta:\n{problem.Solution}", id);
                InlineKeyboardMarkup keyboard;
                if (!force)
                {
                    keyboard = new InlineKeyboardMarkup(new[]
                    {
                                            new []
                                            {
                                                InlineKeyboardButton.WithCallbackData("Riscrivi il titolo", "callback_data_11"),
                                                InlineKeyboardButton.WithCallbackData("Riscrivi la descrizione", "callback_data_12")


                                            },
                                                new []
                                            {

                                                InlineKeyboardButton.WithCallbackData("Riscrivi la soluzione", "callback_data_19"),
                                                InlineKeyboardButton.WithCallbackData("Cancella tutto", "callback_data_13"),
                                                                        },
                                        new[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("Invia segnalazione", "callback_data_14"),
                                        }
                                });

                }
                else
                {
                    keyboard = new InlineKeyboardMarkup(new[]
                  {
                                            new []
                                            {
                                                InlineKeyboardButton.WithCallbackData("Riscrivi il titolo", "callback_data_11"),
                                                InlineKeyboardButton.WithCallbackData("Riscrivi la descrizione", "callback_data_12")


                                            },
                                                new []
                                            {

                                                InlineKeyboardButton.WithCallbackData("Riscrivi la soluzione", "callback_data_19"),
                                                InlineKeyboardButton.WithCallbackData("Cancella tutto", "callback_data_13"),
                                                                        },
                                        new[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("Invia segnalazione", "callback_data_14"),

                                        }
                                        ,
                                        new[]
                                        {
                                              InlineKeyboardButton.WithCallbackData($"Bypassa AI (In caso di abuso, {problem.Person.Name}, sarai bannato. )", "callback_data_20"),
                                        }
                                });
                }

                var mes = await botClient.SendTextMessageAsync(id, "E' tutto pronto? ", replyMarkup: keyboard);
                ADDTOCHAT(id, mes.MessageId);
            }
        }
        public async Task Menu(long id)
        {

            Person p = await schoolContext.Students.FirstOrDefaultAsync(x => x.TelegramId.Equals(id));
            if (p != null)
            {
                string tmpcall = "callback_data_10";
                if (p.isJustStudent)
                {
                    tmpcall = "callback_data_1";
                }
                var keyboard = new InlineKeyboardMarkup(new[]
                               {
                        new []
                                {
                             InlineKeyboardButton.WithCallbackData("🚩Segnala problema🚩",tmpcall),
                                    InlineKeyboardButton.WithCallbackData("Le tue segnalazioni", "callback_data_7"),



                                },
                                new []
                                {
                                     InlineKeyboardButton.WithCallbackData("🌟Scrivi un annuncio🌟", "callback_data_8"),

                                             InlineKeyboardButton.WithCallbackData("Visualizza Annunci", "callback_data_39"),

                                }
                                ,
                                new []
                                {

                                     InlineKeyboardButton.WithCallbackData("Scrivi ad un'altra classe", "callback_data_9"),
                                     InlineKeyboardButton.WithCallbackData("Vedi messaggi", "callback_data_30"),

                                }
                                ,
                                     new []
                                {

                                     InlineKeyboardButton.WithCallbackData("📊 Vota i sondaggi 📊", "callback_data_24"),

                                }
                                     ,
                                     new []
                                {

                                     InlineKeyboardButton.WithCallbackData("TOP 10 TrustPoints", "callback_data_40"),

                                }
               });

                var mes = await botClient.SendTextMessageAsync(id, $"Ciao {p.Name}, benvenuto su VMeta.\n\n-🌟🌟I tuoi TrustPoints: {p.TrustPoints}🌟🌟\n\n-📊📊Sondaggi da votare: {p.LastDecision}📊📊\n\nClicca il pulsante per la funzionalità interessata", replyMarkup: keyboard);
                ADDTOCHAT(id, mes.MessageId);
            }
        }
        public bool RegisterNewAccountRequest(string name, string surname, string code, string email)
        {
            List<RegisterRequest> tmr = GestioneFile.ReadXMLRequestRegister();
            if (!tmr.Exists(x => x.Code.Equals(code)))
            {
                tmr.Add(new RegisterRequest(name, surname, code, email));
                GestioneFile.WriteXMLRequestRegister(tmr);
                return true;
            }
            else return false;

        }

        public async Task SendMessage(string text, long id){

            if (id != -1)
            {
                try
                {
                    var mes = await botClient.SendTextMessageAsync(
                          chatId: id,
                          text: text
                          );
                    ADDTOCHAT(id, mes.MessageId);
                }catch(Exception e) {

                    Console.WriteLine("Nel SendMessage [text,id]");

                }
            }
        }
        public async Task SendMessage(string text, long id,bool isStoredInChat)
        {

            if (id != -1)
            {
                try
                {
                    var mes = await botClient.SendTextMessageAsync(
                          chatId: id,
                          text: text
                          );
                    if(isStoredInChat)
                    ADDTOCHAT(id, mes.MessageId);
                }
                catch (Exception e) { Console.WriteLine("Eccezione nella catch del SendMessage method"); }
            }
        }
        public async Task MandaPulsantiCategorie(long id)
        {
            var keyboard = new InlineKeyboardMarkup(new[]
                          {
                        new []
                                {
                                    InlineKeyboardButton.WithCallbackData("Infrastrutture", "callback_data_3"),
                                    InlineKeyboardButton.WithCallbackData("Bullismo", "callback_data_4"),
                                        InlineKeyboardButton.WithCallbackData("Altro", "callback_data_6")
                                },
                                new []
                                {
                                    InlineKeyboardButton.WithCallbackData("Relazione professori-studenti", "callback_data_5")
                                
                                }
               });

            var mes = await botClient.SendTextMessageAsync(id, "Selezionare la categoria del problema: ", replyMarkup: keyboard);
            ADDTOCHAT(id, mes.MessageId);
        }
        public bool DeleteWritingProblem(long id)
        {
            if (WritingProblems.ContainsKey(id))
            {
                WritingProblems.Remove(id);
                return true;
            }
            else return false;
            
        }
        public async Task ProponiSoluzioni(long id)
        {
            var keyboard = new InlineKeyboardMarkup(new[]
                         {
                        new []
                                {
                                    InlineKeyboardButton.WithCallbackData("Sì ne propongo una.", "callback_data_17"),
                                    InlineKeyboardButton.WithCallbackData("No, non ne ho...", "callback_data_18"),
                                      
                                }
               });

            var mes = await botClient.SendTextMessageAsync(id, "Hai una soluzione al problema?", replyMarkup: keyboard);
            ADDTOCHAT(id, mes.MessageId);
        }
        public void RiavviaClient(string token)
        {
            active = false;
            botClient = new TelegramBotClient(token);
            
            cts = new CancellationTokenSource();

            receiverOptions = new()
            {
                AllowedUpdates = Array.Empty<UpdateType>() // receive all update types except ChatMember related updates
            };

            botClient.StartReceiving(
                     updateHandler: HandleUpdateAsync,
                     pollingErrorHandler: HandlePollingErrorAsync,
                     receiverOptions: receiverOptions,
                     cancellationToken: cts.Token
          );


        }
        public async Task CLEAR(long id)
        {
           

                TimeSpan ore = new TimeSpan(47, 0, 0);
                TimeSpan testing;
                mtx.Wait();
                if (MesssagesIdPerChat.ContainsKey(id))
                    foreach (var messid in MesssagesIdPerChat[id])
                    {
                        testing = DateTime.Now - messid.Key;

                        if (testing < ore)
                        {
                            try
                            {
                                await botClient.DeleteMessageAsync(id, messid.Value);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Nel clear method");
                            }
                        }

                    }
                MesssagesIdPerChat.Remove(id);
                mtx.Release();


            

        }
        public void ADDTOCHAT(long id, int messageid)
        {
            mtx.Wait();

            if (MesssagesIdPerChat.ContainsKey(id))
                MesssagesIdPerChat[id].Add(DateTime.Now,messageid);
            else
            {
                Dictionary<DateTime,int> messages = new Dictionary<DateTime, int>();
                messages.Add(DateTime.Now, messageid);
                MesssagesIdPerChat.Add(id, messages);

            }
            mtx.Release();
        }
        void EliminateALLmessages()
        {

            foreach (KeyValuePair<long,Dictionary<DateTime, int>> kvp in MesssagesIdPerChat)
            {
                kvp.Value.Clear();    

             }

            GestioneFile.WriteXMLTelegramChats(MesssagesIdPerChat);
        
        }
        async void CheckChatQuality()
        {
            TimeSpan tm = new TimeSpan(40, 10, 0);
            List<long> ids = new List<long>();
            while (active)
            {
                mtx.Wait();    
                foreach (KeyValuePair<long, Dictionary<DateTime, int>> kvp in MesssagesIdPerChat)
                {

                    TimeSpan ore = new TimeSpan(47, 0, 0);
                    TimeSpan testing;
                    List<KeyValuePair<DateTime, int>> listatmp = new List<KeyValuePair<DateTime, int>>();
                    foreach (KeyValuePair<DateTime, int> k in kvp.Value)
                    {
                        listatmp.Add(k);
                    }
                    if (listatmp.Count > 0)
                    {
                        testing = DateTime.Now - listatmp[listatmp.Count - 1].Key;
                        if (testing > tm)
                        {
                            ids.Add(kvp.Key);
                         
                        }
                    }
                }
                mtx.Release();

                foreach (long id in ids)
                    await CLEAR(id);
               

                Thread.Sleep(1000*300);
            }


        }
        public bool DeleteWritingLetter(long id)
        {
            if (WritingLetterss.ContainsKey(id))
            {
                WritingLetterss.Remove(id);
                return true;
            }
            else return false;
        }
        public bool DeleteWritingAnnouncement(long id)
        {
            if (WritingAnnoucement.ContainsKey(id))
            {
                WritingAnnoucement.Remove(id);
                return true;
            }
            else return false;
        }
        public async Task RiepilogoLettera(long id)
        {
            var keyboard = new InlineKeyboardMarkup(new[]
                          {
                                            new []
                                            {
                                                InlineKeyboardButton.WithCallbackData("Riscrivi il messaggio", "callback_data_25"),
                                                InlineKeyboardButton.WithCallbackData("Elimina il messaggio", "callback_data_26")
                                            },
                                                new []
                                            {

                                                InlineKeyboardButton.WithCallbackData("Conferma e invia il messaggio", "callback_data_29"),

                                                                        },

                                });

            if (WritingLetterss.ContainsKey(id))
            {
                WritingLetterss[id].Title = WritingLetterss[id].Body;
                var res = await botClient.SendTextMessageAsync(id, $"Perfetto! Ecco qua il tuo messaggio:\n{WritingLetterss[id].Body}\n\nConfermi ciò che hai scritto?", replyMarkup: keyboard);
                ADDTOCHAT(id, res.MessageId);
            }
            else { await SendMessage("Recapito della lettera fallito, riprovare", id); await Menu(id); }
        }
        public async Task RiepilogoAnnuncio(long id)
        {
            var keyboard = new InlineKeyboardMarkup(new[]
                                {
                                            new []
                                            {
                                                InlineKeyboardButton.WithCallbackData("Riscrivi il titolo", "callback_data_33"),
                                                InlineKeyboardButton.WithCallbackData("Riscrivi la descrizione", "callback_data_34")
                                            },
                                            new []
                                            {

                                                InlineKeyboardButton.WithCallbackData("Elimina l'annuncio", "callback_data_35")                                                                
                                            },
                                            new[]
                                            { 
                                                InlineKeyboardButton.WithCallbackData("Conferma e invia l'annuncio", "callback_data_36") 
                                            }
                                 });
           
            var res = await botClient.SendTextMessageAsync(id, $"Perfetto! Ecco qua il tuo messaggio:\n{WritingAnnoucement[id].ToString()}\n\nConfermi ciò che hai scritto?", replyMarkup: keyboard);
            ADDTOCHAT(id, res.MessageId);
        }
        public async Task SendLetter(Letter l)
        {

            try
            {
                long FromId = l.People.Find(x => x.ToString().Equals(l.Destination)).TelegramId;
                schoolContext.Letters.Add(l);
                schoolContext.SaveChanges();
                await SendMessage("Il messaggio è stato accettato ed elaborato. Verrà immediatamente spedito a " + l.Destination + ".", l.People.Find(x => x.ToString().Equals(l.Author)).TelegramId);

                DeleteWritingLetter(l.People.Find(x => x.ToString().Equals(l.Author)).TelegramId);
                await CLEAR(l.People.Find(x => x.ToString().Equals(l.Author)).TelegramId);
                if (!WritingLetterss.ContainsKey(FromId) && !WritingProblems.ContainsKey(FromId))
                {
                    await CLEAR(FromId);
                    var chat = await botClient.GetChatAsync(l.People.Find(x => x.ToString().Equals(l.Author)).TelegramId);
                    string username = chat.Username;
                    string link = $"https://t.me/{username}";

                    var keyboard = new InlineKeyboardMarkup(new[]
                               {
                                                              new[]
                                                              {
                                                                 new InlineKeyboardButton("Rispondi in privato") { Url = link }
                                                              }
                                                       });                  

                    var mess = await botClient.SendTextMessageAsync(FromId, $"HAI UN NUOVO MESSAGGIO.\n\n{l.Body}\n\n{l.Author} ", replyMarkup: keyboard);
                    ADDTOCHAT(FromId, mess.MessageId);

                    await Menu(l.People.Find(x => x.ToString().Equals(l.Destination)).TelegramId);          

                }

                await Menu(l.People.Find(x => x.ToString().Equals(l.Author)).TelegramId);
            }
            catch (Exception ex) {

                DeleteWritingLetter(l.People.Find(x => x.ToString().Equals(l.Author)).TelegramId);
                await SendMessage("C'è stato un errore nel mandare il messaggio", l.People.Find(x => x.ToString().Equals(l.Author)).TelegramId); 
            
            }
         

        }
        void TrustPointsRank()
        {
            CreateRank();
            while (true)
            {
                // Ottieni l'orario corrente
                DateTime now = DateTime.Now;

                // Calcola il prossimo orario delle 8:00
                DateTime nextRun = now.Hour >= 8 ? now.Date.AddDays(1).AddHours(8) : now.Date.AddHours(8);

                // Calcola quanto tempo manca fino al prossimo orario delle 8:00
                TimeSpan timeToWait = nextRun - now;

                Console.WriteLine($"Prossima esecuzione alle: {nextRun}");

                // Metti il thread in pausa fino a quando non arriva l'orario specificato
                Thread.Sleep(timeToWait);

                // Esegui la tua azione
                CreateRank();
            }
        }
        void CreateRank()
        {

            List<Person> people = new List<Person>(schoolContext.Students);

            people.Sort((x, y) => y.TrustPoints.CompareTo(x.TrustPoints));

            int counter = 1;
            classificaMtx.WaitOne();
            topten.Clear();
            for (int i = 0; i < people.Count; i++)
            {
                topten.Add(people[i]);
                counter++;
                if (counter == 11)
                    break;
            }
            classificaMtx.ReleaseMutex();

        }
    }  
}
