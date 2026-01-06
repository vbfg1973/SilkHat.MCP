namespace SilkHat.Sample.App;

public sealed class ComplexitySamples
{
    public int CalculateScore(int value)
    {
        var total = 0;
        if (value > 10)
        {
            total++;
        }
        else
        {
            total += 2;
        }

        for (var i = 0; i < value; i++)
        {
            if (i % 2 == 0 && value > 0)
            {
                total++;
            }
        }

        switch (value)
        {
            case 0:
                total++;
                break;
            default:
                total--;
                break;
        }

        try
        {
            if (value < 0 || value == 3)
            {
                total++;
            }
        }
        catch
        {
            total--;
        }

        return total;
    }

    public string FetchHTTPServerStatus(string serverId)
    {
        var helper = new StatusHelper();
        helper.Grade11PlusScore = 10;
        var score = helper.Grade11PlusScore;
        return helper.GetHTTPStatus(serverId + score);
    }
}

public sealed class StatusHelper
{
    public int Grade11PlusScore { get; set; }

    public string GetHTTPStatus(string serverId)
    {
        return $"HTTP {serverId}";
    }
}
