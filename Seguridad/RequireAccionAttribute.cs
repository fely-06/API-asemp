using System;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireAccionAttribute : Attribute
{
    public string Clave { get; }

    public RequireAccionAttribute(string clave)
    {
        Clave = clave;
    }
}
