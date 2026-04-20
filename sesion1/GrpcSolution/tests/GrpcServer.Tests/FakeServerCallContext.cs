using Grpc.Core;

namespace GrpcServer.Tests;

/// <summary>
/// Implementación mínima de ServerCallContext para usar en tests unitarios.
/// Permite testear los servicios gRPC sin necesidad de un servidor real.
/// </summary>
public class FakeServerCallContext : ServerCallContext
{
    private readonly CancellationToken _cancellationToken;

    private FakeServerCallContext(CancellationToken cancellationToken = default)
    {
        _cancellationToken = cancellationToken;
    }

    public static FakeServerCallContext Crear(CancellationToken ct = default) =>
        new(ct);

    // ── Implementaciones obligatorias de ServerCallContext ──────────────────

    protected override string MethodCore        => "TestMethod";
    protected override string HostCore          => "localhost";
    protected override string PeerCore          => "127.0.0.1";
    protected override DateTime DeadlineCore    => DateTime.MaxValue;
    protected override Metadata RequestHeadersCore  => new();
    protected override CancellationToken CancellationTokenCore => _cancellationToken;
    protected override Metadata ResponseTrailersCore => new();
    protected override Status StatusCore
    {
        get => Status.DefaultSuccess;
        set { }
    }
    protected override WriteOptions? WriteOptionsCore
    {
        get => null;
        set { }
    }
    protected override AuthContext AuthContextCore =>
        new AuthContext("test", new Dictionary<string, List<AuthProperty>>());

    protected override ContextPropagationToken CreatePropagationTokenCore(
        ContextPropagationOptions? options) =>
        throw new NotImplementedException();

    protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) =>
        Task.CompletedTask;
}
