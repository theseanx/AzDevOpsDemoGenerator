using System.Net;

namespace RestAPI.Viewmodel
{
    public class BaseViewModel
    {
        public HttpStatusCode HttpStatusCode { get; set; }
        public string Message { get; set; }
    }
}
