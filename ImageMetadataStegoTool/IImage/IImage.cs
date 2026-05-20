namespace ImageMetadataStegoTool;

public interface IImage
{
    byte[] Write(byte[] package);
    byte[] Read();
}
