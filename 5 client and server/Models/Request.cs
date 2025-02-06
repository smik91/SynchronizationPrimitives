namespace Common
{
    public class Request
    {
        public ActionType Action { get; set; }
        public Employee Employee { get; set; } = new Employee();
    }
}
