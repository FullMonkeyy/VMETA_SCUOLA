namespace VMETA_1.Entities
{
    public class Decision
    {

        //La decisione è relazionata ad una persona ed a un sondaggio
        public int Id { get; set; }
        public Person Person { get; set; }
        public string Selected { get; set; }
        public Pool Pool { get; set; }        
        public bool isChosen {  get; set; }
        public DateTime Data_Votazione{ get; set; }

        public Decision() {
            isChosen = false;
            Selected = "##NOTSELECTED##";
        }

        public Decision(Person person,Pool p) { 
        
            Person = person;
            Pool = p;
            Selected = "##NOTSELECTED##";
            isChosen = false;
        }
        public override string ToString() {

            return $"{Person.ToString()} ha votato: {Selected}";
        
        }
        public string AvaibleOptions() {

            string tmp="";

            Pool.Options.ForEach(option => tmp += option);

            return tmp;

        }
        public bool MakeYourDecision(string selected) {
            if (!Pool.Options.Contains(selected)) return false;

            Selected = selected;
            isChosen = true;
            Data_Votazione=DateTime.Now;    
            return true ;
        }     
        public string PoolTitle()
        {
            return Pool.Titolo;
        }
        public bool IsChina() {

            if (Selected.Equals("##NOTSELECTED##"))
                return false;
            return true;

        } 


    }
}
