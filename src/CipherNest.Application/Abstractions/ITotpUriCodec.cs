using CipherNest.Application.Models;

namespace CipherNest.Application.Abstractions;

public interface ITotpUriCodec
{
    TotpUriProfile Parse(string uriText);
    string Format(TotpUriProfile profile);
}
