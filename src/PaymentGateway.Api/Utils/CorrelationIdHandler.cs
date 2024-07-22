public class CorrelationIdHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdHandler(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var correlationId = _httpContextAccessor.HttpContext?.Request.Headers["X-Correlation-ID"].FirstOrDefault()
            ?? _httpContextAccessor.HttpContext?.Items["CorrelationId"] as string;

        if (!string.IsNullOrEmpty(correlationId))
            request.Headers.Add("X-Correlation-ID", correlationId);

        return await base.SendAsync(request, cancellationToken);
    }
}