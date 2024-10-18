using System;

namespace VMETA_1.Entities
{
    public class Classroom
    {
        public int Id { get; set; }
        public string Year { get; set; }
        public string Section { get; set; }
        public string Specialization { get; set; }
        public List<Problem>? Problems { get; set; }
        public List<Person>? People { get; set; }

        public Classroom(string y, string sec, string spe)
        {
         
            Year = y;
            Section = sec;
            Specialization = spe;
            Problems = new List<Problem>();
            People = new List<Person>();
        }
        public Classroom() { }

        public override string ToString()
        {
            return Year + Section + Specialization;
        }
        public override bool Equals(object? obj)
        {
            if (!(obj is Classroom) || obj == null) return false;
            Classroom other = obj as Classroom;

            if (other.ToString().Equals(this.ToString())) return true;
            else return false;

        }
    }
}
