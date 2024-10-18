namespace VMETA_1.Entities
{
    public class Pool
    {

        //Un sondaggio è relazionato solo con più decisioni

        public int Id { get; set; }
        public string Titolo {  get; set; }
        public string Descrizione {  get; set; }
        public List<string> Options { get; set; }
        public List<Decision> Votes { get; set; } 
        public bool IsOnlyRappresentanti {  get; set; }
        public DateTime Data_inserimento { get; set; }

        public Pool() {

            Votes = new List<Decision>();
            Data_inserimento = DateTime.Now;
        }
        public Pool(string title, string description, List<string> options, bool isOnlyRappresentanti)
        {

            Votes = new List<Decision>();
            Titolo = title;
            Descrizione = description;
            Options = options;
            IsOnlyRappresentanti = isOnlyRappresentanti;
            Data_inserimento = DateTime.Now;
        }
        public bool AddDecision(Decision d) {
            if (d == null) return false;

            Votes.Add(d);
            return true;
        }
        public override string ToString() {

            return $"Titolo:{Titolo}\n\nDescrizione:\n{Descrizione}";
        
        }
        public Dictionary<string, int> GetsResults() { 
        
            Dictionary<string,int> tmp=new Dictionary<string, int> ();
            foreach(string option in Options)
            {
              
                tmp.Add(option, Votes.FindAll(x=> x.Selected.Equals(option)).Distinct().ToList().Count);               


            }

            return tmp;
        
        }

    }
}
