// Create a new instance of a container.
using System.Diagnostics;
using DotNet.Testcontainers.Builders;

var container = new ContainerBuilder("testcontainers/helloworld:1.3.0")
  // Bind port 8080 of the container to a random port on the host.
  .WithPortBinding(8080, true)
  // Wait until the HTTP endpoint of the container is available.
  .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(8080)))
  // Build the container configuration.
  .Build();

// Start the container.
await container.StartAsync()
  .ConfigureAwait(false);

// Create a new instance of HttpClient to send HTTP requests.
using var httpClient = new HttpClient();

// Construct the request URI by specifying the scheme, hostname, assigned random host port, and the endpoint "uuid".
var requestUri = new UriBuilder(Uri.UriSchemeHttp, container.Hostname, container.GetMappedPublicPort(8080), "uuid").Uri;

Console.WriteLine($"El contenedor está listo en: {requestUri}");

// Send an HTTP GET request to the specified URI and retrieve the response as a string.
var guid = await httpClient.GetStringAsync(requestUri)
  .ConfigureAwait(false);

// Ensure that the retrieved UUID is a valid GUID.
Debug.Assert(Guid.TryParse(guid, out _));

