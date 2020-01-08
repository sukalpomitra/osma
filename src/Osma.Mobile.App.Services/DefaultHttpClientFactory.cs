using System;
using System.Net.Http;

namespace Osma.Mobile.App.Services
{
    public class DefaultHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new HttpClient();
    }
}