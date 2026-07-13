namespace JustSaying.Fluent;

/// <summary>
/// Extension methods for <see cref="MessagingBusBuilder"/> to provide type-safe access to custom properties.
/// </summary>
public static class MessagingBusBuilderExtensions
{
    /// <summary>
    /// Gets a property value from the builder's Properties dictionary with type-safe access.
    /// </summary>
    /// <typeparam name="T">The type of the property value.</typeparam>
    /// <param name="builder">The messaging bus builder.</param>
    /// <param name="key">The property key.</param>
    /// <returns>The property value if found and of the correct type; otherwise, the default value for the type.</returns>
    public static T GetProperty<T>(this MessagingBusBuilder builder, string key)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (key == null) throw new ArgumentNullException(nameof(key));

        if (builder.Properties.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }

        return default;
    }

    /// <summary>
    /// Gets a property value from the builder's Properties dictionary with type-safe access.
    /// </summary>
    /// <typeparam name="T">The type of the property value.</typeparam>
    /// <param name="builder">The messaging bus builder.</param>
    /// <param name="key">The property key.</param>
    /// <param name="value">When this method returns, contains the property value if found; otherwise, the default value for the type.</param>
    /// <returns><see langword="true"/> if the property was found and is of the correct type; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetProperty<T>(this MessagingBusBuilder builder, string key, out T value)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (key == null) throw new ArgumentNullException(nameof(key));

        if (builder.Properties.TryGetValue(key, out var objValue) && objValue is T typedValue)
        {
            value = typedValue;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Sets a property value in the builder's Properties dictionary with type-safe access.
    /// </summary>
    /// <typeparam name="T">The type of the property value.</typeparam>
    /// <param name="builder">The messaging bus builder.</param>
    /// <param name="key">The property key.</param>
    /// <param name="value">The property value to set.</param>
    /// <returns>The messaging bus builder for method chaining.</returns>
    public static MessagingBusBuilder SetProperty<T>(this MessagingBusBuilder builder, string key, T value)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (key == null) throw new ArgumentNullException(nameof(key));

        builder.Properties[key] = value;
        return builder;
    }

    /// <summary>
    /// Gets a required property value from the builder's Properties dictionary.
    /// Throws if the property is not found or is not of the expected type.
    /// </summary>
    /// <typeparam name="T">The type of the property value.</typeparam>
    /// <param name="builder">The messaging bus builder.</param>
    /// <param name="key">The property key.</param>
    /// <returns>The property value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the property is not found or is not of the expected type.</exception>
    public static T GetRequiredProperty<T>(this MessagingBusBuilder builder, string key)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (key == null) throw new ArgumentNullException(nameof(key));

        if (!builder.Properties.TryGetValue(key, out var value))
        {
            throw new InvalidOperationException($"Required property '{key}' was not found in MessagingBusBuilder.Properties.");
        }

        if (!(value is T typedValue))
        {
            throw new InvalidOperationException(
                $"Property '{key}' is not of the expected type '{typeof(T).Name}'. Actual type: '{value?.GetType().Name ?? "null"}'.");
        }

        return typedValue;
    }
}
