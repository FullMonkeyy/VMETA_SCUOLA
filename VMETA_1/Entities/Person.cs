namespace VMETA_1.Entities
{
    public class Person
    {
        public int Id { get; set; }
        public double TrustPoints { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public DateTime Birthday { get; set; }
        public DateTime DataInserimento { get; set; }
        public Classroom Classroom { get; set; }
        public List<Problem>? Problem { get; set; }
        public bool WeeklyAnnouncement{ get; set; }
        public List<Announcement> Announcements { get; set; }
        public List<Letter> Letters { get; set; }
        
       
        //Una persona è relazionata con più decisioni, non con i sondaggi
        public List<Decision> Decisions { get; set; }
        public int LastDecision { get { return Decisions.FindAll(x => !x.isChosen).Distinct().Count(); } }
        public long TelegramId { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public bool isJustStudent {  get; set; }

        public Person(){ 
            DataInserimento = DateTime.Now;
            Decisions = new List<Decision>();
            Problem = new List<Problem>();
            Announcements = new List<Announcement>();
            WeeklyAnnouncement=false;
        }
        public Person(string n, string s, DateTime dt, Classroom cl, long teleID, string ema,string phone, bool isStudent)
        {
            TrustPoints = 0;
            Name = n;
            Surname = s;
            Birthday = dt;
            Classroom = cl;
            TelegramId = teleID;
            Email = ema;
            Problem = new List<Problem>();
            Decisions = new List<Decision>();
            Announcements = new List<Announcement>();
            Phone = phone;
            DataInserimento = DateTime.Now;
            this.isJustStudent = isStudent;
        }
        public override string ToString()
        {
            return Name + " " + Surname;
        }

    }
}
