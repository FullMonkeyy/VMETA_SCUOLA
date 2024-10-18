namespace VMETA_1.Entities
{
    public class Letter
    {
        public int Id { get; set; }
        public List<Person> People {get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        DateTime InsertionDate { get; set; }
        public bool AI_Forced { get; set; }
        public int CountTry { get; set; }
        public string Author {  get; set; }
        public string Destination { get; set; }

       
        public Letter() {
            People = new List<Person>();
        }

        public override string ToString()
        {
            return $"{Title}\n{Body}\n{People[0].ToString()}";
        }


    }
}
