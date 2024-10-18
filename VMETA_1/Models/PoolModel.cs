using VMETA_1.Classes;
using VMETA_1.Entities;

namespace VMETA_1.Models
{
    public class PoolModel
    {
        public int Id { get; set; }
        public string Titolo { get; set; }
        public string Descrizione { get; set; }
        public List<OptionModel> Votes { get; set; }
        
        public PoolModel(Pool p) {
        
            Titolo= p.Titolo;  
            Descrizione= p.Descrizione;
            Votes=new List<OptionModel>();
            Id= p.Id;
            foreach (KeyValuePair<string, int> kvp in p.GetsResults()) {
                Votes.Add(new OptionModel(kvp.Key, kvp.Value));
            }

                    
        }
        


    }
}
