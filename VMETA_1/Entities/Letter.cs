namespace VMETA_1.Entities
{
    public class Letter
    {
        public int Id { get; set; }
        public List<Person>? People {get; set; }
        public string? Title { get; set; }
        public string? Body { get; set; }
        DateTime InsertionDate { get; set; }
        public bool? AI_Forced { get; set; }
        public int? CountTry { get; set; }
        public string? Author {  get; set; }
        public string? Destination { get; set; }
        public bool? AI_Analyzing { get; set; }
        public int? YEARAuthor {  get; set; }
        public double TrustPoints {  get; set; }
        public Letter() {
            People = new List<Person>();
            AI_Analyzing = false;
            TrustPoints = 0;
        }

        public override string ToString()
        {
            try
            {
                return $"Messaggio originale\n{Title}\n\nMessaggio rielaborato\n{Body}\n\n-Messaggio inviato da {People[0].ToString()} per {People[1].ToString()}";
            }
            catch (Exception ex)
            {
                return $"Messaggio originale\n{Title}\n\nMessaggio rielaborato\n{Body}\n\n-Messaggio inviato da {People[0].ToString()} ";
            }
        }


    }
}
