﻿using Spectre.Console.Cli;

namespace app.Infrastructure;

public sealed class TypeResolver : ITypeResolver, IDisposable
{
    private readonly IServiceProvider provider;

    public TypeResolver(IServiceProvider provider) => this.provider = provider ?? throw new ArgumentNullException(nameof(provider));

    public object? Resolve(Type? type) => type == null ? null : provider.GetService(type);

    public void Dispose()
    {
        if (provider is IDisposable disposable)
            disposable.Dispose();
    }
}