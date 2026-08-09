using CipherNest.Application.Models;
using CipherNest.Domain.Models;

namespace CipherNest.Application.Abstractions;

public interface IPasswordGenerator
{
    string Generate(GeneratorOptions options);
    PasswordStrengthResult Evaluate(string secret);
}
