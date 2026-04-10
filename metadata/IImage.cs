namespace metadata;

public interface IImage
{
    public void Write(byte[] package);
    public byte[] Read();
}
