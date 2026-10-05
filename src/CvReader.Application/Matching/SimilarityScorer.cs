namespace CvReader.Application.Matching;

public static class SimilarityScorer
{
    // bge-m3 cosine benzerlikleri pratikte bu aralıkta çıkıyor; gerçek CV'lerle denenip ayarlanmalı.
    public const double Floor = 0.25;
    public const double Ceiling = 0.60;

    // Ham cosine benzerliğini (ör. 0.42) 0-100 arası bir skora çevirir.
    public static double ToScore(double similarity)
    {
        var scaled = (similarity - Floor) / (Ceiling - Floor) * 100;
        return Math.Round(Math.Clamp(scaled, 0, 100), 1);
    }
}
