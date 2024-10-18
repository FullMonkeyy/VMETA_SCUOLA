namespace VMETA_1.Classes
{
    public class OptionModel
    {
        public string Title { get; set; }
        public int Votes {  get; set; }
        public OptionModel(string t, int v) { 
        
            Title = t;
            Votes = v;
        
        }
    }
}
