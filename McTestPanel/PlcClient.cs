using System;
using System.Threading;
using System.Threading.Tasks;
using McpXLib;
using McpXLib.Enums;

namespace McTestPanel;

public sealed class PlcClient : IDisposable
{
    private readonly McpX _client;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public PlcClient(McpX client)
    {
        _client = client;
    }

    public void Open()
    {
        // McpX does not maintain a persistent session; no-op.
    }

    public void Close()
    {
        _client.Dispose();
    }

    public async Task<bool> ReadBitAsync(Prefix prefix, string address)
    {
        await _lock.WaitAsync();
        try
        {
            return await _client.ReadAsync<bool>(prefix, address);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<short> ReadWordAsync(Prefix prefix, string address)
    {
        await _lock.WaitAsync();
        try
        {
            return await _client.ReadAsync<short>(prefix, address);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task WriteBitAsync(Prefix prefix, string address, bool value)
    {
        await _lock.WaitAsync();
        try
        {
            await _client.WriteAsync(prefix, address, value);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task WriteWordAsync(Prefix prefix, string address, short value)
    {
        await _lock.WaitAsync();
        try
        {
            await _client.WriteAsync(prefix, address, value);
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose()
    {
        _client.Dispose();
        _lock.Dispose();
    }
}
