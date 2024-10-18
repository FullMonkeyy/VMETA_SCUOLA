namespace VMETA_1.Classes
{
    public class Messaggio
    {
        public string Message { get; set; }
        public string Role { get; set; }
        public DateTime DataInserimento { get; set; }
        public Messaggio() { }
        public Messaggio(string m, string r, DateTime dt)
        {
            Message = m;
            Role = r;
            DataInserimento = dt;
        }
        public override string ToString()
        {

            return "ROLE: " + Role + "\n" + Message;

        }
    }
}
