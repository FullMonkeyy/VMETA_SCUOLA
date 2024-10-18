using VMETA_1.Entities;

namespace VMETA_1.Models
{
    public class ClassroomModel
    {

   
        public string Year { get; set; }
        public string Section { get; set; }
        public string Specialization { get; set; }
        public List<ProblemModel>? Problems { get; set; }
        public List<PersonModel>? People { get; set; }

        public ClassroomModel(Classroom cls) { 
        
            Year = cls.Year;
            Section = cls.Section;
            Specialization = cls.Specialization;
            Problems=new List<ProblemModel>();
            cls.Problems.ForEach(x => Problems.Add(new ProblemModel(x)));
            People = new List<PersonModel>();
            cls.People.ForEach(x => People.Add(new PersonModel(x)));

        }

    }
}
