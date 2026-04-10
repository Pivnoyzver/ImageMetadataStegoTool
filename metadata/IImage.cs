namespace metadata;

public interface IImage
{
    public void Write(byte[] data);
    public byte[] Read();
}
