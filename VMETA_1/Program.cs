using System.Text.Json.Nodes;
using Newtonsoft.Json;
using OllamaSharp.Models.Chat;
using OllamaSharp.Streamer;
using System.IO;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using HtmlAgilityPack;
using Message = OllamaSharp.Models.Chat.Message;

using System.Xml;

using VMETA_1.Classes;
using VMETA_1.Entities;
using VMETA_1.Models;
using System.Text;
using System;


var builder = WebApplication.CreateBuilder(args);
bool BUSY_VANESSA = false;
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

List<RegisterRequest> TelegramCodes = new List<RegisterRequest>();

EmailServiceVMeta emailServiceVMeta = new EmailServiceVMeta();
//emailServiceVMeta.SendEmail("OGGETTO LETTERA3", "TITOLO DELLA LETTERA", "CORPO DELLA LETTERA");
VanessaCore _core = null;
SchoolContext schoolContext = new SchoolContext();


Queue<Problem> _problem_queue = new Queue<Problem>();
Queue<Letter> _letter_queue = new Queue<Letter>();
Queue<Announcement> _announcement_queue = new Queue<Announcement>();
//VANESSA GEMINI
//TelegramBot telegramBot = new TelegramBot("7162917894:AAF54AXNjF0fauZW3vgUsxBbuYvaLogR5HM",schoolContext);
//VANESSA

GestioneFile.WriteFTP("TelegramChats.xml");


Mutex mutex = new Mutex();
Mutex mutex2 = new Mutex();
Mutex mutex1 = new Mutex();
Mutex mutex3 = new Mutex();

Semaphore semaphore = new Semaphore(1, 2000);


string apidev = "7093295868:AAFba7c8l2qvdsfBTaP4LnxGPIN1HMuaGnM";
string apirelease = "7315698486:AAH-stu67C5SRi6FP8fJdW1Y1j6HIS-GpzU";
string telegramAPI= apirelease; 
TelegramBot telegramBot = new TelegramBot(telegramAPI, schoolContext);
telegramBot.ProblemaPronto += AddProblem;
telegramBot.RiavvioNecessario += ReStart;
telegramBot.LetteraPronta += AddLetter;
telegramBot.AnnuncioPronta += AddAnnouncement;
telegramBot.RichiestaDaCompletare += NewPerson;
IResponseStreamer<ChatResponseStream?> Streamer = null;
CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
Thread AiProblemSender = new Thread(AnalizzaCoda);
Thread AILetterSender = new Thread(AnalizzaCodaLettere);
Thread AIAnnouncementSender = new Thread(AnalizzaCodaAnnuncio);



string BotResponse = "";
Streamer = new ActionResponseStreamer<ChatResponseStream>((stream) =>
{
    mutex.WaitOne();
    BotResponse += stream.Message.Content;
    mutex.ReleaseMutex();
}
);
_core = new VanessaCore(Streamer);

AiProblemSender.Start();
AiProblemSender.IsBackground = true;
AILetterSender.Start();
AILetterSender.IsBackground = true;
AIAnnouncementSender.Start();
AIAnnouncementSender.IsBackground = true;

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

//APIS
//GET
app.MapGet("/api/Start", () =>
{
     return Results.Ok("grande");
});
app.MapGet("/api/GetResponse", async () => {

    mutex.WaitOne();
    string response = BotResponse;
    mutex.ReleaseMutex();
    return new Response(response);

});
app.MapGet("/api/Clear", async () =>
{
    BotResponse = "";
    return Results.Ok("Completato");
});
app.MapGet("/api/GetStudents", async () => {

    List<PersonModel> models = new List<PersonModel>();

    foreach (Person p in schoolContext.Students.Include(x => x.Classroom).Include(x => x.Problem))
    {
        models.Add(new PersonModel(p));
    }
    models.Sort((x, y) => x.Surname.CompareTo(y.Surname));
    return Results.Ok(models);

});
app.MapGet("/api/GetClassrooms", async () => {


    List<ClassroomModel> models = new List<ClassroomModel>();

    foreach (Classroom cl in schoolContext.Classrooms.Include(x => x.People).Include(x => x.Problems))
    {
        models.Add(new ClassroomModel(cl));
    }

    return Results.Ok(models);

});
app.MapGet("/api/GetProblems", async () =>
{

    List<ProblemModel> models = new List<ProblemModel>();

    foreach (Problem p in schoolContext.Problems.Include(x => x.Person))
    {
        models.Add(new ProblemModel(p));
    }
    models.Sort((x,y)=> x.Person.CompareTo(y.Person));
    return Results.Ok(models);

});
app.MapGet("/api/GetPools", async () =>
{

    List<PoolModel> models = new List<PoolModel>();

    foreach (Pool p in schoolContext.Pools.Include(x => x.Votes))
    {
        models.Add(new PoolModel(p));
    }

    return Results.Ok(models);

});
app.MapGet("/api/GetAnnouncements", async () =>
{

    List<AnnouncementModel> models = new List<AnnouncementModel>();

    foreach (Announcement p in schoolContext.Announcements.Include(x => x.Announcer))
    {
        models.Add(new AnnouncementModel(p));
    }

    return Results.Ok(models);

});
app.MapGet("/api/SearchStudentsCognome/{cognome}", async (string cognome) => {

    List<PersonModel> tmp = new List<PersonModel>();
    foreach(Person p in schoolContext.Students.Include(x=> x.Classroom).Where(x=> x.Surname.ToLower().StartsWith(cognome.ToLower())))
    {
        tmp.Add(new PersonModel(p));
    }

    return tmp;

});
app.MapGet("/api/RestartBotTelegram", () => {

    telegramBot.RiavviaClient(telegramAPI);
    return Results.Ok();
});
//POST
app.MapPost("/api/SendMessage", async (JsonObject json) =>
{
    string jasonstring = json.ToString();

    Message? message = JsonConvert.DeserializeObject<Message>(jasonstring);


    await _core.TalkWithVanessa(message.Content,true);

    /////////////////////////////////////////
    return Results.Accepted("id libro:");

});
app.MapPost("/api/SendPerson", async (JsonObject json) =>
{
    //string jasonstring = json.ToString();
    var ja = json.ToArray();
    // Message? message = JsonConvert.DeserializeObject<Message>(jasonstring);
    //0 nome 1 cognome 2 classe 3 birth 4 email 5 cellphone 6 Telegramcode request
    Console.WriteLine(json.ToString());
    string classe = ja[2].Value.ToString();
    string year = classe[0] + "";
    string sect = classe[1] + "";
    string spec = classe.Substring(2);
    Classroom tmp = schoolContext.Classrooms.FirstOrDefault(x => x.Year.Equals(year) && x.Section.Equals(sect) && x.Specialization.Equals(spec));
    string name = "cazzo";

    bool TCODEALREADYExists = false;

    if (tmp != null)
    {

        string Name = ja[0].Value.ToString();
        string Cognome = ja[1].Value.ToString();
        string email = ja[4].Value.ToString();
        string Phonw = ja[5].Value.ToString();
        string TelegramCODE="";
        TelegramCodes = GestioneFile.ReadXMLRequestRegister();
        if (ja[6].Value.ToString().Length >= 3)
        {
            if (TelegramCodes.Exists(x=>x.Code.Equals(ja[6].Value.ToString())))
            {
                TCODEALREADYExists = true;
            }
            else TelegramCODE = ja[6].Value.ToString();
        }
        else
        {

            string tmpcode;
            do
            {
                tmpcode = GenerateRandomString(8);
                tmpcode=tmpcode.ToUpper();

            } while (TelegramCodes.Exists(x => x.Code.Equals(ja[6].Value.ToString())));            
            TelegramCODE=tmpcode;

            if (!(email.Length > 5 && email.Contains("@") && email.Contains("isiskeynes.it"))){
                emailServiceVMeta.SendEmail("VMeta autenticazione", "Codice sicurezza", $"Ciao {Name},<br> è stato richiesto un codice di autenticazione per utilizzare VMeta su telegram.<br><br>Per autenticarti scrivi questo messaggio:   <b>/code:{tmpcode}</b><br>A questo bot: <a href='https://t.me/Vmeta_bot'>VMeta</a><br><br><b>IMPORTANTE!</b><br>Non condividere con nessuno queste informazioni.<br>Il codice rappresenta la <b>tua utenza Telegram</b> verso il sistema perciò fai attenzione ad un eventuale <b>furto di identità</b>.<br><br>Cordialmente,<br><br>-VMeta", $"s-{Cognome.ToLower().Replace(" ", string.Empty)}.{Name.ToLower().Replace(" ", string.Empty)}@isiskeynes.it");
            }
            else
            {
                emailServiceVMeta.SendEmail("VMeta autenticazione", "Codice sicurezza", $"Ciao {Name},<br> è stato richiesto un codice di autenticazione per utilizzare VMeta su telegram.<br><br>Per autenticarti scrivi questo messaggio:   <b>/code:{tmpcode}</b><br>A questo bot: <a href='https://t.me/Vmeta_bot'>VMeta</a><br><br><b>IMPORTANTE!</b><br>Non condividere con nessuno queste informazioni.<br>Il codice rappresenta la <b>tua utenza Telegram</b> verso il sistema perciò fai attenzione ad un eventuale <b>furto di identità</b>.<br><br>Cordialmente,<br><br>-VMeta", email);

            }
        }

        if (!TCODEALREADYExists) {

            bool check;
            bool.TryParse(ja[7].Value.ToString(), out check);
            if (Name.Length > 0)
            {
                Person p = new Person(Name, Cognome, DateTime.MinValue, tmp, -1, email, Phonw, check);

                if (telegramBot.RegisterNewAccountRequest(Name, Cognome, TelegramCODE,email))
                {

                    schoolContext.Students.Add(p);
                    schoolContext.SaveChanges();
                    return Results.Accepted("Operation succed");
                }
                else return Results.BadRequest("Codice telegram già preso");

            }
        }
        else Results.BadRequest("Telegram code già esistente");

    }
    /////////////////////////////////////////
    return Results.BadRequest("Operation failed");

});
app.MapPost("/api/SendPool", async (JsonObject json) => {

    if (json != null)
    {
        var ja = json.ToArray();
        string titolo = ja[0].Value.ToString();
        string descrizione = ja[1].Value.ToString();
        List<string> option = new List<string>();

        var cose = ja[2].Value;
        for (int i = 0; i < cose.AsArray().LongCount(); i++)
        {

            option.Add(cose.AsArray()[i].ToString());

        }

        bool rappresentante;
        bool.TryParse(ja[3].Value.ToString(), out rappresentante);

        Pool newpool = new Pool(titolo, descrizione, option, rappresentante);
        schoolContext.Pools.Add(newpool);
        schoolContext.SaveChanges();
        Decision d;
        if (!rappresentante)
        {

            foreach (Person p in schoolContext.Students.Include(x => x.Decisions))
            {

                d = new Decision(p, newpool);
                schoolContext.Decisions.Add(d);

                newpool.AddDecision(d);
                p.Decisions.Add(d);

                await NotificaNuovoPool(newpool.Titolo, p);

            }
            schoolContext.SaveChanges();
            Console.WriteLine("Ho creato il pool: " + newpool.Titolo);
        }
        else
        {

            foreach (Person p in schoolContext.Students.Include(x => x.Decisions).Where(x => x.isJustStudent.Equals(false)))
            {


                d = new Decision(p, newpool);
                schoolContext.Decisions.Add(d);

                newpool.AddDecision(d);
                p.Decisions.Add(d);
                await NotificaNuovoPool(newpool.Titolo, p);

            }
            schoolContext.SaveChanges();
            Console.WriteLine("Ho creato il pool: "+newpool.Titolo);
        }


    }
    Console.WriteLine("WE");
});
app.MapPost("/api/SendVoice", async (IFormFile file) =>
{
    if (file == null || file.Length == 0)
    {
        return Results.BadRequest("Nessun file caricato.");
    }

    // Definisci il percorso in cui salvare il file
    var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");

    // Crea la cartella "uploads" se non esiste
    if (!Directory.Exists(uploadPath))
    {
        Directory.CreateDirectory(uploadPath);
    }

    // Salva il file con il nome originale
    var filePath = Path.Combine(uploadPath, file.FileName);

    using (var stream = new FileStream(filePath, FileMode.Create))
    {
        await file.CopyToAsync(stream);
    }

    return Results.Ok(new { filePath });
});
app.MapPost("/api/SendMessageALL", async (RequestSendMessage rsm) => {

    List<string> list = new List<string>() {"E","R","1","2","3","4","5"};

    if (rsm == null)
        return Results.BadRequest("Nessun Request send message");
    else if (!list.Contains(rsm.Destination))
        return Results.BadRequest("Destinazione non valida");
    else if (!(rsm.Title.Length > 0) || !(rsm.Body.Length > 0))
        return Results.BadRequest("Titolo o Descrizione inconsistente");
    else
    {
       


            Letter letternew;
            switch (rsm.Destination)
            {
                case "E":
                    foreach (Person destination in schoolContext.Students.Where(x => x.TelegramId != -1))
                    {

                        letternew = new Letter();
                        letternew.Author = "ADMIN";
                        letternew.Destination = destination.ToString();
                 
                        letternew.People.Add(destination);
                        letternew.Title = rsm.Title;
                        letternew.Body = rsm.Body;
                        letternew.AI_Forced = false;

                        schoolContext.Letters.Add(letternew);

                        await telegramBot.SendMessage($"MESSAGGIO ADMIN\n\n {letternew.Title}\n\n{letternew.Body}", destination.TelegramId);
                    }
                    schoolContext.SaveChanges();
                    break;
                case "R":
                    foreach (Person destination in schoolContext.Students.Where(x => x.TelegramId != -1 && x.isJustStudent))
                    {

                        letternew = new Letter();
                        letternew.Author = "ADMIN";
                        letternew.Destination = destination.ToString();
         
                        letternew.People.Add(destination);
                        letternew.Title = rsm.Title;
                        letternew.Body = rsm.Body;
                        letternew.AI_Forced = false;

                        schoolContext.Letters.Add(letternew);

                        await telegramBot.SendMessage($"MESSAGGIO ADMIN\n\n {letternew.Title}\n\n{letternew.Body}", destination.TelegramId);
                    }
                    schoolContext.SaveChanges();
                    break;
                default:

                    foreach (Person destination in schoolContext.Students.Include(x => x.Classroom).Where(x => x.TelegramId != -1 && x.Classroom.Year.Equals(rsm.Destination)))
                    {

                        letternew = new Letter();
                        letternew.Author = "ADMIN";
                    letternew.Destination = destination.ToString();

                        letternew.People.Add(destination);
                        letternew.Title = rsm.Title;
                        letternew.Body = rsm.Body;
                        letternew.AI_Forced = false;

                        schoolContext.Letters.Add(letternew);

                        await telegramBot.SendMessage($"MESSAGGIO ADMIN\n\n {letternew.Title}\n\n{letternew.Body}", destination.TelegramId);

                    }
                    schoolContext.SaveChanges();

                    break;
            }

            return Results.Ok();

    
    }
});
//DELETE
app.MapDelete("/api/DeletePerson/{id}", async (string id) => {

    List<string> tmp = id.Trim().Split(' ').ToList();

    string Name = tmp[0];
    Name = Name.Trim();
    string Cognome = string.Join(" ", tmp.Skip(1));
    Cognome = Cognome.Trim();

    Person todelete = schoolContext.Students.FirstOrDefault(x => x.Name.Trim().Equals(Name) && x.Surname.Trim().Equals(Cognome));

    if (todelete != null)
    {

       
        todelete.Classroom = null;

        schoolContext.Students.Remove(todelete);
        if(todelete.Problem!=null)
            schoolContext.Problems.RemoveRange(todelete.Problem);
        if (todelete.Announcements != null)
            schoolContext.Announcements.RemoveRange(todelete.Announcements);
        if (todelete.Letters != null)
            schoolContext.Letters.RemoveRange(todelete.Letters);
        schoolContext.SaveChanges();
        return Results.Accepted("Tolto");

    }
    else return Results.BadRequest("Non esiste il tizio in questione");


});
app.MapDelete("/api/DeleteIssue/{id}", async (int id) => {



    Problem todelete = schoolContext.Problems.FirstOrDefault(x => x.Id.Equals(id));

    if (todelete != null)
    {

        todelete.Person = null;
        todelete.Classroom = null;

        schoolContext.Problems.Remove(todelete);
        schoolContext.SaveChanges();
        return Results.Accepted("Tolto");

    }
    else return Results.BadRequest("Non esiste il tizio in questione");


});
app.MapDelete("/api/DeletePool/{id}", async (int id) =>
{
    Pool todelete = await schoolContext.Pools.FirstOrDefaultAsync(x => x.Id.Equals(id));
    if (todelete != null)
    {


        


        schoolContext.Pools.Remove(todelete);
        schoolContext.SaveChanges();
        Console.WriteLine("Ho eliminato il sondaggio: "+todelete.Titolo);
        return Results.Accepted("Tolto");

    }
    else return Results.BadRequest("Non esiste il tizio in questione");

});
app.MapDelete("/api/DeleteAnnouncement/{id}", async (int id) =>
{
    Announcement todelete = await schoolContext.Announcements.FirstOrDefaultAsync(x => x.id.Equals(id));
    if (todelete != null)
    {


        schoolContext.Announcements.Remove(todelete);
        schoolContext.SaveChanges();
        return Results.Accepted("Tolto");

    }
    else return Results.BadRequest("Non esiste il tizio in questione");

});
//PUT
app.MapPut("/api/ModificaAI/{id}", async (int id) => { 

    Problem problem = schoolContext.Problems.FirstOrDefault(x=> x.Id.Equals(id));

    if (problem!=null)
    {
        problem.AI_Forced = false;
        schoolContext.SaveChanges();
        return Results.Ok();
    }
    else return Results.BadRequest();


});
app.MapPut("/api/MakeRappresentante/{id}", async (string id) =>
{
    List<string> tmp = id.Split("_").ToList();
    Person p = await schoolContext.Students.FirstOrDefaultAsync(x => x.Name.Equals(tmp[0]) && x.Surname.Equals(tmp[1]));

    if (p != null) { 
    
        p.isJustStudent = false;
        schoolContext.SaveChanges();
        return Results.Ok();

    }
    else return Results.BadRequest();

});
app.MapPut("/api/MakeStudent/{id}", async (string id) =>
{
    List<string> tmp = id.Split("_").ToList();
    Person p = await schoolContext.Students.FirstOrDefaultAsync(x => x.Name.Equals(tmp[0]) && x.Surname.Equals(tmp[1]));

    if (p != null)
    {

        p.isJustStudent = true;
        schoolContext.SaveChanges();
        return Results.Ok();

    }
    else return Results.BadRequest();

});


async Task NotificaNuovoPool(string title, Person p)
{


    if (p.LastDecision > 1)
    {
        await telegramBot.SendMessage($"{title.ToUpper()}\n\nCiao, è appena stato aggiunto un nuovo sondaggio!\nAttualmente devi votare per {p.LastDecision} sondaggi.\nFai valere la tua opinione.", p.TelegramId);
    }
    else
    {
        await telegramBot.SendMessage($"{title.ToUpper()}\n\nCiao, è appena stato aggiunto un nuovo sondaggio!\nFai valere la tua opinione.", p.TelegramId);
    }

}


void AddProblem(object sendere, Problem p)
{
    mutex2.WaitOne();
 
    _problem_queue.Enqueue(p);
    Queue<Problem> queuetmp = new Queue<Problem>(_problem_queue);
    _problem_queue.OrderByDescending(x => x.TrustPoints);

    for (int i = 0; i < queuetmp.Count; i++) {

        try
        {
            if (queuetmp.ToList()[i]!= _problem_queue.ToList()[i])
            {
            _problem_queue.ToList()[i].TrustPoints += 0.25;
            }
        }
        catch (Exception e) {

            Console.WriteLine("Nel AddProblem [text,id]");
        }

    }
  
    mutex2.ReleaseMutex();
}
void AddLetter(object sender, Letter l)
{
    mutex1.WaitOne();
    _letter_queue.Enqueue(l);
    Queue<Letter> queuetmp = new Queue<Letter>(_letter_queue);
    _letter_queue.OrderByDescending(x => x.TrustPoints);

    for (int i = 0; i < queuetmp.Count; i++)
    {
        try
        {
            if (queuetmp.ToList()[i] != _letter_queue.ToList()[i])
            {
                _letter_queue.ToList()[i].TrustPoints += 0.25;
            }
        }
        catch(Exception e) {

            Console.WriteLine("Eccezione nell'add letter");
        }

    }

    mutex1.ReleaseMutex();
}
void AddAnnouncement(object sender, Announcement a) {
    
    mutex3.WaitOne();
    _announcement_queue.Enqueue(a);
    Queue<Announcement> queuetmp = new Queue<Announcement>(_announcement_queue);
    _announcement_queue.OrderByDescending(x => x.TrustPoints);

    for (int i = 0; i < queuetmp.Count; i++)
    {

        if (queuetmp.ToList()[i] != _announcement_queue.ToList()[i])
        {
            _announcement_queue.ToList()[i].TrustPoints += 0.25;
        }

    }

    mutex3.ReleaseMutex();

}
void ReStart(object sender)
{

    telegramBot.RiavviaClient(telegramAPI);

}
void NewPerson(object sender, RegisterRequest RR, string classe, long tmptelegram)
{
    if (classe.Length >= 3)
    {
        string tmpcode;
        string TelegramCODE;
        string Name = RR.Name;
        string year = classe[0] + "";
        string sect = classe[1] + "";
        string spec = classe.Substring(2);
        Classroom tmp = schoolContext.Classrooms.FirstOrDefault(x => x.Year.Equals(year) && x.Section.Equals(sect) && x.Specialization.Equals(spec));
        if (tmp != null)
        {
            do
            {
                tmpcode = GenerateRandomString(8);
                tmpcode = tmpcode.ToUpper();

            } while (TelegramCodes.Exists(x => x.Code.Equals(tmpcode)));
            TelegramCODE = tmpcode;
            telegramBot.SendMessage("Ok, ti arriverà presto una email con il codice da inserire", tmptelegram);
            emailServiceVMeta.SendEmail("VMeta autenticazione", "Codice sicurezza", $"Ciao {Name},<br> è stato richiesto un codice di autenticazione per utilizzare VMeta su telegram.<br><br>Per autenticarti scrivi questo messaggio:   <b> /code:{tmpcode}</b><br>A questo bot: <a href='https://t.me/Vmeta_bot'>VMeta</a><br><br><b>IMPORTANTE!</b><br>Non condividere con nessuno queste informazioni.<br>Il codice rappresenta la <b>tua utenza Telegram</b> verso il sistema perciò fai attenzione ad un eventuale <b>furto di identità</b>.<br><br>Cordialmente,<br><br>-VMeta", RR.Email);

            Person p = new Person(Name, RR.Surname, DateTime.MinValue, tmp, -1, RR.Email, "nessuno", false);
            if (telegramBot.RegisterNewAccountRequest(Name, RR.Surname, TelegramCODE, RR.Email))
            {

                schoolContext.Students.Add(p);
                schoolContext.SaveChanges();
            }
        }
        Console.WriteLine("Nuovo tizio inserito");
    }
}

async void AnalizzaCoda()
{
    while (true)
    {
        if (_problem_queue.Count > 0)
        {


            semaphore.WaitOne();
            Problem testing = _problem_queue.Dequeue();
            testing.AI_Analyzing = true;

            //string final = "Questa segnalazione contiene PAROLACCE come cazzo, merda, figlio di puttana etc.. , MINACCE DI MORTE oppure OFFESE RAZIALI COME NEGRO e simili? scrivi SOLO E SOLTANTO \"SI\" in caso AFFERMATIVO scrivi solo \"NO\"  in caso NEGATIVO\n\n\n" + testing.ToString();

            string final = "Questa segnalazione contiene parolacce, bestemmie o insulti raziali? Risondi solo con SI (in caso affermativo) e NO (in caso negativo)\n\n{" + testing.ToString() + "}";
            BotResponse = "";
            _core.CLEARCONTEXT();
            await _core.TalkWithVanessa(final,true);

            while (BotResponse.Length > 2) 
            {
                BotResponse = "";
                await _core.TalkWithVanessa("Puoi solo scrivere SI in caso affermativo e NO in caso negativo", true);

            }


            if (BotResponse.Equals("NO"))
            {

                try
                {
                    schoolContext.Problems.Add(testing);
                    schoolContext.SaveChanges();
                   

                    await telegramBot.CLEAR(testing.Person.TelegramId);
                    await telegramBot.SendMessage("La richiesta è stata accettata e sarà inserita in database.\nSi prenderanno provvedimenti a fine settimana. ", testing.Person.TelegramId);
                    bool soluzone = false;
                    if(testing.Solution== "Nessuna soluzione proposta.")
                    {
                        await telegramBot.SendMessage("Questa segnalazione ti farà guadagnare 0.25 trustpoints", testing.Person.TelegramId);
                        testing.Person.TrustPoints += 0.25;
                    }
                    else if (testing.Solution != "-NOT SETTED5353453453435375698")
                    {
                       await telegramBot.SendMessage("Per aver proposto una segnalazione, ti sarà assegnato 1 TrustPoint!\n\n-Se la soluzione dovesse essere ritenuta non efficiente ti verrà assegnato solo 0.5 TrustPoints.\n\n-Nel caso in cui la soluzione stessa sia inutile o non necessaria, perderai 2 TrustPoints.", testing.Person.TelegramId);
                        testing.Person.TrustPoints += 1;
                    }
                    schoolContext.SaveChanges();
                    await telegramBot.Menu(testing.Person.TelegramId);
                    telegramBot.DeleteWritingProblem(testing.Person.TelegramId);
                }
                catch (Exception e) {
                    schoolContext.Problems.Remove(testing);
                    telegramBot.DeleteWritingProblem(testing.Person.TelegramId);
                    await telegramBot.SendMessage("E' stato riscontrato un problema, ci scusiamo per l'imprevisto", testing.Person.TelegramId);
                    await telegramBot.SendMessage("E' stato riscontrato un problema, ci scusiamo per l'imprevisto", telegramBot.DavideID);

                }
            }
            else
            {
                BotResponse = "";

                await _core.TalkWithVanessa("Pensi ancora che la segnalazione contenga parolace, offese raziali o minacce di morte? Scrivi di nuovo il tuo giudizio (SI / NO)");
                while (BotResponse.Length > 2)
                {
                    BotResponse = "";
                    await _core.TalkWithVanessa("Puoi solo scrivere SI in caso affermativo e NO in caso negativo", true);

                }

                if (BotResponse.Equals("NO"))
                {
                    try
                    {
                        schoolContext.Problems.Add(testing);
                        schoolContext.SaveChanges();


                        await telegramBot.CLEAR(testing.Person.TelegramId);
                        await telegramBot.SendMessage("La richiesta è stata accettata e sarà inserita in database.\nSi prenderanno provvedimenti a fine settimana. ", testing.Person.TelegramId);
                        bool soluzone = false;
                        if (testing.Solution == "Nessuna soluzione proposta.")
                        {
                            await telegramBot.SendMessage("Questa segnalazione ti farà guadagnare 0.25 trustpoints", testing.Person.TelegramId);
                            testing.Person.TrustPoints += 0.25;
                        }
                        else if (testing.Solution != "-NOT SETTED5353453453435375698")
                        {
                            await telegramBot.SendMessage("Per aver proposto una segnalazione, ti sarà assegnato 1 TrustPoint!\n\n-Se la soluzione dovesse essere ritenuta non efficiente ti verrà assegnato solo 0.5 TrustPoints.\n\n-Nel caso in cui la soluzione stessa sia inutile o non necessaria, perderai 2 TrustPoints.", testing.Person.TelegramId);
                            testing.Person.TrustPoints += 1;
                        }
                        schoolContext.SaveChanges();
                        await telegramBot.Menu(testing.Person.TelegramId);
                        telegramBot.DeleteWritingProblem(testing.Person.TelegramId);
                    }
                    catch (Exception e)
                    {
                        schoolContext.Problems.Remove(testing);
                        telegramBot.DeleteWritingProblem(testing.Person.TelegramId);
                        await telegramBot.SendMessage("E' stato riscontrato un problema, ci scusiamo per l'imprevisto", testing.Person.TelegramId);
                        await telegramBot.SendMessage("E' stato riscontrato un problema, ci scusiamo per l'imprevisto", telegramBot.DavideID);

                    }
                }
                else
                {
                    await telegramBot.SendMessage("La richiesta non è stata accettata... Hai perso 0.50 trustpoints\nE' risultata inappropriata la segnalazione", testing.Person.TelegramId);
                    testing.Person.TrustPoints -= 0.50;
                    schoolContext.SaveChanges();
                    testing.AI_Analyzing = false;                   
                    await telegramBot.Riepilogo(testing.Person.TelegramId, true);

                }
                

            }

            semaphore.Release();

        }
        Thread.Sleep(20);

    }



}
async void AnalizzaCodaLettere()
{
    Letter testing;
    

    while (true)
    {
     
        if (_letter_queue.Count > 0)
        {

            semaphore.WaitOne();
            testing = _letter_queue.Dequeue();
            testing.AI_Analyzing = true;
            string mex;
            // string final = "Questo messaggio contiene PAROLACCE come cazzo, merda, figlio di puttana etc.. , MINACCE DI MORTE, TERMINI OMOFOBI oppure OFFESE RAZIALI COME NEGRO e simili? scrivi SOLO SI in caso AFFERMATIVO scrivi solo NO in caso NEGATIVO\n\n\n" + testing.Title;
            string final = "Questo messaggio contiene parolacce, bestemmie o insulti raziali? Risondi solo con SI (in caso affermativo) e NO (in caso negativo)\n\n{" + testing.ToString() + "}";
            BotResponse = "";

            _core.CLEARCONTEXT();
            await _core.TalkWithVanessa(final, true);
            while (BotResponse.Length > 2)
            {
                BotResponse = "";
                await _core.TalkWithVanessa("Puoi solo scrivere SI in caso affermativo e NO in caso negativo", true);

            }

            if (BotResponse.Contains("NO"))
            {

                _core.CLEARCONTEXT();
                mex = $"Ciao, mi chiamo {testing.People.Find(x => x.ToString().Equals(testing.Author)).ToString()} e vorrei che scrivessi questo messaggio a {testing.People.Find(x => x.ToString().Equals(testing.Destination)).ToString()}.\nTi chiedo di rielaborarlo da parte mia. SCRIVI SOLO IL MESSAGGIO RIELABORATO COME SE DOVESSI MANDARLO TU PERò DA PARTE MIA.\n\nMESSAGGIO:\n{testing.Body}";
                BotResponse = "";

                await _core.TalkWithVanessa(mex);
                testing.Title = testing.Body;
                testing.Body = BotResponse;
                BotResponse = "";
                await telegramBot.SendLetter(testing);
            }
            else
            {
                BotResponse = "";
                _core.CLEARCONTEXT();
                await _core.TalkWithVanessa("Pensi ancora che \n\n\" " + testing.Body + "\"\n\ncontenga parolace, offese raziali o minacce di morte? Scrivi di nuovo il tuo giudizio (SI / NO)");
                while (BotResponse.Length > 2)
                {
                    BotResponse = "";
                    await _core.TalkWithVanessa("Puoi solo scrivere SI in caso affermativo e NO in caso negativo", true);

                }

                if (BotResponse.Contains("NO"))
                {
                    mex = $"Ciao, mi chiamo {testing.People.Find(x => x.ToString().Equals(testing.Author)).ToString()} e vorrei che scrivessi questo messaggio a {testing.People.Find(x => x.ToString().Equals(testing.Destination)).ToString()}.\nTi chiedo di rielaborarlo da parte mia. SCRIVI SOLO IL MESSAGGIO RIELABORATO COME SE DOVESSI MANDARLO TU PERò DA PARTE MIA A CONDIZIONE CHE NON CI SIANO PAROLACCE, BESTEMMIE O MINACCIE DI MORTE.\n\nMESSAGGIO:\n{testing.Body}";
                    BotResponse = "";

                    await _core.TalkWithVanessa(mex);
                    testing.Title = testing.Body;
                    testing.Body = BotResponse;
                    BotResponse = "";
                    await telegramBot.SendLetter(testing);

                }
                else
                {
                    
                    await telegramBot.CLEAR(testing.People.Find(x => x.ToString().Equals(testing.Author)).TelegramId);                   
                    await telegramBot.SendMessage("Il messaggio non è stato accettato.\nHai perso 0.50 trustpoints", testing.People.Find(x => x.ToString().Equals(testing.Author)).TelegramId);
                    testing.People.Find(x => x.ToString().Equals(testing.Author)).TrustPoints -= 0.50;
                    schoolContext.SaveChanges();
                    testing.AI_Analyzing = false;                   
                    await telegramBot.RiepilogoLettera(testing.People.Find(x => x.ToString().Equals(testing.Author)).TelegramId);

                }
               

            }

            semaphore.Release();


        }
        
        Thread.Sleep(20);
    }
}
async void AnalizzaCodaAnnuncio() {

    while (true)
    {
        if (_announcement_queue.Count > 0)
        {

            semaphore.WaitOne();
            Announcement testing = _announcement_queue.Dequeue();
            testing.AI_Analyzing = true;

            //   string final = "Questa segnalazione contiene PAROLACCE come cazzo, merda, figlio di puttana etc.. , MINACCE DI MORTE oppure OFFESE RAZIALI COME NEGRO e simili? scrivi SOLO SI in caso AFFERMATIVO scrivi solo NO in caso NEGATIVO\n\n\n" + testing.ToString();
            string final = "Quest'annuncio contiene parolacce, bestemmie o insulti raziali? Risondi solo con SI (in caso affermativo) e NO (in caso negativo)\n\n{" + testing.ToString() + "}";
            BotResponse = "";

            _core.CLEARCONTEXT();
            await _core.TalkWithVanessa(final, true);
            while (BotResponse.Length > 2)
            {
                BotResponse = "";
                await _core.TalkWithVanessa("Puoi solo scrivere SI in caso affermativo e NO in caso negativo", true);

            }

            if (BotResponse.Equals("NO"))
            {

                try
                {
                    schoolContext.Announcements.Add(testing);
                    schoolContext.SaveChanges();
                    await telegramBot.CLEAR(testing.Announcer.TelegramId);
                    await telegramBot.SendMessage("La richiesta è stata accettata e sarà inserita in database.\nSi prenderanno provvedimenti a fine settimana. ", testing.Announcer.TelegramId);

                    testing.Announcer.WeeklyAnnouncement = true;
                    await telegramBot.Menu(testing.Announcer.TelegramId);
                    telegramBot.DeleteWritingAnnouncement(testing.Announcer.TelegramId);
                }
                catch (Exception e)
                {
                    schoolContext.Announcements.Remove(testing);
                    telegramBot.DeleteWritingAnnouncement(testing.Announcer.TelegramId);
                    await telegramBot.SendMessage("E' stato riscontrato un problema, ci scusiamo per l'imprevisto", testing.Announcer.TelegramId);
                    await telegramBot.SendMessage("E' stato riscontrato un problema, ci scusiamo per l'imprevisto", telegramBot.DavideID);

                }


            }
            else
            {
                BotResponse = "";

                await _core.TalkWithVanessa("Pensi ancora che la segnalazione contenga parolace, offese raziali o minacce di morte? Scrivi di nuovo il tuo giudizio (SI / NO)");
                while (BotResponse.Length > 2)
                {
                    BotResponse = "";
                    await _core.TalkWithVanessa("Puoi solo scrivere SI in caso affermativo e NO in caso negativo", true);

                }

                if (BotResponse.Equals("NO"))
                {

                    try
                    {
                        schoolContext.Announcements.Add(testing);
                        schoolContext.SaveChanges();
                        await telegramBot.CLEAR(testing.Announcer.TelegramId);
                        await telegramBot.SendMessage("La richiesta è stata accettata e sarà inserita in database.\nSi prenderanno provvedimenti a fine settimana. ", testing.Announcer.TelegramId);


                        testing.Announcer.WeeklyAnnouncement = true;
                        await telegramBot.Menu(testing.Announcer.TelegramId);
                        telegramBot.DeleteWritingAnnouncement(testing.Announcer.TelegramId);
                    }
                    catch (Exception e)
                    {
                        schoolContext.Announcements.Remove(testing);
                        telegramBot.DeleteWritingAnnouncement(testing.Announcer.TelegramId);
                        await telegramBot.SendMessage("E' stato riscontrato un problema, ci scusiamo per l'imprevisto", testing.Announcer.TelegramId);
                        await telegramBot.SendMessage("E' stato riscontrato un problema, ci scusiamo per l'imprevisto", telegramBot.DavideID);

                    }
                }
                else
                {
                    await telegramBot.SendMessage("La richiesta non è stata accettata...\nHai perso 0.5 trustpoints\nIl linguaggio utilizzato è risultato inappropriato", testing.Announcer.TelegramId);
                    testing.AI_Analyzing = false;
                    testing.Announcer.TrustPoints -= 0.5;
                    schoolContext.SaveChanges();
                    await telegramBot.RiepilogoAnnuncio(testing.Announcer.TelegramId);

                }


            }

            semaphore.Release();

        }
        Thread.Sleep(20);

    }



}

string GenerateRandomString(int length)
{
    Random random = new Random();
    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    StringBuilder result = new StringBuilder(length);

    for (int i = 0; i < length; i++)
    {
        result.Append(chars[random.Next(chars.Length)]);
    }

    return result.ToString();
}

List<Classroom> robe = new List<Classroom>();
async Task FottiClassi()
{
    Classroom classroom = new Classroom();
    var httpClient = new HttpClient();
    var html = await httpClient.GetStringAsync("https://www.isiskeynes.edu.it/pagine/libri-di-testo-as-2024-25");
    var doc = new HtmlDocument();
    doc.LoadHtml(html);
    HtmlNode tbodyNode = doc.DocumentNode.SelectSingleNode("//tbody");
    if (tbodyNode != null)
    {
        // Iterate through the child nodes of the tbody
        foreach (HtmlNode trNode in tbodyNode.ChildNodes)
        {
            // Ensure the node is a table row (<tr>)
            if (trNode.Name == "tr")
            {
                // Iterate through the child nodes of the table row
                foreach (HtmlNode tdNode in trNode.ChildNodes)
                {
                    // Ensure the node is a table data (<td>) cell
                    if (tdNode.Name == "td")
                    {
                        classroom.Year = tdNode.FirstChild.InnerHtml[0] + "";
                        classroom.Section = tdNode.FirstChild.InnerHtml[1] + "";
                        classroom.Specialization = tdNode.FirstChild.InnerHtml.Substring(2);

                        robe.Add(classroom);
                        classroom = new Classroom();

                    }
                }
            }
        }
    }

}

async Task ResettaTutto()
{
    foreach (Person per in schoolContext.Students)
    {
        per.Classroom = null;
        per.Problem = null;
        per.Decisions = null;

    }
    schoolContext.Classrooms.RemoveRange(schoolContext.Classrooms);
    schoolContext.Problems.RemoveRange(schoolContext.Problems);
    schoolContext.Students.RemoveRange(schoolContext.Students);
    schoolContext.Pools.RemoveRange(schoolContext.Pools);
    schoolContext.Decisions.RemoveRange(schoolContext.Decisions);
    schoolContext.Announcements.RemoveRange(schoolContext.Announcements);
    schoolContext.Letters.RemoveRange(schoolContext.Letters);
    schoolContext.SaveChanges();

    await FottiClassi();
    schoolContext.Classrooms.AddRange(robe);
    schoolContext.SaveChanges();
    

}

async Task RimuoviDuplicati() { 

    List<Person> daRimuover=new List<Person>();
    List<Person> tmp= new List<Person> ();
    List<Person> tmp1 = new List<Person>();
    List<Person> tmp2 = new List<Person>();
    Person pers;
    tmp2 = schoolContext.Students.ToList();
    foreach (Person p in tmp2) {
        
        if (!daRimuover.Contains(p))
        {
            tmp.Clear();
            tmp1 = schoolContext.Students.Where(x => x.Name.Equals(p.Name) && x.Surname.Equals(p.Surname)).ToList();
            tmp.AddRange(tmp1);
            if (tmp.Count > 1)
            {

                if (tmp.Exists(x => x.TelegramId != -1))
                {

                    pers = tmp.Find(x => x.TelegramId != -1);

                    if (pers != null)
                    {
                        tmp.Remove(pers);
                    }
                }
                else
                {

                    tmp.Remove(tmp[0]);

                }
                daRimuover.AddRange(tmp);
            }
        }
    }
    foreach (Person p in daRimuover) {

        p.Classroom = null;
        p.Announcements = null;        
    
    }
    schoolContext.RemoveRange(daRimuover);
    schoolContext.SaveChanges();

}


//await RimuoviDuplicati();

app.Run();



