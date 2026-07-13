
namespace IBSMobile.Statics
{
    public class Response
    {
        public bool error { get; set; } = false;
        public string? message { get; set; } = default!;
        public object? data { get; set; } = default!;
    }

    public class ResponseNoData
    {
        public bool error { get; set; } = false;
        public string? message { get; set; } = default!;
    }

}

