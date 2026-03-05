namespace Particular.Analyzers.Tests.Helpers;

public static class ModifierLists
{
    public static readonly string[] PrivateModifiers = ["", "private"];

    public static readonly string[] NonPrivateModifiers = ["public", "protected", "internal", "protected internal", "private protected"];

    public static readonly string[] InterfacePrivateModifiers =
    [
        "private",
    ];

    public static readonly string[] InterfaceNonPrivateModifiers =
    [
        "",
        "public",
        "internal",
        "protected",
        "protected internal",
        "private protected",
    ];
}