﻿namespace VMETA_1.Entities
{
    public class Letter
    {
        public int Id { get; set; }
        public List<Person>? People {get; set; }
        public string? Title { get; set; }
        public string? Body { get; set; }
        public DateTime InsertionDate { get; set; }
        public bool? AI_Forced { get; set; }
        public int? CountTry { get; set; }
        public string? Author {  get; set; }
        public string? Destination { get; set; }
        public bool? AI_Analyzing { get; set; }
        public int? YEARAuthor {  get; set; }
        public double TrustPoints {  get; set; }
        public Letter() {
            People = new List<Person>();
            AI_Analyzing = false;
            TrustPoints = 0;
            InsertionDate = DateTime.Now;
        }

        public override string ToString()
        {
            try
            {
                if (!Author.Equals("ADMIN"))
                {
                    if (Author != null && Destination != null)
                        return $"Messaggio:\n{Body}\n\n-Messaggio inviato da {Author} per {Destination}\n\nSpedito il {InsertionDate.ToLongDateString()}";
                    else if (Destination != null)
                        return $"Messaggio:\n{Body}\n\n-Messaggio inviato da {Author} per {Destination} ";
                    else
                        return $"Messaggio:\n{Body}";
                }
                return $"---NOTIFICA DI SISTEMA---\n\n{Title}\n\n{Body}";
            }
            catch (Exception ex)
            {
                return $"Messaggio:\n{Body}\n\nSpedito il {InsertionDate.ToLongDateString()}";
            }
        }


    }
}
