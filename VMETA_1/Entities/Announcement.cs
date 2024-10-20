using System.ComponentModel;
using System.Globalization;

namespace VMETA_1.Entities
{
    public class Announcement
    {
        public int id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public Person Announcer {  get; set; }
        public int? ClassroomYEAR {  get; set; }
        public DateTime DataInserimento { get; set; }
        public int TrustPoints { get; set; }
        public bool AI_Forced { get; set; } 
        public int CountTry { get; set; }
        public bool AI_Analyzing {  get; set; }
        public Announcement() { DataInserimento = DateTime.Now; AI_Analyzing = false; }
        public Announcement(string t,string d, Person a,bool AIForced)
        {
            DataInserimento = DateTime.Now;
            Title = t;
            Description=d;
            Announcer = a;
            AI_Forced = AIForced;
        }

        public override string ToString()
        {
            string tmp ="";
            if (ClassroomYEAR != null) {
                tmp += ClassroomYEAR;
                return $"Titolo\n{Title}\n\nDescription\n{Description}\n\nStudente del {tmp}° anno ";

            }else
            return $"Titolo\n{Title}\n\nDescription\n{Description} ";
        }

    }
}
