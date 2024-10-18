using VMETA_1.Entities;

namespace VMETA_1.Classes
{
    public class RegisterRequest
    {
        public string Name {  get; set; }
        public string Surname {  get; set; }
        public string Code {  get; set; }
        public bool isRegistred {  get; set; }
        DateTime _dtRequest;
        public DateTime date { get { return _dtRequest; } }

        public RegisterRequest(string n ,string s, string c)
        {
            Name = n;
            Surname = s;
            Code = c;
            isRegistred = false;
            _dtRequest=DateTime.Now;
        }


    }
}
