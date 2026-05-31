using Microsoft.JSInterop;

namespace Ayapos.EndUser.Services.Owner;

public sealed class OwnerSessionState
{
    private const string StorageKey = "ayapos.owner.session";

    private readonly IJSRuntime _jsRuntime;

    public OwnerSessionState(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public string Token { get; private set; } = string.Empty;
    public string Username { get; private set; } = string.Empty;
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(Token);

    public async Task InitializeAsync()
    {
        var session = await _jsRuntime.InvokeAsync<StoredOwnerSession?>("ayaposSession.get", StorageKey);
        if (session is null)
            return;

        Token = session.Token ?? string.Empty;
        Username = session.Username ?? string.Empty;
    }

    public async Task SignInAsync(string token, string username)
    {
        Token = token;
        Username = username;

        await _jsRuntime.InvokeVoidAsync("ayaposSession.set", StorageKey, new StoredOwnerSession
        {
            Token = token,
            Username = username
        });
    }

    public async Task SignOutAsync()
    {
        Token = string.Empty;
        Username = string.Empty;
        await _jsRuntime.InvokeVoidAsync("ayaposSession.remove", StorageKey);
    }

    private sealed class StoredOwnerSession
    {
        public string? Token { get; set; }
        public string? Username { get; set; }
    }
}
