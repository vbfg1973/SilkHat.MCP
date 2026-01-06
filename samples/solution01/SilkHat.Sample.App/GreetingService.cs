namespace SilkHat.Sample.App;

public interface IGreetingProvider
{
    string GetGreeting(string name);
}

public sealed class FriendlyGreetingProvider : IGreetingProvider
{
    public string GetGreeting(string name) => $"Hey {name}!";
}

public sealed class FormalGreetingProvider : IGreetingProvider
{
    public string GetGreeting(string name) => $"Hello {name}.";
}

public sealed class GreetingService
{
    private readonly IGreetingProvider _provider;

    public GreetingService(IGreetingProvider provider)
    {
        _provider = provider;
    }

    public string Greet(string name)
    {
        return _provider.GetGreeting(name);
    }
}

public sealed class PublicEntry
{
    public string Run(string name)
    {
        var provider = new FriendlyGreetingProvider();
        var service = new GreetingService(provider);
        return service.Greet(name);
    }
}
