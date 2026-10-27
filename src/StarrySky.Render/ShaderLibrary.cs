using System;
using System.IO;
using System.Reflection;
using Vortice.D3DCompiler;
using Vortice.Direct3D;
using Vortice.Direct3D11;

namespace StarrySky.Render;

/// <summary>Compiles embedded HLSL sources once and caches the resulting shader objects.</summary>
internal static class ShaderLibrary
{
    public static ID3D11VertexShader CreateVertexShader(ID3D11Device device, string resource, string entry, out byte[] bytecode)
    {
        var blob = Compile(resource, entry, "vs_5_0");
        bytecode = blob.AsSpan().ToArray();
        var shader = device.CreateVertexShader(bytecode);
        blob.Dispose();
        return shader;
    }

    public static ID3D11PixelShader CreatePixelShader(ID3D11Device device, string resource, string entry)
    {
        var blob = Compile(resource, entry, "ps_5_0");
        var shader = device.CreatePixelShader(blob.AsSpan().ToArray());
        blob.Dispose();
        return shader;
    }

    private static Blob Compile(string resource, string entry, string profile)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Missing embedded shader resource '{resource}'.");
        using var reader = new StreamReader(stream);
        var source = reader.ReadToEnd();

        // D3DCompileFromFile keeps the call free of unsafe pointer marshalling; the
        // temporary file exists only for the duration of the compile.
        var path = Path.Combine(Path.GetTempPath(), $"starrysky-{Guid.NewGuid():N}.hlsl");
        File.WriteAllText(path, source);
        try
        {
            Compiler.CompileFromFile(
                path,
                null,
                null,
                entry,
                profile,
                ShaderFlags.None,
                EffectFlags.None,
                out var code,
                out var errors);

            if (code is null)
            {
                var message = errors is null ? "unknown shader error" : System.Text.Encoding.UTF8.GetString(errors.AsSpan());
                errors?.Dispose();
                throw new InvalidOperationException($"Shader '{resource}' ({entry}) failed: {message}");
            }

            errors?.Dispose();
            return code;
        }
        finally
        {
            try
            {
                File.Delete(path);
            }
            catch (IOException)
            {
                // A leftover temp file is not worth failing the renderer over.
            }
        }
    }
}


