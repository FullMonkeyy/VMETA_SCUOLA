using VMETA_1.Entities;

namespace VMETA_1.Classes
{
    public class RegisterRequest
    {
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Code { get; set; }
        public bool isRegistred { get; set; }
        public string Email { get; set; }
        DateTime _dtRequest;
        public DateTime date { get { return _dtRequest; } }
        public RegisterRequest() { }
        public RegisterRequest(string n, string s, string c, string e)
        {
            Name = n;
            Surname = s;
            Code = c;
            isRegistred = false;
            _dtRequest = DateTime.Now;
            Email = e;
        }
        public override bool Equals(object? obj)
        {

            if (obj == null || !(obj is RegisterRequest)) return false;

            RegisterRequest other = obj as RegisterRequest;

            if (other.Name.ToLower().Equals(Name.ToLower()) && other.Surname.ToLower().Equals(Surname.ToLower())) return true;
            else return false;

        }


    }
}
