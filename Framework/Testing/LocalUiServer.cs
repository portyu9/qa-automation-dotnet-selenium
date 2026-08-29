using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace UiTests.Framework.Testing;

/// <summary>
/// Repository-owned loopback HTTP fixture used by the default browser contract.
/// It deliberately avoids external DNS, TLS, accounts, and service availability.
/// </summary>
public sealed class LocalUiServer : IDisposable
{
    public const int Port = 3200;
    public static readonly Uri DefaultBaseUrl = new($"http://127.0.0.1:{Port}/");

    private readonly CancellationTokenSource cancellation = new();
    private readonly TcpListener listener = new(IPAddress.Loopback, Port);
    private readonly ConcurrentDictionary<int, Task> activeClients = new();
    private readonly Task acceptLoop;
    private int nextClientId;
    private bool disposed;

    public LocalUiServer()
    {
        listener.Start();
        acceptLoop = Task.Run(() => AcceptLoopAsync(cancellation.Token));
    }

    public Uri BaseUrl => DefaultBaseUrl;

    private async Task AcceptLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await listener.AcceptTcpClientAsync(token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                break;
            }
            catch (SocketException) when (token.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException) when (token.IsCancellationRequested)
            {
                break;
            }

            var clientId = Interlocked.Increment(ref nextClientId);
            var task = HandleClientSafelyAsync(client, token);
            activeClients[clientId] = task;
            _ = task.ContinueWith(
                completedTask =>
                {
                    _ = completedTask;
                    activeClients.TryRemove(clientId, out _);
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
    }

    private static async Task HandleClientSafelyAsync(TcpClient client, CancellationToken token)
    {
        try
        {
            await HandleClientAsync(client, token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // Expected if fixture teardown interrupts an in-flight request.
        }
        catch (IOException) when (token.IsCancellationRequested)
        {
            // Expected when the listener/client transport is torn down.
        }
        catch (ObjectDisposedException) when (token.IsCancellationRequested)
        {
            // Expected when fixture teardown closes transport resources.
        }
    }

    private static async Task HandleClientAsync(TcpClient client, CancellationToken token)
    {
        using (client)
        using (var stream = client.GetStream())
        using (var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, leaveOpen: true))
        {
            var requestLine = await reader.ReadLineAsync(token);
            if (string.IsNullOrWhiteSpace(requestLine)) return;

            string? header;
            do
            {
                header = await reader.ReadLineAsync(token);
            }
            while (!string.IsNullOrEmpty(header));

            var parts = requestLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var method = parts.Length > 0 ? parts[0] : string.Empty;
            var target = parts.Length > 1 ? parts[1] : "/";

            if (!string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase))
            {
                await WriteResponseAsync(stream, 405, "Method Not Allowed", "text/plain; charset=utf-8", "Method Not Allowed", token);
                return;
            }

            var path = new Uri(DefaultBaseUrl, target).AbsolutePath;
            var response = path switch
            {
                "/" => (200, "OK", "text/html; charset=utf-8", LoginPage),
                "/inventory.html" => (200, "OK", "text/html; charset=utf-8", InventoryPage),
                "/interactions.html" => (200, "OK", "text/html; charset=utf-8", InteractionsPage),
                "/health" => (200, "OK", "application/json; charset=utf-8", "{\"status\":\"ok\"}"),
                _ => (404, "Not Found", "text/plain; charset=utf-8", "Not Found")
            };

            await WriteResponseAsync(stream, response.Item1, response.Item2, response.Item3, response.Item4, token);
        }
    }

    private static async Task WriteResponseAsync(
        NetworkStream stream,
        int status,
        string reason,
        string contentType,
        string body,
        CancellationToken token)
    {
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var header =
            $"HTTP/1.1 {status} {reason}\r\n" +
            $"Content-Type: {contentType}\r\n" +
            $"Content-Length: {bodyBytes.Length}\r\n" +
            "Cache-Control: no-store\r\n" +
            "X-Content-Type-Options: nosniff\r\n" +
            "Connection: close\r\n\r\n";
        var headerBytes = Encoding.ASCII.GetBytes(header);

        await stream.WriteAsync(headerBytes, token);
        await stream.WriteAsync(bodyBytes, token);
        await stream.FlushAsync(token);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        cancellation.Cancel();
        listener.Stop();

        try
        {
            acceptLoop.GetAwaiter().GetResult();
            Task.WhenAll(activeClients.Values.ToArray()).GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
            // Expected during fixture teardown.
        }
        finally
        {
            cancellation.Dispose();
        }
    }

    private const string LoginPage = """
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Authentication Fixture</title>
</head>
<body>
  <main>
    <h1>Authentication Fixture</h1>
    <form id="login-form">
      <label for="user-name">Username</label>
      <input id="user-name" autocomplete="username">
      <label for="password">Password</label>
      <input id="password" type="password" autocomplete="current-password">
      <button id="login-button" type="submit">Sign in</button>
      <p id="login-error" role="alert" hidden></p>
    </form>
  </main>
  <script>
    document.getElementById('login-form').addEventListener('submit', (event) => {
      event.preventDefault();
      const username = document.getElementById('user-name').value;
      const password = document.getElementById('password').value;
      const error = document.getElementById('login-error');
      if (username === 'standard_user' && password === 'secret_sauce') {
        window.location.assign('/inventory.html');
        return;
      }
      error.textContent = 'Invalid username or password';
      error.hidden = false;
    });
  </script>
</body>
</html>
""";

    private const string InventoryPage = """
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Inventory Fixture</title>
</head>
<body>
  <main id="inventory_container">
    <h1>Inventory Fixture</h1>
    <article><h2>Fixture Item A</h2></article>
    <article><h2>Fixture Item B</h2></article>
  </main>
</body>
</html>
""";

    private const string InteractionsPage = """
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Browser Capability Surface</title>
</head>
<body>
  <main>
    <h1 id="capability-title">Browser Capability Surface</h1>
    <button id="open-alert" type="button">Open alert</button>
    <button id="open-popup" type="button">Open inventory window</button>
    <iframe id="details-frame" title="Capability frame" srcdoc="<p id='frame-value'>frame-ready</p>"></iframe>
  </main>
  <script>
    document.getElementById('open-alert').addEventListener('click', () => alert('fixture-alert'));
    document.getElementById('open-popup').addEventListener('click', () => window.open('/inventory.html', '_blank'));
  </script>
</body>
</html>
""";
}
