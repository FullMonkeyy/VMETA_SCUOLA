﻿using VMETA_1.Entities;

namespace VMETA_1.Models
{
    public class ProblemModel
    {

        public string Title { get; set; }
        public string Description { get; set; }
        public string Person { get; set; }
        public string Classroom { get; set; }
        public string Solution { get; set; }
        public bool Secret { get; set; }
        public string isStudent {  get; set; }
        public string Category {  get; set; }
        public string InsertDate {  get; set; }
        public bool AIForced {  get; set; }
        public int Id {  get; set; }
        public double TP {  get; set; }

        public ProblemModel() { }   
        public ProblemModel(Problem p)
        {

            Title = p.Title;
            Description = p.Description;
            if (p.Person != null && p.isStudente.Equals("true"))
                Person = p.Person.ToString();
            else Person = "SECRET";
            if (p.Classroom != null) 
            Classroom = p.Classroom.ToString();
            Solution = p.Solution;
            Secret = p.Secret;
            isStudent=p.isStudente;
            Category = p.Category;
            InsertDate=p.DataInserimento.ToShortDateString();
            AIForced = p.AI_Forced;
            Id= p.Id;
            TP = p.TrustPoints;

        }

    }
}
