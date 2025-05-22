using VMETA_1.Entities;

namespace VMETA_1.Models
{
    public class PersonModel
    {

        public string Name { get; set; }    
        public string Surname {  get; set; }
        public string Email { get; set; }   
        public string Phone { get; set; }
        public string Classroom { get; set; }
        public bool IsJustStudent { get; set; }
        public bool IsRegistred {  get; set; }
        public long TelegramId {  get; set; }

        public List<ProblemModel> Problems { get; set; }   

        public PersonModel() { }
        public PersonModel(Person p)
        {
            Name = p.Name;
            Surname = p.Surname;
            Email = p.Email;
            Phone = p.Phone;
            if(p.Classroom != null)
            Classroom=p.Classroom.ToString();
            Problems = new List<ProblemModel>();
            p.Problem.ForEach(x=> Problems.Add(new ProblemModel(x)));
            if (p.TelegramId != -1)
            {
                IsRegistred = true;
            }
            else
            {
                IsRegistred = false;
            }

            IsJustStudent=p.isJustStudent;
            TelegramId = p.TelegramId;

        }


    }
}
