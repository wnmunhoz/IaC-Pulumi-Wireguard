using ActiveLine.WireGuard.Host.Stacks;
using Pulumi;

// O Pulumi CLI executa "dotnet run" neste projeto; o Deployment conversa
// com o engine via gRPC e registra cada recurso criado dentro da Stack.
return await Deployment.RunAsync<WireGuardStack>();
