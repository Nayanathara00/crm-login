namespace WebApplication1.Models
{
    public class Project
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public float Cost { get; set; }
        public string TechLead { get; set; }
        public string Customer { get; set; }
    }
}


