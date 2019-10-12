using System;
using System.ComponentModel;
using System.Text.Json;
using Newtonsoft.Json;

namespace JustSaying.Fluent
{
    /// <summary>
    /// A class containing extension methods for the <see cref="ServicesBuilder"/> class.  This class cannot be inherited.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ServicesBuilderExtensions
    {
        /// <summary>
        /// Configures JustSaying to use <see cref="Newtonsoft.Json.JsonSerializer"/> for serialization.
        /// </summary>
        /// <param name="builder">The <see cref="ServicesBuilder"/> to configure.</param>
        /// <returns>
        /// The <see cref="ServicesBuilder"/> passed as the value of <paramref name="builder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        public static ServicesBuilder WithNewtonsoftJson(this ServicesBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.WithNewtonsoftJson(null as JsonSerializerSettings);
        }

        /// <summary>
        /// Configures JustSaying to use <see cref="Newtonsoft.Json.JsonSerializer"/> for serialization.
        /// </summary>
        /// <param name="builder">The <see cref="ServicesBuilder"/> to configure.</param>
        /// <param name="settings">The JSON serialization settings to use.</param>
        /// <returns>
        /// The <see cref="ServicesBuilder"/> passed as the value of <paramref name="builder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        public static ServicesBuilder WithNewtonsoftJson(this ServicesBuilder builder, JsonSerializerSettings settings)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.WithNewtonsoftJson(() => settings);
        }

        /// <summary>
        /// Configures JustSaying to use <see cref="Newtonsoft.Json.JsonSerializer"/> for serialization.
        /// </summary>
        /// <param name="builder">The <see cref="ServicesBuilder"/> to configure.</param>
        /// <param name="factory">A delegate to a method to use to get the JSON serializer settings to use.</param>
        /// <returns>
        /// The <see cref="ServicesBuilder"/> passed as the value of <paramref name="builder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="builder"/> or <paramref name="factory"/> is <see langword="null"/>.
        /// </exception>
        public static ServicesBuilder WithNewtonsoftJson(this ServicesBuilder builder, Func<JsonSerializerSettings> factory)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return builder.WithMessageSerializationFactory(
                () => new Messaging.MessageSerialization.NewtonsoftSerializationFactory(factory()));
        }

        /// <summary>
        /// Configures JustSaying to use <see cref="System.Text.Json.JsonSerializer"/> for serialization.
        /// </summary>
        /// <param name="builder">The <see cref="ServicesBuilder"/> to configure.</param>
        /// <returns>
        /// The <see cref="ServicesBuilder"/> passed as the value of <paramref name="builder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        public static ServicesBuilder WithSystemTextJson(this ServicesBuilder builder)
        {
            return builder.WithSystemTextJson(null as JsonSerializerOptions);
        }

        /// <summary>
        /// Configures JustSaying to use <see cref="System.Text.Json.JsonSerializer"/> for serialization.
        /// </summary>
        /// <param name="builder">The <see cref="ServicesBuilder"/> to configure.</param>
        /// <param name="settings">The JSON serialization options to use.</param>
        /// <returns>
        /// The <see cref="ServicesBuilder"/> passed as the value of <paramref name="builder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        public static ServicesBuilder WithSystemTextJson(this ServicesBuilder builder, JsonSerializerOptions options)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.WithSystemTextJson(() => options);
        }

        /// <summary>
        /// Configures JustSaying to use <see cref="System.Text.Json.JsonSerializer"/> for serialization.
        /// </summary>
        /// <param name="builder">The <see cref="ServicesBuilder"/> to configure.</param>
        /// <param name="factory">A delegate to a method to use to get the JSON serializer options to use.</param>
        /// <returns>
        /// The <see cref="ServicesBuilder"/> passed as the value of <paramref name="builder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="builder"/> or <paramref name="factory"/> is <see langword="null"/>.
        /// </exception>
        public static ServicesBuilder WithSystemTextJson(this ServicesBuilder builder, Func<JsonSerializerOptions> factory)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return builder.WithMessageSerializationFactory(
                () => new Messaging.MessageSerialization.SystemTextJsonSerializationFactory(factory()));
        }
    }
}
