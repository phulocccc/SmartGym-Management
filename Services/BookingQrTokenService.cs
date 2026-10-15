using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;

namespace SmartGymManagement.Services;

public sealed class BookingQrTokenService(IDataProtectionProvider provider)
{
    private readonly IDataProtector _protector = provider.CreateProtector("SmartGym.BookingCheckIn.v1");

    public string Issue(int bookingId, int memberId, DateTimeOffset? expiresAt = null)
        => "SGYM:" + _protector.Protect($"{bookingId}:{memberId}:{(expiresAt ?? DateTimeOffset.UtcNow.AddMinutes(10)).ToUnixTimeSeconds()}");

    public bool TryRead(string? token, out int bookingId, out int memberId)
    {
        bookingId = memberId = 0;
        if (string.IsNullOrWhiteSpace(token) || token.Length > 2000 || !token.StartsWith("SGYM:", StringComparison.Ordinal)) return false;
        try
        {
            var parts = _protector.Unprotect(token[5..]).Split(':');
            return parts.Length == 3 && int.TryParse(parts[0], out bookingId) && bookingId > 0 &&
                int.TryParse(parts[1], out memberId) && memberId > 0 && long.TryParse(parts[2], out var expires) &&
                expires > DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
        catch (Exception exception) when (exception is CryptographicException or FormatException) { return false; }
    }
}
