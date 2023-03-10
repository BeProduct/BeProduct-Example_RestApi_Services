using System.Net;
using System.Security;

namespace OAuth2.Helpers 
{
    public class Server : IDisposable
    {
        private readonly HttpListener listener;
        private readonly Action<HttpListenerContext> handler;
        private Thread thread;

        private Server(HttpListener listener, Action<HttpListenerContext> handler)
        {
            this.listener = listener;
            this.handler = handler;
        }

        public static Server Create(string url, Action<HttpListenerContext> handler,
            AuthenticationSchemes authenticationSchemes = AuthenticationSchemes.Anonymous)
        {
            HttpListener listener = new HttpListener
            {
                Prefixes = { url },
                AuthenticationSchemes = authenticationSchemes
            };
            Server server = new Server(listener, handler);

            server.Start();

            return server;
        }

        public void Start()
        {
            if (this.listener.IsListening)
            {
                return;
            }

            this.listener.Start();

            this.thread = new Thread(() =>
            {
                HttpListenerContext context = this.listener.GetContext();
                this.handler(context);
                context.Response.Close();
            })
            {
                Name = "WebServer"
            };

            this.thread.Start();
        }

        public void Dispose()
        {
            try
            {
                this.thread.Abort();
            }
            catch (ThreadStateException threadStateException)
            {
                Console.WriteLine("Issue aborting thread - {0}.", threadStateException.Message);
            }
            catch (SecurityException securityException)
            {
                Console.WriteLine("Issue aborting thread - {0}.", securityException.Message);
            }

            if (this.listener.IsListening)
            {
                try
                {
                    this.listener.Stop();
                }
                catch (ObjectDisposedException objectDisposedException)
                {
                    Console.WriteLine("Issue stopping listener - {0}", objectDisposedException.Message);
                }
            }

            this.listener.Close();
        }
    }
}
