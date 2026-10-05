namespace CvReader.Application.Matching;

public static class SimilarityScorer
{
    // bge-m3'te iki terimin cosine benzerliği: aynı terim 1.00, yakın anlamlılar ve çeviriler 0.75-0.95
    // ("sql"-"mysql" 0.83, "yazılım"-"software" 0.91), ilgisiz terimler 0.72'nin altında kalıyor.
    public const double Floor = 0.72;
    public const double Ceiling = 0.95;

    // Ham cosine benzerliğini (ör. 0.83) 0-100 arası bir skora çevirir.
    public static double ToScore(double similarity)
    {
        var scaled = (similarity - Floor) / (Ceiling - Floor) * 100;
        return Math.Round(Math.Clamp(scaled, 0, 100), 1);
    }
}
