using System;

namespace Cloneable;

internal sealed class GeneratedCode
{
    public GeneratedCode(string fileName, string code)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentNullException(fileName);
        }
        
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentNullException(code);
        }
        
        FileName = fileName;
        Code = code;
    }

    public string FileName { get; }
    public string Code { get; }
}