public struct Option<T>
{
    private OneOf<T, None> _value;

    private Option(T value)
        => _value = value;

    private Option(None value)
        => _value = value;

    public static implicit operator Option<T>(T value)
        => new Option<T>(value);

    public static implicit operator Option<T>(None value)
        => new Option<T>(value);

    public static Option<T> None
        => new Option<T>(new None());

    public void Switch(Action<T> successfulFunc, Action<None> noneFunc)
        => _value.Switch(successfulFunc.ThrowIfDefault(nameof(successfulFunc)), noneFunc.ThrowIfDefault(nameof(noneFunc)));

    public TResult Match<TResult>(Func<T, TResult> successfulFunc, Func<None, TResult> noneFunc) =>
        _value.Match(successfulFunc.ThrowIfDefault(nameof(successfulFunc)), noneFunc.ThrowIfDefault(nameof(noneFunc)));

    public OneOf<T, None> ToOneOf() => _value;

    public bool IsSome() => _value.IsT0;

    public bool IsNone() => _value.IsT1;

    public Option<T> Or(Func<Option<T>> otherFunc)
        => Match(x => x, _ => otherFunc.ThrowIfDefault(nameof(otherFunc))());

    public T Or(Func<T> otherFunc)
        => Match(x => x, _ => otherFunc.ThrowIfDefault(nameof(otherFunc))());

    public Option<T> Or(Option<T> other)
        => Match(x => x, _ => other);

    public T Or(T otherValue)
        => Match(x => x, _ => otherValue);

    public T? OrDefault()
#pragma warning disable CS8603 // Possible null reference return.
        => Match(x => x, static _ => default);
#pragma warning restore CS8603 // Possible null reference return.
}

internal static class NullArgsHelper
{
    public static T ThrowIfDefault<T>(this T value, string argName) where T : class
        => value ?? throw new ArgumentNullException(argName);
}
