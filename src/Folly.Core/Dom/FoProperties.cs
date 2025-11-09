namespace Folly.Dom;

/// <summary>
/// Represents properties on an FO element.
/// </summary>
public sealed class FoProperties
{
    private readonly Dictionary<string, string> _properties = new();

    /// <summary>
    /// Gets or sets a property value.
    /// </summary>
    public string? this[string name]
    {
        get => _properties.TryGetValue(name, out var value) ? value : null;
        set
        {
            if (value != null)
                _properties[name] = value;
            else
                _properties.Remove(name);
        }
    }

    /// <summary>
    /// Gets all property names.
    /// </summary>
    public IEnumerable<string> Names => _properties.Keys;

    /// <summary>
    /// Checks if a property is defined.
    /// </summary>
    public bool HasProperty(string name) => _properties.ContainsKey(name);

    /// <summary>
    /// Gets a length property value in points.
    /// </summary>
    public double GetLength(string name, double defaultValue = 0)
    {
        var value = this[name];
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        return LengthParser.Parse(value);
    }

    /// <summary>
    /// Gets a string property value.
    /// </summary>
    public string GetString(string name, string defaultValue = "")
    {
        return this[name] ?? defaultValue;
    }
}
