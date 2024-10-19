namespace VMETA_1.Entities
{
    public class Problem
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public Person Person { get; set; }
        public Classroom? Classroom { get; set; }
        public DateTime DataInserimento { get; set; }
        public string? Solution { get; set; }
        public bool Secret { get; set; }
        public string Category {  get; set; }
        public string isStudente { get; set; }
        public int TrustPoints { get; set; }
        public bool AI_Forced {  get; set; }
        public int CountTry {  get; set; }

        public Problem() {
            DataInserimento=DateTime.Now;
            AI_Forced = false;
            Title = "-NOT SETTED5353453453435375698";
            Description = "-NOT SETTED5353453453435375698";
            Solution = "-NOT SETTED5353453453435375698";
        }
        public Problem(string t, string d, Person p, Classroom c, string solution, bool secret, string category, string is_student, bool aI_Forced)
        {
            Title = t;
            Description = d;
            Person = p;
            Classroom = c;
            Solution = solution;
            Secret = secret;
            Category = category;
            isStudente = is_student;
            TrustPoints = Person.TrustPoints;
            DataInserimento = DateTime.Now;
            AI_Forced = aI_Forced;  
        }
        public override string ToString()
        {
            
            string header;
            if (isStudente.Equals("true"))
                header = $"Segnalazione in qualità di {Person.Name} {Person.Surname} {Person.Classroom.ToString()} \n\n";
            else header = $"Segnalazione in qualità di rappresentante di classe {Person.Classroom.ToString()}\n\n";

            string middle="Not setted";
            string bottom = "Not setted";
            string solution = "Not setted";


            if (!Title.Equals("-NOT SETTED5353453453435375698"))
                middle = $"Title: {Title}\n\n";
            if (!Description.Equals("-NOT SETTED5353453453435375698"))
                bottom = $"Description: {Description}\n\n";
            if (!Solution.Equals("-NOT SETTED5353453453435375698"))
                solution = $"Solution: {Solution}";

            return header + middle + bottom +solution;   

        }

    }
}
