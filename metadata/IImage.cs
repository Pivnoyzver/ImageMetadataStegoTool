namespace metadata;

public interface IImage
{
    string FilePath { get; }
    IImage Write(byte[] package);
    byte[] Read();
}
