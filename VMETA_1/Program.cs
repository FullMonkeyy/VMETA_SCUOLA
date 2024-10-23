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


var builder = WebApplication.CreateBuilder(args);
bool BUSY_VANESSA = false;
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
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

TelegramBot telegramBot = new TelegramBot(apirelease, schoolContext);
telegramBot.ProblemaPronto += AddProblem;
telegramBot.RiavvioNecessario += ReStart;
telegramBot.LetteraPronta += AddLetter;
telegramBot.AnnuncioPronta += AddAnnouncement;
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
//POST
app.MapPost("/api/SendMessage", async (JsonObject json) =>
{
    string jasonstring = json.ToString();

    Message? message = JsonConvert.DeserializeObject<Message>(jasonstring);


    await _core.TalkWithVanessa(message.Content);

    /////////////////////////////////////////
    return Results.Accepted("id libro:");

});
app.MapPost("/api/SendPerson", (JsonObject json) =>
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


    if (tmp != null)
    {

        string Name = ja[0].Value.ToString();
        string Cognome = ja[1].Value.ToString();
        string email = ja[4].Value.ToString();
        string Phonw = ja[5].Value.ToString();
        string TelegramCODE = ja[6].Value.ToString();
        bool check;
        bool.TryParse(ja[7].Value.ToString(), out check);
        if (Name.Length > 0)
        {
            Person p = new Person(Name, Cognome, DateTime.MinValue, tmp, -1, email, Phonw, check);

            if (telegramBot.RegisterNewAccountRequest(Name, Cognome, TelegramCODE))
            {

                schoolContext.Students.Add(p);
                schoolContext.SaveChanges();
                return Results.Accepted("Operation succed");
            }
            else return Results.BadRequest("Codice telegram già preso");

        }

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

        todelete.Problem = null;
        todelete.Classroom = null;

        schoolContext.Students.Remove(todelete);
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


        schoolContext.Decisions.RemoveRange(todelete.Votes);


        schoolContext.SaveChanges();


        schoolContext.Pools.Remove(todelete);
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
        catch (Exception e) { }

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
        catch(Exception e) { }

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

    telegramBot.RiavviaClient("7502523717:AAHuuedxcjwGwIarfZUrMCEfsQbsyXHPwbY");

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

            string final = "Questa segnalazione contiene PAROLACCE come cazzo, merda, figlio di puttana etc.. , MINACCE DI MORTE oppure OFFESE RAZIALI COME NEGRO e simili? scrivi SOLO SI in caso AFFERMATIVO scrivi solo NO in caso NEGATIVO\n\n\n" + testing.ToString();
            BotResponse = "";

            await _core.TalkWithVanessa(final);

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


                if (BotResponse.Equals("NO"))
                {
                    try
                    {
                        schoolContext.Problems.Add(testing);
                        schoolContext.SaveChanges();
                        await telegramBot.CLEAR(testing.Person.TelegramId);
                        await telegramBot.SendMessage("La richiesta è stata accettata e sarà inserita in database.\nSi prenderanno provvedimenti a fine settimana. ", testing.Person.TelegramId);


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
                    BotResponse = "";
                    await _core.TalkWithVanessa("Scrivi solo il motivo per il quale la richiesta non viene accettata argomentando adeguatamente.");
                    await telegramBot.SendMessage("La richiesta non è stata accettata...", testing.Person.TelegramId);
                    await telegramBot.SendMessage(BotResponse, testing.Person.TelegramId);
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
            string final = "Questo messaggio contiene PAROLACCE come cazzo, merda, figlio di puttana etc.. , MINACCE DI MORTE, TERMINI OMOFOBI oppure OFFESE RAZIALI COME NEGRO e simili? scrivi SOLO SI in caso AFFERMATIVO scrivi solo NO in caso NEGATIVO\n\n\n" + testing.Title;
            BotResponse = "";

            await _core.TalkWithVanessa(final);

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
                BotResponse = "";

                await _core.TalkWithVanessa("Pensi ancora che \n\n\" " + testing.Body + "\"\n\ncontenga parolace, offese raziali o minacce di morte? Scrivi di nuovo il tuo giudizio (SI / NO)");


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
                    BotResponse = "";
                    mex = $"Perché hai rifiutato l'elaborazione del messaggio? Scrivi le motivazioni";
                    await _core.TalkWithVanessa(mex);
                    string me = BotResponse;
                    await telegramBot.CLEAR(testing.People.Find(x => x.ToString().Equals(testing.Author)).TelegramId);
                    await telegramBot.CLEAR(testing.People.Find(x => x.ToString().Equals(testing.Destination)).TelegramId);
                    await telegramBot.SendMessage("Il messaggio non è stato accettato.", testing.People.Find(x => x.ToString().Equals(testing.Author)).TelegramId);
                    await telegramBot.SendMessage(me, testing.People.Find(x => x.ToString().Equals(testing.Author)).TelegramId);
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

            string final = "Questa segnalazione contiene PAROLACCE come cazzo, merda, figlio di puttana etc.. , MINACCE DI MORTE oppure OFFESE RAZIALI COME NEGRO e simili? scrivi SOLO SI in caso AFFERMATIVO scrivi solo NO in caso NEGATIVO\n\n\n" + testing.ToString();
            BotResponse = "";

            await _core.TalkWithVanessa(final);

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
                    await _core.TalkWithVanessa("Scrivi solo il motivo per il quale la richiesta non viene accettata argomentando adeguatamente.");
                    await telegramBot.SendMessage("La richiesta non è stata accettata...", testing.Announcer.TelegramId);
                    await telegramBot.SendMessage(BotResponse, testing.Announcer.TelegramId);
                    await telegramBot.Riepilogo(testing.Announcer.TelegramId, true);

                }


            }

            semaphore.Release();

        }
        Thread.Sleep(20);

    }



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
await ResettaTutto();
async Task ResettaTutto()
{
    foreach (Person p in schoolContext.Students)
    {
        p.Classroom = null;
        p.Problem = null;
        p.Decisions = null;

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



app.Run();



