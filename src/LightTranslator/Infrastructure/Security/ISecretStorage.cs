namespace LightTranslator.Infrastructure.Security;

public interface ISecretStorage
{
    void Save(string name, string secret);

    string? Load(string name);

    void Delete(string name);
}