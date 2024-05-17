using System.Diagnostics;

namespace Socksy.Core.Common;

public sealed class BandWidthLimiter
{
    private readonly int _limitInBytes;
    private readonly SemaphoreSlim _semaphore;
    private long _lastBucketUpdateTime;
    private int _bucket;

    public BandWidthLimiter(int limitInBytes)
    {
        _limitInBytes = limitInBytes/10;
        _semaphore = new SemaphoreSlim(1, 1);
    }

    public async ValueTask<int> GetAllowedBytes(int requestedAmount)
    {
        await _semaphore.WaitAsync();
        try
        {
            var timestamp = Stopwatch.GetTimestamp();
            if(Stopwatch.GetElapsedTime(_lastBucketUpdateTime).TotalMilliseconds >= 100)
            {
                _bucket = _limitInBytes;
                _lastBucketUpdateTime = timestamp;
            }

            int res = requestedAmount;
            if(res > _bucket)
                res = _bucket;

            _bucket -= res;
            return res;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}