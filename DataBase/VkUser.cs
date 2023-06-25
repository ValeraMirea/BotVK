namespace Bot.DataBase
{
    public class VkUser
    {
        public long Id { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public double Rating { get; set; } = 0.0;
        public int Warnings { get; set; } = 0;
        public bool Ban { get; set; } = false;
    }
}