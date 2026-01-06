namespace SilkHat.Sample.App;

public enum SampleEnum
{
    Alpha,
    Beta
}

public struct SampleStruct
{
    public int Value;
}

public record SampleRecord(string Name);

public readonly record struct SampleRecordStruct(int Value);

public delegate void SampleDelegate(string input);
