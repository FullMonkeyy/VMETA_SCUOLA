using VMETA_1.Entities;

namespace VMETA_1.Models
{
    public class AnnouncementModel
    {
        public int Id { get; set; }
        public string Title {  get; set; }
        public string Description { get; set; }
        public DateTime insertionDate { get; set; }
        public int AnnouncerId { get; set; }
        public string AnnouncerName { get; set; }
        public int ClassroomYEAR { get; set; }
        public AnnouncementModel() { }
        public AnnouncementModel(Announcement a)
        {

            Title = a.Title;
            Id=a.id;
            Description = a.Description;
            insertionDate=a.DataInserimento;
            AnnouncerId = a.Announcer.Id;
            AnnouncerName = a.Announcer.ToString();
            if(a.ClassroomYEAR!=null)
            ClassroomYEAR=(int)a.ClassroomYEAR;




        }


    }
}
