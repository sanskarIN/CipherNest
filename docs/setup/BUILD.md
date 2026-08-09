# Build and Run

## Prerequisites

Install a current .NET 10 SDK and the .NET MAUI workload. Platform targets additionally require Android SDK/JDK, Windows App SDK tooling on Windows, or Xcode on a supported Mac for Apple targets.

```bash
dotnet workload restore
dotnet restore CipherNest.slnx
dotnet build CipherNest.slnx -c Debug
dotnet test CipherNest.slnx -c Debug
```

Run a target from the IDE or with `dotnet build -t:Run` using a platform-specific target framework.

Signing keys and store credentials must be supplied through local/CI secret stores and never committed.
