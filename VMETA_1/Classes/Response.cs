namespace VMETA_1.Classes
{
    public class Response
    {
        public string Content { get; set; }
        public Response(string content)
        {
            Content = content;
        }
        public Response() { }
    }
}
