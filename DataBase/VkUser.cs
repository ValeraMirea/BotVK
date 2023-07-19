namespace Bot.DataBase
{
    public class VkUser
    {
        public long Id { get; set; }
        //public string FirstName { get; set; } = "";
        //public string LastName { get; set; } = "";
        public double Rating { get; set; } = 0.0;
        public int Number_of_messages { get; set; } = 0;
        public int Warnings { get; set; } = 0;
        public int Affordable_reputation {get; set;} = 5;
        public int Available_risk { get; set; } = 1;
        public TimeOnly TimeOnly { get; set; } = new TimeOnly();
        public bool Ban { get; set; } = false;
    }
}